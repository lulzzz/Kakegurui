using System.Collections.Generic;
using System.Linq;
using YumekoJabami.Data;
using YumekoJabami.Models;

namespace ItsukiSumeragi.Managers
{
    /// <summary>
    /// 字典数据库操作
    /// </summary>
    public class CodesManager
    {
        /// <summary>
        /// 数据库实例
        /// </summary>
        private readonly SystemContext _context;

        /// <summary>
        /// 数据库实例
        /// </summary>
        /// <param name="context">数据库实例</param>
        public CodesManager(SystemContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 按系统查询字典集合
        /// </summary>
        /// <returns>查询结果</returns>
        public List<Code> GetList()
        {
            return _context.Codes.ToList();
        }

    }
}
