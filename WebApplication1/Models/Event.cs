using System;
using System.ComponentModel.DataAnnotations;

namespace CrownTowersClub.Models
{
    /// <summary>
    /// Represents an event in the Club Event Management module.
    /// Maps to the dbo.Events table.
    /// </summary>
    public sealed class Event
    {
        public int EventId { get; set; }

        [Required(ErrorMessage = "Please enter an event name.")]
        [StringLength(200)]
        public string EventName { get; set; }

        public string Description { get; set; }

        [Required(ErrorMessage = "Please select an event type.")]
        [StringLength(60)]
        public string EventType { get; set; }

        [StringLength(100)]
        public string CustomEventType { get; set; }

        public DateTime? StartDateTime { get; set; }

        public DateTime? EndDateTime { get; set; }

        public string VenueDetails { get; set; }

        public string VenueName { get; set; }

        public string VenueAddress { get; set; }

        public string VenueImagePath { get; set; }

        public string EventImagePath { get; set; }

        [StringLength(20)]
        public string ManualStatus { get; set; }

        public string DisplayStatus
        {
            get
            {
                // Manual statuses always take priority.
                if (ManualStatus == "Draft" || ManualStatus == "Cancelled")
                {
                    return ManualStatus;
                }

                // Cannot calculate an automatic status without both dates.
                if (!StartDateTime.HasValue || !EndDateTime.HasValue)
                {
                    return "Unknown";
                }

                DateTime now = DateTime.Now;

                if (now < StartDateTime.Value)
                {
                    return "Upcoming";
                }

                if (now >= EndDateTime.Value)
                {
                    return "Completed";
                }

                return "Ongoing";
            }
        }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}