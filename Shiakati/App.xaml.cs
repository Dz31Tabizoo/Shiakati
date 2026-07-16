using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Serilog;
using Shiakati.Helpers;
using Shiakati.Services.Implementations;
using Shiakati.Services.Interfaces;
using Shiakati.Services.Interfaces.DataServices;
using Shiakati.ViewModels;
using Shiakati.Views;
using System.Data;
using System.Net.Http;
using System.Windows;

namespace Shiakati
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider? ServiceProvider { get; private set; }
        public static string ApiBaseUrl { get; private set; } = "";

        public App()
        {
            //serilog configuration
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File("logs/shiakati_log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
                .CreateLogger();

            //Configuration setup
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsetting.json", optional: false, reloadOnChange: true);

            var configuration = builder.Build();

            // Read the base URL
            ApiBaseUrl = configuration["ApiBaseUrl"];

            //DI configuration
            var services = new ServiceCollection();
            ConfigureServices(services, ApiBaseUrl);
            ServiceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(ServiceCollection services, string baseUrl)
        {
            // . Logging
            services.AddLogging(configure => configure.AddSerilog());
            // . Singleton hard
            services.AddSingleton<AuthService>();
            // . Lie l'interface
            services.AddSingleton<IAuthenticationClientService>(sp => sp.GetRequiredService<AuthService>());


            // . Service d'Auth (SANS intercepteur car il crée le token)
            services.AddHttpClient<AuthService>(client =>
            {
                client.BaseAddress = new Uri(baseUrl);
            });


            // . Le Handler pour le Token
            services.AddTransient<AuthenticationHandler>();

            // ---------------------------------------------------------
            // C'EST ICI QU'ON AJOUTE LES SERVICES (Client Typé)
            // ---------------------------------------------------------



            services.AddHttpClient<ICatalogService, CatalogService>(client =>
            {
                client.BaseAddress = new Uri(baseUrl);

            })
            .AddHttpMessageHandler<AuthenticationHandler>(); // Sécurité activée ! // Injection automatique du token !


            

            services.AddHttpClient<IProductVariantsService, ProductVariantsService>(client =>
            {
                client.BaseAddress = new Uri(baseUrl);
            })
                .AddHttpMessageHandler<AuthenticationHandler>();

            services.AddHttpClient<ISaleService, SaleService>(client =>
            {
                client.BaseAddress = new Uri(baseUrl);
            })
            .AddHttpMessageHandler<AuthenticationHandler>();

            services.AddHttpClient<IStockMovementService, StockMovementService>(client =>
            {
                client.BaseAddress = new Uri(baseUrl);
            })
             .AddHttpMessageHandler<AuthenticationHandler>();

            services.AddHttpClient<IClientService, ClientService>(client =>
            {
                client.BaseAddress = new Uri(baseUrl);
            }).AddHttpMessageHandler<AuthenticationHandler>();

            services.AddHttpClient<IReservationService, ReservationService>(client =>
            {
                client.BaseAddress = new Uri(baseUrl);
            }).AddHttpMessageHandler<AuthenticationHandler>();

            services.AddHttpClient<IDashBordService, DashBordService>(client =>
            {
                client.BaseAddress = new Uri(baseUrl);
            }).AddHttpMessageHandler<AuthenticationHandler>();

            services.AddHttpClient<ISupplierService, SupplierService>(client =>
            {
                client.BaseAddress = new Uri(baseUrl);
            }).AddHttpMessageHandler<AuthenticationHandler>();

            // ---------------------------------------------------------
            // . Enregistrement des Views et ViewModels
            // ---------------------------------------------------------
            services.AddSingleton<MainView>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<PosContainerViewModel>();
            services.AddTransient<POSViewModel>();
            services.AddTransient<LoginView>();
            services.AddTransient<LoginViewModel>();
            services.AddSingleton<StockViewModel>();
            services.AddTransient<StockView>();
            services.AddTransient<SalesHistoryViewModel>();
            services.AddTransient<SalesHistoryView>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<SettingsView>();
            services.AddTransient<StockMovementsViewModel>();
            services.AddTransient<StockMovementsView>();
            services.AddTransient<ClientListViewModel>();
            services.AddTransient<ClientListView>();
            services.AddTransient<ClientDetailViewModel>();
            services.AddTransient<ClientDetailView>();
            services.AddTransient<ReservationsViewModel>();
            services.AddTransient<ReservationsView>();
            services.AddTransient<DashBordViewModel>();
            services.AddTransient<DashBordView>();
            services.AddTransient<SupplierViewModel>();
            services.AddTransient<SupplierView>();
            services.AddTransient<AddInvoiceItemDialog>();

            // . Autres services utilitaires
            services.AddSingleton<IPrintService, PrintService>();
            services.AddTransient<IBarCodePrintService, BarcodePrintService>();
            services.AddSingleton<ICacheService, AppCacheService>();

            // Data Services
            services.AddSingleton<AppDataService>();
            services.AddSingleton<ICatalogDataService>(sp => sp.GetRequiredService<AppDataService>());
            services.AddSingleton<IClientDataService>(sp => sp.GetRequiredService<AppDataService>());
            services.AddSingleton<IDashBordDataService>(sp => sp.GetRequiredService<AppDataService>());
            services.AddSingleton<IDataLoader>(sp => sp.GetRequiredService<AppDataService>());
            services.AddSingleton<IStockDataService>(sp => sp.GetRequiredService<AppDataService>());
            services.AddSingleton<IReservationDataService>(sp => sp.GetRequiredService<AppDataService>());

        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var login = ServiceProvider!.GetRequiredService<LoginView>();
            login.Show();
        }
        protected override void OnExit(ExitEventArgs e)
        {
            Log.CloseAndFlush();
            (ServiceProvider as IDisposable)?.Dispose();
            base.OnExit(e);
        }
    }
}

