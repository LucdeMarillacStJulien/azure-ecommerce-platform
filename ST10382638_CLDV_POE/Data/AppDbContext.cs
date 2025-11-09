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
        public DbSet<Admin> Admin { get; set; }
        public DbSet<CartItem> CartItem { get; set; }

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            b.Entity<CartItem>(e =>
            {
                e.HasKey(x => new { x.UserId, x.ProductId }); // composite key
                e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
                e.Property(x => x.ProductName).HasMaxLength(256);
                e.Property(x => x.ImageUrl).HasMaxLength(1024);
            });
        }
    }
}
