// Name & Surname: Luc de Marillac St Julien
// Student Number: ST10382638
// Group: 1

// References:
// https://chatgpt.com/c/690f7551-669c-8329-9b07-041233d40273
// https://www.w3schools.com/cs/index.php
// https://www.w3schools.com/bootstrap5/index.php
// https://www.w3schools.com/js/default.asp

using Microsoft.EntityFrameworkCore;
using ST10382638_CLDV_POE.Models;

namespace ST10382638_CLDV_POE.Data
{
    /// <summary>
    /// Entity Framework Core database context containing DbSets and model configuration.
    /// </summary>
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// Constructor accepting DbContext options and passing to base.
        /// </summary>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DbSets representing tables for the application domain.
        public DbSet<User> User { get; set; }
        public DbSet<Role> Role { get; set; }
        public DbSet<UserRole> UserRole { get; set; }
        public DbSet<Customer> Customer { get; set; }
        public DbSet<Admin> Admin { get; set; }
        public DbSet<CartItem> CartItem { get; set; }

        /// <summary>
        /// Fluent API configuration for entities and keys/constraints.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder b)
        {
            // Call base to ensure default configurations are applied.
            base.OnModelCreating(b);

            // Configure CartItem with composite key and column constraints.
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
//------------------------------------------...ooo000 END OF FILE 000ooo...------------------------------------------------------//
