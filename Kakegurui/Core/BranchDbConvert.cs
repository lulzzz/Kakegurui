using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Kakegurui.Core
{
    /// <summary>
    /// 分表处理
    /// </summary>
    public static class BranchDbConvert
    {
        /// <summary>
        /// 数据库连接字符串格式
        /// </summary>
        public const string DbFormat = "server={0};port={1};user={2};password={3};database={4};CharSet=utf8";

        /// <summary>
        /// 分表的时间级别
        /// </summary>
        public static DateTimeLevel DateLevel { get; set; } = DateTimeLevel.Month;

        /// <summary>
        /// 获取时间点的表名
        /// </summary>
        /// <param name="baseTimePoint">基准时间点</param>
        /// <returns>时间点的表名</returns>
        public static string GetTableName(DateTime baseTimePoint)
        {
            return baseTimePoint.ToString("yyyyMM");
        }

        /// <summary>
        /// 获取时间点的sql
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="baseTimePoint">基准时间点</param>
        /// <returns>时间点的sql</returns>
        private static string GetSql(string tableName, DateTime baseTimePoint)
        {
            return $"SELECT * FROM {tableName}_{GetTableName(baseTimePoint)}";
        }

        /// <summary>
        /// 根据起止时间获取是否需要分表查询
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="queryable">数据源和表名</param>
        /// <returns>分表后的数据源集合</returns>
        public static List<IQueryable<T>> GetQuerables<T>(DateTime startTime, DateTime endTime, Tuple<IQueryable<T>, string> queryable)
            where T:class
        {
            if (string.IsNullOrEmpty(queryable.Item2))
            {
                return new List<IQueryable<T>>
                {
                    queryable.Item1
                };
            }
            DateTime currentTimePoint = TimePointConvert.CurrentTimePoint(DateTimeLevel.Month);
            DateTime startTimePoint = TimePointConvert.CurrentTimePoint(DateTimeLevel.Month, startTime);
            DateTime endTimePoint = TimePointConvert.CurrentTimePoint(DateTimeLevel.Month, endTime);

            List<IQueryable<T>> list = new List<IQueryable<T>>();
            for (DateTime date = startTimePoint; date <= endTimePoint; date = TimePointConvert.NextTimePoint(DateTimeLevel.Month, date))
            {
                list.Add(date == currentTimePoint ? queryable.Item1 : queryable.Item1.FromSql(GetSql(queryable.Item2, date)));
            }

            return list;
        }
    }
}
