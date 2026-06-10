using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shiakati.Models;

namespace Shiakati.Services.Interfaces
{
    public interface IReservationService

    {
        Task<bool> CreateReservationAsync(CreateReservationRequest request);

        Task<List<ReservationDto>> GetReservationsAsync(string? status = null);
        Task<bool> CancelReservationAsync(int id);
        Task<bool> FulfillReservationAsync(int id);
    }
}
