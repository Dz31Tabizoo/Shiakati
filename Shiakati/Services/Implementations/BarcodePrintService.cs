using Shiakati.Models;
using Shiakati.Services.Interfaces;
using System.Drawing;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZXing;
using ZXing.Windows.Compatibility;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using Pen = System.Windows.Media.Pen;

public class BarcodePrintService : IBarCodePrintService
{
    // Dimensions en unités WPF (96 DPI)
    private const double LabelWidth = 151.0;  // 40mm
    private const double LabelHeight = 75.0; // 20mm
    private const double Margin = 2.0;

    public void PrintBarCode(BarecodeLabelData data, string printerName = "", int copies = 1)
    {
        if (copies <= 0) return;

        PrintDialog printDialog = new PrintDialog();

        // 1. Configurer l'imprimante
        if (!string.IsNullOrEmpty(printerName))
        {
            var server = new LocalPrintServer();
            try
            {
                printDialog.PrintQueue = server.GetPrintQueue(printerName);
            }
            catch
            {
                System.Windows.MessageBox.Show($"Imprimante '{printerName}' introuvable. Veuillez vérifier vos paramètres.");
                return;
            }
        }

        printDialog.PrintTicket.CopyCount = copies;
        printDialog.PrintTicket.PageMediaSize = new PageMediaSize(LabelWidth, LabelHeight);

        // 2. Créer le visuel à imprimer
        DrawingVisual visual = new DrawingVisual();

        // ====================================================================================
        // LE SECRET POUR LES IMPRIMANTES THERMIQUES : DÉSACTIVER LE LISSAGE (ANTI-ALIASING)
        // ====================================================================================
        RenderOptions.SetEdgeMode(visual, EdgeMode.Aliased); // Force les bords nets (pas de gris)
        RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.NearestNeighbor); // Empêche le flou des images redimensionnées
        TextOptions.SetTextFormattingMode(visual, TextFormattingMode.Display); // Optimise le texte pour la lisibilité
        TextOptions.SetTextRenderingMode(visual, TextRenderingMode.Aliased); // Enlève le lissage des polices (ClearType)
        // ====================================================================================

        using (DrawingContext dc = visual.RenderOpen())
        {
            // Fond blanc pur
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, LabelWidth, LabelHeight));

            Typeface typeface = new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
            double fontSize = 10.0;

            // --- LIGNE 1 : MARQUE + NOM ---
            string brandPrefix = data.BrandName.Length >= 3
                ? data.BrandName.Substring(0, 3).ToUpper()
                : data.BrandName.PadRight(3, 'X').ToUpper();

            string fullTitle = $"   {brandPrefix} {data.VariantName}";
            FormattedText titleText = FormatText(fullTitle, typeface, fontSize, LabelWidth - (Margin * 2));
            dc.DrawText(titleText, new System.Windows.Point(Margin, 2));

            // --- LIGNE 2 : CODE-BARRES ---
            // On demande à ZXing de générer une image légèrement plus grande pour avoir plus de pixels bruts
            // que l'on va réduire avec "NearestNeighbor" pour une netteté absolue.
            BitmapSource barcodeImage = GenerateBarCodeImage(data.Barcode, (int)LabelWidth * 2, 70);
            if (barcodeImage != null)
            {
                // On dessine l'image en taille normale, WPF va la compresser sans faire de flou
                dc.DrawImage(barcodeImage, new Rect(5, 16, LabelWidth - 10, 35));
            }

            // --- LIGNE 3 : TAILLE ET PRIX ---
            string bottomString = $"{data.ProductSize}   {data.Price:N2} DA";
            FormattedText bottomText = FormatText(bottomString, typeface, fontSize, LabelWidth - (Margin * 2));

            double bottomX = (LabelWidth - bottomText.Width) / 2;
            dc.DrawText(bottomText, new System.Windows.Point(bottomX, 55));
        }

        // 3. Lancer l'impression
        printDialog.PrintVisual(visual, $"Code-barres {data.Barcode} ({copies} copies)");
    }

    private BitmapSource GenerateBarCodeImage(string content, int width, int height)
    {
        var writer = new BarcodeWriter
        {
            Format = BarcodeFormat.CODE_128,
            Options = new ZXing.Common.EncodingOptions
            {
                Width = width,
                Height = height,
                Margin = 0,
                PureBarcode = true
            }
        };

        using (var bitmap = writer.Write(content))
        {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            try
            {
                int stride = bitmapData.Stride;
                int bufferSize = stride * bitmapData.Height;

                var bitmapSource = BitmapSource.Create(
                    bitmapData.Width,
                    bitmapData.Height,
                    96,
                    96,
                    System.Windows.Media.PixelFormats.Bgr32,
                    null,
                    bitmapData.Scan0,
                    bufferSize,
                    stride);

                bitmapSource.Freeze();
                return bitmapSource;
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
        }
    }

    private FormattedText FormatText(string text, Typeface typeface, double size, double maxWidth)
    {
        var formattedText = new FormattedText(
            text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            size,
            Brushes.Black,
            VisualTreeHelper.GetDpi(new UserControl()).PixelsPerDip);

        formattedText.MaxTextWidth = maxWidth;
        formattedText.MaxTextHeight = size * 1.5;
        formattedText.Trimming = TextTrimming.CharacterEllipsis;

        return formattedText;
    }
}