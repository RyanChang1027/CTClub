using System;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using CrownTowersClub.Domain;
//using CrownTowersClub.Infrastructure.Logging;
using CrownTowersClub.Infrastructure.Session;
using CrownTowersClub.Mappers;
using CrownTowersClub.Models;
using CrownTowersClub.Repositories;
using CrownTowersClub.ViewModels;

namespace CrownTowersClub.Controllers
{
    public sealed class MemberController : Controller
    {
        private const string DraftStatus = ApplicationStatus.Draft;
        private const string SubmittedStatus = ApplicationStatus.PendingForVerification;

        private readonly IMemberRepository repository;
        private readonly ICorporateEmployeeRepository corporateRepository;

        public MemberController(
            IMemberRepository repository,
            ICorporateEmployeeRepository corporateRepository)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(
                    "IMemberRepository was not registered.");
            }
            this.repository = repository;

            if (corporateRepository == null)
            {
                throw new ArgumentNullException(
                    "ICorporateEmployeeRepository was not registered.");
            }
            this.corporateRepository = corporateRepository;
        }

        private DevSessionUser CurrentUser => Session[DevSessionKeys.User] as DevSessionUser;

        /* ============================================================
           1. Read Actions (Pages & JSON)
           ============================================================ */

        [HttpGet]
        public ActionResult Index(string NRIC, bool manual = false)
        {
            var user = CurrentUser;
            ViewBag.UserRole = user?.Role;

            // If manual entry, clear any pre-filled data and force manual mode
            if (manual)
            {
                ViewBag.IsManualEntry = true;
                ViewBag.InitialNric = string.Empty;
                ViewBag.InitialMemberJson = null;
                return View();
            }

            ViewBag.IsManualEntry = false;

            // SECURITY: Force the NRIC to be the logged-in user's NRIC.
            var forcedNric = user != null ? user.NRIC : NormalizeNric(NRIC);
            ViewBag.InitialNric = forcedNric ?? string.Empty;

            // If arriving via Resume, load the editable Draft/Returned row
            Member initialMember = null;
            if (!string.IsNullOrWhiteSpace(forcedNric))
            {
                initialMember = repository.GetDraftByNric(forcedNric);
            }

            ViewBag.InitialMemberJson = initialMember == null
                ? null
                : Newtonsoft.Json.JsonConvert.SerializeObject(initialMember);

            return View();
        }

        [HttpGet]
        public ActionResult Mine()
        {
            if (CurrentUser == null) return RedirectToAction("Index", "Home");

            var rows = repository.GetByNric(CurrentUser.NRIC);
            return View(rows);
        }

        [HttpGet]
        public ActionResult Scan()
        {
            var user = CurrentUser;

            if (user == null)
                return RedirectToAction("Index", "Home");

            if (user.Role != DevRoles.Staff &&
                user.Role != DevRoles.DepartmentHead &&
                user.Role != DevRoles.Committee)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.UserRole = user.Role;

            return View();
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            var user = CurrentUser;

            // Only Committee can edit existing members
            if (user == null || user.Role != DevRoles.Committee)
                return RedirectToAction("Index", "Home");

            var member = repository.GetById(id);
            if (member == null) return HttpNotFound();

            ViewBag.IsManualEntry = false;
            ViewBag.InitialNric = member.NRIC;
            ViewBag.InitialMemberJson = Newtonsoft.Json.JsonConvert.SerializeObject(member);
            ViewBag.IsEditMode = true;

            return View("Index");
        }

        [HttpGet]
        public JsonResult GetStaffDetails(string NRIC)
        {
            var user = CurrentUser;
            if (user == null) return Json(ApiResponse<object>.Fail("Session expired."), JsonRequestBehavior.AllowGet);

            string normalizedNric = NormalizeNric(NRIC);

            // SECURITY CHECK: Ensure the NRIC they are requesting matches their own session NRIC
            if (user.Role != DevRoles.Committee && user.NRIC != normalizedNric)
            {
                return Json(ApiResponse<object>.Fail("You can only retrieve your own staff details."), JsonRequestBehavior.AllowGet);
            }

            CorporateEmployee employee = corporateRepository.GetByNric(normalizedNric);
            if (employee == null)
            {
                return Json(ApiResponse<object>.Fail("No staff record was found for this NRIC."), JsonRequestBehavior.AllowGet);
            }

            return Json(ApiResponse<CorporateEmployee>.Ok(employee, "Staff details retrieved."), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult Get(int id)
        {
            if (id <= 0) return Json(ApiResponse<object>.Fail("A valid member ID is required."), JsonRequestBehavior.AllowGet);

            Member member = repository.GetById(id);
            if (member == null) return Json(ApiResponse<object>.Fail("Member was not found."), JsonRequestBehavior.AllowGet);

            return Json(ApiResponse<Member>.Ok(member), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult LookupByMembershipNumber(string membershipNumber)
        {
            var user = CurrentUser;

            // Check whether the user is logged in and has permission to scan.
            if (user == null)
            {
                return Json(
                    ApiResponse<object>.Fail("Your session has expired. Please log in again."),
                    JsonRequestBehavior.AllowGet);
            }

            if (user.Role != DevRoles.Staff &&
                user.Role != DevRoles.DepartmentHead &&
                user.Role != DevRoles.Committee)
            {
                return Json(
                    ApiResponse<object>.Fail("You are not authorized to scan membership cards."),
                    JsonRequestBehavior.AllowGet);
            }

            // Validate the membership number received from the scanner.
            if (string.IsNullOrWhiteSpace(membershipNumber))
            {
                return Json(
                    ApiResponse<object>.Fail("A membership number is required."),
                    JsonRequestBehavior.AllowGet);
            }

            try
            {
                membershipNumber = membershipNumber.Trim();

                // Find the member using the repository.
                Member member = repository.GetByMembershipNumber(membershipNumber);

                if (member == null)
                {
                    return Json(
                        ApiResponse<object>.Fail("No membership record was found."),
                        JsonRequestBehavior.AllowGet);
                }

                // Return only the information required by the scanner page.
                var result = new
                {
                    Name = member.Name,
                    MembershipNumber = member.MembershipNumber,
                    CompanyName = member.CompanyName,
                    Department = member.Department,
                    AdmissionDate = member.AdmissionDate,
                    MembershipStatus = member.MembershipStatus,
                    CurrentTermStatus = member.CurrentTermStatus,
                    MembershipYear = member.MembershipYear
                };

                return Json(
                    ApiResponse<object>.Ok(result, "Membership record retrieved successfully."),
                    JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(
                    ApiResponse<object>.Fail("An error occurred while retrieving the membership record."),
                    JsonRequestBehavior.AllowGet);
            }
        }

        /* ============================================================
           2. Write Actions (Save Draft & Submit)
           ============================================================ */

        [HttpPost]
        public ActionResult SaveDraft(MemberFormDto form)
        {
            if (form == null) return Fail("A draft payload is required.");
            if (string.IsNullOrWhiteSpace(form.NRIC)) return Fail("NRIC is required to save a draft.");
            if (!ModelState.IsValid) return Fail(GetModelStateErrors());

            NormalizeForm(form);

            Member existing = null;

            if (form.MemberId > 0)
            {
                existing = repository.GetById(form.MemberId);
                if (existing == null) return Fail("The draft record was not found.");
                if (!IsEditable(existing)) return Fail("This record is no longer an editable draft.");
            }
            else
            {
                existing = repository.GetDraftByNric(form.NRIC);
                if (existing != null) form.MemberId = existing.MemberId;
            }

            if (HasDuplicateNric(form.NRIC, form.MemberId > 0 ? (int?)form.MemberId : null))
            {
                return Fail("This NRIC has already been registered.");
            }

            Member member = MemberMapper.ToMember(form, DraftStatus);
            member.MemberId = form.MemberId;

            try
            {
                if (existing == null)
                {
                    member.MemberId = repository.InsertDraft(member);
                }
                else
                {
                    if (!repository.UpdateDraft(member)) return Fail("The draft could not be updated.");
                }

                return Json(ApiResponse<object>.Ok(new { MemberId = member.MemberId }, "Your draft has been saved successfully."));
            }
            catch (SqlException exception)
            {
                //FileLogger.LogException(exception, "MemberController.SaveDraft");
                return Fail("A database error occurred. Please contact support.");
            }
            catch (Exception ex)
            {
                //FileLogger.LogException(ex, "MemberController.SaveDraft");
                return Fail("An unexpected error occurred. Please try again.");
            }
        }

        [HttpPost]
        public ActionResult Submit(MemberFormDto form)
        {
            if (form == null) return Fail("A membership payload is required.");
            if (!ModelState.IsValid) return Fail(GetModelStateErrors());

            var user = CurrentUser;
            bool isCommittee = user != null && user.Role == DevRoles.Committee;

            // ============================================================
            // PATH 1: COMMITTEE EDITING AN EXISTING MEMBER
            // ============================================================
            // We check if it's an edit using the form.IsEditMode flag.
            // NEVER trust MemberId alone, as a malicious user could spoof it.
            if (isCommittee && form.IsEditMode && form.MemberId > 0)
            {
                var existingMember = repository.GetById(form.MemberId);
                if (existingMember == null) return Fail("The member record was not found.");

                // Update ONLY the allowed fields 
                existingMember.MaritalStatus = form.MaritalStatus;
                existingMember.CompanyName = form.CompanyName;
                existingMember.CompanyAddress = form.CompanyAddress;
                existingMember.Department = form.Department;
                existingMember.Occupation = form.Occupation;
                existingMember.HeadOfDepartment = form.HeadOfDepartment;
                existingMember.Floor = form.Floor;
                existingMember.Tel = form.Tel;
                existingMember.Email = form.Email;
                existingMember.HomeAddress = form.HomeAddress;
                existingMember.Hobbies = form.Hobbies;

                try
                {
                    if (!repository.Update(existingMember)) return Fail("The details could not be updated.");
                    return Json(ApiResponse<Member>.Ok(existingMember, "Member details updated successfully."));
                }
                catch (SqlException exception)
                {
                    //FileLogger.LogException(exception, "MemberController.Submit - Edit");
                    return Fail("A database error occurred. Please contact support.");
                }
                catch (Exception ex)
                {
                    //FileLogger.LogException(ex, "MemberController.Submit - Edit");
                    return Fail("An unexpected error occurred. Please try again.");
                }
            }

            // ============================================================
            // PATH 2: STANDARD SUBMISSION (New Application or Resume Draft)
            // ============================================================

            ValidateFinalApplication(form);
            if (!ModelState.IsValid) return Fail(GetModelStateErrors());

            NormalizeForm(form);

            // Check if this is a Committee manual entry (entering data for someone else)
            bool isCommitteeManualEntry = isCommittee && user.NRIC != form.NRIC;

            string targetStatus = isCommitteeManualEntry
                ? ApplicationStatus.VerifiedByDeptHead
                : SubmittedStatus;

            Member existing = null;

            if (form.MemberId > 0)
            {
                existing = repository.GetById(form.MemberId);
                if (existing == null) return Fail("The membership record was not found.");
                if (!IsEditable(existing)) return Fail("This application has already been submitted.");
                if (HasDuplicateNric(form.NRIC, form.MemberId)) return Fail("This NRIC has already been registered.");
            }
            else
            {
                existing = repository.GetDraftByNric(form.NRIC);
                if (existing != null)
                {
                    form.MemberId = existing.MemberId;
                }
                else if (HasDuplicateNric(form.NRIC, null))
                {
                    return Fail("This NRIC has already been registered.");
                }
            }

            Member member = MemberMapper.ToMember(form, targetStatus);
            member.MemberId = form.MemberId;

            try
            {
                if (existing == null)
                {
                    member.MemberId = repository.Insert(member);
                }
                else
                {
                    if (!repository.SubmitDraftWithStatus(member, targetStatus))
                    {
                        return Fail("The application could not be submitted.");
                    }
                }

                return Json(ApiResponse<Member>.Ok(member, "Application submitted."));
            }
            catch (SqlException exception)
            {
                //FileLogger.LogException(exception, "MemberController.Submit");
                return Fail("A database error occurred. Please contact support.");
            }
            catch (Exception ex)
            {
                //FileLogger.LogException(ex, "MemberController.Submit");
                return Fail("An unexpected error occurred. Please try again.");
            }
        }

        [HttpDelete]
        public JsonResult Delete(int id)
        {
            if (id <= 0) return Json(ApiResponse<object>.Fail("A valid member ID is required."));
            if (!repository.Delete(id)) return Json(ApiResponse<object>.Fail("Member was not found."));
            return Json(ApiResponse<object>.Ok(null, "Member deleted."));
        }

        /* ============================================================
           3. Validation
           ============================================================ */

        private void ValidateFinalApplication(MemberFormDto form)
        {
            Require(form.Name, "Name", "Please enter your full name.");
            Require(form.NRIC, "NRIC", "Please enter your NRIC.");
            Require(form.Gender, "Gender", "Please select your gender.");
            Require(form.Race, "Race", "Please select your race.");
            Require(form.MaritalStatus, "MaritalStatus", "Please select your marital status.");
            Require(form.Nationality, "Nationality", "Please enter your nationality.");

            if (!form.DateOfBirth.HasValue) ModelState.AddModelError("DateOfBirth", "Please enter your date of birth.");

            Require(form.PlaceOfBirth, "PlaceOfBirth", "Please enter your place of birth.");
            Require(form.CompanyName, "CompanyName", "Please enter your company name.");
            Require(form.CompanyAddress, "CompanyAddress", "Please enter your company address.");
            Require(form.Department, "Department", "Please enter your department.");
            Require(form.Occupation, "Occupation", "Please enter your occupation.");
            Require(form.HeadOfDepartment, "HeadOfDepartment", "Please enter your Head of Department.");
            Require(form.Floor, "Floor", "Please enter your floor.");
            Require(form.Tel, "Tel", "Please enter your telephone number.");
            Require(form.Email, "Email", "Please enter your email address.");
            Require(form.HomeAddress, "HomeAddress", "Please enter your home address.");
            Require(form.Hobbies, "Hobbies", "Please enter your hobbies.");

            if (!form.Declaration1) ModelState.AddModelError("Declaration1", "You must agree to the first declaration.");
            if (!form.Declaration2) ModelState.AddModelError("Declaration2", "You must agree to the second declaration.");
        }

        private void Require(string value, string fieldName, string message)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                ModelState.AddModelError(fieldName, message);
            }
        }

        /* ============================================================
           4. Helpers
           ============================================================ */

        private static void NormalizeForm(MemberFormDto form)
        {
            form.NRIC = NormalizeNric(form.NRIC);
            form.Tel = NormalizePhone(form.Tel);
        }

        private static string NormalizeNric(string nric)
        {
            return string.IsNullOrWhiteSpace(nric) ? null : nric.Trim();
        }

        private static string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return phone;
            return System.Text.RegularExpressions.Regex.Replace(phone, @"[\s\-\(\)]", "");
        }

        private static bool IsEditable(Member member)
        {
            if (member == null || string.IsNullOrWhiteSpace(member.ApplicationStatus)) return false;

            return string.Equals(member.ApplicationStatus, DraftStatus, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(member.ApplicationStatus, ApplicationStatus.ReturnedForCorrection, StringComparison.OrdinalIgnoreCase);
        }

        private bool HasDuplicateNric(string nric, int? excludingId)
        {
            if (string.IsNullOrWhiteSpace(nric)) return false;
            return repository.ExistsByNric(nric, excludingId);
        }

        private string GetModelStateErrors()
        {
            string errors = string.Join(" ", ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message)));

            return string.IsNullOrWhiteSpace(errors)
                ? "Please complete all required fields correctly."
                : errors;
        }

        private JsonResult Fail(string message)
        {
            return Json(ApiResponse<object>.Fail(message), JsonRequestBehavior.DenyGet);
        }

        /* ============================================================
            5. Digital Membership Card
            ============================================================ */
        [HttpGet]
        public ActionResult Card()
        {
            var user = CurrentUser;
            if (user == null) return RedirectToAction("Index", "Home");

            // Fetch the user's latest application/membership
            var myApps = repository.GetByNric(user.NRIC);
            var activeMembership = myApps.FirstOrDefault(m => m.MembershipStatus == "Active");

            if (activeMembership == null)
            {
                TempData["ErrorMessage"] = "You do not have an active membership to view a card.";
                return RedirectToAction("Mine", "Member");
            }

            var viewModel = new MembershipCardViewModel
            {
                Name = activeMembership.Name,
                NRIC = activeMembership.NRIC,
                MembershipNumber = activeMembership.MembershipNumber,
                AdmissionDate = activeMembership.AdmissionDate,
                Department = activeMembership.Department,
                CompanyName = activeMembership.CompanyName,
                ValidYear = activeMembership.MembershipYear
            };

            return View(viewModel);
        }

        [HttpGet]
        public ActionResult CardV2()
        {
            var user = CurrentUser;
            if (user == null) return RedirectToAction("Index", "Home");

            var myApps = repository.GetByNric(user.NRIC);
            var activeMembership = myApps.FirstOrDefault(m => m.MembershipStatus == "Active");

            if (activeMembership == null)
            {
                TempData["ErrorMessage"] = "You do not have an active membership to view a card.";
                return RedirectToAction("Mine", "Member");
            }

            var viewModel = new MembershipCardViewModel
            {
                Name = activeMembership.Name,
                NRIC = activeMembership.NRIC,
                MembershipNumber = activeMembership.MembershipNumber,
                AdmissionDate = activeMembership.AdmissionDate,
                Department = activeMembership.Department,
                CompanyName = activeMembership.CompanyName,
                ValidYear = activeMembership.MembershipYear
            };

            return View(viewModel);
        }
    }
}