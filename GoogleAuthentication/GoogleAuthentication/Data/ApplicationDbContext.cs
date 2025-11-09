using GoogleAuthentication.Models;

using Microsoft.EntityFrameworkCore;

namespace GoogleAuthentication.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> opts) : base(opts) { }

        public DbSet<ApplicationUser> Users { get; set; } = default!;
        public DbSet<LoginHistory> LoginHistories { get; set; } = default!;
        public DbSet<Medicine> Medicines { get; set; } = default!;
    }
}
