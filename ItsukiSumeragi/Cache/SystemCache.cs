using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using YumekoJabami.Models;

namespace ItsukiSumeragi.Cache
{
    public static class SystemCache
    {
        /// <summary>
        /// 初始化系统缓存
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="codes">字典集合</param>
        public static void InitSystemCache(this IMemoryCache memoryCache, List<Code> codes)
        {
            List<Code> oldCodes=memoryCache.Get<List<Code>>(GetCodesKey());
            if (oldCodes != null)
            {
                foreach (Code oldCode in oldCodes)
                {
                    memoryCache.Remove(GetCodeKey(oldCode.Key, oldCode.Value));
                }
            }

            memoryCache.Set(GetCodesKey(), codes);

            foreach (Code newCode in codes)
            {
                memoryCache.Set(GetCodeKey(newCode.Key, newCode.Value), newCode);
            }
        }

        /// <summary>
        /// 获取字典缓存
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="type">枚举类型</param>
        /// <param name="value">字典值</param>
        /// <returns>字典</returns>
        public static string GetCode(this IMemoryCache memoryCache, Type type, int value)
        {
            Code code = memoryCache.Get<Code>(GetCodeKey(type.Name, value));
            return code?.Description;
        }

        /// <summary>
        /// 获取字典键
        /// </summary>
        /// <param name="key">字典键</param>
        /// <param name="value">字典值</param>
        /// <returns>字典键</returns>
        private static string GetCodeKey( string key, int value)
        {
            return $"code_{key}_{value}";
        }

        /// <summary>
        /// 获取字典集合
        /// </summary>
        /// <param name="memoryCache">缓存</param>
        /// <param name="type">枚举类型</param>
        /// <returns>字典集合</returns>
        public static List<Code> GetCodes(this IMemoryCache memoryCache, Type type)
        {
            return memoryCache.TryGetValue(GetCodesKey(), out List<Code> codes)
                ? codes.Where(c=>c.Key==type.Name).ToList()
                : new List<Code>();
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
