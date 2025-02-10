using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Entities.Models
{
    public partial class Company
    {
        public Company()
        {
            //Profiles = new List<UserProfile>();
            Connections = new List<Connection>();
        }
        [Key]
        public int idCompany { get; set; }
        public string CompanyName { get; set; }
        public string? MainFolder { get; set; }
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
        public string? mail1 { get; set; }
        public string? mail2 { get; set; }
        public string? mail3 { get; set; }
        public string? mail4 { get; set; }
        public string? mail5 { get; set; }
        public string? urlDomain { get; set; }
        public bool hasBusinessUnit { get; set; }
        public string senderEmail { get; set; }
        public int senderSMTP { get; set; }
        public List<Connection> Connections { get; set; }
    }

    [Owned]
    public partial class Connection
    {
        public int idCompany { get; set; }
        public int idConnection { get; set; }
        public string ConnectionName { get; set; }
        public string ConnectionString { get; set; }
        public bool Default { get; set; }
    }
}
