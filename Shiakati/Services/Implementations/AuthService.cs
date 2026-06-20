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
                        Token = result.Token,
                        Role = result.Role,
                        UserId = result.UserID
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


    public async Task<bool> ChangePasswordAsync(string oldPassword, string newPassword)
    {
        SetAuthHeader();
        var response = await _httpClient.PutAsJsonAsync("api/auth/change-password", new
        {
            oldPassword,
            newPassword
        });
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ChangeUsernameAsync(string password, string newUsername)
    {
        SetAuthHeader();
        var response = await _httpClient.PutAsJsonAsync("api/auth/change-username", new
        {
            password,
            newUsername
        });
        return response.IsSuccessStatusCode;
    }

    private void SetAuthHeader()
    {
        if (CurrentSession != null && !string.IsNullOrWhiteSpace(CurrentSession.Token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentSession.Token);
        }
    }

}