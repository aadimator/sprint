using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Sprint.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
            builder.Entity<ApplicationUser>()
                 .HasOne(u => u.Department)
                 .WithMany(d => d.Users)
                 .HasForeignKey(u => u.DepartmentId)
                 .IsRequired(false)
                 .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Paper>()
                .HasOne(p => p.Uploader)
                .WithMany(u => u.Uploads)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public DbSet<Paper> Paper { get; set; }
        public DbSet<Department> Department { get; set; }
        public DbSet<ApplicationUser> ApplicationUser { get; set; }
    }
}
