using ItsukiSumeragi.Models;
using Microsoft.EntityFrameworkCore;
using YumekoJabami.Models;

namespace ItsukiSumeragi.Data
{
    /// <summary>
    /// 设备和路口信息数据库
    /// </summary>
    public class DeviceContext : DbContext
    {
        public DbSet<TrafficRoadCrossing> RoadCrossings { get; set; }

        public DbSet<TrafficRoadSection> RoadSections { get; set; }

        public DbSet<TrafficLocation> Locations { get; set; }

        public DbSet<TrafficDevice> Devices { get; set; }

        public DbSet<TrafficDevice_TrafficChannel> Device_Channels { get; set; }

        public DbSet<TrafficChannel> Channels { get; set; }

        public DbSet<TrafficChannel_TrafficViolation> Channel_Violations { get; set; }

        public DbSet<TrafficChannel_TrafficViolationParameter> Channel_ViolationParameters { get; set; }

        public DbSet<TrafficLane> Lanes { get; set; }

        public DbSet<TrafficRegion> Regions { get; set; }

        public DbSet<TrafficShape> Shapes { get; set; }

        public DbSet<TrafficTag> Tags { get; set; }

        public DbSet<TrafficViolation> Violations { get; set; }

        public DbSet<TrafficViolation_TrafficTag> Violation_Tags { get; set; }

        public DbSet<TrafficViolation_TrafficViolationParameter> Violation_Parameters { get; set; }

        public DbSet<TrafficViolationParameter> ViolationParameters { get; set; }

        public DbSet<TrafficVersion> Version { get; set; }

        public DeviceContext(DbContextOptions<DeviceContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TrafficDevice>()
                .ToTable("Device_Device")
                .HasKey(d=>d.DeviceId);
            modelBuilder.Entity<TrafficDevice>()
                .HasIndex(d => d.Ip)
                .IsUnique();
            modelBuilder.Entity<TrafficDevice>()
                .HasMany(d => d.Device_Channels)
                .WithOne(r => r.Device);

            modelBuilder.Entity<TrafficDevice_TrafficChannel>()
                .ToTable("Device_Device_Channel")
                .HasKey(r => new { r.DeviceId, r.ChannelId })
                .HasName("PK_Device_Channel");
            modelBuilder.Entity<TrafficDevice_TrafficChannel>()
                .HasOne(r => r.Channel)
                .WithOne(c => c.Device_Channel)
                .HasConstraintName("FK_Device_Channel");

            modelBuilder.Entity<TrafficChannel>()
                .ToTable("Device_Channel")
                .HasKey(c => c.ChannelId );
            modelBuilder.Entity<TrafficChannel>()
                .HasIndex(c => c.ChannelDeviceId)
                .IsUnique();

            modelBuilder.Entity<TrafficChannel>()
                .HasMany(c => c.Lanes)
                .WithOne(l => l.Channel);
            modelBuilder.Entity<TrafficChannel>()
                .HasMany(c => c.Regions)
                .WithOne(r => r.Channel);
            modelBuilder.Entity<TrafficChannel>()
                .HasMany(c => c.Shapes)
                .WithOne(s => s.Channel);
            modelBuilder.Entity<TrafficChannel>()
                .HasOne(c => c.RoadCrossing)
                .WithMany(c => c.Channels);
            modelBuilder.Entity<TrafficChannel>()
                .HasOne(c => c.RoadSection)
                .WithMany(s => s.Channels);
            modelBuilder.Entity<TrafficChannel>()
                .HasOne(c => c.TrafficLocation)
                .WithMany(l => l.Channels);
            modelBuilder.Entity<TrafficChannel>()
                .HasMany(c => c.Channel_Violations)
                .WithOne(v => v.Channel);
            modelBuilder.Entity<TrafficChannel>()
                .HasMany(c => c.Channel_ViolationParameters)
                .WithOne(v => v.Channel);

            modelBuilder.Entity<TrafficChannel_TrafficViolation>()
                .ToTable("Device_Channel_Violation")
                .HasKey(r => new { r.ChannelId, r.ViolationId })
                .HasName("PK_Channel_Violation");
            modelBuilder.Entity<TrafficChannel_TrafficViolation>()
                .HasOne(r => r.Violation)
                .WithMany(v => v.Channel_Violations)
                .HasConstraintName("FK_Channel_Violation");

            modelBuilder.Entity<TrafficChannel_TrafficViolationParameter>()
                .ToTable("Device_Channel_ViolationParameter")
                .HasKey(r => new { r.ChannelId,r.ViolationId, r.Key })
                .HasName("PK_Channel_ViolationParameter");
            modelBuilder.Entity<TrafficChannel_TrafficViolationParameter>()
                .HasOne(r => r.Parameter)
                .WithMany(v => v.Channel_Parameters)
                .HasConstraintName("FK_Channel_ViolationParameter");

            modelBuilder.Entity<TrafficRoadCrossing>()
                .ToTable("Device_RoadCrossing")
                .HasKey(r => r.CrossingId);

            modelBuilder.Entity<TrafficRoadSection>()
                .ToTable("Device_RoadSection")
                .HasKey(r => r.SectionId);

            modelBuilder.Entity<TrafficLocation>()
                .ToTable("Device_Location")
                .HasKey(l => l.LocationId);
            modelBuilder.Entity<TrafficLocation>()
                .HasIndex(l => l.LocationCode)
                .IsUnique();

            modelBuilder.Entity<TrafficLane>()
                .ToTable("Device_Lane")
                .HasKey(c => new { c.ChannelId,c.LaneId });

            modelBuilder.Entity<TrafficRegion>()
                .ToTable("Device_Region")
                .HasKey(r => new { r.ChannelId, r.RegionIndex });

            modelBuilder.Entity<TrafficShape>()
                .ToTable("Device_Shape")
                .HasKey(s => new { s.ChannelId, s.TagName,s.ShapeIndex });

            modelBuilder.Entity<TrafficTag>()
                .ToTable("Device_Tag")
                .HasKey(t => t.TagName);

            modelBuilder.Entity<TrafficViolation>()
                .ToTable("Device_Violation")
                .HasKey(v => v.ViolationId);
            modelBuilder.Entity<TrafficViolation>()
                .HasMany(v => v.Violation_Tags)
                .WithOne(t => t.Violation);
            modelBuilder.Entity<TrafficViolation>()
                .HasMany(v => v.Channel_Violations)
                .WithOne(r => r.Violation);
            modelBuilder.Entity<TrafficViolation>()
                .HasMany(v => v.Violation_Parameters)
                .WithOne(f => f.Violation);

            modelBuilder.Entity<TrafficTag>()
                .ToTable("Device_Tag")
                .HasKey(t => t.TagName);

            modelBuilder.Entity<TrafficViolationParameter>()
                .ToTable("Device_ViolationParameter")
                .HasKey(t => t.Key);

            modelBuilder.Entity<TrafficViolation_TrafficTag>()
                .ToTable("Device_Violation_Tag")
                .HasKey(r => new { r.ViolationId, r.TagName })
                .HasName("PK_Violation_Tag");
            modelBuilder.Entity<TrafficViolation_TrafficTag>()
                .HasOne(r => r.Tag)
                .WithMany(v => v.Violation_Tags)
                .HasConstraintName("FK_Violation_Tag"); 

            modelBuilder.Entity<TrafficViolation_TrafficViolationParameter>()
                .ToTable("Device_Violation_Parameter")
                .HasKey(r => new { r.ViolationId, r.Key })
                .HasName("PK_Violation_Parameter");
            modelBuilder.Entity<TrafficViolation_TrafficViolationParameter>()
                .HasOne(r => r.Parameter)
                .WithMany(p => p.Violation_Parameters)
                .HasConstraintName("FK_Violation_Parameter");

            modelBuilder.Entity<TrafficVersion>()
                .ToTable("Device_Version")
                .HasKey(p => p.Version);
        }

    }
}
