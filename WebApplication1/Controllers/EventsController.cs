using System.Linq;
using System.Web.Mvc;
using CrownTowersClub.Infrastructure.Session;
using CrownTowersClub.Repositories;
using CrownTowersClub.ViewModels;
using System.Collections.Generic;

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

        [HttpGet]
        public ActionResult Create()
        {
            var user = Session[DevSessionKeys.User] as DevSessionUser;

            if (user == null || user.Role != DevRoles.Committee)
            {
                return RedirectToAction("Index", "Home");
            }

            var viewModel = BuildCreateViewModel();

            return View(viewModel);
        }

        private EventCreateViewModel BuildCreateViewModel()
        {
            return new EventCreateViewModel
            {
                EventTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "Annual Dinner", Text = "Annual Dinner" },
                new SelectListItem { Value = "Club Gathering", Text = "Club Gathering" },
                new SelectListItem { Value = "Special Event", Text = "Special Event" },
                new SelectListItem { Value = "Other", Text = "Other" }
            },

                Venues = eventRepository.GetActiveVenues()
                    .Select(venue => new SelectListItem
                    {
                        Value = venue.VenueId.ToString(),
                        Text = venue.VenueName
                    })
                    .ToList()
            };
        }
    }
}