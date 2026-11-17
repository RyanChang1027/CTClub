namespace CrownTowersClub.Models
{
    public sealed class CorporateEmployee
    {
        public string NRIC { get; set; }

        public string Name { get; set; }

        public string Gender { get; set; }

        public string Race { get; set; }

        public string MaritalStatus { get; set; }

        public string Nationality { get; set; }

        /*
         * Always returned as yyyy-MM-dd.
         */
        public string DateOfBirth { get; set; }

        public string PlaceOfBirth { get; set; }

        public string CompanyName { get; set; }

        public string CompanyAddress { get; set; }

        public string Department { get; set; }

        public string Occupation { get; set; }

        public string Tel { get; set; }

        public string Email { get; set; }

        public string HomeAddress { get; set; }
    }
}
