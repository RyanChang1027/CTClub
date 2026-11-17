using System.Collections.Generic;
using CrownTowersClub.Models;

namespace CrownTowersClub.ViewModels
{
    public sealed class DirectoryViewModel
    {
        public int SelectedYear { get; set; }

        public IReadOnlyList<Member> Members { get; set; }

        // NEW: Holds the latest payment for each member (Key = MemberId)
        public Dictionary<int, Payment> LatestPayments { get; set; }

        public string SearchTerm { get; set; }
        public string CurrentTab { get; set; }

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }

        // Tab counts
        public int CountAll { get; set; }
        public int CountPending { get; set; }
        public int CountApproved { get; set; }
        public int CountRejected { get; set; }
    }
}