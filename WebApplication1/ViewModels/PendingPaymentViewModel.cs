namespace CrownTowersClub.ViewModels
{
    public sealed class PendingPaymentViewModel
    {
        public int MemberId { get; set; }
        public int MembershipTermId { get; set; }
        public string MembershipNumber { get; set; }
        public string Name { get; set; }
        public string NRIC { get; set; }
        public string Department { get; set; }
        public short MembershipYear { get; set; }
        public decimal FeeAmount { get; set; }
        public string TermStatus { get; set; }
        public string MembershipStatus { get; set; }
    }
}