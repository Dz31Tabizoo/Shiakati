using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

/// <summary>
/// Logique d'interaction pour LoginView.xaml
/// </summary>
namespace Shiakati.Views
{
    public partial class LoginView : Window
    {
        public event Action? LoginSucceeded;
        private SplashLoadingWindow? _splashWindow;

        public LoginView(LoginViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;

            viewModel.RequestClose += () =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    this.Close();
                });
            };

            // Forward the login success from ViewModel to the event
            viewModel.LoginCompleted += () =>
            {
                LoginSucceeded?.Invoke();
            };

            viewModel.LoadingStateChanged += OnLoadingStateChanged;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void OnLoadingStateChanged(bool isLoading)
        {
            // Use Dispatcher to ensure UI changes happen on the UI Thread
            Dispatcher.Invoke(() =>
            {
                if (isLoading)
                {
                    // Show Splash
                    _splashWindow = new SplashLoadingWindow((LoginViewModel)this.DataContext);
                    _splashWindow.Owner = this; // Optional: keeps it on top of login
                    _splashWindow.Show();
                }
                else
                {
                    // Close Splash
                    _splashWindow?.Close();
                    _splashWindow = null;
                }
            });
        }


    }
}


