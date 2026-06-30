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

            var dialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp;*.pdf)|*.jpg;*.jpeg;*.png;*.bmp;*.pdf"
            };
            if (dialog.ShowDialog() == true)
            {
                var file = new FileInfo(dialog.FileName);
                var uploaded = await _supplierService.UploadInvoiceAsync(SelectedSupplier.SupplierId, file.FullName);
                SelectedSupplier.Invoices.Add(uploaded);

                // Refresh the list to update the binding
                var index = Suppliers.IndexOf(SelectedSupplier);
                Suppliers[index] = SelectedSupplier;
            }
        }

        [RelayCommand]
        private async Task DeleteInvoice(int invoiceId)
        {
            if (SelectedSupplier == null) return;
            await _supplierService.DeleteInvoiceAsync(invoiceId);
            var inv = SelectedSupplier.Invoices.FirstOrDefault(i => i.Id == invoiceId);
            if (inv != null) SelectedSupplier.Invoices.Remove(inv);
        }
    }
}