using Shiakati.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Services.Interfaces.DataServices
{
    public interface ISupplierDataService
    {
        ObservableCollection<SupplierDto> Suppliers { get; }

        Task LoadSuppliersAsync(bool forceRefresh = false);

        // ─── CRUD Operations ─────────────────────────────────────────────
        Task<SupplierDto> CreateSupplierAsync(SupplierDto dto);
        Task UpdateSupplierAsync(SupplierDto dto);
        Task DeleteSupplierAsync(int id);

        // ─── Invoice Operations ──────────────────────────────────────────
        Task<InvoiceImageDto> UploadInvoiceAsync(int supplierId, string? filePath, DateTime? invoiceDate = null, int? productsTotal = null, decimal? totalAmount = null, decimal? amountPaid = null);
        Task<InvoiceImageDto> UpdateInvoiceAsync(UpdateInvoiceRequest request, string? newFilePath = null);
        Task DeleteInvoiceAsync(int invoiceId);

        // ─── Invoice Item Operations ─────────────────────────────────────
        Task<List<SupplierInvoiceItemDto>> GetInvoiceItemsAsync(int invoiceId);
        Task<SupplierInvoiceItemDto> AddInvoiceItemAsync(int invoiceId, AddInvoiceItemRequest request);
        Task DeleteInvoiceItemAsync(int itemId);

        event Action? SupplierDataChanged;
    }
}
