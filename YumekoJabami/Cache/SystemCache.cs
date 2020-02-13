using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using YumekoJabami.Codes;
using YumekoJabami.Models;

namespace YumekoJabami.Cache
{
    public static class SystemCache
    {
        /// <summary>
        /// 初始化系统缓存
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="codes">字典集合</param>
        public static void InitSystemCache(this IMemoryCache memoryCache, List<TrafficCode> codes)
        {
            List<TrafficCode> oldCodes=memoryCache.Get<List<TrafficCode>>(GetCodesKey());
            if (oldCodes != null)
            {
                foreach (TrafficCode oldCode in oldCodes)
                {
                    memoryCache.Remove(GetCodeKey(oldCode.System, oldCode.Key, oldCode.Value));
                }
            }

            memoryCache.Set(GetCodesKey(), codes);

            foreach (TrafficCode newCode in codes)
            {
                memoryCache.Set(GetCodeKey(newCode.System, newCode.Key, newCode.Value), newCode);
            }
        }



        /// <summary>
        /// 获取字典缓存
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="system">系统编号</param>
        /// <param name="type">枚举类型</param>
        /// <param name="value">字典值</param>
        /// <returns>字典</returns>
        public static string GetCode(this IMemoryCache memoryCache, SystemType system, Type type, int value)
        {
            TrafficCode code = memoryCache.Get<TrafficCode>(GetCodeKey(system, type.Name, value));
            return code?.Description;
        }

        /// <summary>
        /// 获取字典键
        /// </summary>
        /// <param name="system">系统编号</param>
        /// <param name="key">字典键</param>
        /// <param name="value">字典值</param>
        /// <returns>字典键</returns>
        private static string GetCodeKey(SystemType system, string key, int value)
        {
            return $"code_{(int)system}_{key}_{value}";
        }

        /// <summary>
        /// 获取字典集合
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="system">系统编号</param>
        /// <param name="type">枚举类型</param>
        /// <returns>字典集合</returns>
        public static List<TrafficCode> GetCodes(this IMemoryCache memoryCache, SystemType system, Type type)
        {
            return memoryCache.TryGetValue(GetCodesKey(), out List<TrafficCode> codes)
                ? codes.Where(c=>c.System==system&&c.Key==type.Name).ToList()
                : new List<TrafficCode>();
        }

        /// <summary>
        /// 获取字典集合键
        /// </summary>
        /// <returns>字典集合键</returns>
        private static string GetCodesKey()
        {
            return "code";
        }
    }
}
