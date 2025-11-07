using Microsoft.EntityFrameworkCore;
using ST10382638_CLDV_POE.Models;

namespace ST10382638_CLDV_POE.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> User { get; set; }
        public DbSet<Role> Role { get; set; }
        public DbSet<UserRole> UserRole { get; set; }
        public DbSet<Customer> Customer { get; set; }

    }
}
