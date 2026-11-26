using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using Newtonsoft.Json;
using CrownTowersClub.Infrastructure.Data;
using CrownTowersClub.Models;

namespace CrownTowersClub.Repositories
{
    public sealed class EventRepository : IEventRepository
    {
        private readonly IDbConnectionFactory connectionFactory;

        public EventRepository(IDbConnectionFactory connectionFactory)
        {
            if (connectionFactory == null)
                throw new ArgumentNullException("connectionFactory");

            this.connectionFactory = connectionFactory;
        }

        // ============================================================
        // 1. Read Actions
        // ============================================================

        public IReadOnlyList<Event> GetAll(string searchTerm)
        {
            const string sql = @"
                SELECT *
                FROM dbo.Events
                ORDER BY
                    StartDateTime DESC,
                    EventId DESC;";

            string search = string.IsNullOrWhiteSpace(searchTerm)
                ? null
                : searchTerm.Trim();

            using (IDbConnection db = connectionFactory.Create())
            {
                List<Event> events = db.Query<Event>(sql).ToList();

                foreach (Event eventItem in events)
                {
                    PopulateVenueDisplayDetails(eventItem);
                }

                if (search != null)
                {
                    events = events
                        .Where(e =>
                            (e.EventName != null &&
                             e.EventName.IndexOf(
                                 search,
                                 StringComparison.OrdinalIgnoreCase) >= 0)
                            ||
                            (e.VenueName != null &&
                             e.VenueName.IndexOf(
                                 search,
                                 StringComparison.OrdinalIgnoreCase) >= 0))
                        .ToList();
                }

                return events;
            }
        }

        public Event GetById(int eventId)
        {
            const string sql = @"
                SELECT *
                FROM dbo.Events
                WHERE EventId = @eventId;";

            using (IDbConnection db = connectionFactory.Create())
            {
                Event eventItem = db.QuerySingleOrDefault<Event>(
                    sql,
                    new { eventId });

                PopulateVenueDisplayDetails(eventItem);

                return eventItem;
            }
        }

        // ============================================================
        // 2. Write Actions
        // ============================================================

        public int Insert(Event eventItem)
        {
            const string sql = @"
                INSERT INTO dbo.Events
                (
                    EventName,
                    Description,
                    EventType,
                    CustomEventType,
                    StartDateTime,
                    EndDateTime,
                    VenueDetails,
                    EventImagePath,
                    ManualStatus
                )
                VALUES
                (
                    @EventName,
                    @Description,
                    @EventType,
                    @CustomEventType,
                    @StartDateTime,
                    @EndDateTime,
                    @VenueDetails,
                    @EventImagePath,
                    'Draft'
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using (IDbConnection db = connectionFactory.Create())
            {
                return db.ExecuteScalar<int>(sql, eventItem);
            }
        }

        public bool Update(Event eventItem)
        {
            const string sql = @"
                UPDATE dbo.Events
                SET
                    EventName = @EventName,
                    Description = @Description,
                    EventType = @EventType,
                    CustomEventType = @CustomEventType,
                    StartDateTime = @StartDateTime,
                    EndDateTime = @EndDateTime,
                    VenueDetails = @VenueDetails,
                    EventImagePath = @EventImagePath,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE EventId = @EventId;";

            using (IDbConnection db = connectionFactory.Create())
            {
                return db.Execute(sql, eventItem) == 1;
            }
        }

        public bool UpdateManualStatus(int eventId, string manualStatus)
        {
            if (manualStatus != null &&
                manualStatus != "Draft" &&
                manualStatus != "Cancelled")
            {
                throw new ArgumentException(
                    "Invalid manual event status.",
                    "manualStatus");
            }

            const string sql = @"
                UPDATE dbo.Events
                SET
                    ManualStatus = @manualStatus,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE EventId = @eventId;";

            using (IDbConnection db = connectionFactory.Create())
            {
                return db.Execute(
                    sql,
                    new { eventId, manualStatus }) == 1;
            }
        }

        public bool Delete(int eventId)
        {
            const string sql = @"
                DELETE FROM dbo.Events
                WHERE EventId = @eventId;";

            using (IDbConnection db = connectionFactory.Create())
            {
                return db.Execute(sql, new { eventId }) == 1;
            }
        }

        // ============================================================
        // 3. Venue Snapshot Handling
        // ============================================================

        private static void PopulateVenueDisplayDetails(Event eventItem)
        {
            if (eventItem == null ||
                string.IsNullOrWhiteSpace(eventItem.VenueDetails))
            {
                return;
            }

            VenueOption venue =
                JsonConvert.DeserializeObject<VenueOption>(
                    eventItem.VenueDetails);

            if (venue == null)
                return;

            eventItem.VenueName = venue.VenueName;
            eventItem.VenueImagePath = venue.VenueImagePath;
        }
    }
}