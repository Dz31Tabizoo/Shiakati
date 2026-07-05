using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Shiakati.Models;
using Shiakati.Services.Interfaces;
using Shiakati.Views;

namespace Shiakati.ViewModels
{
    public partial class SupplierViewModel : ObservableObject
    {
        private readonly ISupplierService _supplierService;

        public ObservableCollection<SupplierDto> Suppliers { get; } = new();

        [ObservableProperty]
        private SupplierDto? _selectedSupplier;

        

        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private string? _editName;

        [ObservableProperty] private DateTime? _invoiceDate = DateTime.Today;
        [ObservableProperty] private int? _productsTotal;
        [ObservableProperty] private decimal? _totalAmount;
        [ObservableProperty] private decimal? _amountPaid;
        [ObservableProperty] private decimal? _amountRest;
        public bool IsSupplierSelected => SelectedSupplier != null;

        public SupplierViewModel(ISupplierService supplierService)
        {
            _supplierService = supplierService;
            _ = LoadSuppliers();
        }


        public async Task LoadSuppliers()
        {
            var list = await _supplierService.GetAllAsync();
            Suppliers.Clear();
            foreach (var s in list)
                Suppliers.Add(s);
        }

        [RelayCommand]
        private async Task AddSupplier()
        {
            var newSupplier = new SupplierDto { Name = "Nouveau fournisseur" };
            var created = await _supplierService.CreateAsync(newSupplier);
            Suppliers.Add(created);
            SelectedSupplier = created;
        }

        [RelayCommand]
        private async Task SaveSupplier()
        {
            if (SelectedSupplier == null) return;
            await _supplierService.UpdateAsync(SelectedSupplier);
            var index = Suppliers.IndexOf(SelectedSupplier);
            Suppliers[index] = SelectedSupplier;
            IsEditing = false;
        }

        [RelayCommand]
        private async Task DeleteSupplier()
        {
            if (SelectedSupplier == null) return;
            if (MessageBox.Show("Supprimer ce fournisseur ?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                await _supplierService.DeleteAsync(SelectedSupplier.SupplierId);
                Suppliers.Remove(SelectedSupplier);
                SelectedSupplier = null;
            }
        }

        [RelayCommand]
        private async Task UploadInvoice()
        {
            if (SelectedSupplier == null) return;

            // Create a dialog to collect invoice details
            var dialog = new InvoiceUploadDialog
            {
                Owner = Application.Current.MainWindow
            };
            if (dialog.ShowDialog() == true)
            {
                var uploaded = await _supplierService.UploadInvoiceAsync(
                    SelectedSupplier.SupplierId,
                    dialog.FilePath,
                    dialog.InvoiceDate,
                    dialog.ProductsTotal,
                    dialog.TotalAmount,
                    dialog.AmountPaid
                );
                SelectedSupplier.Invoices.Add(uploaded);

                var index = Suppliers.IndexOf(SelectedSupplier);
                Suppliers[index] = SelectedSupplier;
            }
        }

        partial void OnSelectedSupplierChanged(SupplierDto? value)
        {
            OnPropertyChanged(nameof(IsSupplierSelected));
        }

        [RelayCommand]
        private async Task DeleteInvoice(int invoiceId)
        {
            if (SelectedSupplier == null) return;
            await _supplierService.DeleteInvoiceAsync(invoiceId);
            var inv = SelectedSupplier.Invoices.FirstOrDefault(i => i.Id == invoiceId);
            if (inv != null) SelectedSupplier.Invoices.Remove(inv);
        }

        [RelayCommand]
        private async Task EditInvoice(InvoiceImageDto invoice)
        {
            if (SelectedSupplier == null || invoice == null) return;

            var dialog = new InvoiceUploadDialog(invoice)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                // Build the update request
                var request = new UpdateInvoiceRequest
                {
                    InvoiceId = invoice.Id,
                    InvoiceDate = dialog.InvoiceDate,
                    ProductsTotal = dialog.ProductsTotal,
                    TotalAmount = dialog.TotalAmount,
                    AmountPaid = dialog.AmountPaid
                };

                // Pass the new file only if replaced
                string? newFilePath = dialog.FileReplaced ? dialog.FilePath : null;

                var updated = await _supplierService.UpdateInvoiceAsync(request, newFilePath);

                // Replace the old invoice in the ObservableCollection
                var index = SelectedSupplier.Invoices.IndexOf(invoice);
                if (index >= 0)
                    SelectedSupplier.Invoices[index] = updated;

                // Refresh the main supplier list (if needed)
                var supplierIndex = Suppliers.IndexOf(SelectedSupplier);
                if (supplierIndex >= 0)
                    Suppliers[supplierIndex] = SelectedSupplier;
            }
        }
    }
}