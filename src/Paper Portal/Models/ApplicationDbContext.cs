using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;
using Paper_Portal.Models;

namespace Paper_Portal.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
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

            builder.Entity<Paper>()
                .Property(p => p.DownloadsNum)
                .HasDefaultValue(0);

            builder.Entity<Downloads>()
                .HasOne(d => d.Paper)
                .WithMany(p => p.Downloader);
            builder.Entity<Downloads>()
                .HasOne(d => d.User)
                .WithMany(u => u.Downloads);
        }

        public DbSet<Paper> Paper { get; set; }
        public DbSet<Department> Department { get; set; }
        public DbSet<Downloads> Downloads { get; set; }
        public DbSet<ApplicationUser> ApplicationUser { get; set; }
        public DbSet<Admin> Admin { get; set; }
    }
}
