using Entities.Contexts;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Metadata.Ecma335;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Entities.Models
{
    public partial class BecaUser
    {
        public BecaUser()
        {
            //Profiles = new List<UserProfile>();
            //RefreshTokens = new List<RefreshToken>();
        }

        [Key]
        public int idUtente { get; set; }
        public string UserName { get; set; }
        //public string Pwd { get; set; }
        public string? LastName { get; set; }
        public string? FirstName { get; set; }
        public string? Title { get; set; }
        public string? EMail { get; set; }
        public string? Phone { get; set; }
        public bool isConfirmed { get; set; }
        public bool isPrivacyRead { get; set; }
        public bool isPwdChanged { get; set; }
        public List<UserCompany> Companies { get; set; }
        //public string isDefault { get; set; }
        //public int idProfile { get; set; }
        //public string Profile { get; set; }
        //public int idCompany { get; set; }
        //public string CompanyName { get; set; }
        //public string Logo1url { get; set; }
        //public string Logo2url { get; set; }
        //public string Logo3url { get; set; }
        //public string Logo4url { get; set; }
        //public string Logo5url { get; set; }

        [JsonIgnore]
        public string Pwd { get; set; }

        [JsonIgnore]
        public List<RefreshToken> RefreshTokens { get; set; }

        public int idUtenteLoc(int? idCompany) => idCompany == null ? idUtente : Companies.FirstOrDefault(C => C.idCompany == idCompany).idUtenteLoc;
    }

    //[Owned]
    [PrimaryKey(nameof(idProfile), nameof(idCompany))]
    public partial class UserProfile
    {
        [JsonIgnore]
        public int idUtente { get; set; }
        //[Key, Column(Order = 0)]
        public int idProfile { get; set; }
        [JsonIgnore]
        //[Key, Column(Order = 1)]
        public int idCompany { get; set; }
        public string Profile { get; set; }
        public bool PasswordChange { get; set; }
        [JsonIgnore]
        public string? Flags { get; set; }
        public bool isDefault { get; set; }
    }

    //[Owned]
    public partial class UserCompany
    {
        [JsonIgnore]
        public int idUtente { get; set; }
        [JsonIgnore]
        public int idUtenteLoc { get; set; }
        private int isDefault { get; set; }
        [NotMapped]
        public bool IsDefault { get => isDefault != 0; set => isDefault = value ? 1 : 0; }
        [Key]
        public int idCompany { get; set; }
        public string CompanyName { get; set; }
        public string? Logo1url { get; set; }
        public string? Logo2url { get; set; }
        public string? Logo3url { get; set; }
        public string? Logo4url { get; set; }
        public string? Logo5url { get; set; }
        public string? Color1 { get; set; }
        public string? Color2 { get; set; }
        public string? Color3 { get; set; }
        public string? Color4 { get; set; }
        public string? Color5 { get; set; }
        public string MainFolder { get; set; }

        public List<UserProfile> Profiles { get; set; }
        [NotMapped] public List<UserBusinessUnit> BusinessUnits1 { get; set; }
        [NotMapped] public List<UserBusinessUnit> BusinessUnits2 { get; set; }
    }

    public class UserBusinessUnit
    {
        public string code { get; set; }
        public string value { get; set; }
    }
}
