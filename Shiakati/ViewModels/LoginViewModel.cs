using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiakati;
using Shiakati.Helpers;
using Shiakati.Services.Interfaces.APIServices;
using Shiakati.Services.Interfaces.DataServices;
using Shiakati.Views;
using System.Windows;
using System.Windows.Controls;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthenticationClientService _authService;
    private readonly ILogger<LoginViewModel> _logger;
    private readonly IDataLoader _dataLoader;

    public event Action? LoginCompleted;
    public event Action<bool>? LoadingStateChanged;

    public string Version { get => AppVersion.GetVersion(); set { } }

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
    public LoginViewModel(IAuthenticationClientService authService,IDataLoader dataLoader, ILogger<LoginViewModel> logger)
    {
        _authService = authService;
        _logger = logger;
        _dataLoader = dataLoader;
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

        try
        {

            var result = await _authService.LoginAsync(Username, password);

            // Plus de risque de NullReferenceException ici
            if (result.Success)
            {


                await _dataLoader.LoadAllEssentialDataAsync();



                // Navigation vers la vue principale
                var mainView = App.ServiceProvider.GetRequiredService<MainView>();
                Application.Current.MainWindow = mainView;
                mainView.Show();

                // On demande la fermeture de la fenêtre de login proprement
                OnLoginSuccess();
            }
            else
            {
                _logger.LogWarning(result.Message ?? "Login failed for user {Username}", Username);
                ErrorMessage = result.Message ?? "Échec de la connexion.";
                HasError = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Une erreur système est survenue lors de la connexion.");
            ErrorMessage = "Une erreur critique est survenue. Veuillez réessayer.";
            HasError = true;
        }
        finally
        {
            IsLoading = false;
        }

        IsLoading = false;
    }

    partial void OnIsLoadingChanged(bool value)
    {
        LoadingStateChanged?.Invoke(value);
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