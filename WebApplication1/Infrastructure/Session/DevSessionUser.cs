namespace CrownTowersClub.Infrastructure.Session
{
    /*
     * Dev-only representation of the "logged in" user.
     *
     * No authentication is used. The landing page
     * populates this object in Session after looking up
     * the NRIC against the corporate employee database.
     */
    public sealed class DevSessionUser
    {
        public string NRIC { get; set; }

        public string Name { get; set; }

        public string Department { get; set; }

        public string CompanyName { get; set; }

        /*
         * One of:
         * - "Staff"
         * - "DepartmentHead"
         * - "Committee"
         */
        public string Role { get; set; }
    }

    public static class DevRoles
    {
        public const string Staff = "Staff";

        public const string DepartmentHead = "DepartmentHead";

        public const string Committee = "Committee";
    }

    public static class DevSessionKeys
    {
        public const string User = "DevUser";
    }
}