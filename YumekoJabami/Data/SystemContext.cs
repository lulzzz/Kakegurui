using YumekoJabami.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace YumekoJabami.Data
{
    /// <summary>
    /// 系统数据库
    /// </summary>
    public class SystemContext : IdentityDbContext<IdentityUser>
    {
        public SystemContext(DbContextOptions<SystemContext> options)
            : base(options)
        {

        }

        public DbSet<TrafficClaim> TrafficClaims { get; set; }

        public DbSet<TrafficCode> Codes { get; set; }

        public DbSet<TrafficParameter> Parameters { get; set; }

        public DbSet<TrafficVersion> Version { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<IdentityUser>()
                .ToTable("System_User");
            builder.Entity<IdentityUser>()
                .Property(u => u.EmailConfirmed)
                .HasColumnType("TINYINT");
            builder.Entity<IdentityUser>()
                .Property(u => u.LockoutEnabled)
                .HasColumnType("TINYINT");
            builder.Entity<IdentityUser>()
                .Property(u => u.PhoneNumberConfirmed)
                .HasColumnType("TINYINT");
            builder.Entity<IdentityUser>()
                .Property(u => u.TwoFactorEnabled)
                .HasColumnType("TINYINT");
            builder.Entity<IdentityUser>()
                .Property(c => c.Id)
                .HasColumnType("VARCHAR(100)");
            builder.Entity<IdentityUser>()
                .Property(u => u.UserName)
                .HasColumnType("VARCHAR(100)");
            builder.Entity<IdentityUser>()
                .Property(u => u.NormalizedUserName)
                .HasColumnType("VARCHAR(100)");
            builder.Entity<IdentityUser>()
                .Property(u => u.Email)
                .HasColumnType("VARCHAR(100)");
            builder.Entity<IdentityUser>()
                .Property(u => u.NormalizedEmail)
                .HasColumnType("VARCHAR(100)");
            builder.Entity<IdentityUser>()
                .Property(u => u.PasswordHash)
                .HasColumnType("VARCHAR(100)");
            builder.Entity<IdentityUser>()
                .Property(u => u.SecurityStamp)
                .HasColumnType("VARCHAR(100)");
            builder.Entity<IdentityUser>()
                .Property(u => u.ConcurrencyStamp)
                .HasColumnType("VARCHAR(100)");
            builder.Entity<IdentityUser>()
                .Property(u => u.PhoneNumber)
                .HasColumnType("VARCHAR(100)");

            builder.Entity<IdentityRole>()
                .ToTable("System_Role");
            builder.Entity<IdentityRole>()
                .Property(c => c.Id)
                .HasColumnType("VARCHAR(100)");
            builder.Entity<IdentityRole>()
                .Property(c => c.Name)
                .HasColumnType("VARCHAR(100)");
            builder.Entity<IdentityRole>()
                .Property(c => c.NormalizedName)
                .HasColumnType("VARCHAR(100)");
            builder.Entity<IdentityRole>()
                .Property(c => c.ConcurrencyStamp)
                .HasColumnType("VARCHAR(100)");

            builder.Entity<IdentityRoleClaim<string>>()
                .ToTable("System_Role_Claim");
            builder.Entity<IdentityRoleClaim<string>>()
                .Property(c => c.RoleId)
                .HasColumnType("VARCHAR(100)");
            builder.Entity<IdentityRoleClaim<string>>()
                .Property(c => c.ClaimType)
                .HasColumnType("VARCHAR(100)");
            builder.Entity<IdentityRoleClaim<string>>()
                .Property(c => c.ClaimValue)
                .HasColumnType("VARCHAR(100)");

            builder.Entity<IdentityUserRole<string>>()
                .ToTable("System_User_Role");
            builder.Entity<IdentityUserRole<string>>()
                .Property(c => c.UserId)
                .HasColumnType("VARCHAR(100)");
            builder.Entity<IdentityUserRole<string>>()
                .Property(c => c.RoleId)
                .HasColumnType("VARCHAR(100)");

            builder.Entity<IdentityUserClaim<string>>()
                .ToTable("System_User_Claim");
            builder.Entity<IdentityUserClaim<string>>()
                .Property(c => c.UserId)
                .HasColumnType("VARCHAR(100)");
            builder.Entity<IdentityUserClaim<string>>()
                .Property(c => c.ClaimValue)
                .HasColumnType("VARCHAR(100)");
            builder.Entity<IdentityUserClaim<string>>()
                .Property(c => c.ClaimType)
                .HasColumnType("VARCHAR(100)");

            builder.Entity<IdentityUserLogin<string>>()
                .ToTable("System_User_Login");
            builder.Entity<IdentityUserLogin<string>>()
                .Property(c => c.UserId)
                .HasColumnType("VARCHAR(100)");
            builder.Entity<IdentityUserLogin<string>>()
                .Property(c => c.ProviderKey)
                .HasColumnType("VARCHAR(100)");
            builder.Entity<IdentityUserLogin<string>>()
                .Property(c => c.LoginProvider)
                .HasColumnType("VARCHAR(100)");
            builder.Entity<IdentityUserLogin<string>>()
                .Property(c => c.ProviderDisplayName)
                .HasColumnType("VARCHAR(100)");

            builder.Entity<IdentityUserToken<string>>()
                .ToTable("System_User_Token");
            builder.Entity<IdentityUserToken<string>>()
                .Property(c => c.UserId)
                .HasColumnType("VARCHAR(100)");
            builder.Entity<IdentityUserToken<string>>()
                .Property(c => c.LoginProvider)
                .HasColumnType("VARCHAR(100)");
            builder.Entity<IdentityUserToken<string>>()
                .Property(c => c.Name)
                .HasColumnType("VARCHAR(100)");
            builder.Entity<IdentityUserToken<string>>()
                .Property(c => c.Value)
                .HasColumnType("VARCHAR(100)");

            builder.Entity<TrafficClaim>()
                .ToTable("System_Claim")
                .HasKey(c => new { c.Type, c.Value });

            builder.Entity<TrafficCode>()
                .ToTable("System_Code")
                .HasKey(c => new { c.System, c.Key, c.Value });

            builder.Entity<TrafficParameter>()
                .ToTable("System_Parameter")
                .HasKey(p => new { p.Type, p.Key });

            builder.Entity<TrafficVersion>()
                .ToTable("System_Version")
                .HasKey(p => p.Version);
        }
    }
}
