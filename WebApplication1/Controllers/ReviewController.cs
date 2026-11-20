using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using CrownTowersClub.Domain;
//using CrownTowersClub.Infrastructure.Logging;
using CrownTowersClub.Infrastructure.Session;
using CrownTowersClub.Models;
using CrownTowersClub.Repositories;
using CrownTowersClub.ViewModels;

namespace CrownTowersClub.Controllers
{
    public sealed class ReviewController : Controller
    {
        private readonly IMemberRepository repository;

        public ReviewController(IMemberRepository repository)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(
                    "IMemberRepository was not registered.");
            }
            this.repository = repository;
        }

        private DevSessionUser CurrentUser => Session[DevSessionKeys.User] as DevSessionUser;

        /* ============================================================
           1. Read Actions (Queues, Detail, Directory, Payments)
           ============================================================ */

        [HttpGet]
        public ActionResult Pending(int page = 1)
        {
            if (!IsDepartmentHead())
                return RedirectToAction("Index", "Home");

            const int pageSize = 15;

            var viewModel = repository.GetPendingByHodPaged(
                CurrentUser.NRIC,
                page,
                pageSize);

            return View(viewModel);
        }

        [HttpGet]
        public ActionResult Verified()
        {
            if (!IsCommittee()) return RedirectToAction("Index", "Home");

            var rows = repository.GetVerifiedAll();
            return View(rows);
        }

        [HttpGet]
        public ActionResult Detail(int id)
        {
            var user = CurrentUser;
            if (user == null) return RedirectToAction("Index", "Home");

            Member member = repository.GetById(id);
            if (member == null) return View("~/Views/Shared/Forbidden.cshtml");

            /* ============================================================
              SECURITY CHECKS (IDOR Protection)
              ============================================================ */

            // 1. Staff can ONLY view their own applications
            if (user.Role == DevRoles.Staff && member.NRIC != user.NRIC)
            {
                return View("~/Views/Shared/Forbidden.cshtml");
            }

            // 2. Dept Heads can ONLY view their own department's applications
            if (user.Role == DevRoles.DepartmentHead &&
                !string.Equals(member.Department, user.Department, StringComparison.OrdinalIgnoreCase))
            {
                // Exception: Let them view their own application
                if (member.NRIC != user.NRIC)
                {
                    return View("~/Views/Shared/Forbidden.cshtml");
                }
            }

            // 3. Committee can view everything, so no extra check needed.

            /* ============================================================
               ACTION PERMISSIONS (Who sees the buttons)
               ============================================================ */

            var canDeptHeadAct =
                user.Role == DevRoles.DepartmentHead &&
                string.Equals(member.ApplicationStatus, ApplicationStatus.PendingForVerification, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(member.Department, user.Department, StringComparison.OrdinalIgnoreCase);

            var canCommitteeAct =
                user.Role == DevRoles.Committee &&
                (string.Equals(member.ApplicationStatus, ApplicationStatus.VerifiedByDeptHead, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(member.ApplicationStatus, ApplicationStatus.Approved, StringComparison.OrdinalIgnoreCase));

            ViewBag.ReadOnly = !canDeptHeadAct && !canCommitteeAct;
            ViewBag.CanDeptHeadAct = canDeptHeadAct;
            ViewBag.CanCommitteeAct = canCommitteeAct;

            // Fetch Membership History & Payments if they exist
            var terms = new List<MembershipTerm>();
            var payments = new List<Payment>();

            if (member.MembershipId.HasValue)
            {
                terms = repository.GetTermsByMembershipId(member.MembershipId.Value).ToList();
                payments = repository.GetPaymentsByMemberId(member.MemberId).ToList();
            }

            var viewModel = new MemberDetailViewModel
            {
                Member = member,
                Terms = terms,
                Payments = payments
            };

            return View(viewModel);
        }

        [HttpGet]
        public ActionResult Directory(string search, string tab = "All", int page = 1, int? year = null)
        {
            if (!IsCommittee()) return RedirectToAction("Index", "Home");

            int selectedYear = year ?? DateTime.Now.Year;
            int pageSize = 15;

            // Safety check for negative page numbers
            if (page < 1) page = 1;

            var viewModel = repository.GetDirectory(search, tab, page, pageSize, selectedYear);
            return View(viewModel);
        }

        [HttpGet]
        public ActionResult PaymentDashboard(string tab = "Pending", int? year = null)
        {
            if (!IsCommittee()) return RedirectToAction("Index", "Home");

            int selectedYear = year ?? DateTime.Now.Year;
            var viewModel = repository.GetPaymentDashboard(tab, selectedYear);

            viewModel.NextYear = DateTime.Now.Year + 1;
            viewModel.DefaultRenewalFee = 24.00m;

            return View(viewModel);
        }

        /* ============================================================
           2. Renewal & Expiry Actions (JSON)
           ============================================================ */

        [HttpPost]
        public JsonResult GenerateRenewals(int year, decimal feeAmount)
        {
            if (!IsCommittee()) return Fail("You are not allowed to perform this action.");
            if (year <= DateTime.Now.Year) return Fail("Renewal year must be in the future.");
            if (feeAmount <= 0) return Fail("Fee amount must be greater than zero.");

            try
            {
                int count = repository.GenerateRenewals(year, feeAmount);

                if (count == 0)
                {
                    return Ok($"No new renewals were generated. All eligible members already have a {year} term.");
                }

                return Ok($"Successfully generated {count} renewal terms for {year}.");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ReviewController.GenerateRenewals");
            }
        }

        [HttpPost]
        public JsonResult RunExpiryCheck()
        {
            if (!IsCommittee()) return Fail("You are not allowed to perform this action.");

            try
            {
                int count = repository.ProcessOverdueTerms();

                if (count == 0)
                {
                    return Ok("Check complete. No overdue payments were found.");
                }

                return Ok($"Check complete. {count} overdue terms were marked as Expired, and memberships were updated to Suspended.");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ReviewController.RunExpiryCheck");
            }
        }

        /* ============================================================
           3. Department Head Actions (JSON)
           ============================================================ */

        [HttpPost]
        public JsonResult Verify(int id)
        {
            if (!IsDepartmentHead()) return Fail("You are not allowed to perform this action.");

            try
            {
                bool updated = repository.VerifyByDeptHead(id, CurrentUser.Department ?? string.Empty, CurrentUser.Name);
                if (!updated) return Fail("This application is no longer pending.");

                return Ok("Application verified.");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ReviewController.Verify");
            }
        }

        [HttpPost]
        public JsonResult RejectByDeptHead(int id, string reason)
        {
            if (!IsDepartmentHead()) return Fail("You are not allowed to perform this action.");
            if (string.IsNullOrWhiteSpace(reason)) return Fail("Please enter a reason for rejection.");

            try
            {
                bool updated = repository.RejectByDeptHead(id, CurrentUser.Department ?? string.Empty, reason.Trim(), CurrentUser.Name);
                if (!updated) return Fail("This application is no longer pending.");

                return Ok("Application rejected.");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ReviewController.RejectByDeptHead");
            }
        }

        [HttpPost]
        public JsonResult ReturnByDeptHead(int id, string reason)
        {
            if (!IsDepartmentHead()) return Fail("You are not allowed to perform this action.");
            if (string.IsNullOrWhiteSpace(reason)) return Fail("Please enter a reason for returning the application.");

            try
            {
                bool updated = repository.ReturnByDeptHead(id, CurrentUser.Department ?? string.Empty, reason.Trim(), CurrentUser.Name);
                if (!updated) return Fail("This application is no longer pending.");

                return Ok("Application returned for correction.");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ReviewController.ReturnByDeptHead");
            }
        }

        /* ============================================================
           4. Committee Actions (JSON)
           ============================================================ */

        [HttpPost]
        public JsonResult Approve(int id)
        {
            if (!IsCommittee()) return Fail("You are not allowed to perform this action.");

            try
            {
                string membershipNumber = repository.ApproveByCommittee(id, CurrentUser.Name);

                if (string.IsNullOrEmpty(membershipNumber))
                {
                    return Fail("This application is no longer awaiting approval.");
                }

                return Ok($"Application approved! Membership Number: {membershipNumber}");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ReviewController.Approve");
            }
        }

        [HttpPost]
        public JsonResult RejectByCommittee(int id, string reason)
        {
            if (!IsCommittee()) return Fail("You are not allowed to perform this action.");
            if (string.IsNullOrWhiteSpace(reason)) return Fail("Please enter a reason for rejection.");

            try
            {
                bool updated = repository.RejectByCommittee(id, reason.Trim(), CurrentUser.Name);
                if (!updated) return Fail("This application is no longer awaiting approval.");

                return Ok("Application rejected.");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ReviewController.RejectByCommittee");
            }
        }

        [HttpPost]
        public JsonResult ProcessPayment(int membershipTermId, string paymentMethod, string remarks, decimal amount)
        {
            if (!IsCommittee()) return Fail("You are not allowed to perform this action.");
            if (string.IsNullOrWhiteSpace(paymentMethod)) return Fail("Payment method is required.");

            try
            {
                string receiptNumber = repository.RecordPayment(membershipTermId, paymentMethod.Trim(), remarks?.Trim(), amount);

                if (string.IsNullOrEmpty(receiptNumber))
                {
                    return Fail("This term is no longer pending payment.");
                }

                return Ok($"Payment recorded! Receipt Number: {receiptNumber}");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ReviewController.ProcessPayment");
            }
        }

        [HttpPost]
        public JsonResult CancelMembership(int id, string reason)
        {
            if (!IsCommittee()) return Fail("You are not allowed to perform this action.");
            if (string.IsNullOrWhiteSpace(reason)) return Fail("Please enter a reason for cancellation.");

            try
            {
                bool cancelled = repository.CancelMembership(id, reason.Trim(), CurrentUser.Name);

                if (!cancelled)
                {
                    return Fail("No active membership was found to cancel.");
                }

                return Ok("Membership successfully cancelled.");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ReviewController.CancelMembership");
            }
        }

        [HttpPost]
        public JsonResult DeleteRecord(int id)
        {
            if (!IsCommittee()) return Fail("You are not allowed to perform this action.");

            try
            {
                var member = repository.GetById(id);
                if (member == null) return Fail("Record not found.");

                // Safety Check: Only allow deleting Drafts or Rejected applications
                bool isDeletable = member.ApplicationStatus == ApplicationStatus.Draft ||
                                   member.ApplicationStatus.Contains("Rejected");

                if (!isDeletable)
                {
                    return Fail("You can only delete Draft or Rejected applications. Active members must be cancelled instead.");
                }

                bool deleted = repository.Delete(id);
                if (!deleted) return Fail("The record could not be deleted.");

                return Ok("Record deleted successfully.");
            }
            catch (Exception ex)
            {
                // If they somehow bypass the UI and try to delete a member with a Foreign Key (Payments/Memberships)
                if (ex.Message.Contains("FK_") || ex.Message.Contains("conflicted with the REFERENCE constraint"))
                {
                    return Fail("Cannot delete this record because they have an active membership or payment history.");
                }

                return HandleException(ex, "ReviewController.DeleteRecord");
            }
        }

        /* ============================================================
           5. Helpers
           ============================================================ */

        private bool IsDepartmentHead()
        {
            var user = CurrentUser;
            return user != null && user.Role == DevRoles.DepartmentHead;
        }

        private bool IsCommittee()
        {
            var user = CurrentUser;
            return user != null && user.Role == DevRoles.Committee;
        }

        private JsonResult Ok(string message)
        {
            return Json(ApiResponse<object>.Ok(null, message), JsonRequestBehavior.DenyGet);
        }

        private JsonResult Fail(string message)
        {
            return Json(ApiResponse<object>.Fail(message), JsonRequestBehavior.DenyGet);
        }

        private JsonResult HandleException(Exception ex, string context)
        {
            //FileLogger.LogException(ex, context);
            return Fail("An unexpected error occurred. Please try again or contact support.");
        }
    }
}