using Microsoft.EntityFrameworkCore;
using NishinotouinYuriko.Models;
using YumekoJabami.Models;

namespace NishinotouinYuriko.Data
{
    /// <summary>
    /// 违法行为数据库
    /// </summary>
    public class ViolationContext : DbContext
    {

        public DbSet<ViolationStruct> Violations { get; set; }

        public DbSet<TrafficVersion> Version { get; set; }

        public ViolationContext(DbContextOptions<ViolationContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ViolationStruct>()
                .ToTable("Violation_Violation")
                .HasKey(v=>v.Id);
            modelBuilder.Entity<ViolationStruct>()
                .HasIndex(v => new { v.DateTime, v.DataId });


            modelBuilder.Entity<TrafficVersion>()
                .ToTable("Violation_Version")
                .HasKey(p => p.Version);
        }

    }

}
