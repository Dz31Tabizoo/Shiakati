using Serilog;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Shiakati.ViewModels;
using Shiakati.Services.Interfaces;
using Shiakati.Services.Implementations;
using Shiakati.Views;
using Microsoft.Extensions.Http;
using Shiakati.Helpers;
using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace Shiakati
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider? ServiceProvider { get; private set; }



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
            var baseUrl = configuration["ApiBaseUrl"];

            //DI configuration
            var services = new ServiceCollection();
            ConfigureServices(services, baseUrl);
            ServiceProvider = services.BuildServiceProvider();


        }


        private void ConfigureServices(ServiceCollection services, string baseUrl)
        {
            //Logger
            services.AddLogging(configure => configure.AddSerilog());


            // 1. Register the Handler as Transient (Required by HttpClientFactory)
            services.AddTransient<AuthenticationHandler>();



            // 2. Register AuthService as a Singleton (Crucial for holding CurrentUser state)
            // We use AddHttpClient which automatically injects an HttpClient into AuthService.
            // Notice we do NOT attach the AuthenticationHandler here.
            services.AddHttpClient<IAuthenticationClientService, AuthService>(client =>
            {
                client.BaseAddress = new Uri(baseUrl);
            });



            // 3. Create a Named Client for all other services that require Authentication
            // This is the client that intercepts requests and adds the Bearer token.
            services.AddHttpClient("AuthenticatedClient", client =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddHttpMessageHandler<AuthenticationHandler>(); // Attach the interceptor!

            // C'EST ICI QUE VOUS AJOUTEREZ VOS FUTURS SERVICES
            // ---------------------------------------------------------

            // Par exemple, quand vous créerez votre ProductService pour gérer le stock, 
            // vous l'ajouterez comme ceci :
            /*
            services.AddTransient<IProductService>(provider =>
            {
                var factory = provider.GetRequiredService<IHttpClientFactory>();
                var client = factory.CreateClient("AuthenticatedClient"); // On utilise le profil sécurisé
                return new ProductService(client);
            });
            */


            // Register your services and view models here
            services.AddSingleton<MainView>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<PosContainerViewModel>();
            services.AddTransient<POSViewModel>();
            services.AddTransient<LoginView>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<StockViewModel>();
            services.AddTransient<StockView>();
            services.AddTransient<SalesHistoryViewModel>();
            services.AddTransient<SalesHistoryView>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<SettingsView>();

            services.AddSingleton<IPrintService, PrintService>();
            services.AddTransient<IBarCodePrintService, BarcodePrintService>();

            // Example: services.AddTransient<IMyService, MyService>();

            //Cache service registration
            services.AddSingleton<ICacheService, AppCacheService>();
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
