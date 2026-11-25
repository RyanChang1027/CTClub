using System.Web.Mvc;
using CrownTowersClub.Infrastructure.Session;
using CrownTowersClub.Repositories;
using CrownTowersClub.ViewModels;

namespace CrownTowersClub.Controllers
{
    public sealed class EventsController : Controller
    {
        private readonly IEventRepository eventRepository;

        public EventsController(IEventRepository eventRepository)
        {
            this.eventRepository = eventRepository;
        }

        [HttpGet]
        public ActionResult Index(string searchTerm)
        {
            var user = Session[DevSessionKeys.User] as DevSessionUser;

            if (user == null || user.Role != DevRoles.Committee)
            {
                return RedirectToAction("Index", "Home");
            }

            var viewModel = new EventIndexViewModel
            {
                Events = eventRepository.GetAll(searchTerm),
                SearchTerm = searchTerm
            };

            return View(viewModel);
        }
    }
}