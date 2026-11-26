using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using CrownTowersClub.Models;
using Newtonsoft.Json;

namespace CrownTowersClub.Services
{
    public sealed class VenueConfigurationProvider
    {
        public IReadOnlyList<VenueOption> GetActiveVenues()
        {
            string filePath = HttpContext.Current.Server.MapPath(
                "~/Content/app/data/venues.json");

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(
                    "The venue configuration file could not be found.",
                    filePath);
            }

            string json = File.ReadAllText(filePath);

            List<VenueOption> venues =
                JsonConvert.DeserializeObject<List<VenueOption>>(json);

            if (venues == null)
            {
                throw new InvalidOperationException(
                    "The venue configuration file contains invalid data.");
            }

            return venues
                .Where(v => v != null && v.IsActive)
                .OrderBy(v => v.VenueName)
                .ToList();
        }
    }
}