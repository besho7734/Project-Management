using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Project_Management.Models;

namespace Project_Management.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) :
            base(options)
        {
        }
        public DbSet<Project> Projects { get; set; }
        public DbSet<task> tasks { get; set; }
        public DbSet<ApplicationUser> applicationUsers { get; set; }
        public DbSet<ChatMessage> chatMessages { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
