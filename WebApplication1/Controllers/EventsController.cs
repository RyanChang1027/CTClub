using System.Web.Mvc;
using CrownTowersClub.Infrastructure.Session;

namespace CrownTowersClub.Controllers
{
    public sealed class EventsController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            var user = Session[DevSessionKeys.User] as DevSessionUser;

            if (user == null || user.Role != DevRoles.Committee)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }
    }
}