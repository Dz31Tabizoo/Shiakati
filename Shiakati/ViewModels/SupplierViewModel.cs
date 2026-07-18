using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Shiakati.Models;
using Shiakati.Services.Implementations;
using Shiakati.Services.Interfaces;
using Shiakati.Services.Interfaces.DataServices;
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
        private readonly ISupplierDataService _supplierDataService;
        private readonly IStockDataService _prod;
        private readonly ILogger<SupplierViewModel> _logger;


        public ObservableCollection<SupplierDto> Suppliers => _supplierDataService.Suppliers;

        [ObservableProperty] private SupplierDto? _selectedSupplier;

        [ObservableProperty] private InvoiceImageDto? _selectedInvoice;

        [ObservableProperty] private ObservableCollection<SupplierInvoiceItemDto> _invoiceItems = new();

        [ObservableProperty] private bool _isEditing;

        [ObservableProperty] private string? _editName;

        [ObservableProperty] private DateTime? _invoiceDate = DateTime.Today;
        [ObservableProperty] private int? _productsTotal;
        [ObservableProperty] private decimal? _totalAmount;
        [ObservableProperty] private decimal? _amountPaid;
        [ObservableProperty] private decimal? _amountRest;
        public bool IsSupplierSelected => SelectedSupplier != null;

        public SupplierViewModel(ISupplierDataService supplierDataService,ILogger<SupplierViewModel> logger, IStockDataService prod)
        {
            
            _supplierDataService = supplierDataService;
            _prod = prod;
            _logger = logger;

            _supplierDataService.SupplierDataChanged += OnSupplierDataChanged;
            _ = LoadSuppliers();
            
        }


        public async Task LoadSuppliers()
        {
            await _supplierDataService.LoadSuppliersAsync();
                      
        }

        [RelayCommand] private async Task AddSupplier()
        {
            var newSupplier = new SupplierDto { Name = "Nouveau fournisseur" };
            var created = await _supplierDataService.CreateSupplierAsync(newSupplier);
            
            SelectedSupplier = created;
        }

        [RelayCommand] private async Task SaveSupplier()
        {
            if (SelectedSupplier == null) return;
            await _supplierDataService.UpdateSupplierAsync(SelectedSupplier);

            
            IsEditing = false;
        }

        [RelayCommand] private async Task DeleteSupplier()
        {
            if (SelectedSupplier == null) return;
            if (MessageBox.Show("Supprimer ce fournisseur ?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                await _supplierDataService.DeleteSupplierAsync(SelectedSupplier.SupplierId);
                
                SelectedSupplier = null;
            }
        }

        [RelayCommand] private async Task UploadInvoice()
        {
            if (SelectedSupplier == null) return;

            var dialog = new InvoiceUploadDialog { Owner = Application.Current.MainWindow };
            if (dialog.ShowDialog() == true)
            {
                await _supplierDataService.UploadInvoiceAsync(
                    SelectedSupplier.SupplierId,
                    dialog.FilePath,
                    dialog.InvoiceDate,
                    dialog.ProductsTotal,
                    dialog.TotalAmount,
                    dialog.AmountPaid
                );
                // ✅ UI updates automatically
            }
        }


        partial void OnSelectedSupplierChanged(SupplierDto? value)
        {
            OnPropertyChanged(nameof(IsSupplierSelected));
        }

        [RelayCommand] private async Task DeleteInvoice(int invoiceId)
        {
            if (SelectedSupplier == null) return;
            if (MessageBox.Show("Supprimer cette facture ?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                await _supplierDataService.DeleteInvoiceAsync(invoiceId);
                // ✅ UI updates automatically
            }
        }

        [RelayCommand] private async Task EditInvoice(InvoiceImageDto invoice)
        {
            if (SelectedSupplier == null || invoice == null) return;

            var dialog = new InvoiceUploadDialog(invoice) { Owner = Application.Current.MainWindow };
            if (dialog.ShowDialog() == true)
            {
                var request = new UpdateInvoiceRequest
                {
                    InvoiceId = invoice.Id,
                    InvoiceDate = dialog.InvoiceDate,
                    ProductsTotal = dialog.ProductsTotal,
                    TotalAmount = dialog.TotalAmount,
                    AmountPaid = dialog.AmountPaid
                };

                string? newFilePath = dialog.FileReplaced ? dialog.FilePath : null;
                await _supplierDataService.UpdateInvoiceAsync(request, newFilePath);
                // ✅ UI updates automatically
            }
        }

        // inovice items Commands

        [RelayCommand] private async Task LoadInvoiceItems(InvoiceImageDto invoice)
        {
            if (invoice == null) return;
            var items = await _supplierDataService.GetInvoiceItemsAsync(invoice.Id);
            InvoiceItems.Clear();
            foreach (var item in items)
                InvoiceItems.Add(item);
        }



        [RelayCommand] private async Task AddInvoiceItem()
        {
            if (SelectedInvoice == null) return;

            var dialog = new AddInvoiceItemDialog(_prod) { Owner = Application.Current.MainWindow };
            if (dialog.ShowDialog() == true)
            {
                var request = new AddInvoiceItemRequest
                {
                    VariantId = dialog.SelectedVariantId,
                    Quantity = dialog.Quantity,
                    UnitCost = dialog.UnitCost,
                    Notes = dialog.Notes
                };
                await _supplierDataService.AddInvoiceItemAsync(SelectedInvoice.Id, request);
                // ✅ UI updates automatically (Data Service refreshes supplier)
            }
        }

        [RelayCommand] private async Task DeleteInvoiceItem(int itemId)
        {
            if (MessageBox.Show("Supprimer cet article ?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                await _supplierDataService.DeleteInvoiceItemAsync(itemId);                
            }
        }

        partial void OnSelectedInvoiceChanged(InvoiceImageDto? value)
        {
            if (value != null)
                _ = LoadInvoiceItems(value);
        }

        private async void OnSupplierDataChanged()
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() => LoadSuppliers());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du rechargement des fournisseurs : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Dispose()
        {
            _supplierDataService.SupplierDataChanged -= OnSupplierDataChanged;
        }
    }
}