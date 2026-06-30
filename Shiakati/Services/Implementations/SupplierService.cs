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

        // ─── DELETE: api/suppliers/{id} ───────────────────────────────────
        public async Task DeleteAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/suppliers/{id}");
            response.EnsureSuccessStatusCode();
        }

        // ─── POST: api/suppliers/{id}/upload-invoice ─────────────────────
        public async Task<InvoiceImageDto> UploadInvoiceAsync(int supplierId, string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            using var form = new MultipartFormDataContent();
            using var fileStream = File.OpenRead(filePath);
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            // The name "file" must match the parameter name in the API Controller (IFormFile file)
            form.Add(fileContent, "file", Path.GetFileName(filePath));

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
    }
}
