using Microsoft.EntityFrameworkCore;
using Rebloom.Models;

namespace Rebloom.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
    }
}
