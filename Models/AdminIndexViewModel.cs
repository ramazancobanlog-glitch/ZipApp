using System.Collections.Generic;

namespace login.Models
{
    public class AdminIndexViewModel
    {
        public IEnumerable<AdminCartViewModel> Pending { get; set; } = Enumerable.Empty<AdminCartViewModel>();
        public IEnumerable<AdminCartViewModel> Confirmed { get; set; } = Enumerable.Empty<AdminCartViewModel>();
    }
}
