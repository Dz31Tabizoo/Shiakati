using System.Drawing;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Shiakati.Models;
using ZXing;
using ZXing.Windows.Compatibility;
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;

namespace Shiakati.Services.Implementations
{
    public class BarcodePrintService
    {
        // Dimensions en unités WPF (96 DPI)
        private const double LabelWidth = 151.0;  // 40mm
        private const double LabelHeight = 75.0; // 20mm
        private const double Margin = 2.0;

        public void PrintBarcode(BarecodeLabelData data, string printerName)
        {
            PrintDialog printDialog = new PrintDialog();

            // 1. Configurer l'imprimante
            if (!string.IsNullOrEmpty(printerName))
            {
                var server = new LocalPrintServer();
                printDialog.PrintQueue = server.GetPrintQueue(printerName);
            }

            // 2. Créer le visuel à imprimer
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                // Fond blanc (facultatif mais recommandé pour éviter le fond transparent)
                dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, LabelWidth, LabelHeight));

                Typeface typeface = new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
                double fontSize = 9.0; // Taille de police réduite pour s'adapter

                // --- LIGNE 1 : MARQUE (3 Lettres) + NOM DU PRODUIT ---
                string brandPrefix = data.BrandName.Length >= 3
                    ? data.BrandName.Substring(0, 3).ToUpper()
                    : data.BrandName.PadRight(3, 'X').ToUpper();

                string fullTitle = $"{brandPrefix} {data.VariantName}";

                // Logique de troncature si le texte est trop long pour la largeur (151px - marges)
                FormattedText titleText = FormatText(fullTitle, typeface, fontSize, LabelWidth - (Margin * 2));

                dc.DrawText(titleText, new System.Windows.Point(Margin, 2));

                // --- LIGNE 2 : CODE-BARRES ---
                BitmapSource barcodeImage = GenerateBarcodeImage(data.Barcode, (int)LabelWidth - 10, 35);
                if (barcodeImage != null)
                {
                    // Positionner le code-barres au milieu (Y = 15)
                    dc.DrawImage(barcodeImage, new Rect(5, 16, barcodeImage.PixelWidth, barcodeImage.PixelHeight));
                }

                // --- LIGNE 3 : TAILLE ET PRIX ---
                string bottomString = $"{data.ProductSize}   {data.Price:N2} DA";
                FormattedText bottomText = FormatText(bottomString, typeface, fontSize, LabelWidth - (Margin * 2));

                // Centrer le texte du bas
                double bottomX = (LabelWidth - bottomText.Width) / 2;
                dc.DrawText(bottomText, new System.Windows.Point(bottomX, 55));
            }

            // 3. Lancer l'impression
            printDialog.PrintTicket.PageMediaSize = new PageMediaSize(LabelWidth, LabelHeight);
            printDialog.PrintVisual(visual, $"Code-barres {data.Barcode}");
        }

        /// <summary>
        /// Génère le code-barres avec ZXing et le convertit en format lisible par WPF
        /// </summary>
        private BitmapSource GenerateBarcodeImage(string content, int width, int height)
        {
            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.CODE_128, // Format standard pour les stocks
                Options = new ZXing.Common.EncodingOptions
                {
                    Width = width,
                    Height = height,
                    Margin = 0, // Enlever la marge interne pour maximiser la taille
                    PureBarcode = true // True = on ne dessine pas le texte en dessous (on le fait nous-même si besoin)
                }
            };

            var bitmap = writer.Write(content);

            // Convertir System.Drawing.Bitmap en BitmapSource pour WPF
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height,
                bitmap.HorizontalResolution, bitmap.VerticalResolution,
                PixelFormats.Bgr32, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);
            return bitmapSource;
        }

        /// <summary>
        /// Formate le texte et le coupe automatiquement (ajoute "...") s'il dépasse la largeur max
        /// </summary>
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
            formattedText.MaxTextHeight = size * 1.5; // Limiter à une seule ligne
            formattedText.Trimming = TextTrimming.CharacterEllipsis; // Coupe et met "..." à la fin si c'est trop long

            return formattedText;
        }
    }
}
