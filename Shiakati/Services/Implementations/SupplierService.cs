using Shiakati.Models; // Your DTOs namespace
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Shiakati.Services.Interfaces;

namespace Shiakati.Services.Implementations
{
    public class SupplierService : ISupplierService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public SupplierService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        // ─── GET: api/suppliers ───────────────────────────────────────────
        public async Task<List<SupplierDto>> GetAllAsync()
        {
            var response = await _httpClient.GetAsync("api/suppliers");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<SupplierDto>>(_jsonOptions) ?? new List<SupplierDto>();
        }

        // ─── POST: api/suppliers ──────────────────────────────────────────
        public async Task<SupplierDto> CreateAsync(SupplierDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/suppliers", dto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<SupplierDto>(_jsonOptions);
        }

        // ─── PUT: api/suppliers/{id} ──────────────────────────────────────
        public async Task UpdateAsync(SupplierDto dto)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/suppliers/{dto.SupplierId}", dto);
            response.EnsureSuccessStatusCode();
        }

        public async Task<InvoiceImageDto> UpdateInvoiceAsync(UpdateInvoiceRequest request, string? newFilePath = null)
        {
            using var form = new MultipartFormDataContent();

            // Add metadata fields
            form.Add(new StringContent(request.InvoiceId.ToString()), "InvoiceId");
            if (request.InvoiceDate.HasValue)
                form.Add(new StringContent(request.InvoiceDate.Value.ToString("yyyy-MM-ddTHH:mm:ss")), "InvoiceDate");
            if (request.ProductsTotal.HasValue)
                form.Add(new StringContent(request.ProductsTotal.Value.ToString()), "ProductsTotal");
            if (request.TotalAmount.HasValue)
                form.Add(new StringContent(request.TotalAmount.Value.ToString("N2")), "TotalAmount");
            if (request.AmountPaid.HasValue)
                form.Add(new StringContent(request.AmountPaid.Value.ToString("N2")), "AmountPaid");

            // Add new file if provided
            if (!string.IsNullOrEmpty(newFilePath) && File.Exists(newFilePath))
            {
                byte[] fileBytes = File.ReadAllBytes(newFilePath);
                var fileStream = new MemoryStream(fileBytes);
                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                form.Add(fileContent, "file", Path.GetFileName(newFilePath));
            }

            var response = await _httpClient.PutAsync($"api/suppliers/invoices/{request.InvoiceId}", form);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<InvoiceImageDto>(_jsonOptions);
        }

        // ─── DELETE: api/suppliers/{id} ───────────────────────────────────
        public async Task DeleteAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/suppliers/{id}");
            response.EnsureSuccessStatusCode();
        }

        // ─── POST: api/suppliers/{id}/upload-invoice ─────────────────────
        public async Task<InvoiceImageDto> UploadInvoiceAsync(
                                                int supplierId,
                                                string? filePath,
                                                DateTime? invoiceDate = null,
                                                int? productsTotal = null,
                                                decimal? totalAmount = null,
                                                decimal? amountPaid = null)
        {
            using var form = new MultipartFormDataContent();

            // ─── Add file only if provided ───
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                var fileStream = File.OpenRead(filePath);   // No using!
                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                form.Add(fileContent, "file", Path.GetFileName(filePath));
            }

            // ─── Add other fields ───
            if (invoiceDate.HasValue)
                form.Add(new StringContent(invoiceDate.Value.ToString("yyyy-MM-ddTHH:mm:ss")), "invoiceDate");
            if (productsTotal.HasValue)
                form.Add(new StringContent(productsTotal.Value.ToString()), "productsTotal");
            if (totalAmount.HasValue)
                form.Add(new StringContent(totalAmount.Value.ToString()), "totalAmount");
            if (amountPaid.HasValue)
                form.Add(new StringContent(amountPaid.Value.ToString()), "amountPaid");

            var response = await _httpClient.PostAsync($"api/suppliers/{supplierId}/upload-invoice", form);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<InvoiceImageDto>(_jsonOptions);
        }


        // ─── DELETE: api/suppliers/invoices/{invoiceId} ──────────────────
        public async Task DeleteInvoiceAsync(int invoiceId)
        {
            var response = await _httpClient.DeleteAsync($"api/suppliers/invoices/{invoiceId}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<List<SupplierInvoiceItemDto>> GetInvoiceItemsAsync(int invoiceId)
        {
            var response = await _httpClient.GetAsync($"api/suppliers/invoices/{invoiceId}/items");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<SupplierInvoiceItemDto>>(_jsonOptions) ?? new();
        }

        public async Task<SupplierInvoiceItemDto> AddInvoiceItemAsync(int invoiceId, AddInvoiceItemRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync($"api/suppliers/invoices/{invoiceId}/items", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<SupplierInvoiceItemDto>(_jsonOptions);
        }

        public async Task DeleteInvoiceItemAsync(int itemId)
        {
            var response = await _httpClient.DeleteAsync($"api/suppliers/invoices/items/{itemId}");
            response.EnsureSuccessStatusCode();
        }


    }
}
