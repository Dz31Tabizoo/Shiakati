using Shiakati.Models;
using Shiakati.Services.Interfaces;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZXing.Common;


using System.Printing;

public class BarcodePrintService : IBarCodePrintService
{
    

    public void PrintBarCode(BarecodeLabelData data, string printerName = "", int copies = 1)
    {
        if (copies <= 0) return;

        // 1. Définir les dimensions exactes en unités WPF (96 DPI)
        // 40mm = ~151 units, 20mm = ~75 units
        double width = (40.0 / 25.4) * 96.0;
        double height = (20.0 / 25.4) * 96.0;

        // 2. Créer un conteneur (StackPanel) pour organiser le texte et le code-barres
        var container = new StackPanel
        {
            Width = width,
            Height = height,
            Background = Brushes.White,
            Orientation = Orientation.Vertical,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        // On force le rendu net
        RenderOptions.SetEdgeMode(container, EdgeMode.Unspecified);
        TextOptions.SetTextFormattingMode(container, TextFormattingMode.Display);
        TextOptions.SetTextRenderingMode(container, TextRenderingMode.Aliased);

        // --- Ligne 1 : Marque et Nom (Petit pour que ça rentre) ---
        container.Children.Add(new TextBlock
        {
            Text = $"{data.BrandName} {data.VariantName}".ToUpper(),
            FontSize = 9, // Taille réduite
            FontWeight = FontWeights.Regular,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 2, 0, 0),
            TextWrapping = TextWrapping.NoWrap,
            MaxWidth = width - 10
        });

        // --- Ligne 2 : Code-barres ---
        // On génère le code-barres (réutilise votre méthode GenerateBarcode96Dpi)
        var barcodeImg = new Image
        {
            Source = GenerateBarcode96Dpi(data.Barcode, (int)width - 20, 30),
            Height = 35,
            Margin = new Thickness(0, 2, 0, 0),
            Stretch = Stretch.Fill
        };
        RenderOptions.SetBitmapScalingMode(barcodeImg, BitmapScalingMode.NearestNeighbor);

        container.Children.Add(barcodeImg);

        // --- Ligne 3 : Taille et Prix ---
        container.Children.Add(new TextBlock
        {
            Text = $"{data.ProductSize} - {data.Price:N2} DA",
            FontSize = 10,
            FontWeight = FontWeights.DemiBold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 2, 0, 0)
        });

        // 3. ÉTAPE CRUCIALE : Forcer le layout de WPF avant l'impression
        container.Measure(new Size(width, height));
        container.Arrange(new Rect(0, 0, width, height));
        container.UpdateLayout();

        // 4. Lancement de l'impression
        PrintDialog pd = new PrintDialog();
        if (!string.IsNullOrEmpty(printerName))
        {
            pd.PrintQueue = new LocalPrintServer().GetPrintQueue(printerName);
        }

        // Configurer le ticket d'impression
        pd.PrintTicket.PageMediaSize = new PageMediaSize(40, 20);
        pd.PrintTicket.PageOrientation = PageOrientation.Portrait;
        pd.PrintTicket.CopyCount = copies;

        // Imprimer le conteneur
        pd.PrintVisual(container, "Label");
    }
    private BitmapSource GenerateBarcode96Dpi(string content, int width, int height)
    {
        // On précise <System.Drawing.Bitmap> ici
        var writer = new ZXing.BarcodeWriter<System.Drawing.Bitmap>
        {
            Format = ZXing.BarcodeFormat.CODE_128,
            // On ajoute le Renderer pour dire à ZXing de produire une Bitmap
            Renderer = new ZXing.Windows.Compatibility.BitmapRenderer(),
            Options = new ZXing.Common.EncodingOptions
            {
                Width = width,
                Height = height,
                Margin = 0,
                PureBarcode = true // Pour ne pas avoir le texte sous le code (on le gère nous-mêmes)
            }
        };

        using (var bitmap = writer.Write(content))
        {
            // Conversion de System.Drawing.Bitmap vers WPF BitmapSource
            IntPtr hBitmap = bitmap.GetHbitmap();
            try
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                // Très important pour éviter les fuites de mémoire (Memory Leaks)
                DeleteObject(hBitmap);
            }
        }
    }

    // Ajoutez cette importation native en haut de votre classe pour DeleteObject
    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    public static extern bool DeleteObject(IntPtr hObject);
}