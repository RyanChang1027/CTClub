using System.Web.Mvc;
using CrownTowersClub.Infrastructure.Session;
using CrownTowersClub.Repositories;
using System;
using System.Linq;

namespace CrownTowersClub.Controllers
{
    public sealed class HomeController : Controller
    {
        private readonly IMemberRepository memberRepository;

        public HomeController(IMemberRepository memberRepository)
        {
            if (memberRepository == null)
            {
                throw new InvalidOperationException("IMemberRepository was not registered.");
            }

            this.memberRepository = memberRepository;
        }

        [HttpGet]
        public ActionResult Index()
        {
            var user = Session[DevSessionKeys.User] as DevSessionUser;

            if (user == null)
            {
                // Not logged in (picker state)
                ViewBag.Error = TempData["DevBeginError"] as string;
                return View();
            }

            // Logged in (Dashboard state)
            ViewBag.User = user;

            // 1. Get the user's latest application
            var myApps = memberRepository.GetByNric(user.NRIC);
            var latestApp = myApps.FirstOrDefault();

            // 2. Get stats based on role
            if (user.Role == DevRoles.DepartmentHead)
            {
                var pending = memberRepository.GetPendingByHod(user.NRIC);
                ViewBag.PendingReviewsCount = pending.Count;
            }
            else if (user.Role == DevRoles.Committee)
            {
                var verified = memberRepository.GetVerifiedAll();
                var payments = memberRepository.GetPendingPayments(DateTime.Now.Year);

                ViewBag.VerifiedCount = verified.Count;
                ViewBag.PaymentsCount = payments.Count;
            }

            return View(latestApp);
        }
    }
}