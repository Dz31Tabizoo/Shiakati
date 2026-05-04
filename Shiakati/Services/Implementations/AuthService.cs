using Shiakati.Models;
using Shiakati.Services.Interfaces;
using System.Net.Http;
using System.Net.Http.Json;     
using Microsoft.Extensions.Logging;

namespace Shiakati.Services.Implementations
{
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
                    if (result != null && result.Success)
                    {
                        CurrentSession = new AuthSession { UserId = result.UserID, UserName = result.UserName, Token = result.Token };
                        _logger.LogInformation("Login successful for user: {Username}", username);
                    }
                    OnAuthenticationStateChanged?.Invoke();

                    return result ?? new LoginResponseModel { Success = false, Message = "Invalid response from server." };
                }

                _logger.LogWarning("Login failed for user: {Username}. Status Code: {StatusCode}", username, response.StatusCode);
                return new LoginResponseModel { Success = false, Message = "Login failed. Please check your credentials." };
                //log positive and negative outcomes for debugging and monitoring
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login attempt failed for user: {Username}", username);
                return new LoginResponseModel { Success = false, Message = $"An error occurred: {ex.Message}" };

            }
        }

        public void Logout()
        {
            CurrentSession = null;
            OnAuthenticationStateChanged?.Invoke();
            _logger.LogInformation("User {Username} logged out.", CurrentSession?.UserName);
        }
    }
}
