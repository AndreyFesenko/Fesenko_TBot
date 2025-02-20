using Fesenko_TBot.Models;
using Microsoft.EntityFrameworkCore;

namespace Fesenko_TBot
{
    public class OctopusDbContext : DbContext
    {
        public DbSet<Incident> Incident { get; set; }
        public DbSet<Engineer> Engineer { get; set; }
        public DbSet<ATM> ATM { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(Environment.GetEnvironmentVariable("Connection", EnvironmentVariableTarget.User));
        }
    }
}
