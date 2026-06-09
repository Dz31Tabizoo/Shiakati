

namespace Shiakati.Models
{
    public class CreateReservationRequest
    {
        public int ClientId { get; set; }
        public decimal DepositAmount { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string? Notes { get; set; }
        public List<ReservationItemDto> Items { get; set; } = new();
    }
}
