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
