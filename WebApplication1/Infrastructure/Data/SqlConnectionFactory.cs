using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace CrownTowersClub.Infrastructure.Data
{
    public sealed class SqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string connectionString;

        public SqlConnectionFactory()
        {
            connectionString = ConfigurationManager
                .ConnectionStrings["CrownTowersClubConnection"].ConnectionString;
        }

        public IDbConnection Create()
        {
            return new SqlConnection(connectionString);
        }
    }
}
