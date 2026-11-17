using CrownTowersClub.Models;

namespace CrownTowersClub.Mappers
{
    public static class MemberMapper
    {
        public static Member ToMember(
            MemberFormDto source,
            string applicationStatus)
        {
            if (source == null)
            {
                return null;
            }

            return new Member
            {
                MemberId = source.MemberId,
                Name = source.Name,
                NRIC = source.NRIC,
                Gender = source.Gender,
                Race = source.Race,
                MaritalStatus = source.MaritalStatus,
                Nationality = source.Nationality,
                DateOfBirth = source.DateOfBirth,
                PlaceOfBirth = source.PlaceOfBirth,
                CompanyName = source.CompanyName,
                CompanyAddress = source.CompanyAddress,
                Department = source.Department,
                Occupation = source.Occupation,
                HeadOfDepartment = source.HeadOfDepartment,
                Floor = source.Floor,
                Tel = source.Tel,
                Email = source.Email,
                HomeAddress = source.HomeAddress,
                Hobbies = source.Hobbies,
                Declaration1 = source.Declaration1,
                Declaration2 = source.Declaration2,
                ApplicationStatus = applicationStatus
            };
        }
    }
}
