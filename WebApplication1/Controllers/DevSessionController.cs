using System;
using System.Web.Mvc;
using CrownTowersClub.Infrastructure.Session;
using CrownTowersClub.Models;
using CrownTowersClub.Repositories;

namespace CrownTowersClub.Controllers
{
    public sealed class DevSessionController : Controller
    {
        private readonly ICorporateEmployeeRepository corporateRepository;

        public DevSessionController(
            ICorporateEmployeeRepository corporateRepository)
        {
            if (corporateRepository == null)
            {
                throw new ArgumentNullException(
                    "corporateRepository");
            }

            this.corporateRepository = corporateRepository;
        }

        /*
         * Called by the three landing-page buttons.
         *
         * Looks up the supplied NRIC, stores the resulting
         * user in Session, then redirects to the role's
         * primary page.
         */
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult Begin(string pICNo, string role)
        {
            if (!IsDevEnabled())
            {
                return View("~/Views/Shared/Forbidden.cshtml");
            }

            var normalizedNric =
                string.IsNullOrWhiteSpace(pICNo)
                    ? null
                    : pICNo.Trim();

            if (string.IsNullOrWhiteSpace(normalizedNric))
            {
                TempData["DevBeginError"] =
                    "Please enter an NRIC.";

                return RedirectToAction("Index", "Home");
            }

            if (!IsKnownRole(role))
            {
                TempData["DevBeginError"] =
                    "Please choose a role.";

                return RedirectToAction("Index", "Home");
            }

            CorporateEmployee employee =
                corporateRepository.GetByNric(normalizedNric);

            if (employee == null)
            {
                TempData["DevBeginError"] =
                    "No staff record was found for this NRIC.";

                return RedirectToAction("Index", "Home");
            }

            Session[DevSessionKeys.User] =
                new DevSessionUser
                {
                    NRIC = employee.NRIC,
                    Name = employee.Name,
                    Department = employee.Department,
                    CompanyName = employee.CompanyName,
                    Role = role
                };

            //return Redirect(GetHomeActionForRole(role));
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public ActionResult Logout()
        {
            if (!IsDevEnabled())
            {
                return View("~/Views/Shared/Forbidden.cshtml");
            }

            Session.Clear();

            return RedirectToAction("Index", "Home");
        }

        /* ============================================================
           Helpers
           ============================================================ */
        private static string GetHomeActionForRole(string role)
        {
            switch (role)
            {
                case DevRoles.DepartmentHead:
                    return "/Review/Pending";

                case DevRoles.Committee:
                    return "/Review/Verified";

                default:
                    return "/Member/Mine";
            }
        }

        private static bool IsKnownRole(string role)
        {
            return role == DevRoles.Staff ||
                   role == DevRoles.DepartmentHead ||
                   role == DevRoles.Committee;
        }

        private static bool IsDevEnabled()
        {
            return System.Web.HttpContext.Current != null &&
                   System.Web.HttpContext.Current.IsDebuggingEnabled;
        }
    }
}