using System;
using System.ComponentModel.DataAnnotations;

namespace CrownTowersClub.Models
{
    /// <summary>
    /// Represents an available event venue.
    /// Maps to the dbo.EventVenues table.
    /// </summary>
    public sealed class EventVenue
    {
        public int VenueId { get; set; }

        [Required(ErrorMessage = "Please enter a venue name.")]
        [StringLength(150)]
        public string VenueName { get; set; }

        [StringLength(500)]
        public string VenueImagePath { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}