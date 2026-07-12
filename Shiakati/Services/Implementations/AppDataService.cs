
using Shiakati.Services.Interfaces;
using Shiakati.Services.Interfaces.DataServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shiakati.Models;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows;

namespace Shiakati.Services.Implementations
{
    internal class AppDataService : ICatalogDataService,IClientDataService
    {
        private readonly ICatalogService _catalogService;
        private readonly ICacheService _cache;
        private readonly ILogger <AppDataService> _logger;

        public ObservableCollection<BrandsModel> Brands { get; } = new(); 
        public ObservableCollection<CategoryModel> Categories { get; } = new();
        public ObservableCollection<ClientSummaryDto> Clients { get; } = new();

        private bool _catalogLoaded;
        private bool _clientsLoaded;

        public event Action? DataChanged;

        public AppDataService(
            ICatalogService catalogService,
            ICacheService cache,
            ILogger<AppDataService> logger)
        {
            _catalogService = catalogService;
            _cache = cache;
            _logger = logger;
        }

        // ───  ICatalogDataService ────────────────────────────────────────────
        public async Task LoadCatalogAsync()
        {
            if (_catalogLoaded) return;

            try
            {
                var data = await _cache.GetOrLoadAsync(CacheKeys.Catalog, async () =>
                {
                    _logger.LogInformation("Fetching catalog from API");
                    return await _catalogService.GetInitialGatalogDataAsync();
                }, TimeSpan.FromHours(1)); // Cache for 1 hour

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Brands.Clear();
                    Categories.Clear();
                    foreach (var brand in data.Brands)
                        Brands.Add(brand);
                    foreach (var category in data.Categories)
                        Categories.Add(category);
                    _catalogLoaded = true;
                });

                _logger.LogInformation("Catalog loaded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load catalog");
                throw; // Re-throw to let caller handle
            }
        }
        public async Task<CategoryModel> AddCategoryAsync(CategoryModel category)
        {
            try
            {
                // 1. Call the domain service to create the category
                var created = await _catalogService.AddCategoryModelAsync(category);

                // 2. Invalidate cache (catalog data changed)
                _cache.Remove(CacheKeys.Catalog);

                // 3. Update the UI collection on the UI thread
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Categories.Add(created);
                });

                // 4. Notify subscribers
                DataChanged?.Invoke();

                return created;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add category");
                throw;
            }
        }


        // ───  IClientDataService ────────────────────────────────────────────
        // to be continued with methods for loading clients, adding clients, updating clients, etc., similar to the catalog methods above.














        // ─── Load All Essential Data (for login) ──────────────────────
        public async Task LoadAllEssentialDataAsync()
        {
            try
            {
                await LoadCatalogAsync(); // For now, just catalog
                // Later we'll add Clients, Suppliers, etc.
                _logger.LogInformation("All essential data loaded.");
                DataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load essential data");
                throw;
            }
        }

        
    }
}