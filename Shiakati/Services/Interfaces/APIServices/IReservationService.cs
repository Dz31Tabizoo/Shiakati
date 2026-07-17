using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shiakati.Models;

namespace Shiakati.Services.Interfaces.APIServices
{
    public interface IReservationService

    {
        Task<List<ReservationDto>> GetReservationsAsync(string? status = null);
        Task<int?> CreateReservationAsync(CreateReservationRequest request);
        Task<bool> CancelReservationAsync(int id);
        Task<bool> FulfillReservationAsync(int id, decimal amountPaid);
    }
}
