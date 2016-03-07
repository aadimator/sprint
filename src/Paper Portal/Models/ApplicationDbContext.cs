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
            builder.Entity<Paper>()
                .HasOne(p => p.Uploader)
                .WithMany(u => u.Papers)
                //.HasForeignKey(p => p.UploaderId)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Paper>()
                .HasOne(p => p.Downloader)
                .WithMany(u => u.Papers)
                .HasForeignKey(p => p.DownloaderId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        }
        public DbSet<Paper> Paper { get; set; }
    }
}
