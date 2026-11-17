using System.Data;

namespace CrownTowersClub.Infrastructure.Data
{
    public interface IDbConnectionFactory
    {
        IDbConnection Create();
    }
}

