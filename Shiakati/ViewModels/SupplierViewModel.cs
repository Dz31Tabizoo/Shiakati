using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Shiakati.Models;
using Shiakati.Services.Implementations;
using Shiakati.Services.Interfaces;
using Shiakati.Views;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Shiakati.ViewModels
{
    public partial class SupplierViewModel : ObservableObject
    {
        //APPdATA COMMING
        private readonly ISupplierService _supplierService;
        private readonly IProductVariantsService _prod;


        public ObservableCollection<SupplierDto> Suppliers { get; } = new();

        [ObservableProperty]
        private SupplierDto? _selectedSupplier;

        [ObservableProperty]
        private InvoiceImageDto? _selectedInvoice;

        [ObservableProperty] private ObservableCollection<SupplierInvoiceItemDto> _invoiceItems = new();



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

        public SupplierViewModel(ISupplierService supplierService, IProductVariantsService prod)
        {
            _supplierService = supplierService;
            _prod = prod;
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
            var result = MessageBox.Show("Supprimer cette facture ?", "Confirmation", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                await _supplierService.DeleteInvoiceAsync(invoiceId);
                var inv = SelectedSupplier.Invoices.FirstOrDefault(i => i.Id == invoiceId);
                if (inv != null) SelectedSupplier.Invoices.Remove(inv);
            }
            else
            {
                return;
            }

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

        // inovice items Commands

        [RelayCommand]
        private async Task LoadInvoiceItems(InvoiceImageDto invoice)
        {
            if (invoice == null) return;
            // Do NOT set SelectedInvoice here
            var items = await _supplierService.GetInvoiceItemsAsync(invoice.Id);
            InvoiceItems.Clear();
            foreach (var item in items)
                InvoiceItems.Add(item);
        }

        
        [RelayCommand]
        private async Task AddInvoiceItem()
        {
            if (SelectedInvoice == null) return;

            var dialog = new AddInvoiceItemDialog(_prod) // Use correct service
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                var request = new AddInvoiceItemRequest
                {
                    VariantId = dialog.SelectedVariantId,
                    Quantity = dialog.Quantity,
                    UnitCost = dialog.UnitCost,
                    Notes = dialog.Notes
                };
                var newItem = await _supplierService.AddInvoiceItemAsync(SelectedInvoice.Id, request);
                InvoiceItems.Add(newItem);
            }
        }

        [RelayCommand]
        private async Task DeleteInvoiceItem(int itemId)
        {
            if (MessageBox.Show("Supprimer cet article ?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                await _supplierService.DeleteInvoiceItemAsync(itemId);
                var item = InvoiceItems.FirstOrDefault(i => i.SupplierInvoiceItemId == itemId);
                if (item != null) InvoiceItems.Remove(item);
            }
        }

        partial void OnSelectedInvoiceChanged(InvoiceImageDto? value)
        {
            if (value != null)
                _ = LoadInvoiceItems(value);
        }
    }
}