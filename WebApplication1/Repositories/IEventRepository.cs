using System.Collections.Generic;
using CrownTowersClub.Models;

namespace CrownTowersClub.Repositories
{
    /// <summary>
    /// Defines the database operations for Event Management.
    /// </summary>
    public interface IEventRepository
    {
        // Read Actions

        /// <summary>
        /// Gets all events, optionally filtered by event name or venue.
        /// </summary>
        IReadOnlyList<Event> GetAll(string searchTerm);

        /// <summary>
        /// Gets one event using its unique ID.
        /// </summary>
        Event GetById(int eventId);

        // Write Actions

        /// <summary>
        /// Inserts a new event using the ManualStatus provided by the event object.
        /// Draft events use "Draft"; finalized events use NULL for automatic status calculation.
        /// </summary>
        int Insert(Event eventItem);

        /// <summary>
        /// Updates an existing event.
        /// </summary>
        bool Update(Event eventItem);

        /// <summary>
        /// Changes the manual status to Draft, Cancelled or NULL.
        /// NULL means the displayed status is calculated automatically.
        /// </summary>
        bool UpdateManualStatus(int eventId, string manualStatus);

        /// <summary>
        /// Permanently deletes an event.
        /// </summary>
        bool Delete(int eventId);
    }
}