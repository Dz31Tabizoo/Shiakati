using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiakati.Models;
using Shiakati.Properties;
using Shiakati.Services.Interfaces;
using Shiakati.Views;
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
        private readonly IPrintService _printService;

        private List<ReservationDto> _allReservations = new();

        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string? _statusFilter = "active";   // current selected filter

        public ObservableCollection<ReservationDto> Reservations { get; } = new();

        public ReservationsViewModel(IReservationService reservationService,IPrintService print)
        {
            _printService = print;
            _reservationService = reservationService;
            _ = LoadAllAsync();   // fetch everything once at startup
        }

        private bool PrintTicket(ReceipModel receipt)
        {
            try
            {
                string printerToUse = Settings.Default.TicketPrinterName;
                _printService.PrintReceipt(receipt, printerToUse);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'impression du ticket : {ex.Message}", "Erreur Imprimante", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        [RelayCommand]
        private async Task LoadAllAsync()
        {
            IsLoading = true;
            try
            {
                _allReservations = await _reservationService.GetReservationsAsync(null);  // null = no filter
            }
            finally
            {
                IsLoading = false;
                ApplyFilter();   // show only "active" by default
            }
        }

        [RelayCommand]
        private void FilterByStatus(string? status)
        {
            StatusFilter = status;
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var filtered = _allReservations.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(StatusFilter))
            {
                switch (StatusFilter.ToLower())
                {
                    case "active":
                        filtered = filtered.Where(r => r.IsActive);
                        break;
                    case "fulfilled":
                        filtered = filtered.Where(r => r.IsFulfilled);
                        break;
                    case "cancelled":
                        filtered = filtered.Where(r => r.IsCancelled);
                        break;
                        // "all" or empty → no filter
                }
            }

            Reservations.Clear();
            foreach (var r in filtered)
                Reservations.Add(r);
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
                await LoadAllAsync();
            }
        }

        [RelayCommand]
        private async Task FulfillReservationAsync(ReservationDto reservation)
        {
            if (reservation == null || reservation.IsFulfilled) return;

            decimal remaining = reservation.TotalAmount - reservation.DepositAmount;
            var dialog = new HonorReservationDialog(reservation.DepositAmount, remaining);
            if (dialog.ShowDialog() == true)
            {
                var success = await _reservationService.FulfillReservationAsync(reservation.ReservationId, dialog.AmountPaid);
                if (success)
                {
                    // Print honor ticket
                    var receipt = new ReceipModel
                    {
                        TicketNumber = "RES-V-" + reservation.ReservationId,
                        Date = DateTime.Now,
                        TotalAmount = reservation.TotalAmount,
                        PaidAmount = dialog.AmountPaid,
                        RemainingDebt = dialog.NewDebt,      // NewDebt is calculated by the dialog
                        ClientName = reservation.ClientFullName,
                        DocumentType = "HONOR",
                        Items = reservation.Items.Select(c => new ReceiptItem
                        {
                            Designation = c.DisplayName,
                            Quantity = c.Quantity,
                            UnitPrice = c.UnitPrice,                           

                        }).ToList(),
                        TotalDiscount = reservation.Items.Sum(c=> c.TotalDiscount)?? 0 
                    };
                    PrintTicket(receipt);

                    MessageBox.Show("Réservation Validée.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadAllAsync();
                }
                else
                {
                    MessageBox.Show("Erreur lors de l'opération.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

    }
}
