
using System.Drawing;
using System.Drawing.Printing;
using QRCoder;
using System.Drawing.Imaging;
using Shiakati.Models;
using Shiakati.Services.Interfaces;
using System.IO;
using System.Windows.Controls;

namespace Shiakati.Services.Implementations
{
    public class PrintService : IPrintService
    {
        private ReceipModel _currentReceip;

        public void PrintReceipt(ReceipModel receipt, string configuredPrinterName = "")
        {
            _currentReceip = receipt;

            using (PrintDocument printDoc = new PrintDocument())
            {
                printDoc.PrintPage += PrintDoc_PrintPage;

                // If you have a specific thermal printer saved in your app settings, use it.
                // If you leave it empty, Windows will automatically use the Default Printer.
                if (!string.IsNullOrEmpty(configuredPrinterName))
                {
                    printDoc.PrinterSettings.PrinterName = configuredPrinterName;
                }

                // BOOM! Prints instantly, silently, with no UI conflicts.
                printDoc.Print();

            }
        }

        private void PrintDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;

            float yPos = 10;
            float leftMargin = 10;
            float rightMargin = 270;
            float paperWidth = 280;


            Font titleFont = new Font("Montserrat",10, FontStyle.Regular);
            Font miniFont = new Font("Montserrat", 6, FontStyle.Regular);
            Font regularFont = new Font("Montserrat", 9, FontStyle.Regular);
            Font BlodFont = new Font("Montserrat", 9, FontStyle.Bold);
            Font arabicFont = new Font("Cairo", 12, FontStyle.Bold);

            // Formats d'alignement
            StringFormat centerFormat = new StringFormat { Alignment = StringAlignment.Center };
            StringFormat rightFormat = new StringFormat { Alignment = StringAlignment.Far };
            // Format spécifique pour l'arabe (Lecture de droite à gauche)
            StringFormat arabicFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.DirectionRightToLeft
            };


            try
            {
                // 1. Define the URI (Path) to the resource inside your app
                // Note: Make sure the folder names match exactly what is in your Solution Explorer
                Uri resourceUri = new Uri("pack://application:,,,/Resources/Photos/Shiakati Black and white - VERTICAL.png");

                // 2. Open a stream to read the file from inside the .exe
                var streamInfo = System.Windows.Application.GetResourceStream(resourceUri);

                if (streamInfo != null)
                {
                    // 3. Convert the Stream into a System.Drawing.Image
                    using (System.Drawing.Image logoImage = System.Drawing.Image.FromStream(streamInfo.Stream))
                    {
                        float logoWidth = 80;
                        float logoHeight = 170;
                        float logoX = (paperWidth - logoWidth) / 2;

                        g.DrawImage(logoImage, logoX, yPos, logoWidth, logoHeight);
                        yPos += logoHeight + 10;
                    }
                }
            }
            catch (Exception ex)
            {
                // If you misspell the folder name, it will crash here. 
                // You can write a temporary Console.WriteLine or just ignore it so the ticket still prints without a logo.
                Console.WriteLine($"Logo error: {ex.Message}");
            }


            // - 
            // Titre
            g.DrawString($"شياكتي " + " Shiakati", arabicFont, Brushes.Black, paperWidth / 2, yPos, arabicFormat);
            yPos += 25;
            g.DrawString("N° Tel: (+213) 560.80.90.90", regularFont, Brushes.Black, paperWidth / 2, yPos, centerFormat);
            yPos += 15;
            g.DrawString("Adresse: Rue 1er Novembre, La Cia, Chlef 02", regularFont, Brushes.Black, paperWidth / 2, yPos, centerFormat);
            yPos += 25;

            if (_currentReceip.IsEdited)
            {
                g.DrawString($"*Vente Modifiée* Ticket N°: {_currentReceip.TicketNumber}", regularFont, Brushes.Black, paperWidth / 2, yPos, centerFormat);
                yPos += 15;
            }
            else
            {
                g.DrawString($"Ticket N°: {_currentReceip.TicketNumber}", regularFont, Brushes.Black, paperWidth / 2, yPos, centerFormat);
                yPos += 15;
            }                
            g.DrawString($"Date: {_currentReceip.Date:dd/MM/yyyy HH:mm}", regularFont, Brushes.Black, paperWidth / 2, yPos, centerFormat);
            yPos += 22;

            //separation  -                                                       -  
            g.DrawString("______________________________________", regularFont, Brushes.Black, leftMargin, yPos);
            yPos += 3;
            g.DrawString("______________________________________", regularFont, Brushes.Black, leftMargin, yPos);
            yPos += 25;



            foreach (var item in _currentReceip.Items)
            {
                g.DrawString(item.Designation, BlodFont, Brushes.Black, leftMargin, yPos);
                yPos += 15;

                // Line: Qty x UnitPrice ...... TotalPrice
                string leftText = $"{item.Quantity} x {item.UnitPrice:N2} DA";
                string rightText = $"{item.TotalPrice:N2} DA";

                // Draw left and right parts
                g.DrawString(leftText, regularFont, Brushes.Black, leftMargin + 10, yPos);
                g.DrawString(rightText, regularFont, Brushes.Black, rightMargin, yPos, rightFormat);

                yPos += 25;
            }

            //separation  - - - -  - 
            g.DrawString("______________________________________", regularFont, Brushes.Black, leftMargin, yPos);
            yPos += 20;


            // 4. TOTAUX ET REMISES
            if (_currentReceip.TotalDiscount > 0)
            {
                g.DrawString("Sous-total :", regularFont, Brushes.Black, leftMargin, yPos);
                g.DrawString($"{_currentReceip.TotalAmount + _currentReceip.TotalDiscount:N2} DA", regularFont, Brushes.Black, rightMargin, yPos, rightFormat);
                yPos += 15;

                g.DrawString("Remise :", regularFont, Brushes.Black, leftMargin, yPos);
                g.DrawString($"- {_currentReceip.TotalDiscount:N2} DA", regularFont, Brushes.Black, rightMargin, yPos, rightFormat);
                yPos += 10;

                g.DrawString("________________________", regularFont, Brushes.Black, paperWidth/2, yPos,centerFormat);
                yPos += 25;
            }


            g.DrawString("TOTAL A PAYER :", titleFont, Brushes.Black, leftMargin, yPos);
            g.DrawString($" {_currentReceip.TotalAmount:N2} DA", titleFont, Brushes.Black, rightMargin, yPos, rightFormat);
            yPos += 30;
            
            // 5. MESSAGE DE REMERCIEMENT (En Arabe)
            g.DrawString("شكرا لمروركم الطيب", arabicFont, Brushes.Black, paperWidth / 2, yPos, arabicFormat);
            yPos += 20;

            // 6. CODE QR (Généré avec QRCoder)
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(_currentReceip.TicketNumber, QRCodeGenerator.ECCLevel.Q);
                using (QRCode qrCode = new QRCode(qrCodeData))
                {
                    // Le chiffre "3" définit la taille des pixels du QR
                    Bitmap qrCodeImage = qrCode.GetGraphic(3);
                    // On centre le QR Code
                    g.DrawImage(qrCodeImage, (paperWidth - qrCodeImage.Width) / 2, yPos);
                    yPos += qrCodeImage.Height + 5;
                }

                g.DrawString("Software: NumidixLab", miniFont, Brushes.Black, paperWidth / 2, yPos, centerFormat);

                // Optionnel : Ajouter un espace blanc à la fin pour que l'imprimante coupe au bon endroit
                g.DrawString(" ", regularFont, Brushes.Black, leftMargin, yPos + 30);
            }

        }

    }
}
