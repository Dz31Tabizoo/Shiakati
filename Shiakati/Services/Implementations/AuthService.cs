using Microsoft.Extensions.Logging;
using Shiakati.Models;
using Shiakati.Services.Interfaces;
using System.Net.Http;
using System.Net.Http.Json;

public class AuthService : IAuthenticationClientService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthService> _logger;

    public AuthSession? CurrentSession { get; private set; }
    public bool IsLoggedIn => CurrentSession != null && !string.IsNullOrWhiteSpace(CurrentSession.Token);

    public event Action? OnAuthenticationStateChanged;

    public AuthService(HttpClient httpClient, ILogger<AuthService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<LoginResponseModel> LoginAsync(string username, string password)
    {
        _logger.LogInformation("Login attempt for user: {Username}", username);
        var loginData = new { Username = username, Password = password };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginData);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponseModel>();

                if (result != null && !string.IsNullOrWhiteSpace(result.Token))
                {
                    CurrentSession = new AuthSession
                    {
                        UserName = result.Username,
                        Token = result.Token
                    };

                    _logger.LogInformation("Login successful for user: {Username}", username);
                    OnAuthenticationStateChanged?.Invoke();

                    // On force le succès si ce n'est pas géré par le modèle de l'API
                    result.Success = true;
                    return result;
                }

                _logger.LogWarning("Login succeeded but token is missing for user: {Username}", username);
            }
            else
            {
                _logger.LogWarning("Login failed for user: {Username}. Status code: {StatusCode}", username, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login attempt failed (Exception) for user: {Username}", username);
        }

        // En cas d'échec, on retourne un objet propre au lieu de null
        return new LoginResponseModel
        {
            Success = false,
            Message = "Échec de la connexion. Veuillez vérifier vos informations ou contacter l'administrateur."
        };
    }

    public void Logout()
    {
        CurrentSession = null;
        OnAuthenticationStateChanged?.Invoke();
    }
}