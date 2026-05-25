using Shiakati.Models;
using Shiakati.Services.Interfaces;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

public class BarcodePrintService : IBarCodePrintService
{
    // Label dimensions
    private const double LabelWidthMm = 40.0;
    private const double LabelHeightMm = 20.0;

    // Printer hardware resolution (Xprinter XP-237B: 8 dpmm = 203 DPI)
    private const double PrinterDpi = 203.0;

    public void PrintBarCode(BarecodeLabelData data, string printerName = "", int copies = 1)
    {
        if (copies <= 0) return;

        // --- Layout at 96 DPI (WPF logical pixels), matching label size exactly ---
        double layoutWidth = LabelWidthMm / 25.4 * 96.0;   // 151 px
        double layoutHeight = LabelHeightMm / 25.4 * 96.0; // 76 px

        var container = new StackPanel
        {
            Width = layoutWidth,
            Height = layoutHeight,
            Background = Brushes.White,
            Orientation = Orientation.Vertical,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        TextOptions.SetTextFormattingMode(container, TextFormattingMode.Display);
        TextOptions.SetTextRenderingMode(container, TextRenderingMode.Aliased);

        // --- Line 1: Brand + Name ---
        container.Children.Add(new TextBlock
        {
            Text = $"{data.BrandName} {data.VariantName}".ToUpper(),
            FontSize = 10,
            FontWeight = FontWeights.Regular,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 2, 0, 2),
            TextWrapping = TextWrapping.NoWrap,
            FontFamily = new FontFamily("Arial") // common font for better printer compatibility
        });

        // --- Line 2: Barcode (generated at 203 DPI, downscaled slightly to fit) ---
        int barcodePixelWidth = (int)(LabelWidthMm / 25.4 * PrinterDpi); // 319 px at 203 DPI
        int barcodePixelHeight = 70; // plenty of height for a clean scan

        var barcodeImg = new Image
        {
            Source = GenerateBarcode(data.Barcode, barcodePixelWidth, barcodePixelHeight),
            Width = layoutWidth - 6,            // almost full label width (145 px)
            Height = 32,                        // ~8.5 mm on the label
            Margin = new Thickness(0, 1, 0, 0),
            Stretch = Stretch.Fill              // uniform downscale from 319×70 → 145×32
        };
        RenderOptions.SetBitmapScalingMode(barcodeImg, BitmapScalingMode.HighQuality);

        container.Children.Add(barcodeImg);

        // --- Line 3: Size + Price ---
        container.Children.Add(new TextBlock
        {
            Text = $"{data.ProductSize} - {data.Price:N2} DA",
            FontSize = 10,
            FontWeight = FontWeights.DemiBold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 3, 0, 0)
        });

        // Force layout at label size
        container.Measure(new Size(layoutWidth, layoutHeight));
        container.Arrange(new Rect(0, 0, layoutWidth, layoutHeight));
        container.UpdateLayout();

        // --- Print ---
        var pd = new PrintDialog();
        if (!string.IsNullOrEmpty(printerName))
            pd.PrintQueue = new LocalPrintServer().GetPrintQueue(printerName);

        // Page size: 40 mm × 20 mm in 1/100 mm units
        pd.PrintTicket.PageMediaSize = new PageMediaSize(
            PageMediaSizeName.Unknown,
            (int)(LabelWidthMm * 10),   // 400
            (int)(LabelHeightMm * 10)); // 200
        pd.PrintTicket.PageOrientation = PageOrientation.Portrait;
        pd.PrintTicket.CopyCount = copies;

        pd.PrintVisual(container, "Shiakati Label");
    }

    /// <summary>
    /// Creates a barcode bitmap with quiet zone, at the given pixel size.
    /// </summary>
    private BitmapSource GenerateBarcode(string content, int width, int height)
    {
        var writer = new BarcodeWriter<System.Drawing.Bitmap>
        {
            Format = BarcodeFormat.CODE_128,
            Renderer = new BitmapRenderer(),
            Options = new EncodingOptions
            {
                Width = width,
                Height = height,
                Margin = 10,            // required quiet zone for fast scanning
                PureBarcode = true
            }
        };

        using (var bitmap = writer.Write(content))
        {
            IntPtr hBitmap = bitmap.GetHbitmap();
            try
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap, IntPtr.Zero, Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }
    }

    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    public static extern bool DeleteObject(IntPtr hObject);
}



