using System;
using System.Data;
using Dapper;
using CrownTowersClub.Models;
using CrownTowersClub.Infrastructure.Data;

namespace CrownTowersClub.Repositories
{
    public sealed class CorporateEmployeeRepository
        : ICorporateEmployeeRepository
    {
        private readonly IDbConnectionFactory connectionFactory;

        public CorporateEmployeeRepository(
            IDbConnectionFactory connectionFactory)
        {
            if (connectionFactory == null)
            {
                throw new ArgumentNullException(
                    "connectionFactory");
            }

            this.connectionFactory = connectionFactory;
        }

        public CorporateEmployee GetByNric(string nric)
        {
            //const string sql = @"
            //    SELECT
            //        LTRIM(RTRIM(cICNoNew)) AS NRIC,
            //        cFullName AS Name,
            //        UPPER(cSex) AS Gender,
            //        UPPER(cRace) AS Race,
            //        UPPER(cMaritalStatus) AS MaritalStatus,
            //        cNationality AS Nationality,

            //        CASE
            //            WHEN cDOB IS NULL THEN NULL
            //            ELSE CONVERT(varchar(10), cDOB, 23)
            //        END AS DateOfBirth,

            //        cHomeTown AS PlaceOfBirth,
            //        cCompany AS CompanyName,
            //        cCompanyAddress AS CompanyAddress,
            //        cDepartment AS Department,
            //        cPosition AS Occupation,
            //        cTelNoMobile AS Tel,
            //        cEmailInt AS Email,
            //        cAddressHome AS HomeAddress
            //    FROM dbo.fnEmployee_Get()
            //    WHERE LTRIM(RTRIM(cICNoNew)) = @NRIC;";

            const string sql = @"
                SELECT
                    LTRIM(RTRIM(cICNoNew)) AS NRIC,
                    cFullName AS Name,
                    cSex AS Gender,
                    cRace AS Race,
                    cMaritalStatus AS MaritalStatus,
                    cNationality AS Nationality,

                    CASE
                        WHEN cDOB IS NULL THEN NULL
                        ELSE CONVERT(varchar(10), cDOB, 23)
                    END AS DateOfBirth,

                    cHomeTown AS PlaceOfBirth,
                    cCompany AS CompanyName,
                    cCompanyAddress AS CompanyAddress,
                    cDepartment AS Department,
                    cPosition AS Occupation,
                    cTelNoMobile AS Tel,
                    cEmailInt AS Email,
                    cAddressHome AS HomeAddress
                FROM dbo.fnEmployee_Get()
                WHERE LTRIM(RTRIM(cICNoNew)) = @NRIC;";

            using (IDbConnection connection =
                   connectionFactory.Create())
            {
                return connection.QuerySingleOrDefault<CorporateEmployee>(
                    sql,
                    new
                    {
                        NRIC = nric
                    });
            }
        }
    }
}
