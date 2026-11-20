using CrownTowersClub.Models;
using CrownTowersClub.ViewModels;
using System.Collections.Generic;

namespace CrownTowersClub.Repositories
{
    /// <summary>
    /// Defines the data access operations for Members, Memberships, and Terms.
    /// </summary>
    public interface IMemberRepository
    {
        /* ============================================================
           1. Read Actions (Fetching Data)
           ============================================================ */

        /// <summary>
        /// Gets a single member by their unique ID.
        /// </summary>
        Member GetById(int id);

        Member GetByMembershipNumber(string membershipNumber);

        /// <summary>
        /// Gets all applications (history) for a specific NRIC, ordered by newest first.
        /// Used in "My Applications".
        /// </summary>
        IReadOnlyList<Member> GetByNric(string nric);

        /// <summary>
        /// Gets a specific Draft or Returned application by NRIC so the user can resume it.
        /// </summary>
        Member GetDraftByNric(string nric);

        /// <summary>
        /// Checks if an NRIC is already registered (excluding Drafts/Returned applications).
        /// </summary>
        bool ExistsByNric(string nric, int? excludingId = null);

        /// <summary>
        /// Gets the term history (2025, 2026, 2027) for a specific Membership.
        /// </summary>
        IReadOnlyList<MembershipTerm> GetTermsByMembershipId(int membershipId);

        /// <summary>
        /// Gets all payment receipts for a specific Member.
        /// </summary>
        IReadOnlyList<Payment> GetPaymentsByMemberId(int memberId);


        /* ============================================================
            2. Write Actions (Creating & Updating)
            ============================================================ */
        
        /// <summary>
        /// Inserts a brand new application with the specified status (e.g., Pending or Verified).
        /// </summary>
        int Insert(Member member);

        /// <summary>
        /// Inserts a new application explicitly as a "Draft".
        /// </summary>
        int InsertDraft(Member member);

        /// <summary>
        /// Updates an existing "Draft" or "Returned for Correction" application.
        /// </summary>
        bool UpdateDraft(Member member);

        /// <summary>
        /// Updates an existing member's details (used by Committee for Editing).
        /// Does not change the ApplicationStatus.
        /// </summary>
        bool Update(Member member);

        /// <summary>
        /// Transitions an existing Draft/Returned application to a new status (e.g., Pending or Verified).
        /// </summary>
        bool SubmitDraftWithStatus(Member member, string targetStatus);

        /// <summary>
        /// Permanently deletes a member record.
        /// </summary>
        bool Delete(int id);


        /* ============================================================
           3. Department Head Actions
           ============================================================ */

        /// <summary>
        /// Marks an application as Verified (moves to Committee queue).
        /// </summary>
        bool VerifyByDeptHead(int memberId, string expectedDepartment, string changedBy);

        /// <summary>
        /// Marks an application as Rejected by Dept Head.
        /// </summary>
        bool RejectByDeptHead(int memberId, string expectedDepartment, string reason, string changedBy);

        /// <summary>
        /// Returns an application to the user for correction (sets status to Returned for Correction).
        /// </summary>
        bool ReturnByDeptHead(int memberId, string expectedDepartment, string reason, string changedBy);

        int GetPendingByHodCount(string hodNric);

        ApplicationListViewModel GetPendingByHodPaged(
            string hodNric,
            int page,
            int pageSize);

        /* ============================================================
           4. Committee Actions
           ============================================================ */

        /// <summary>
        /// Approves the application, generates a Membership Number, creates a Membership,
        /// and creates the first year's MembershipTerm. Returns the new Membership Number.
        /// </summary>
        string ApproveByCommittee(int memberId, string changedBy);

        /// <summary>
        /// Marks an application as Rejected by Committee.
        /// </summary>
        bool RejectByCommittee(int memberId, string reason, string changedBy);

        bool CancelMembership(int memberId, string reason, string changedBy);

        ApplicationListViewModel GetVerifiedPaged(int page, int pageSize);

        int GetVerifiedCount();

        /* ============================================================
           5. Payments & Renewals
           ============================================================ */

        /// <summary>
        /// Gets the data for the Payment Dashboard (Stats, Pending List, or History).
        /// </summary>
        PaymentDashboardViewModel GetPaymentDashboard(string tab, int year);

        /// <summary>
        /// Gets all members who have a 'Pending Payment' term for the selected year.
        /// </summary>
        IReadOnlyList<PendingPaymentViewModel> GetPendingPayments(int year);

        /// <summary>
        /// Records a payment, generates a Receipt Number, and activates the Term/Membership.
        /// Returns the Receipt Number.
        /// </summary>
        string RecordPayment(int membershipTermId, string paymentMethod, string remarks, decimal amount);

        /// <summary>
        /// Generates new MembershipTerms (e.g., for 2027) for all Active/Suspended members.
        /// Returns the number of terms created.
        /// </summary>
        int GenerateRenewals(int year, decimal feeAmount);

        /// <summary>
        /// Finds all Pending Payments that are past their DueDate, marks them as Expired,
        /// and Suspends the associated Membership.
        /// </summary>
        int ProcessOverdueTerms();


        /* ============================================================
           6. Directory (Search & Pagination)
           ============================================================ */

        /// <summary>
        /// Gets a paginated, filtered list of members for the "All Applications" page.
        /// </summary>
        DirectoryViewModel GetDirectory(string searchTerm, string tabFilter, int page, int pageSize, int year);
    }
}