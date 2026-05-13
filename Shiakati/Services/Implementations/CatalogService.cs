using Shiakati.Services.Interfaces;
using Shiakati.Models;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows.Controls.Ribbon.Primitives;
using System.Windows;

namespace Shiakati.Services.Implementations
{
    public class CatalogService : ICatalogService
    {
        private readonly HttpClient _http;

        public CatalogService(HttpClient http)
        {
            _http = http;
        }

        public async Task<(List<BrandsModel> Brands, List<CategoryModel> Categories)> GetInitialGatalogDataAsync()
        {
            try
            {
                // On appelle la route exacte de votre contrôleur
                var response = await _http.GetAsync("api/brands/GetCatalog");
                response.EnsureSuccessStatusCode();

                var dtos = await response.Content.ReadFromJsonAsync<List<BrdCatgResponseDto>>();

                if (dtos == null) return (new(), new());

                // 1. Mapping des Marques
                var brands = dtos.Select(d => new BrandsModel
                {
                    BrandID = d.BrandId,
                    BrandName = d.BrandName,
                    CategoryID = d.CategoryId
                }).ToList();

                // 2. Extraction des Catégories uniques
                var categories = dtos
                    .Where(d => d.CategoryId.HasValue)
                    .GroupBy(d => d.CategoryId)
                    .Select(group => new CategoryModel
                    {
                        CategoryID = group.Key!.Value,
                        CategoryName = group.First().CategoryName ?? "Inconnu"
                    }).ToList();

                return (brands, categories);
            }catch(Exception ex)
            {
                //add logs
                MessageBox.Show($"{ex}");
                return (new(),new());
            }

        }
    }
}
