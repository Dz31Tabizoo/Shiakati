using System.Drawing;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Shiakati.Models;
using Shiakati.Services.Interfaces;
using ZXing;
using ZXing.Windows.Compatibility;
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;

namespace Shiakati.Services.Implementations
{
    public class BarcodePrintService : IBarCodePrintService
    {
        // Dimensions en unités WPF (96 DPI)
        private const double LabelWidth = 151.0;  // 40mm
        private const double LabelHeight = 75.0; // 20mm
        private const double Margin = 2.0;

        public void PrintBarCode(BarecodeLabelData data, string printerName = "", int copies = 1)
        {
            if (copies <= 0) return; // Sécurité

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

            // ==========================================
            // C'EST ICI QU'ON DÉFINIT LE NOMBRE DE COPIES
            // Cela envoie 1 seul fichier à l'imprimante, qui va l'imprimer N fois très vite
            // ==========================================
            printDialog.PrintTicket.CopyCount = copies;
            printDialog.PrintTicket.PageMediaSize = new PageMediaSize(LabelWidth, LabelHeight);

            // 2. Créer le visuel à imprimer
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                // Fond blanc
                dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, LabelWidth, LabelHeight));

                Typeface typeface = new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
                double fontSize = 9.0;

                // --- LIGNE 1 : MARQUE + NOM ---
                string brandPrefix = data.BrandName.Length >= 3
                    ? data.BrandName.Substring(0, 3).ToUpper()
                    : data.BrandName.PadRight(3, 'X').ToUpper();

                string fullTitle = $"{brandPrefix} {data.VariantName}";
                FormattedText titleText = FormatText(fullTitle, typeface, fontSize, LabelWidth - (Margin * 2));
                dc.DrawText(titleText, new System.Windows.Point(Margin, 2));

                // --- LIGNE 2 : CODE-BARRES ---
                BitmapSource barcodeImage = GenerateBarCodeImage(data.Barcode, (int)LabelWidth - 10, 35);
                if (barcodeImage != null)
                {
                    dc.DrawImage(barcodeImage, new Rect(5, 16, barcodeImage.PixelWidth, barcodeImage.PixelHeight));
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
        /// <summary>
        /// Génère le code-barres avec ZXing et le convertit en format lisible par WPF
        /// </summary>
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

            // Utiliser "using" pour libérer la mémoire de la bitmap GDI+ immédiatement
            using (var bitmap = writer.Write(content))
            {
                var bitmapData = bitmap.LockBits(
                    new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb); // Forcer le format 32 bits

                try
                {
                    // Calcul précis du Stride pour un format 32 bits (4 octets par pixel)
                    int stride = bitmapData.Stride;
                    int bufferSize = stride * bitmapData.Height;

                    // Création de la BitmapSource avec les paramètres validés
                    var bitmapSource = BitmapSource.Create(
                        bitmapData.Width,
                        bitmapData.Height,
                        96, // DPI standard
                        96,
                        System.Windows.Media.PixelFormats.Bgr32, // Format WPF correspondant
                        null,
                        bitmapData.Scan0,
                        bufferSize,
                        stride);

                    // Très important : Figer l'image pour qu'elle soit utilisable sur d'autres threads (impression)
                    bitmapSource.Freeze();
                    return bitmapSource;
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                }
            }
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
