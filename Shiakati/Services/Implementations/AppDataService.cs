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
using Shiakati.Services.Interfaces.APIServices;
using Shiakati.Services.Interfaces.CacheService;

namespace Shiakati.Services.Implementations
{
    internal class AppDataService : ICatalogDataService,IClientDataService,IDashBordDataService,IDataLoader,IStockDataService,IReservationDataService,ISaleDataService,IStockMovementDataService,ISupplierDataService
    {
        private readonly ICatalogService _catalogService;
        private readonly IClientService _clientService;
        private readonly IDashBordService _dashBordService;
        private readonly IProductVariantsService _stockService;
        private readonly IReservationService _reservationService;
        private readonly ISaleService _saleService;
        private readonly IStockMovementService _stockMovementService;
        private readonly ISupplierService _supplierService;

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
        public ObservableCollection<SaleModel> Sales { get; } = new();
        public ObservableCollection<StockMovementModel> Movements { get; } = new();
        public ObservableCollection<SupplierDto> Suppliers { get; } = new();



        private bool _catalogLoaded;
        private bool _clientsLoaded;
        private bool _productsLoaded;
        private bool _variantsLoaded;
        private bool _salesLoaded;
        private bool _reservationsLoaded;
        private bool _movementsLoaded;
        private bool _suppliersLoaded;

        public event Action? CatalogDataChanged;
        public event Action? ClientsDataChanged;
        public event Action? DashBordDataChanged;
        public event Action? StockDataChanged;
        public event Action? ReservationDataChanged;
        public event Action? SalesDataChanged;
        public event Action? MovementDataChanged;
        public event Action? SupplierDataChanged;

        public event Action? AllDataLoaded;



        public AppDataService(
            ICatalogService catalogService,
            ICacheService cache,
            ISaleService saleService,
            IClientService clientService,
            IDashBordService dashBordService,
            IStockMovementService stockMovementService,
            ISupplierService supplierService,
            IProductVariantsService stockService,IReservationService reservationService,
            ILogger<AppDataService> logger)
        {
            _catalogService = catalogService;
            _cache = cache;
            _dashBordService = dashBordService;
            _clientService = clientService;
            _stockService = stockService;
            _reservationService = reservationService;
            _stockMovementService = stockMovementService;
            _supplierService = supplierService;
            _saleService = saleService;
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
                CatalogDataChanged?.Invoke();

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

            ClientsDataChanged?.Invoke();
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
            ClientsDataChanged?.Invoke();
        }

        public async Task<bool> AddVersementAsync(CreateVersementRequest request)
        {
            var success = await _clientService.AddVersementAsync(request);
            if (success)
            {
                _cache.Remove(CacheKeys.Clients);
                await LoadClientsAsync(forceRefresh:true); // Refresh the entire list to update balances
                ClientsDataChanged?.Invoke();
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
                ClientsDataChanged?.Invoke();
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
                DashBordDataChanged?.Invoke();
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
                StockDataChanged?.Invoke();
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
                StockDataChanged?.Invoke();
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
                StockDataChanged?.Invoke();
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
                ReservationDataChanged?.Invoke();
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
                ReservationDataChanged?.Invoke();
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
                ReservationDataChanged?.Invoke();
            }
            return result;
        }


        // ─── SalesService ──────────────────────────────────────────

        public async Task LoadSalesAsync(string? search = null, DateTime? from = null, DateTime? to = null, bool forceRefresh = false)
        {
            // For specific searches/filters, always fetch fresh (no cache)
            if (!string.IsNullOrEmpty(search) || from.HasValue || to.HasValue)
            {
                var data = await _saleService.GetSalesAsync(search, from, to) ?? new List<SaleModel>();
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Sales.Clear();
                    foreach (var sale in data)
                        Sales.Add(sale);
                });
                return;
            }

            // For "all sales", use cache
            if (!forceRefresh && _salesLoaded) return;
            if (forceRefresh) _salesLoaded = false;

            var cachedData = await _cache.GetOrLoadAsync(CacheKeys.Sales, async () =>
            {
                _logger.LogInformation("Fetching sales from API");
                return await _saleService.GetSalesAsync(null, null, null) ?? new List<SaleModel>();
            });

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Sales.Clear();
                foreach (var sale in cachedData)
                    Sales.Add(sale);
                _salesLoaded = true;
            });
        }

        public async Task<SaleResponse?> GetSaleAsync(int saleId)
        {
            // Always fetch fresh for detail – no cache
            return await _saleService.GetSaleAsync(saleId);
        }

        public async Task<SaleCreationResult?> CreateSaleAsync(SaleRequest request)
        {
            var result = await _saleService.CreateSaleAsync(request);
            if (result != null)
            {
                _cache.Remove(CacheKeys.Sales);
                await LoadSalesAsync(forceRefresh: true);
                SalesDataChanged?.Invoke();
            }
            return result;
        }

        public async Task<bool> UpdateSaleAsync(int saleId, UpdateSaleRequest request)
        {
            var result = await _saleService.UpdateSaleAsync(saleId, request);
            if (result)
            {
                _cache.Remove(CacheKeys.Sales);
                await LoadSalesAsync(forceRefresh: true);
                SalesDataChanged?.Invoke();
            }
            return result;
        }

        public async Task<bool> VoidSaleAsync(int saleId)
        {
            var result = await _saleService.VoidSaleAsync(saleId);
            if (result)
            {
                _cache.Remove(CacheKeys.Sales);
                await LoadSalesAsync(forceRefresh: true);
                SalesDataChanged?.Invoke();
            }
            return result;
        }


        // ─── Stock Movements ──────────────────────────────────────────

        public async Task LoadMovementsAsync(DateTime? from = null, DateTime? to = null, bool forceRefresh = false)
        {
            // If filters are applied, always fetch fresh (no cache)
            if (from.HasValue || to.HasValue)
            {
                var data = await _stockMovementService.GetMovementsAsync(from, to) ?? new List<StockMovementModel>();
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Movements.Clear();
                    foreach (var movement in data)
                        Movements.Add(movement);
                });
                return;
            }

            // For "all movements", use cache
            if (!forceRefresh && _movementsLoaded) return;
            if (forceRefresh) _movementsLoaded = false;

            var cachedData = await _cache.GetOrLoadAsync(CacheKeys.StockMovements, async () =>
            {
                _logger.LogInformation("Fetching stock movements from API");
                return await _stockMovementService.GetMovementsAsync(null, null) ?? new List<StockMovementModel>();
            });

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Movements.Clear();
                foreach (var movement in cachedData)
                    Movements.Add(movement);
                _movementsLoaded = true;
            });
        }

        // ───  SupplierService ────────────────────────────────────────────


        public async Task LoadSuppliersAsync(bool forceRefresh = false)
        {
            if (!forceRefresh && _suppliersLoaded) return;
            if (forceRefresh) _suppliersLoaded = false;

            var data = await _cache.GetOrLoadAsync(CacheKeys.Suppliers, async () =>
            {
                _logger.LogInformation("Fetching suppliers from API");
                return await _supplierService.GetAllAsync() ?? new List<SupplierDto>();
            });

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Suppliers.Clear();
                foreach (var supplier in data)
                    Suppliers.Add(supplier);
                _suppliersLoaded = true;
            });
        }

        public async Task<SupplierDto> CreateSupplierAsync(SupplierDto dto)
        {
            var result = await _supplierService.CreateAsync(dto);
            if (result != null)
            {
                _cache.Remove(CacheKeys.Suppliers);
                await LoadSuppliersAsync(forceRefresh: true);
                SupplierDataChanged?.Invoke();
            }
            return result;
        }

        public async Task UpdateSupplierAsync(SupplierDto dto)
        {
            await _supplierService.UpdateAsync(dto);
            _cache.Remove(CacheKeys.Suppliers);
            await LoadSuppliersAsync(forceRefresh: true);
            SupplierDataChanged?.Invoke();
        }

        public async Task DeleteSupplierAsync(int id)
        {
            await _supplierService.DeleteAsync(id);
            _cache.Remove(CacheKeys.Suppliers);
            await LoadSuppliersAsync(forceRefresh: true);
            SupplierDataChanged?.Invoke();
        }

        // ─── Invoice Operations ────────────────────────────────────────────

        public async Task<InvoiceImageDto> UploadInvoiceAsync(int supplierId, string? filePath, DateTime? invoiceDate = null, int? productsTotal = null, decimal? totalAmount = null, decimal? amountPaid = null)
        {
            var result = await _supplierService.UploadInvoiceAsync(supplierId, filePath, invoiceDate, productsTotal, totalAmount, amountPaid);
            if (result != null)
            {
                _cache.Remove(CacheKeys.Suppliers);
                await LoadSuppliersAsync(forceRefresh: true);
                SupplierDataChanged?.Invoke();
            }
            return result;
        }

        public async Task<InvoiceImageDto> UpdateInvoiceAsync(UpdateInvoiceRequest request, string? newFilePath = null)
        {
            var result = await _supplierService.UpdateInvoiceAsync(request, newFilePath);
            if (result != null)
            {
                _cache.Remove(CacheKeys.Suppliers);
                await LoadSuppliersAsync(forceRefresh: true);
                SupplierDataChanged?.Invoke();
            }
            return result;
        }

        public async Task DeleteInvoiceAsync(int invoiceId)
        {
            await _supplierService.DeleteInvoiceAsync(invoiceId);
            _cache.Remove(CacheKeys.Suppliers);
            await LoadSuppliersAsync(forceRefresh: true);
            SupplierDataChanged?.Invoke();
        }

        // ─── Invoice Item Operations ──────────────────────────────────────

        public async Task<List<SupplierInvoiceItemDto>> GetInvoiceItemsAsync(int invoiceId)
        {
            // Always fetch fresh – no cache
            return await _supplierService.GetInvoiceItemsAsync(invoiceId) ?? new List<SupplierInvoiceItemDto>();
        }

        public async Task<SupplierInvoiceItemDto> AddInvoiceItemAsync(int invoiceId, AddInvoiceItemRequest request)
        {
            var result = await _supplierService.AddInvoiceItemAsync(invoiceId, request);
            if (result != null)
            {
                // Invalidate supplier cache because invoice items affect the supplier's data
                _cache.Remove(CacheKeys.Suppliers);
                await LoadSuppliersAsync(forceRefresh: true);
                SupplierDataChanged?.Invoke();
            }
            return result;
        }

        public async Task DeleteInvoiceItemAsync(int itemId)
        {
            await _supplierService.DeleteInvoiceItemAsync(itemId);
            _cache.Remove(CacheKeys.Suppliers);
            await LoadSuppliersAsync(forceRefresh: true);
            SupplierDataChanged?.Invoke();
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
                await LoadSalesAsync();
                await LoadMovementsAsync();
                await LoadSuppliersAsync();

                await GetDashboardDataAsync(new DashboardFilterRequest { StartDate = DateTime.Now.AddDays(-7), EndDate = DateTime.Now }); // Load recent dashboard stats
                _logger.LogInformation("All essential data loaded.");

                AllDataLoaded?.Invoke();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load essential data");
                throw;
            }
        }        
    }
}