using System.Collections.Generic;
using CrownTowersClub.Models;
using CrownTowersClub.ViewModels;

namespace CrownTowersClub.ViewModels
{
    public sealed class PaymentDashboardViewModel
    {
        public int SelectedYear { get; set; }

        // Stats
        public decimal TotalCollected { get; set; }
        public decimal TotalOutstanding { get; set; }
        public int PendingCount { get; set; }

        // Data
        public IReadOnlyList<PendingPaymentViewModel> PendingPayments { get; set; }
        public IReadOnlyList<Payment> PaymentHistory { get; set; }

        // State
        public string CurrentTab { get; set; }

        // Renewal properties
        public int NextYear { get; set; }
        public decimal DefaultRenewalFee { get; set; }
    }
}