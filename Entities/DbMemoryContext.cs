using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Entities.Contexts
{
    public class DbMemoryContext : DbContext
    {
        public DbSet<BecaUser> Users { get; set; }
        public DbSet<Company> Companies { get; set; }

        private readonly IConfiguration Configuration;

        public DbMemoryContext(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            // in memory database used for simplicity, change to a real db for production applications
            options.UseInMemoryDatabase("MemoryDb");
        }
    }
}