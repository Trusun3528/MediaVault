using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PhotoStorage.Models;

namespace PhotoStorage.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Photo> Photos => Set<Photo>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Photo>()
            .HasOne(p => p.User)
            .WithMany(u => u.Photos)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}