using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shiakati.Models;

namespace Shiakati.Services.Interfaces.APIServices
{
    public interface ISupplierService
    {
        Task<List<SupplierDto>> GetAllAsync();



        Task<InvoiceImageDto> UpdateInvoiceAsync(UpdateInvoiceRequest request, string? newFilePath = null);        
        Task<SupplierDto> CreateAsync(SupplierDto dto);
        Task UpdateAsync(SupplierDto dto);
        Task DeleteAsync(int id);
        Task<InvoiceImageDto> UploadInvoiceAsync(int supplierId,
                                                string? filePath,
                                                DateTime? invoiceDate = null,
                                                int? productsTotal = null,
                                                decimal? totalAmount = null,
                                                decimal? amountPaid = null
                                                );
        Task DeleteInvoiceAsync(int invoiceId);

        Task<List<SupplierInvoiceItemDto>> GetInvoiceItemsAsync(int invoiceId);
        Task<SupplierInvoiceItemDto> AddInvoiceItemAsync(int invoiceId, AddInvoiceItemRequest request);
        Task DeleteInvoiceItemAsync(int itemId);


    }
}
