using System.Collections.Generic;
using CrownTowersClub.Models;

namespace CrownTowersClub.ViewModels
{
    public sealed class EventIndexViewModel
    {
        public IReadOnlyList<Event> Events { get; set; }

        public string SearchTerm { get; set; }
    }
}