
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
    internal class AppDataService : ICatalogDataService,IClientDataService,IDashBordDataService,IDataLoader,IStockDataService,IReservationDataService
    {
        private readonly ICatalogService _catalogService;
        private readonly IClientService _clientService;
        private readonly IDashBordService _dashBordService;
        private readonly IProductVariantsService _stockService;
        private readonly IReservationService _reservationService;

        private readonly ICacheService _cache;
        private readonly ILogger <AppDataService> _logger;

        public ObservableCollection<BrandsModel> Brands { get; } = new(); 
        public ObservableCollection<CategoryModel> Categories { get; } = new();
        public ObservableCollection<ClientSummaryDto> Clients { get; } = new();
        public ObservableCollection<StockMovementModel> StockMovements { get; }
        public ObservableCollection<StockAlertDto> StockAlerts { get; }
        public ObservableCollection<ProductModel> Products { get; } = new();
        public ObservableCollection<ProductVariantModel> Variants { get; } = new();
        public ObservableCollection<ReservationDto> Reservations { get; } = new();


        private bool _catalogLoaded;
        private bool _clientsLoaded;
        private bool _productsLoaded;
        private bool _variantsLoaded;
        private bool _reservationsLoaded;

        public event Action? DataChanged;

        public AppDataService(
            ICatalogService catalogService,
            ICacheService cache,
            IClientService clientService,
            IDashBordService dashBordService,
            IProductVariantsService stockService,IReservationService reservationService,
            ILogger<AppDataService> logger)
        {
            _catalogService = catalogService;
            _cache = cache;
            _dashBordService = dashBordService;
            _clientService = clientService;
            _stockService = stockService;
            _reservationService = reservationService;
            _logger = logger;
        }

        // ───  CatalogService ────────────────────────────────────────────
        public async Task LoadCatalogAsync(bool forceRefresh = false)
        {
            if (!forceRefresh && _catalogLoaded) return;

            // If forcing refresh, reset the flag
            if (forceRefresh) _catalogLoaded = false;

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
        public async Task LoadClientsAsync(bool forceRefresh = false)
        {
            if (!forceRefresh && _catalogLoaded) return;

            // If forcing refresh, reset the flag
            if (forceRefresh) _catalogLoaded = false;

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
                await LoadClientsAsync(forceRefresh:true); // Refresh the entire list to update balances
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
                await LoadClientsAsync(forceRefresh: true); // Refresh the entire list
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

        // ───  StockService ────────────────────────────────────────────

        public async Task LoadProductsAsync(bool forceRefresh = false)
        {
            if (!forceRefresh && _catalogLoaded) return;

            // If forcing refresh, reset the flag
            if (forceRefresh) _catalogLoaded = false;

            var data = await _cache.GetOrLoadAsync(CacheKeys.Products, async () =>
            {
                _logger.LogInformation("Fetching products from API");
                return await _stockService.GetProductsAsync() ?? new List<ProductModel>();
            });
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Products.Clear();
                foreach (var product in data)
                    Products.Add(product);
                _productsLoaded = true;
            });
        }

        public async Task LoadVariantsAsync(bool forceRefresh = false)
        {
            if (!forceRefresh && _catalogLoaded) return;

            // If forcing refresh, reset the flag
            if (forceRefresh) _catalogLoaded = false;

            var data = await _cache.GetOrLoadAsync(CacheKeys.Variants, async () =>
            {
                _logger.LogInformation("Fetching product variants from API");
                return await _stockService.GetProductVariantsAsync() ?? new List<ProductVariantModel>();
            });
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Variants.Clear();
                foreach (var variant in data)
                    Variants.Add(variant);
                _variantsLoaded = true;
            });
        }

        public async Task<ProductVariantResponse?> AddProductVariantAsync(AddVariantRequest request)
        {
            var result = await _stockService.AddProductVariantAsync(request);
            if (result != null)
            {
                // Invalidate cache because variant list has changed
                _cache.Remove(CacheKeys.Variants);
                // Reload variants to update the shared collection
                await LoadVariantsAsync(forceRefresh: true);
                DataChanged?.Invoke();
            }
            return result;
        }

        public async Task<ProductVariantResponse?> UpdateProductVariantAsync(UpdateVariantRequest request)
        {
            var result = await _stockService.UpdateProductVariantAsync(request);
            if (result != null)
            {
                _cache.Remove(CacheKeys.Variants);
                await LoadVariantsAsync(forceRefresh: true);
                DataChanged?.Invoke();
            }
            return result;
        }

        public async Task<List<ProductVariantResponse>?> BulkAddVariantsAsync(BulkAddVariantsRequest request)
        {
            var result = await _stockService.BulkAddVariantsAsync(request);
            if (result != null && result.Any())
            {
                _cache.Remove(CacheKeys.Variants);
                await LoadVariantsAsync(forceRefresh: true);
                DataChanged?.Invoke();
            }
            return result;
        }

        // ─── Stock Valuation ──────────────────────────────────────────

        public async Task<StockValuationResponse> GetStockValuationAsync()
        {
            return await _cache.GetOrLoadAsync(CacheKeys.StockValuation, async () =>
            {
                _logger.LogInformation("Fetching stock valuation from API");
                return await _stockService.GetStockValuationAsync() ?? new StockValuationResponse();
            });
        }

        // ─── ReservationService ──────────────────────────────────────────
        public async Task LoadReservationsAsync(string? status = null, bool forceRefresh = false)
        {
            if (string.IsNullOrEmpty(status) || status == "all")
            {
                if (!forceRefresh && _reservationsLoaded) return;
                if (forceRefresh) _reservationsLoaded = false;

                var data = await _cache.GetOrLoadAsync(CacheKeys.Reservations, async () =>
                {
                    _logger.LogInformation("Fetching reservations from API");
                    return await _reservationService.GetReservationsAsync(status) ?? new List<ReservationDto>();
                });
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Reservations.Clear();
                    foreach (var reservation in data)
                        Reservations.Add(reservation);
                    _reservationsLoaded = true;
                });

            }
            else
            {
                // For filtered status, always fetch fresh (no cache)
                var data = await _reservationService.GetReservationsAsync(status) ?? new List<ReservationDto>();
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Reservations.Clear();

                    foreach (var item in data) 
                        Reservations.Add(item);
                });
            }
        }

        public async Task<int?> CreateReservationAsync(CreateReservationRequest request)
        {
            var result = await _reservationService.CreateReservationAsync(request);
            if (result.HasValue && result.Value > 0)
            {
                _cache.Remove(CacheKeys.Reservations);
                await LoadReservationsAsync(forceRefresh: true); // Refresh the list
                DataChanged?.Invoke();
            }
            return result;
        }

        public async Task<bool> CancelReservationAsync(int id)
        {
            var result = await _reservationService.CancelReservationAsync(id);
            if (result)
            {
                _cache.Remove(CacheKeys.Reservations);
                await LoadReservationsAsync(forceRefresh: true); // Refresh the list
                DataChanged?.Invoke();
            }
            return result;
        }

        public async Task<bool> FulfillReservationAsync(int id, decimal amountPaid)
        {
            var result = await _reservationService.FulfillReservationAsync(id, amountPaid);
            if (result)
            {
                _cache.Remove(CacheKeys.Reservations);
                await LoadReservationsAsync(forceRefresh: true); // Refresh the list
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
                await LoadProductsAsync();
                await LoadVariantsAsync();
                await LoadReservationsAsync();
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