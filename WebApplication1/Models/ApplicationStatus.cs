namespace CrownTowersClub.Domain
{
    /// <summary>
    /// Canonical Application Status values for the Members table.
    /// Use these constants instead of raw string literals.
    /// </summary>
    public static class ApplicationStatus
    {
        /* ============================================================
           1. Application Lifecycle (The Joining Process)
           ============================================================ */

        /// <summary>
        /// The application is saved but incomplete. The user can resume and edit this.
        /// </summary>
        public const string Draft = "Draft";

        /// <summary>
        /// The application has been submitted and is waiting for the Department Head.
        /// </summary>
        public const string PendingForVerification = "Pending for Verification";

        /// <summary>
        /// The Department Head has approved it, and it is waiting for the Committee.
        /// </summary>
        public const string VerifiedByDeptHead = "Verified";

        /// <summary>
        /// The application has been returned to the user for correction.
        /// The user must edit and resubmit. (Acts like a Draft).
        /// </summary>
        public const string ReturnedForCorrection = "Returned for Correction";

        /// <summary>
        /// The Committee has approved the application.
        /// The user is now a member. 
        /// </summary>
        public const string Approved = "Approved";

        /// <summary>
        /// The Department Head rejected the application.
        /// </summary>
        public const string RejectedByDeptHead = "Rejected by Dept Head";

        /// <summary>
        /// The Committee rejected the application.
        /// </summary>
        public const string RejectedByCommittee = "Rejected by Committee";
    }

    /// <summary>
    /// Canonical Membership Status values for the Memberships table.
    /// </summary>
    public static class MembershipStatus
    {
        /// <summary>
        /// The application is approved, but the initial fee is not yet paid.
        /// </summary>
        public const string PendingActivation = "Pending Activation";

        /// <summary>
        /// The member has a valid, active term for the current year.
        /// </summary>
        public const string Active = "Active";

        /// <summary>
        /// The membership is temporarily inactive because renewal/payment is overdue.
        /// </summary>
        public const string Suspended = "Suspended";

        /// <summary>
        /// The membership has been permanently terminated.
        /// </summary>
        public const string Cancelled = "Cancelled";
    }

    /// <summary>
    /// Canonical Term Status values for the MembershipTerms table.
    /// </summary>
    public static class TermStatus
    {
        /// <summary>
        /// The annual fee has not been fully paid.
        /// </summary>
        public const string PendingPayment = "Pending Payment";

        /// <summary>
        /// The annual fee has been fully paid and the term is valid.
        /// </summary>
        public const string Active = "Active";

        /// <summary>
        /// The term has ended without successful renewal.
        /// </summary>
        public const string Expired = "Expired";
    }
}