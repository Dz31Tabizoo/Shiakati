using Serilog;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Shiakati.ViewModels;
using Shiakati.Views;

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

            // Register your services and view models here
            services.AddSingleton<MainView>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<PosContainerViewModel>();
            services.AddTransient<POSViewModel>();

            // Example: services.AddTransient<IMyService, MyService>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var login = new LoginView
            {
                // DataContext = ServiceProvider.GetRequiredService<LoginViewModel>()
            };

            login.Show();
        }
    }
}
