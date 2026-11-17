using System;

namespace CrownTowersClub.ViewModels
{
    public sealed class MembershipCardViewModel
    {
        public string Name { get; set; }
        public string NRIC { get; set; }
        public string MembershipNumber { get; set; }
        public DateTime? AdmissionDate { get; set; }
        public string Department { get; set; }
        public string CompanyName { get; set; }
        public int ValidYear { get; set; }
    }
}