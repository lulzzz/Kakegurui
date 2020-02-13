using System;

namespace Kakegurui.Core
{
    /// <summary>
    /// 日期级别
    /// </summary>
    public enum DateTimeLevel
    {
        Minute=1,
        FiveMinutes=2,
        FifteenMinutes=3,
        Hour=4,
        Day=5,
        Month=6,
        Season=7,
        Year=8
    }

    /// <summary>
    /// 时间处理类
    /// </summary>
    public static class TimePointConvert
    {
        /// <summary>
        /// 获取当前时间点
        /// </summary>
        /// <param name="level">时间级别</param>
        /// <returns>当前时间点</returns>
        public static DateTime CurrentTimePoint(DateTimeLevel level)
        {
            return CurrentTimePoint(level, DateTime.Now);
        }

        /// <summary>
        /// 获取上一个时间点
        /// </summary>
        /// <param name="level">时间级别</param>
        /// <param name="dateTime">时间点</param>
        /// <returns>下一个时间点</returns>
        public static DateTime PreTimePoint(DateTimeLevel level, DateTime dateTime)
        {
            if (level == DateTimeLevel.Minute)
            {
                return dateTime.AddMinutes(-1);
            }
            else if (level == DateTimeLevel.FiveMinutes)
            {
                return dateTime.AddMinutes(-5);
            }
            else if (level == DateTimeLevel.FifteenMinutes)
            {
                return dateTime.AddMinutes(-15);
            }
            else if (level == DateTimeLevel.Hour)
            {
                return dateTime.AddHours(-1);
            }
            else if (level == DateTimeLevel.Day)
            {
                return dateTime.AddDays(-1);
            }
            else if (level == DateTimeLevel.Month)
            {
                return dateTime.AddMonths(-1);
            }
            else if (level == DateTimeLevel.Season)
            {
                return dateTime.AddMonths(-3);
            }
            else if (level == DateTimeLevel.Year)
            {
                return dateTime.AddYears(-1);
            }
            else
            {
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// 获取当前基准时间点
        /// </summary>
        /// <param name="level">时间级别</param>
        /// <param name="dateTime">时间点</param>
        /// <returns>当前基准时间点</returns>
        public static DateTime CurrentTimePoint(DateTimeLevel level,DateTime dateTime)
        {
            if (level == DateTimeLevel.Minute)
            {
                return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0);
            }
            else if (level == DateTimeLevel.FiveMinutes)
            {
                int minutes = dateTime.Minute / 5 * 5;
                return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, minutes, 0);
            }
            else if (level == DateTimeLevel.FifteenMinutes)
            {
                int minutes = dateTime.Minute / 15 * 15;
                return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, minutes, 0);
            }
            else if (level == DateTimeLevel.Hour)
            {
                return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0);
            }
            else if (level == DateTimeLevel.Day)
            {
                return dateTime.Date;
            }
            else if (level == DateTimeLevel.Month)
            {
                return new DateTime(dateTime.Year, dateTime.Month, 1);
            }
            else if(level==DateTimeLevel.Year)
            {
                return new DateTime(dateTime.Year, 1, 1);
            }
            else if (level == DateTimeLevel.Season)
            {
                return new DateTime(dateTime.Year, (dateTime.Month-1)/3*3+1, 1);
            }
            else
            {
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// 获取下一个时间点
        /// </summary>
        /// <param name="level">时间级别</param>
        /// <param name="dateTime">时间点</param>
        /// <returns>下一个时间点</returns>
        public static DateTime NextTimePoint(DateTimeLevel level, DateTime dateTime)
        {
            if (level == DateTimeLevel.Minute)
            {
                return dateTime.AddMinutes(1);
            }
            else if (level == DateTimeLevel.FiveMinutes)
            {
                return dateTime.AddMinutes(5);
            }
            else if (level == DateTimeLevel.FifteenMinutes)
            {
                return dateTime.AddMinutes(15);
            }
            else if (level == DateTimeLevel.Hour)
            {
                return dateTime.AddHours(1);
            }
            else if (level == DateTimeLevel.Day)
            {
                return dateTime.AddDays(1);
            }
            else if (level == DateTimeLevel.Month)
            {
                return dateTime.AddMonths(1);
            }
            else if (level == DateTimeLevel.Season)
            {
                return dateTime.AddMonths(3);
            }
            else if(level==DateTimeLevel.Year)
            {
                return dateTime.AddYears(1);
            }
            else
            {
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// 获取时间格式
        /// </summary>
        /// <param name="level">时间级别</param>
        /// <returns>时间格式字符串</returns>
        public static string TimeFormat(DateTimeLevel level)
        {
            if (level == DateTimeLevel.Minute
                || level == DateTimeLevel.FiveMinutes
                || level == DateTimeLevel.FifteenMinutes)
            {
                return "yyyy-MM-dd HH:mm";
            }
            else if (level == DateTimeLevel.Hour)
            {
                return "yyyy-MM-dd HH";
            }
            else if (level == DateTimeLevel.Day)
            {
                return "yyyy-MM-dd";
            }
            else if (level == DateTimeLevel.Month|| level == DateTimeLevel.Season)
            {
                return "yyyy-MM";
            }
            else if(level==DateTimeLevel.Year)
            {
                return "yyyy";
            }
            else
            {
                return null;
            }
        }
    }
}
