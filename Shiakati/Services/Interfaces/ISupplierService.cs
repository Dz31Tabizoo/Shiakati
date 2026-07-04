using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shiakati.Models;

namespace Shiakati.Services.Interfaces
{
    public interface ISupplierService
    {
        Task<List<SupplierDto>> GetAllAsync();
        Task<SupplierDto> CreateAsync(SupplierDto dto);
        Task UpdateAsync(SupplierDto dto);
        Task DeleteAsync(int id);
        Task<InvoiceImageDto> UploadInvoiceAsync(int supplierId,
                                                string filePath,
                                                DateTime? invoiceDate = null,
                                                int? productsTotal = null,
                                                decimal? totalAmount = null,
                                                decimal? amountPaid = null
                                                );
        Task DeleteInvoiceAsync(int invoiceId);
    }
}
