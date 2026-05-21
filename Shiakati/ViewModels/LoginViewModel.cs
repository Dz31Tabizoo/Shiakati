using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiakati;
using Shiakati.Services.Interfaces;
using Shiakati.Views;
using System.Windows;
using System.Windows.Controls;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthenticationClientService _authService;
    private readonly ILogger<LoginViewModel> _logger;

    public event Action? LoginCompleted;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasError;

    // Événement pour informer la vue (Code-Behind) qu'elle doit se fermer
    public event Action? RequestClose;

    // Injection cohérente du ILogger
    public LoginViewModel(IAuthenticationClientService authService, ILogger<LoginViewModel> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [RelayCommand]
    private async Task LoginAsync(object parameter)
    {
        var passwordBox = parameter as PasswordBox;
        var password = passwordBox?.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Veuillez entrer un nom d'utilisateur et un mot de passe.";
            HasError = true;
            return;
        }

        IsLoading = true;
        HasError = false;

        var result = await _authService.LoginAsync(Username, password);

        // Plus de risque de NullReferenceException ici
        if (result.Success)
        {
            // Navigation vers la vue principale
            var mainView = App.ServiceProvider.GetRequiredService<MainView>();
            mainView.Show();

            // On demande la fermeture de la fenêtre de login proprement
            RequestClose?.Invoke();
        }
        else
        {
            _logger.LogWarning(result.Message ?? "Login failed for user {Username}", Username);
            ErrorMessage = result.Message ?? "Échec de la connexion.";
            HasError = true;
        }

        IsLoading = false;
    }

    private void OnLoginSuccess()
    {
        LoginCompleted?.Invoke();
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void CloseApplication()
    {
        Application.Current.Shutdown();
    }
}