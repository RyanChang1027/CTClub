using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.SqlClient;
using CrownTowersClub.Infrastructure.Session;
using CrownTowersClub.Models;
using CrownTowersClub.Repositories;
using CrownTowersClub.ViewModels;
using System.Collections.Generic;
using CrownTowersClub.Services;
using Newtonsoft.Json;

namespace CrownTowersClub.Controllers
{
    public sealed class EventsController : Controller
    {
        private readonly IEventRepository eventRepository;
        private readonly VenueConfigurationProvider venueProvider;

        public EventsController(
            IEventRepository eventRepository,
            VenueConfigurationProvider venueProvider)
        {
            this.eventRepository = eventRepository;
            this.venueProvider = venueProvider;
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

        [HttpGet]
        public ActionResult TableLayout()
        {
            var user = Session[DevSessionKeys.User] as DevSessionUser;

            if (user == null || user.Role != DevRoles.Committee)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EventCreateViewModel viewModel, string submitAction)
        {
            var user = Session[DevSessionKeys.User] as DevSessionUser;

            if (user == null || user.Role != DevRoles.Committee)
            {
                return RedirectToAction("Index", "Home");
            }

            if (viewModel == null)
            {
                return RedirectToAction("Create");
            }

            // Determine whether the user is saving a draft.
            bool isSavingDraft = submitAction == "SaveDraft";

            // Drafts do not require dates, times or a venue.
            if (isSavingDraft)
            {
                ModelState.Remove("StartDate");
                ModelState.Remove("StartTime");
                ModelState.Remove("EndDate");
                ModelState.Remove("EndTime");
                ModelState.Remove("VenueKey");
            }

            // Validate the selected event type.
            string[] allowedEventTypes =
            {
                "Annual Dinner",
                "Club Gathering",
                "Special Event",
                "Other"
            };

            if (!allowedEventTypes.Contains(viewModel.EventType))
            {
                ModelState.AddModelError(
                    "EventType",
                    "Please select a valid event type.");
            }

            // A custom event type is required when Other is selected.
            if (viewModel.EventType == "Other")
            {
                if (string.IsNullOrWhiteSpace(viewModel.CustomEventType))
                {
                    ModelState.AddModelError(
                        "CustomEventType",
                        "Please enter a custom event type.");
                }
                else
                {
                    viewModel.CustomEventType =
                        viewModel.CustomEventType.Trim();
                }
            }
            else
            {
                viewModel.CustomEventType = null;
            }

            // Find and validate the selected active venue.
            VenueOption selectedVenue = null;

            if (!string.IsNullOrWhiteSpace(viewModel.VenueKey))
            {
                selectedVenue = venueProvider.GetActiveVenues()
                    .FirstOrDefault(venue =>
                        string.Equals(
                            venue.VenueKey,
                            viewModel.VenueKey,
                            StringComparison.OrdinalIgnoreCase));

                if (selectedVenue == null)
                {
                    ModelState.AddModelError(
                        "VenueKey",
                        "Please select a valid active venue.");
                }
            }
            else if (!isSavingDraft)
            {
                ModelState.AddModelError(
                    "VenueKey",
                    "Please select a venue.");
            }

            // Combine the date and time fields.
            DateTime? startDateTime = null;
            DateTime? endDateTime = null;

            if (viewModel.StartDate.HasValue &&
                viewModel.StartTime.HasValue)
            {
                startDateTime = viewModel.StartDate.Value.Date
                    .Add(viewModel.StartTime.Value);
            }

            if (viewModel.EndDate.HasValue &&
                viewModel.EndTime.HasValue)
            {
                endDateTime = viewModel.EndDate.Value.Date
                    .Add(viewModel.EndTime.Value);
            }

            if (startDateTime.HasValue &&
                endDateTime.HasValue &&
                endDateTime.Value <= startDateTime.Value)
            {
                ModelState.AddModelError(
                    "EndDate",
                    "The event end date and time must be later than the start date and time.");
            }

            // Return the form if any validation fails.
            if (!ModelState.IsValid)
            {
                var invalidViewModel = BuildCreateViewModel();

                invalidViewModel.EventName = viewModel.EventName;
                invalidViewModel.Description = viewModel.Description;
                invalidViewModel.EventType = viewModel.EventType;
                invalidViewModel.CustomEventType = viewModel.CustomEventType;
                invalidViewModel.StartDate = viewModel.StartDate;
                invalidViewModel.StartTime = viewModel.StartTime;
                invalidViewModel.EndDate = viewModel.EndDate;
                invalidViewModel.EndTime = viewModel.EndTime;
                invalidViewModel.VenueKey = viewModel.VenueKey;

                return View(invalidViewModel);
            }

            // Map the form data to the Event model.
            var eventItem = new Event
            {
                EventName = viewModel.EventName.Trim(),
                Description = viewModel.Description,
                EventType = viewModel.EventType,
                CustomEventType = viewModel.CustomEventType,
                StartDateTime = startDateTime,
                EndDateTime = endDateTime,
                VenueDetails = selectedVenue == null
                    ? null
                    : JsonConvert.SerializeObject(selectedVenue),
                EventImagePath = null,
                ManualStatus = isSavingDraft ? "Draft" : null
            };

            try
            {
                // Insert() automatically saves the event as Draft.
                int eventId = eventRepository.Insert(eventItem);

                TempData["SuccessMessage"] = isSavingDraft
                    ? "Event draft saved successfully!"
                    : "Event created successfully!";

                return RedirectToAction("Index");
            }
            catch (SqlException)
            {
                ModelState.AddModelError(
                    "",
                    "A database error occurred while creating the event. Please try again.");
            }
            catch (Exception)
            {
                ModelState.AddModelError(
                    "",
                    "An unexpected error occurred. Please try again.");
            }

            var errorViewModel = BuildCreateViewModel();

            errorViewModel.EventName = viewModel.EventName;
            errorViewModel.Description = viewModel.Description;
            errorViewModel.EventType = viewModel.EventType;
            errorViewModel.CustomEventType = viewModel.CustomEventType;
            errorViewModel.StartDate = viewModel.StartDate;
            errorViewModel.StartTime = viewModel.StartTime;
            errorViewModel.EndDate = viewModel.EndDate;
            errorViewModel.EndTime = viewModel.EndTime;
            errorViewModel.VenueKey = viewModel.VenueKey;

            return View(errorViewModel);
        }

        private EventCreateViewModel BuildCreateViewModel()
        {
            var venues = venueProvider.GetActiveVenues();

            return new EventCreateViewModel
            {
                EventTypes = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Annual Dinner", Text = "Annual Dinner" },
                    new SelectListItem { Value = "Club Gathering", Text = "Club Gathering" },
                    new SelectListItem { Value = "Special Event", Text = "Special Event" },
                    new SelectListItem { Value = "Other", Text = "Other" }
                },

                Venues = venues
                    .Select(venue => new SelectListItem
                    {
                        Value = venue.VenueKey,
                        Text = venue.VenueName
                    })
                    .ToList(),

                VenueOptions = venues
            };
        }
    }
}