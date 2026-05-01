using System.Drawing;
using System.Drawing.Printing;
using QRCoder;
using Shiakati.Models;
using Shiakati.Services.Interfaces;
using System.IO;

namespace Shiakati.Services.Implementations
{
    public class PrintService : IPrintService
    {
        private ReceipModel _currentReceip;

        public void PrintReceipt(ReceipModel receipt)
        {
            _currentReceip = receipt;

            PrintDocument printDoc=new PrintDocument();
            printDoc.PrintPage += PrintDoc_PrintPage;
        }

        private void PrintDoc_PrintPage(object sender,PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;

            float yPos = 10;
            float leftMargin = 10;
            float rightMargin = 270;
            float paperWidth = 280;


            Font titleFont = new Font("Arial", 12, FontStyle.Bold);
            Font regularFont = new Font("Arial", 9, FontStyle.Regular);
            Font BlodFont = new Font("Arial", 9, FontStyle.Bold);
            Font arabicFont = new Font("Arial", 12, FontStyle.Bold);

            // Formats d'alignement
            StringFormat centerFormat = new StringFormat { Alignment = StringAlignment.Center };
            StringFormat rightFormat = new StringFormat { Alignment = StringAlignment.Far };
            // Format spécifique pour l'arabe (Lecture de droite à gauche)
            StringFormat arabicFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.DirectionRightToLeft
            };

            string logoPath = "\\Resources\\Photos\\Shiakati Black and white.png"; // Chemin vers votre logo
            if (File.Exists(logoPath))
            {
                Image logo = Image.FromFile(logoPath);
                // On redimensionne et on centre le logo (ex: 150x150)
                g.DrawImage(logo, (paperWidth - 150) / 2, yPos, 150, 150);
                yPos += 160;
            }

            // Titre
            g.DrawString("شياكتي", arabicFont, Brushes.Black, paperWidth / 2, yPos, arabicFormat);
            yPos += 20;
            g.DrawString($"Ticket N°: {_currentReceip.TicketNumber}", regularFont, Brushes.Black, paperWidth / 2, yPos, centerFormat);
            yPos += 20;
            g.DrawString($"Date: {_currentReceip.Date:dd/MM/yyyy HH:mm}", regularFont, Brushes.Black, paperWidth / 2, yPos, centerFormat);
            yPos += 30;

            //separation 
            g.DrawString("------------------------------------------------", regularFont, Brushes.Black, leftMargin, yPos);
            yPos += 15;

            foreach (var item in _currentReceip.Items)
            {
                g.DrawString(item.Designation,regularFont, Brushes.Black, leftMargin, yPos);
                yPos += 15;

                // Ligne: Qte x Prix Unitaire ............... Prix Total
                string qtePrice = $"{item.Quantity} x {item.UnitPrice:N2} DA";
                g.DrawString(qtePrice, regularFont, Brushes.Black, leftMargin + 10, yPos);
                g.DrawString($"{item.TotalPrice:N2} DA", regularFont, Brushes.Black, rightMargin, yPos, rightFormat);
                yPos += 15;

                g.DrawString("------------------------------------------------", regularFont, Brushes.Black, leftMargin, yPos);
                yPos += 15;

                // 4. TOTAUX ET REMISES
                if (_currentReceip.TotalDiscount > 0)
                {
                    g.DrawString("Sous-total :", regularFont, Brushes.Black, leftMargin, yPos);
                    g.DrawString($"{_currentReceip.TotalAmount + _currentReceip.TotalDiscount:N2} DA", regularFont, Brushes.Black, rightMargin, yPos, rightFormat);
                    yPos += 15;

                    g.DrawString("Remise :", regularFont, Brushes.Black, leftMargin, yPos);
                    g.DrawString($"- {_currentReceip.TotalDiscount:N2} DA", regularFont, Brushes.Black, rightMargin, yPos, rightFormat);
                    yPos += 15;
                }

                g.DrawString("TOTAL A PAYER :", titleFont, Brushes.Black, leftMargin, yPos);
                g.DrawString($"{_currentReceip.TotalAmount:N2} DA", titleFont, Brushes.Black, rightMargin, yPos, rightFormat);
                yPos += 30;

                // 5. MESSAGE DE REMERCIEMENT (En Arabe)
                g.DrawString("شكرا لمروركم الطيب", arabicFont, Brushes.Black, paperWidth / 2, yPos, arabicFormat);
                yPos += 25;

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
                        yPos += qrCodeImage.Height + 10;
                    }
                }

                g.DrawString("Software: NumidixLab", regularFont, Brushes.Black, leftMargin/2, yPos);

                // Optionnel : Ajouter un espace blanc à la fin pour que l'imprimante coupe au bon endroit
                g.DrawString(" ", regularFont, Brushes.Black, leftMargin, yPos + 30);
            
               
            }
        }

    }
}
