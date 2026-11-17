using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace CrownTowersClub.Models
{
    public sealed class MemberFormDto
    {
        public int MemberId { get; set; }

        [StringLength(150)]
        public string Name { get; set; }

        [StringLength(14)]
        [RegularExpression(
            @"^\d{6}-\d{2}-\d{4}$",
            ErrorMessage =
                "NRIC must follow 000000-00-0000 format.")]
        public string NRIC { get; set; }

        [StringLength(20)]
        public string Gender { get; set; }

        [StringLength(50)]
        public string Race { get; set; }

        [StringLength(30)]
        public string MaritalStatus { get; set; }

        [StringLength(80)]
        public string Nationality { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(150)]
        public string PlaceOfBirth { get; set; }

        [StringLength(150)]
        public string CompanyName { get; set; }

        [StringLength(500)]
        public string CompanyAddress { get; set; }

        [StringLength(100)]
        public string Department { get; set; }

        [StringLength(100)]
        public string Occupation { get; set; }

        [StringLength(150)]
        public string HeadOfDepartment { get; set; }

        [StringLength(30)]
        public string Floor { get; set; }

        [StringLength(30)]
        [RegularExpression(
             @"^[\s+-]*(\d[\s+-]*){9,12}$",
             ErrorMessage = "Please enter a valid Malaysian phone number.")]
        public string Tel { get; set; }

        [StringLength(254)]
        [EmailAddress(
            ErrorMessage =
                "Please enter a valid email address.")]
        public string Email { get; set; }

        [StringLength(500)]
        public string HomeAddress { get; set; }

        [StringLength(500)]
        public string Hobbies { get; set; }

        public bool Declaration1 { get; set; }

        public bool Declaration2 { get; set; }

        public bool IsEditMode { get; set; }
    }
}
