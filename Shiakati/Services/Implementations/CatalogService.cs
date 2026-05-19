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

                var catalogContainer = await response.Content.ReadFromJsonAsync<BrdCatgResponseDto>();

                if (catalogContainer == null) return (new(), new());

                // 1. Mapping des Marques
                var brands = catalogContainer.Brands.Select(c => new BrandsModel
                    {
                        BrandID = c.BrandID,
                        BrandName = c.BrandName,
                        CategoryID = c.CategoryID,
                        CategoryName = c.CategoryName
                    }).ToList();

                // 2. Extraction des Catégories uniques
                var categories = catalogContainer.Categories.Select(c => new CategoryModel
                    {
                        CategoryID = c.CategoryID,
                        CategoryName = c.CategoryName,
                        IconPath = c.IconPath
                }).ToList();

                return (brands, categories);
            }catch(Exception ex)
            {
                //add logs
                MessageBox.Show($"{ex}");
                return (new(),new());
            }

        }


        public async Task<CategoryModel> AddCategoryModelAsync(CategoryModel category)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/categories", category);
                response.EnsureSuccessStatusCode();

                var createdCategory = await response.Content.ReadFromJsonAsync<CategoryModel>();
                return createdCategory ?? new CategoryModel();
            }
            catch (Exception ex)
            {
                //add logs
                MessageBox.Show($"{ex}");
                return new CategoryModel();
            }
        }
    }
}