using Microsoft.Extensions.DependencyInjection;
using Shiakati.Models;
using Shiakati.Services.Interfaces.APIServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace Shiakati.Views
{
    /// <summary>
    /// Logique d'interaction pour SupplierView.xaml
    /// </summary>
    public partial class SupplierView : UserControl
    {
        
        public SupplierView()
        {
            InitializeComponent();
        }

        private static bool _cleanupDone = false;

        private void EnsureTempCleanup()
        {
            if (_cleanupDone) return;
            CleanupTempInvoices();
            _cleanupDone = true;
        }

        private async void PreviewInvoice_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the invoice object from the button's Tag
            var invoice = (sender as FrameworkElement)?.Tag as InvoiceImageDto;
            if (invoice == null) return;

            // 2. Get the authentication service to retrieve the token
            var authService = App.ServiceProvider?.GetRequiredService<IAuthenticationClientService>();
            if (authService == null)
            {
                MessageBox.Show("Erreur d'authentification.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 3. Build the full URL to the file
            string fullUrl = App.ApiBaseUrl.TrimEnd('/') + invoice.FilePath;

            try
            {
                using var httpClient = new HttpClient();
                var token = authService.CurrentSession?.Token;
                if (!string.IsNullOrEmpty(token))
                {
                    httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                // 4. Download the file bytes
                byte[] fileData = await httpClient.GetByteArrayAsync(fullUrl);

                // 5. Determine file type from extension
                string extension = Path.GetExtension(invoice.FileName).ToLowerInvariant();
                var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".webp" };

                if (imageExtensions.Contains(extension))
                {
                    // ─── IMAGE – Display in popup ───
                    using var stream = new MemoryStream(fileData);
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    var previewWindow = new Window
                    {
                        Title = invoice.FileName,
                        Width = 600,
                        Height = 500,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = Application.Current.MainWindow,
                        Content = new Image
                        {
                            Source = bitmap,
                            Stretch = Stretch.Uniform,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(20)
                        }
                    };
                    previewWindow.Show();
                }
                else
                {
                    // ─── DOCUMENT (PDF, DOCX, XLSX) – Open with default app ───
                    string tempFolder = Path.Combine(Path.GetTempPath(), "ShiakatiInvoices");
                    EnsureTempCleanup();
                    if (!Directory.Exists(tempFolder))
                        Directory.CreateDirectory(tempFolder);

                    string tempFilePath = Path.Combine(tempFolder, invoice.FileName);

                    // Ensure unique file name if it already exists
                    int counter = 1;
                    string originalPath = tempFilePath;
                    while (File.Exists(tempFilePath))
                    {
                        string nameWithoutExt = Path.GetFileNameWithoutExtension(originalPath);
                        string ext = Path.GetExtension(originalPath);
                        tempFilePath = Path.Combine(tempFolder, $"{nameWithoutExt}_{counter}{ext}");
                        counter++;
                    }

                    // Save the file to disk
                    await File.WriteAllBytesAsync(tempFilePath, fileData);

                    // Open with the default application
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = tempFilePath,
                        UseShellExecute = true // Important: opens with default app
                    });

                    // Optional: Notify the user where the file was saved
                    MessageBox.Show($"Le fichier a été ouvert avec votre application par défaut.\n\nEmplacement : {tempFilePath}",
                                    "Fichier ouvert", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Impossible de charger le fichier : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CleanupTempInvoices()
        {
            string tempFolder = Path.Combine(Path.GetTempPath(), "ShiakatiInvoices");
            if (!Directory.Exists(tempFolder)) return;

            var files = Directory.GetFiles(tempFolder);
            foreach (var file in files)
            {
                try
                {
                    var info = new FileInfo(file);
                    if (info.CreationTime < DateTime.Now.AddDays(-7))
                        File.Delete(file);
                }
                catch { /* ignore */ }
            }
        }

    }
}
