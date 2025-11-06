using System.Collections.Generic;

namespace login.Models
{
    public class AdminCartViewModel
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public CartStatus Status { get; set; }
        public List<AdminCartItemViewModel> Items { get; set; } = new List<AdminCartItemViewModel>();
        public decimal Total => Items.Sum(i => i.LineTotal);
    }
}
