using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;

namespace CrownTowersClub.ViewModels
{
    public sealed class EventCreateViewModel
    {
        [Required(ErrorMessage = "Please enter an event name.")]
        [StringLength(200, ErrorMessage = "Event name cannot exceed 200 characters.")]
        public string EventName { get; set; }

        [StringLength(4000, ErrorMessage = "Description cannot exceed 4,000 characters.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Please select an event type.")]
        public string EventType { get; set; }

        [StringLength(100)]
        public string CustomEventType { get; set; }

        [Required(ErrorMessage = "Please select a start date.")]
        public DateTime? StartDate { get; set; }

        [Required(ErrorMessage = "Please select a start time.")]
        public TimeSpan? StartTime { get; set; }

        [Required(ErrorMessage = "Please select an end date.")]
        public DateTime? EndDate { get; set; }

        [Required(ErrorMessage = "Please select an end time.")]
        public TimeSpan? EndTime { get; set; }

        [Required(ErrorMessage = "Please select a venue.")]
        public string VenueKey { get; set; }

        public HttpPostedFileBase EventImage { get; set; }

        public IEnumerable<SelectListItem> EventTypes { get; set; }

        public IEnumerable<SelectListItem> Venues { get; set; }
    }
}