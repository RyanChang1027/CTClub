using CrownTowersClub.Models;

namespace CrownTowersClub.Repositories
{
    public interface ICorporateEmployeeRepository
    {
        CorporateEmployee GetByNric(string nric);
    }
}
