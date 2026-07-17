using Shiakati.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Services.Interfaces.APIServices
{
    public interface IAuthenticationClientService
    {
        bool IsLoggedIn { get; }
        AuthSession? CurrentSession { get; }
        event Action? OnAuthenticationStateChanged;

        Task<LoginResponseModel> LoginAsync(string username, string password);
        void Logout();

        Task<bool> ChangePasswordAsync(string oldPassword, string newPassword);
        Task<bool> ChangeUsernameAsync(string password, string newUsername);
        Task<bool> RegisterAsync(string username, string password, string role);

    }
}
