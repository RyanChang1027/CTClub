using System.Collections.Generic;
using CrownTowersClub.Models;

namespace CrownTowersClub.ViewModels
{
    public sealed class MemberDetailViewModel
    {
        public Member Member { get; set; }
        public IReadOnlyList<MembershipTerm> Terms { get; set; }
        public IReadOnlyList<Payment> Payments { get; set; }
    }
}