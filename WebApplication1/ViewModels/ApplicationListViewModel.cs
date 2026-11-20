using System.Collections.Generic;
using CrownTowersClub.Models;

namespace CrownTowersClub.ViewModels
{
    public sealed class ApplicationListViewModel
    {
        public IReadOnlyList<Member> Members { get; set; }

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }
        public int PageSize { get; set; }
    }
}