using QRCoder;
using Shiakati.Models;
using Shiakati.Services.Interfaces;
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows;

public class PrintService : IPrintService
{
    private ReceipModel _currentReceip;

    // Fonts and formats – created once per print job
    private Font _titleFont, _miniFont, _regularFont, _boldFont, _arabicFont;
    private StringFormat _centerFormat, _rightFormat, _arabicFormat;
    private float _leftMargin = 10;
    private float _rightMargin = 270;
    private float _paperWidth = 280;

    public void PrintReceipt(ReceipModel receipt, string configuredPrinterName = "")
    {
        _currentReceip = receipt;

        using (PrintDocument printDoc = new PrintDocument())
        {
            printDoc.PrintPage += PrintDoc_PrintPage;

            if (!string.IsNullOrEmpty(configuredPrinterName))
                printDoc.PrinterSettings.PrinterName = configuredPrinterName;

            printDoc.Print();
        }
    }

    private void PrintDoc_PrintPage(object sender, PrintPageEventArgs e)
    {
        Graphics g = e.Graphics;

        // Initialize fonts and formats
        _titleFont = new Font("Montserrat", 10, System.Drawing.FontStyle.Regular);
        _miniFont = new Font("Montserrat", 6, System.Drawing.FontStyle.Regular);
        _regularFont = new Font("Montserrat", 9, System.Drawing.FontStyle.Regular);
        _boldFont = new Font("Montserrat", 9, System.Drawing.FontStyle.Bold);
        _arabicFont = new Font("Cairo", 12, System.Drawing.FontStyle.Bold);

        _centerFormat = new StringFormat { Alignment = StringAlignment.Center };
        _rightFormat = new StringFormat { Alignment = StringAlignment.Far };
        _arabicFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.DirectionRightToLeft
        };

        float yPos = 10;

        // 1. Draw the logo (common for all documents)
        yPos = DrawLogo(g, yPos);

        // 2. Draw the store header (common)
        yPos = DrawStoreHeader(g, yPos);

        // 3. Draw document-specific content
        switch (_currentReceip.DocumentType)
        {
            case "INVENTORY":
                yPos = PrintInventory(g, yPos);
                break;
            case "RESERVATION":
                yPos = PrintReservation(g, yPos);
                break;
            case "HONOR":
                yPos = PrintHonor(g, yPos);
                break;
            default:
                yPos = PrintSale(g, yPos);
                break;
        }

        // 4. Draw footer (thank you + QR code) – common for all except inventory
        if (_currentReceip.DocumentType != "INVENTORY")
            yPos = DrawFooter(g, yPos);

        // Clean up fonts (optional – they will be disposed when the method ends)
    }

    // ─── HELPERS ─────────────────────────────────────────────────────────

    private float DrawLogo(Graphics g, float yPos)
    {
        try
        {
            Uri resourceUri = new Uri("pack://application:,,,/Resources/Photos/Shiakati Black and white - VERTICAL.png");
            var streamInfo = Application.GetResourceStream(resourceUri);
            if (streamInfo != null)
            {
                using (Image logoImage = Image.FromStream(streamInfo.Stream))
                {
                    float logoWidth = 80;
                    float logoHeight = 170;
                    float logoX = (_paperWidth - logoWidth) / 2;
                    g.DrawImage(logoImage, logoX, yPos, logoWidth, logoHeight);
                    yPos += logoHeight + 10;
                }
            }
        }
        catch { /* ignore */ }
        return yPos;
    }

    private float DrawStoreHeader(Graphics g, float yPos)
    {
        g.DrawString("شياكتي " + " Shiakati", _arabicFont, Brushes.Black, _paperWidth / 2, yPos, _arabicFormat);
        yPos += 25;
        g.DrawString("N° Tel: (+213) 560.80.90.90", _regularFont, Brushes.Black, _paperWidth / 2, yPos, _centerFormat);
        yPos += 15;
        g.DrawString("Adresse: Rue 1er Novembre, La Cia, Chlef 02", _regularFont, Brushes.Black, _paperWidth / 2, yPos, _centerFormat);
        yPos += 25;
        return yPos;
    }

    private float DrawSeparator(Graphics g, float yPos)
    {
        g.DrawString("______________________________________", _regularFont, Brushes.Black, _leftMargin, yPos);
        yPos += 20;
        return yPos;
    }

    private float DrawItems(Graphics g, float yPos)
    {
        foreach (var item in _currentReceip.Items)
        {
            g.DrawString(item.Designation, _boldFont, Brushes.Black, _leftMargin, yPos);
            yPos += 15;

            string leftText = $"{item.Quantity} x {item.UnitPrice:N2} DA";
            string rightText = $"{item.TotalPrice:N2} DA";

            g.DrawString(leftText, _regularFont, Brushes.Black, _leftMargin + 10, yPos);
            g.DrawString(rightText, _regularFont, Brushes.Black, _rightMargin, yPos, _rightFormat);

            yPos += 25;
        }
        return yPos;
    }

    private float DrawFooter(Graphics g, float yPos)
    {
        // Thank you message
        yPos += 10;
        g.DrawString("شكرا لمروركم الطيب", _arabicFont, Brushes.Black, _paperWidth / 2, yPos, _arabicFormat);
        yPos += 20;

        // QR Code
        using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
        {
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(_currentReceip.TicketNumber, QRCodeGenerator.ECCLevel.Q);
            using (QRCode qrCode = new QRCode(qrCodeData))
            {
                Bitmap qrCodeImage = qrCode.GetGraphic(3);
                g.DrawImage(qrCodeImage, (_paperWidth - qrCodeImage.Width) / 2, yPos);
                yPos += qrCodeImage.Height + 5;
            }
        }

        g.DrawString("Software: NumidixLab", _miniFont, Brushes.Black, _paperWidth / 2, yPos, _centerFormat);
        yPos += 30;
        g.DrawString(" ", _regularFont, Brushes.Black, _leftMargin, yPos); // blank line for cutter
        return yPos;
    }

    // ─── DOCUMENT-SPECIFIC PRINTERS ────────────────────────────────────

    private float PrintInventory(Graphics g, float yPos)
    {
        g.DrawString("INVENTAIRE", _titleFont, Brushes.Black, _paperWidth / 2, yPos, _centerFormat);
        yPos += 25;

        g.DrawString($"Marque : {_currentReceip.BrandName}", _regularFont, Brushes.Black, _leftMargin, yPos);
        yPos += 15;
        g.DrawString($"Produit : {_currentReceip.ProductName}", _regularFont, Brushes.Black, _leftMargin, yPos);
        yPos += 20;

        foreach (var item in _currentReceip.Items)
        {
            g.DrawString(item.Designation, _regularFont, Brushes.Black, _leftMargin + 5, yPos);
            yPos += 15;
        }
        return yPos;
    }

    private float PrintReservation(Graphics g, float yPos)
    {
        g.DrawString("RÉSERVATION", _titleFont, Brushes.Black, _paperWidth / 2, yPos, _centerFormat);
        yPos += 20;

        g.DrawString($"Ticket N°: {_currentReceip.TicketNumber}", _regularFont, Brushes.Black, _paperWidth / 2, yPos, _centerFormat);
        yPos += 22;
        g.DrawString($"Date: {_currentReceip.Date:dd/MM/yyyy HH:mm}", _regularFont, Brushes.Black, _paperWidth / 2, yPos, _centerFormat);
        yPos += 15;

        yPos = DrawSeparator(g, yPos);
        yPos = DrawItems(g, yPos);
        yPos = DrawSeparator(g, yPos);

        if (!string.IsNullOrEmpty(_currentReceip.ClientName))
        {
            g.DrawString($"Client : {_currentReceip.ClientName}", _regularFont, Brushes.Black, _leftMargin, yPos);
            yPos += 15;
        }
        g.DrawString($"Dépôt : {_currentReceip.DepositAmount:N2} DA", _regularFont, Brushes.Black, _leftMargin, yPos);
        yPos += 15;
        g.DrawString($"Reste à payer : {_currentReceip.RemainingDebt:N2} DA", _regularFont, Brushes.Black, _leftMargin, yPos);
        yPos += 15;
        if (_currentReceip.ExpirationDate.HasValue)
        {
            g.DrawString($"Expire le : {_currentReceip.ExpirationDate:dd/MM/yyyy}", _regularFont, Brushes.Black, _leftMargin, yPos);
            yPos += 15;
        }
        yPos = DrawSeparator(g, yPos);
        return yPos;
    }

    private float PrintHonor(Graphics g, float yPos)
    {
        g.DrawString("RÉSERVATION VALIDÉE", _titleFont, Brushes.Black, _paperWidth / 2, yPos, _centerFormat);
        yPos += 20;
        g.DrawString($"Ticket N°: {_currentReceip.TicketNumber}", _regularFont, Brushes.Black, _paperWidth / 2, yPos, _centerFormat);
        yPos += 22;
        g.DrawString($"Date: {_currentReceip.Date:dd/MM/yyyy HH:mm}", _regularFont, Brushes.Black, _paperWidth / 2, yPos, _centerFormat);
        yPos += 15;

        yPos = DrawSeparator(g, yPos);
        yPos = DrawItems(g, yPos);
        yPos = DrawSeparator(g, yPos);

        if (!string.IsNullOrEmpty(_currentReceip.ClientName))
        {
            g.DrawString($"Client : {_currentReceip.ClientName}", _regularFont, Brushes.Black, _leftMargin, yPos);
            yPos += 15;
        }

        if (_currentReceip.TotalDiscount > 0)
        {
            g.DrawString("Sous-total :", _regularFont, Brushes.Black, _leftMargin, yPos);
            g.DrawString($"{_currentReceip.TotalAmount + _currentReceip.TotalDiscount:N2} DA", _regularFont, Brushes.Black, _rightMargin, yPos, _rightFormat);
            yPos += 15;

            g.DrawString("Remise :", _regularFont, Brushes.Black, _leftMargin, yPos);
            g.DrawString($"- {_currentReceip.TotalDiscount:N2} DA", _regularFont, Brushes.Black, _rightMargin, yPos, _rightFormat);
            yPos += 15;

            g.DrawString($"Payé maintenant : {_currentReceip.PaidAmount:N2} DA", _regularFont, Brushes.Black, _leftMargin, yPos);
            yPos += 15;
            g.DrawString($"Reste dû : {_currentReceip.RemainingDebt:N2} DA", _regularFont, Brushes.Black, _leftMargin, yPos);
            yPos += 110;

            g.DrawString("________________________", _regularFont, Brushes.Black, _paperWidth / 2, yPos, _centerFormat);
            yPos += 25;
            return yPos;
        }
        else 
        {
            g.DrawString($"Montant total : {_currentReceip.TotalAmount:N2} DA", _regularFont, Brushes.Black, _leftMargin, yPos);
            yPos += 15;
            g.DrawString($"Payé maintenant : {_currentReceip.PaidAmount:N2} DA", _regularFont, Brushes.Black, _leftMargin, yPos);
            yPos += 15;
            g.DrawString($"Reste dû : {_currentReceip.RemainingDebt:N2} DA", _regularFont, Brushes.Black, _leftMargin, yPos);
            yPos += 15;


            yPos = DrawSeparator(g, yPos);
            return yPos;
        }
        
    }

    private float PrintSale(Graphics g, float yPos)
    {
        if (_currentReceip.IsEdited)
            g.DrawString($"*Vente Modifiée* Ticket N°: {_currentReceip.TicketNumber}", _regularFont, Brushes.Black, _paperWidth / 2, yPos, _centerFormat);
        else
            g.DrawString($"Ticket N°: {_currentReceip.TicketNumber}", _regularFont, Brushes.Black, _paperWidth / 2, yPos, _centerFormat);
        yPos += 15;
        g.DrawString($"Date: {_currentReceip.Date:dd/MM/yyyy HH:mm}", _regularFont, Brushes.Black, _paperWidth / 2, yPos, _centerFormat);
        yPos += 18;

        // Double separator
        g.DrawString("______________________________________", _regularFont, Brushes.Black, _leftMargin, yPos);
        yPos += 3;
        g.DrawString("______________________________________", _regularFont, Brushes.Black, _leftMargin, yPos);
        yPos += 25;

        yPos = DrawItems(g, yPos);
        yPos = DrawSeparator(g, yPos);

        // Totals
        if (_currentReceip.TotalDiscount > 0)
        {
            g.DrawString("Sous-total :", _regularFont, Brushes.Black, _leftMargin, yPos);
            g.DrawString($"{_currentReceip.TotalAmount + _currentReceip.TotalDiscount:N2} DA", _regularFont, Brushes.Black, _rightMargin, yPos, _rightFormat);
            yPos += 15;

            g.DrawString("Remise :", _regularFont, Brushes.Black, _leftMargin, yPos);
            g.DrawString($"- {_currentReceip.TotalDiscount:N2} DA", _regularFont, Brushes.Black, _rightMargin, yPos, _rightFormat);
            yPos += 10;

            g.DrawString("________________________", _regularFont, Brushes.Black, _paperWidth / 2, yPos, _centerFormat);
            yPos += 25;
        }

        g.DrawString("TOTAL A PAYER :", _titleFont, Brushes.Black, _leftMargin, yPos);
        g.DrawString($" {_currentReceip.TotalAmount:N2} DA", _titleFont, Brushes.Black, _rightMargin, yPos, _rightFormat);
        yPos += 15;

        if (!string.IsNullOrEmpty(_currentReceip.ClientName))
        {
            g.DrawString($"Client : {_currentReceip.ClientName}", _regularFont, Brushes.Black, _leftMargin, yPos);
            yPos += 15;
            g.DrawString($"Payé : {_currentReceip.PaidAmount:N2} DA", _regularFont, Brushes.Black, _leftMargin, yPos);
            yPos += 15;
            g.DrawString($"Reste dû : {_currentReceip.RemainingDebt:N2} DA", _regularFont, Brushes.Black, _leftMargin, yPos);
            yPos += 15;
        }

        yPos += 20;
        return yPos;
    }

}