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

    public async Task<LoginResponseModel?> LoginAsync(string username, string password)
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
                           // or extract later from token if needed
                        UserName = result.Username,
                        Token = result.Token
                    };
                    _logger.LogInformation("Login successful for user: {Username}", username);
                    OnAuthenticationStateChanged?.Invoke();
                    return result;
                }
                _logger.LogWarning("Login succeeded but response invalid for user: {Username}", username);
                return null;
            }

            _logger.LogWarning("Login failed for user: {Username}. Status code: {StatusCode}", username, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login attempt failed for user: {Username}", username);
            return null;
        }
    }

    public void Logout()
    {
        CurrentSession = null;
        OnAuthenticationStateChanged?.Invoke();
    }
}