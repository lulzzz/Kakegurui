using System;
using System.Linq;
using ItsukiSumeragi.Models;
using Kakegurui.Core;
using Microsoft.EntityFrameworkCore;
using MomobamiKirari.Models;
using MomobamiKirari.Codes;
using YumekoJabami.Data;

namespace MomobamiKirari.Data
{
    /// <summary>
    /// 交通流量数据库
    /// </summary>
    public class FlowContext : SystemContext
    {
        private const string FlowOneTable = "Flow_Lane_One";
        private const string FlowFiveTable = "Flow_Lane_Five";
        private const string FlowFifteenTable = "Flow_Lane_Fifteen";
        private const string FlowHourTable = "Flow_Lane_Hour";

        private const string VehicleTable = "Flow_Vehicle";
        private const string BikeTable = "Flow_Bike";
        private const string PedestrainTable = "Flow_Pedestrain";

        public DbSet<RoadCrossing> RoadCrossings { get; set; }
        public DbSet<RoadSection> RoadSections { get; set; }
        public DbSet<FlowDevice> Devices { get; set; }
        public DbSet<FlowDevice_FlowChannel> Device_Channels { get; set; }
        public DbSet<FlowChannel> Channels { get; set; }
        public DbSet<Lane> Lanes { get; set; }

        public DbSet<LaneFlow_One> LaneFlows_One { get; set; }
        public DbSet<LaneFlow_Five> LaneFlows_Five { get; set; }
        public DbSet<LaneFlow_Fifteen> LaneFlows_Fifteen { get; set; }
        public DbSet<LaneFlow_Hour> LaneFlows_Hour { get; set; }
        public DbSet<SectionStatus> SectionStatuses { get; set; }

        public DbSet<VideoVehicle> Vehicles { get; set; }
        public DbSet<VideoBike> Bikes { get; set; }
        public DbSet<VideoPedestrain> Pedestrains { get; set; }

        public DbSet<TrafficVersion> Version { get; set; }

        public FlowContext(DbContextOptions<FlowContext> options)
            : base(options)
        {
            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<FlowDevice>()
                .ToTable("Flow_Device")
                .HasKey(d => d.DeviceId);
            modelBuilder.Entity<FlowDevice>()
                .HasIndex(d => d.Ip)
                .IsUnique();
            modelBuilder.Entity<FlowDevice>()
                .HasMany(d => d.FlowDevice_FlowChannels)
                .WithOne(r => r.Device);

            modelBuilder.Entity<FlowDevice_FlowChannel>()
                .ToTable("Flow_Device_Channel")
                .HasKey(r => new { r.DeviceId, r.ChannelId })
                .HasName("PK_Device_Channel");
            modelBuilder.Entity<FlowDevice_FlowChannel>()
                .HasOne(r => r.Channel)
                .WithOne(c => c.FlowDevice_FlowChannel)
                .HasConstraintName("FK_Device_Channel");

            modelBuilder.Entity<FlowChannel>()
                .ToTable("Flow_Channel")
                .HasKey(c => c.ChannelId);

            modelBuilder.Entity<FlowChannel>()
                .HasMany(c => c.Lanes)
                .WithOne(l => l.Channel);
            modelBuilder.Entity<FlowChannel>()
                .HasOne(c => c.RoadCrossing)
                .WithMany(c => c.Channels);
            modelBuilder.Entity<FlowChannel>()
                .HasOne(c => c.RoadSection)
                .WithMany(s => s.Channels);

            modelBuilder.Entity<RoadCrossing>()
                .ToTable("Flow_RoadCrossing")
                .HasKey(r => r.CrossingId);

            modelBuilder.Entity<RoadSection>()
                .ToTable("Flow_RoadSection")
                .HasKey(r => r.SectionId);

            modelBuilder.Entity<Lane>()
                .ToTable("Flow_Lane")
                .HasKey(c => new { c.ChannelId, c.LaneId });

            modelBuilder.Entity<LaneFlow_One>()
                .ToTable(FlowOneTable)
                .HasKey(f => f.Id);
            modelBuilder.Entity<LaneFlow_One>()
                .HasIndex(f => new { f.DataId, f.DateTime});

            modelBuilder.Entity<LaneFlow_Five>()
                .ToTable(FlowFiveTable)
                .HasKey(f => f.Id);
            modelBuilder.Entity<LaneFlow_Five>()
                .HasIndex(f => new { f.DataId, f.DateTime });

            modelBuilder.Entity<LaneFlow_Fifteen>()
                .ToTable(FlowFifteenTable)
                .HasKey(f => f.Id);
            modelBuilder.Entity<LaneFlow_Fifteen>()
                .HasIndex(f => new { f.DataId, f.DateTime });

            modelBuilder.Entity<LaneFlow_Hour>()
                .ToTable(FlowHourTable)
                .HasKey(f => f.Id);
            modelBuilder.Entity<LaneFlow_Hour>()
                .HasIndex(f => new { f.DataId, f.DateTime });

            modelBuilder.Entity<SectionStatus>()
                .ToTable("Flow_Section_Hour")
                .HasKey(d => new { d.SectionId, d.DateTime });

            modelBuilder.Entity<VideoVehicle>()
                .ToTable(VehicleTable)
                .HasKey(v => v.Id);
            modelBuilder.Entity<VideoVehicle>()
                .HasIndex(v => new { v.DateTime, v.DataId });

            modelBuilder.Entity<VideoBike>()
                .ToTable(BikeTable)
                .HasKey(v => v.Id);
            modelBuilder.Entity<VideoBike>()
                .HasIndex(v => new { v.DateTime, v.DataId });

            modelBuilder.Entity<VideoPedestrain>()
                .ToTable(PedestrainTable)
                .HasKey(v => v.Id);
            modelBuilder.Entity<VideoPedestrain>()
                .HasIndex(v => new { v.DateTime, v.DataId });

            modelBuilder.Entity<TrafficVersion>()
                .ToTable("Flow_Version")
                .HasKey(p => p.Version);
        }

        /// <summary>
        /// 根据时间级别获取数据源和表名
        /// </summary>
        /// <param name="dateLevel">时间级别</param>
        /// <returns>第一个参数表示数据源，第二个参数表示是否接收分表，null表示不分表</returns>
        public Tuple<IQueryable<LaneFlow>, string> Queryable(DateTimeLevel dateLevel)
        {
            if (dateLevel == DateTimeLevel.Minute)
            {
                return new Tuple<IQueryable<LaneFlow>, string>(LaneFlows_One.AsNoTracking(), FlowOneTable);
            }
            else if (dateLevel == DateTimeLevel.FiveMinutes)
            {
                return new Tuple<IQueryable<LaneFlow>, string>(LaneFlows_Five.AsNoTracking(), FlowFiveTable);

            }
            else if (dateLevel == DateTimeLevel.FifteenMinutes)
            {
                return new Tuple<IQueryable<LaneFlow>, string>(LaneFlows_Fifteen.AsNoTracking(), FlowFifteenTable);

            }
            else if (dateLevel == DateTimeLevel.Hour
                     || dateLevel == DateTimeLevel.Day
                     || dateLevel == DateTimeLevel.Month)
            {
                return new Tuple<IQueryable<LaneFlow>, string>(LaneFlows_Hour.AsNoTracking(), FlowHourTable);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 根据时间级别获取数据源和表名
        /// </summary>
        /// <param name="type">视频结构化数据类型</param>
        /// <returns>第一个参数表示数据源，第二个参数表示是否接收分表，null表示不分表</returns>
        public Tuple<IQueryable<VideoStruct>, string> Queryable(VideoStructType type)
        {
            if (type == VideoStructType.机动车)
            {
                return new Tuple<IQueryable<VideoStruct>, string>(Vehicles.AsNoTracking(), VehicleTable);
            }
            else if (type == VideoStructType.非机动车)
            {
                return new Tuple<IQueryable<VideoStruct>, string>(Bikes.AsNoTracking(), BikeTable);
            }
            else
            {
                return new Tuple<IQueryable<VideoStruct>, string>(Pedestrains.AsNoTracking(), PedestrainTable);
            }
        }
        /// <summary>
        /// 切换数据库
        /// </summary>
        /// <param name="name">旧数据库名</param>
        public void ChangeDatabase(string name)
        {
            string str = $"ALTER TABLE {FlowOneTable} RENAME TO {FlowOneTable}_{name}";
            Database.ExecuteSqlCommand(str);
            str = $"ALTER TABLE {FlowFiveTable} RENAME TO {FlowFiveTable}_{name};";
            Database.ExecuteSqlCommand(str);
            str = $"ALTER TABLE {FlowFifteenTable} RENAME TO {FlowFifteenTable}_{name};";
            Database.ExecuteSqlCommand(str);
            str = $"ALTER TABLE {FlowHourTable} RENAME TO {FlowHourTable}_{name};";
            Database.ExecuteSqlCommand(str);
            string format = @"CREATE TABLE `{0}` (
                      `Id` int(11) NOT NULL AUTO_INCREMENT,
                      `DataId` varchar(100) NOT NULL,
                      `DateTime` datetime NOT NULL,
                      `Cars` int(11) NOT NULL,
                      `Buss` int(11) NOT NULL,
                      `Trucks` int(11) NOT NULL,
                      `Vans` int(11) NOT NULL,
                      `Tricycles` int(11) NOT NULL,
                      `Motorcycles` int(11) NOT NULL,
                      `Bikes` int(11) NOT NULL,
                      `Persons` int(11) NOT NULL,
                      `Distance` int(11) NOT NULL,
                      `TravelTime` double NOT NULL,
                      `HeadDistance` double NOT NULL,
                      `TimeOccupancy` int(11) NOT NULL,
                      `Occupancy` int(11) NOT NULL,
                      `Count` int(11) NOT NULL,
                      PRIMARY KEY (`Id`),
                      KEY `IX_Flow_Lane_DataId_DateTime` (`DataId`,`DateTime`)
                    ) ENGINE=InnoDB AUTO_INCREMENT=15346 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci";
            Database.ExecuteSqlCommand(string.Format(format, FlowOneTable));
            Database.ExecuteSqlCommand(string.Format(format, FlowFiveTable));
            Database.ExecuteSqlCommand(string.Format(format, FlowFifteenTable));
            Database.ExecuteSqlCommand(string.Format(format, FlowHourTable));
        }

        /// <summary>
        /// 修改机动车表
        /// </summary>
        /// <param name="name">旧数据库名</param>
        public void ChangeVehicleTable(string name)
        {
            string str = $"ALTER TABLE {VehicleTable} RENAME TO {VehicleTable}_{name}";
            Database.ExecuteSqlCommand(str);
            Database.ExecuteSqlCommand(
                @"CREATE TABLE `" + VehicleTable + @"` (
                    `Id` int(11) NOT NULL AUTO_INCREMENT,
                    `DataId` varchar(100) NOT NULL,
                    `DateTime` datetime NOT NULL,
                    `Image` mediumtext NOT NULL,
                    `Feature` text NOT NULL,
                    `CountIndex` int(11) NOT NULL,
                    `CarType` int(11) NOT NULL,
                    `CarBrand` varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
                    `CarColor` int(11) NOT NULL,
                    `PlateNumber` varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
                    `PlateType` int(11) NOT NULL,
                    PRIMARY KEY (`Id`),
                    KEY `IX_Flow_Vehicle_DateTime_DataId` (`DateTime`,`DataId`)
                  ) ENGINE=InnoDB AUTO_INCREMENT=6296024 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci");
        }

        /// <summary>
        /// 修改非机动车表
        /// </summary>
        /// <param name="name">旧数据库名</param>
        public void ChangeBikeTable(string name)
        {
            string str = $"ALTER TABLE {BikeTable} RENAME TO {BikeTable}_{name}";
            Database.ExecuteSqlCommand(str);
            Database.ExecuteSqlCommand(
                @"CREATE TABLE `" + BikeTable + @"` (
                    `Id` int(11) NOT NULL AUTO_INCREMENT,
                    `DataId` varchar(100) NOT NULL,
                    `DateTime` datetime NOT NULL,
                    `Image` mediumtext NOT NULL,
                    `Feature` text NOT NULL,
                    `CountIndex` int(11) NOT NULL,
                    `BikeType` int(11) NOT NULL,
                    PRIMARY KEY (`Id`),
                    KEY `IX_Flow_Bike_DateTime_DataId` (`DateTime`,`DataId`)
                  ) ENGINE=InnoDB AUTO_INCREMENT=4041638 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci");
        }

        /// <summary>
        /// 修改行人表
        /// </summary>
        /// <param name="name">旧数据库名</param>
        public void ChangePedestrainTable(string name)
        {
            string str = $"ALTER TABLE {PedestrainTable} RENAME TO {PedestrainTable}_{name}";
            Database.ExecuteSqlCommand(str);
            Database.ExecuteSqlCommand(
                @"CREATE TABLE `" + PedestrainTable + @"` (
                   `Id` int(11) NOT NULL AUTO_INCREMENT,
                   `DataId` varchar(100) NOT NULL,
                   `DateTime` datetime NOT NULL,
                   `Image` mediumtext NOT NULL,
                   `Feature` text NOT NULL,
                   `CountIndex` int(11) NOT NULL,
                   `Sex` int(11) NOT NULL,
                   `Age` int(11) NOT NULL,
                   `UpperColor` int(11) NOT NULL,
                   PRIMARY KEY (`Id`),
                   KEY `IX_Flow_Pedestrain_DateTime_DataId` (`DateTime`,`DataId`)
                 ) ENGINE=InnoDB AUTO_INCREMENT=3734558 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci");
        }
    }

}
