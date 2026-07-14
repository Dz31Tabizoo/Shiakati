
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
    internal class AppDataService : ICatalogDataService,IClientDataService,IDashBordDataService,IDataLoader
    {
        private readonly ICatalogService _catalogService;
        private readonly IClientService _clientService;
        private readonly IDashBordService _dashBordService;

        private readonly ICacheService _cache;
        private readonly ILogger <AppDataService> _logger;

        public ObservableCollection<BrandsModel> Brands { get; } = new(); 
        public ObservableCollection<CategoryModel> Categories { get; } = new();
        public ObservableCollection<ClientSummaryDto> Clients { get; } = new();
        public ObservableCollection<StockMovementModel> StockMovements { get; }
        public ObservableCollection<StockAlertDto> StockAlerts { get; }

        private bool _catalogLoaded;
        private bool _clientsLoaded;

        public event Action? DataChanged;

        public AppDataService(
            ICatalogService catalogService,
            ICacheService cache,
            IClientService clientService,
            IDashBordService dashBordService,
            ILogger<AppDataService> logger)
        {
            _catalogService = catalogService;
            _cache = cache;
            _dashBordService = dashBordService;
            _clientService = clientService;
            _logger = logger;
        }

        // ───  CatalogService ────────────────────────────────────────────
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


        // ───  ClientService ────────────────────────────────────────────
        public async Task LoadClientsAsync()
        {
            if (_clientsLoaded) return;

            var data = await _cache.GetOrLoadAsync(CacheKeys.Clients, async () =>
            {
                _logger.LogInformation("Fetching clients from API");
                return await _clientService.GetClientSummariesAsync(null) ?? new List<ClientSummaryDto>();
            });

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Clients.Clear();
                foreach (var client in data)
                    Clients.Add(client);
                _clientsLoaded = true;
            });
        }

        public async Task<ClientSummaryDto> AddClientAsync(CreateClientRequest client)
        {
            var request = new CreateClientRequest
            {
                FullName = client.FullName,
                PhoneNumber = client.PhoneNumber,
                Address = client.Address,
                Email = client.Email
            };

            var created = await _clientService.CreateClientAsync(request);
            if (created == null)
                throw new Exception("Failed to create client.");

            _cache.Remove(CacheKeys.Clients);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Clients.Add(created);
            });

            DataChanged?.Invoke();
            return created;
        }

        public async Task UpdateClientAsync(ClientSummaryDto client)
        {
            var request = new CreateClientRequest
            {
                FullName = client.FullName,
                PhoneNumber = client.PhoneNumber,
                Address = client.Address,
                Email = client.Email
            };

            var success = await _clientService.UpdateClientAsync(client.ClientId, request);
            if (!success)
                throw new Exception("Failed to update client.");

            _cache.Remove(CacheKeys.Clients);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var existing = Clients.FirstOrDefault(c => c.ClientId == client.ClientId);
                if (existing != null)
                {
                    int index = Clients.IndexOf(existing);
                    Clients[index] = client;
                }
            });

            await LoadClientsAsync(); // Refresh the entire list to ensure balances are updated
            DataChanged?.Invoke();
        }

        public async Task<bool> AddVersementAsync(CreateVersementRequest request)
        {
            var success = await _clientService.AddVersementAsync(request);
            if (success)
            {
                _cache.Remove(CacheKeys.Clients);
                await LoadClientsAsync(); // Refresh the entire list to update balances
                DataChanged?.Invoke();
            }
            return success;
        }

        public async Task<bool> GrantCreditAsync(CreateCreditRequest request)
        {
            var success = await _clientService.GrantCreditAsync(request);
            if (success)
            {
                _cache.Remove(CacheKeys.Clients);
                await LoadClientsAsync(); // Refresh the entire list
                DataChanged?.Invoke();
            }
            return success;
        }

        public async Task<ClientDetailDto?> GetClientDetailAsync(int clientId)
                     => await _clientService.GetClientDetailAsync(clientId);

        public async Task<List<ClientSaleDto>> GetClientSalesAsync(int clientId)
                     => await _clientService.GetClientSalesAsync(clientId);

        // ───  DashBordService ────────────────────────────────────────────

        public async Task<DashboardStatsResponse> GetDashboardDataAsync(DashboardFilterRequest filter)
        {
            // Build cache key based on filter parameters
            string key = $"DashboardStats_{filter.StartDate}_{filter.EndDate}_{filter.UserId}";

            // Try to get from cache; if not, call the domain service
            return await _cache.GetOrLoadAsync(key, async () =>
            {
                _logger.LogInformation("Fetching dashboard stats from API");
                return await _dashBordService.GetDashBordDataAsync(filter) ?? new DashboardStatsResponse();
            });
        }

        public async Task<bool> AcknowledgeAlertAsync(int variantId)
        {
            // Call the domain service to acknowledge the alert
            var result = await _dashBordService.AcknowledgeAlertAsync(variantId);
            if (result)
            {
                // Invalidate dashboard cache because alerts affect the dashboard stats
                _cache.Remove(CacheKeys.DashboardStats);

                // Notify subscribers that data changed (optional)
                DataChanged?.Invoke();
            }
            return result;
        }

        // ─── Load All Essential Data (for login) ──────────────────────
        public async Task LoadAllEssentialDataAsync()
        {
            try
            {
                await LoadCatalogAsync(); // For now, just catalog
                await LoadClientsAsync();
                await GetDashboardDataAsync(new DashboardFilterRequest { StartDate = DateTime.Now.AddDays(-7), EndDate = DateTime.Now }); // Load recent dashboard stats
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