using Shiakati.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Helpers
{
    public class AuthenticationHandler : DelegatingHandler
    {
        private readonly IAuthenticationClientService _authService;

        public AuthenticationHandler(IAuthenticationClientService authService)
        {
            _authService = authService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            var token = _authService.CurrentSession?.Token;
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
