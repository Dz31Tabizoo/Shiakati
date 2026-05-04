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
            //loger configuration
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File("logs/shiakati_log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)             
                .CreateLogger();


            //DI configuration
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();

        }


        private void ConfigureServices(ServiceCollection services)
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
                client.BaseAddress = new Uri("https://localhost:5001/");                
            });

            // 3. Create a Named Client for all other services that require Authentication
            // This is the client that intercepts requests and adds the Bearer token.
            services.AddHttpClient("AuthenticatedClient", client =>
            {
                client.BaseAddress = new Uri("https://localhost:5001/");
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
            services.AddTransient<StockViewModel>();
            services.AddTransient<StockView>();
            services.AddTransient<SalesHistoryViewModel>();
            services.AddTransient<SalesHistoryView>();

            services.AddSingleton<IPrintService, PrintService>();   

            // Example: services.AddTransient<IMyService, MyService>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var login = ServiceProvider!.GetRequiredService<LoginView>();
            login.Show();
        }
    }
}
