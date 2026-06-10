using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiakati.Models;
using Shiakati.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Shiakati.ViewModels
{
    public partial class ReservationsViewModel : ObservableObject
    {
        private readonly IReservationService _reservationService;

        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string? _statusFilter = "active"; // default: show active

        public ObservableCollection<ReservationDto> Reservations { get; } = new();

        public ReservationsViewModel(IReservationService reservationService)
        {
            _reservationService = reservationService;
            _ = LoadAsync();
        }

        [RelayCommand]
        private async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var list = await _reservationService.GetReservationsAsync(StatusFilter);
                Reservations.Clear();
                foreach (var r in list) Reservations.Add(r);
            }
            finally { IsLoading = false; }
        }

        [RelayCommand]
        private async Task CancelReservationAsync(ReservationDto reservation)
        {
            if (reservation == null || reservation.IsCancelled) return;
            if (MessageBox.Show("Annuler cette réservation ? Les articles seront remis en stock.",
                "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            var success = await _reservationService.CancelReservationAsync(reservation.ReservationId);
            if (success)
            {
                MessageBox.Show("Réservation annulée.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadAsync();
            }
        }

        [RelayCommand]
        private async Task FulfillReservationAsync(ReservationDto reservation)
        {
            if (reservation == null || reservation.IsFulfilled) return;
            if (MessageBox.Show("Honorer cette réservation ? Une vente sera créée.",
                "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            var success = await _reservationService.FulfillReservationAsync(reservation.ReservationId);
            if (success)
            {
                MessageBox.Show("Réservation honorée – vente créée.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadAsync();
            }
        }
    }
}
