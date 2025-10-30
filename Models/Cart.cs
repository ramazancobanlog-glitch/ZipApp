using System.Collections.Generic;

namespace login.Models
{
    public enum CartStatus
    {
        Draft = 0,
        AwaitingApproval = 1,
        Confirmed = 2
    }

    public class Cart
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public CartStatus Status { get; set; } = CartStatus.Draft;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<CartItem>? Items { get; set; }
    }
}
