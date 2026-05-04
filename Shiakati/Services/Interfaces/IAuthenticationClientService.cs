using Shiakati.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Services.Interfaces
{
    public interface IAuthenticationClientService
    {
        bool IsLoggedIn { get; }
        AuthSession? CurrentSession { get; }
        event Action? OnAuthenticationStateChanged;

        Task<LoginResponseModel> LoginAsync(string username, string password);
        void Logout();
    }
}
