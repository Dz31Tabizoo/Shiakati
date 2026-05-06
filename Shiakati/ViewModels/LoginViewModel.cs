using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Shiakati.Services.Interfaces;
using System.Windows.Controls;
using Shiakati.Views;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace Shiakati.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IAuthenticationClientService _authService;
        private readonly ILogger _logger = Log.ForContext<LoginViewModel>();

        [ObservableProperty]
        private string _username = string.Empty;
        [ObservableProperty]
        private string _errorMessage = string.Empty;
        [ObservableProperty]
        private bool _isLoading;
        [ObservableProperty]
        private bool _hasError;

        public LoginViewModel(IAuthenticationClientService authService)
        {
            _authService = authService;
        }
        [RelayCommand]
        private async Task LoginAsync(Object parameter)
        {
            var passwordBox = parameter as PasswordBox;
            var password = passwordBox?.Password ?? string.Empty;

            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(password))
            {
                ErrorMessage = "Veuillez entrer un nom d'utilisateur et un mot de passe.";
                HasError = true;
                return;
            }
            IsLoading = true;
            HasError = false;

            var result = await _authService.LoginAsync(Username, password);

            if (result.Success)
            {
                var mainView = App.ServiceProvider.GetRequiredService<MainView>();
                mainView.Show();

                if (passwordBox != null)
                {
                    Window.GetWindow(passwordBox)?.Close();
                }


            }
            else
            {
                _logger.Error(result.Message ?? "Login failed for user {Username}", Username);
                ErrorMessage = "Échec de la connexion. Veuillez vérifier vos informations ou Contacter l'administrateur.";
                HasError = true;
            }
            IsLoading = false;
        }

        [RelayCommand]
        private void CloseApplication()
        {
            Application.Current.Shutdown();
        }
    }
}
