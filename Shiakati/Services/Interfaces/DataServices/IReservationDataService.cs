using Shiakati.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Services.Interfaces.DataServices
{
    public interface IReservationDataService
    {
         ObservableCollection<ReservationDto> Reservations { get; }

        Task LoadReservationsAsync(string? status = null, bool forceRefresh = false);

        Task<int?> CreateReservationAsync(CreateReservationRequest request);
        Task<bool> CancelReservationAsync(int id);
        Task<bool> FulfillReservationAsync(int id, decimal amountPaid);

        event Action? DataChanged;
    }
}
