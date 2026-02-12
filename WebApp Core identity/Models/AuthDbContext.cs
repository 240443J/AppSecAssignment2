using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace WebApp_Core_Identity.Model
{
    public class AuthDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly IConfiguration _configuration;

        // Add AuditLog DbSet
        public DbSet<AuditLog> AuditLogs { get; set; }
 
        // Add PasswordHistory DbSet
        public DbSet<PasswordHistory> PasswordHistories { get; set; }

        //public AuthDbContext(DbContextOptions<AuthDbContext> options):base(options){ }
        public AuthDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string connectionString = _configuration.GetConnectionString("AuthConnectionString"); optionsBuilder.UseSqlServer(connectionString);
 }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure AuditLog table
            builder.Entity<AuditLog>(entity =>
     {
              entity.ToTable("AuditLogs");
      entity.HasIndex(e => e.UserId);
 entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.Action);
            });

   // Configure PasswordHistory table
   builder.Entity<PasswordHistory>(entity =>
 {
   entity.ToTable("PasswordHistories");
        entity.HasIndex(e => e.UserId);
   entity.HasIndex(e => e.CreatedDate);
    });
        }
    }
}