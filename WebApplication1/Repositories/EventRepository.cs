using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
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
                SELECT
                    e.*,
                    v.VenueName,
                    v.VenueImagePath
                FROM dbo.Events e
                INNER JOIN dbo.EventVenues v
                    ON e.VenueId = v.VenueId
                WHERE
                    @SearchTerm IS NULL
                    OR e.EventName LIKE '%' + @SearchTerm + '%'
                    OR v.VenueName LIKE '%' + @SearchTerm + '%'
                ORDER BY
                    e.StartDateTime DESC,
                    e.EventId DESC;";

            string search = string.IsNullOrWhiteSpace(searchTerm)
                ? null
                : searchTerm.Trim();

            using (IDbConnection db = connectionFactory.Create())
            {
                return db.Query<Event>(
                    sql,
                    new { SearchTerm = search }).ToList();
            }
        }

        public Event GetById(int eventId)
        {
            const string sql = @"
                SELECT
                    e.*,
                    v.VenueName,
                    v.VenueImagePath
                FROM dbo.Events e
                INNER JOIN dbo.EventVenues v
                    ON e.VenueId = v.VenueId
                WHERE e.EventId = @eventId;";

            using (IDbConnection db = connectionFactory.Create())
            {
                return db.QuerySingleOrDefault<Event>(
                    sql,
                    new { eventId });
            }
        }

        public IReadOnlyList<EventVenue> GetActiveVenues()
        {
            const string sql = @"
                SELECT *
                FROM dbo.EventVenues
                WHERE IsActive = 1
                ORDER BY VenueName ASC;";

            using (IDbConnection db = connectionFactory.Create())
            {
                return db.Query<EventVenue>(sql).ToList();
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
                    VenueId,
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
                    @VenueId,
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
                    VenueId = @VenueId,
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
    }
}