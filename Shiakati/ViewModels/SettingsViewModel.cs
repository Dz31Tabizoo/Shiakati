using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Shiakati.Properties;

namespace Shiakati.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        [ObservableProperty]
        private string? _selectedPrinterName;

        public ObservableCollection<string> InstalledPrinters { get; } = new ();

        //constractor
        public SettingsViewModel()
        {
            LoadPrinters();
            SelectedPrinterName = Settings.Default.TicketPrinterName;
        }


        private void LoadPrinters()
        {
            InstalledPrinters.Clear();
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                InstalledPrinters.Add(printer);
            }
        }
        [RelayCommand]
        private void SaveSettings()
        {
            // Save the selected printer name to application settings
             Properties.Settings.Default.TicketPrinterName = SelectedPrinterName;
             Properties.Settings.Default.Save();

            System.Windows.MessageBox.Show("Settings saved successfully!", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }


    }
}
