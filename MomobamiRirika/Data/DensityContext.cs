using System;
using System.Linq;
using Kakegurui.Core;
using Microsoft.EntityFrameworkCore;
using MomobamiRirika.Models;
using YumekoJabami.Models;

namespace MomobamiRirika.Data
{
    /// <summary>
    /// 交通密度数据库
    /// </summary>
    public class DensityContext : DbContext
    {
        private const string DensityOneTable = "Density_One";
        private const string DensityFiveTable = "Density_Five";
        private const string DensityFifteenTable = "Density_Fifteen";
        private const string DensityHourTable = "Density_Hour";

        public DbSet<TrafficDensity_One> Densities_One { get; set; }
        public DbSet<TrafficDensity_Five> Densities_Five { get; set; }
        public DbSet<TrafficDensity_Fifteen> Densities_Fifteen { get; set; }
        public DbSet<TrafficDensity_Hour> Densities_hour { get; set; }

        public DbSet<TrafficEvent> Events { get; set; }

        public DbSet<TrafficVersion> Version { get; set; }
        public DensityContext(DbContextOptions<DensityContext> options)
            : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TrafficDensity_One>()
                .ToTable(DensityOneTable)
                .HasKey(d => d.Id);
            modelBuilder.Entity<TrafficDensity_One>()
                .HasIndex(f => new { f.DataId, f.DateTime});

            modelBuilder.Entity<TrafficDensity_Five>()
                .ToTable(DensityFiveTable)
                .HasKey(d => d.Id);
            modelBuilder.Entity<TrafficDensity_Five>()
                .HasIndex(f => new { f.DataId, f.DateTime});

            modelBuilder.Entity<TrafficDensity_Fifteen>()
                .ToTable(DensityFifteenTable)
                .HasKey(d => d.Id);
            modelBuilder.Entity<TrafficDensity_Fifteen>()
                .HasIndex(f => new { f.DataId, f.DateTime});

            modelBuilder.Entity<TrafficDensity_Hour>()
                .ToTable(DensityHourTable)
                .HasKey(d => d.Id);
            modelBuilder.Entity<TrafficDensity_Hour>()
                .HasIndex(f => new { f.DataId, f.DateTime});

            modelBuilder.Entity<TrafficEvent>()
                .ToTable("Density_Event")
                .HasKey(d => d.Id);
            modelBuilder.Entity<TrafficEvent>()
                .HasIndex(f => new { f.DataId, f.DateTime });

            modelBuilder.Entity<TrafficVersion>()
                .ToTable("Density_Version")
                .HasKey(p => p.Version);
        }

        /// <summary>
        /// 根据时间级别获取数据源和表名
        /// </summary>
        /// <param name="dateLevel">时间级别</param>
        /// <returns>第一个参数表示数据源，第二个参数表示是否接收分表，null表示不分表</returns>
        public Tuple<IQueryable<TrafficDensity>, string> Queryable(DateTimeLevel dateLevel)
        {
            if (dateLevel == DateTimeLevel.Minute)
            {
                return new Tuple<IQueryable<TrafficDensity>, string>(Densities_One.AsNoTracking(), DensityOneTable);
            }
            else if (dateLevel == DateTimeLevel.FiveMinutes)
            {
                return new Tuple<IQueryable<TrafficDensity>, string>(Densities_Five.AsNoTracking(), DensityFiveTable);
            }
            else if (dateLevel == DateTimeLevel.FifteenMinutes)
            {
                return new Tuple<IQueryable<TrafficDensity>, string>(Densities_Fifteen.AsNoTracking(), DensityFifteenTable);
            }
            else if (dateLevel == DateTimeLevel.Hour
                     || dateLevel == DateTimeLevel.Day
                     || dateLevel == DateTimeLevel.Month)
            {
                return new Tuple<IQueryable<TrafficDensity>, string>(Densities_hour.AsNoTracking(), DensityHourTable);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 切换数据库
        /// </summary>
        /// <param name="name">旧数据库名</param>
        public void ChangeDatabase(string name)
        {
            string str = $"ALTER TABLE {DensityOneTable} RENAME TO {DensityOneTable}_{name}";
            Database.ExecuteSqlCommand(str);
            str = $"ALTER TABLE {DensityFiveTable} RENAME TO {DensityFiveTable}_{name};";
            Database.ExecuteSqlCommand(str);
            str = $"ALTER TABLE {DensityFifteenTable} RENAME TO {DensityFifteenTable}_{name};";
            Database.ExecuteSqlCommand(str);
            str = $"ALTER TABLE {DensityHourTable} RENAME TO {DensityHourTable}_{name};";
            Database.ExecuteSqlCommand(str);

            string format = @"CREATE TABLE `{0}` (
                            `Id` int(11) NOT NULL AUTO_INCREMENT,
                            `DataId` varchar(100) NOT NULL,
                            `DateTime` datetime NOT NULL,
                            `Value` int(11) NOT NULL,
                            PRIMARY KEY (`Id`),
                            KEY `IX_Density_One_DataId_DateTime` (`DataId`,`DateTime`)
                          ) ENGINE=InnoDB AUTO_INCREMENT=15346 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci";

            Database.ExecuteSqlCommand(string.Format(format, DensityOneTable));
            Database.ExecuteSqlCommand(string.Format(format, DensityFiveTable));
            Database.ExecuteSqlCommand(string.Format(format, DensityFifteenTable));
            Database.ExecuteSqlCommand(string.Format(format, DensityHourTable));
        }
    }

}
