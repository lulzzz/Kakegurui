using System.Collections.Generic;
using System.Linq;

namespace Kakegurui.WebExtensions
{
    /// <summary>
    /// 分页模型
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PageModel<T>
    {
        /// <summary>
        /// 数据集合
        /// </summary>
        public List<T> Datas { get; set; }

        /// <summary>
        /// 数据总数
        /// </summary>
        public int Total { get; set; }
    }

    /// <summary>
    /// 分页查询扩展
    /// </summary>
    public static class QueryablePageExtensions
    {
        /// <summary>
        /// 分页查询
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="queryable">数据源</param>
        /// <param name="pageNum">当前页码,为0时查询所有</param>
        /// <param name="pageSize">每页数量,为0时查询所有</param>
        /// <param name="hasTotal">是否查询总数</param>
        /// <returns>查询结果</returns>
        public static PageModel<T> Page<T>(this IQueryable<T> queryable, int pageNum, int pageSize,bool hasTotal=true)
        {
            return Page(queryable, pageNum, pageSize, 0, true, hasTotal);
        }


        /// <summary>
        /// 分页查询
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="queryable">数据源</param>
        /// <param name="pageNum">当前页码,为0时查询所有</param>
        /// <param name="pageSize">每页数量,为0时查询所有</param>
        /// <param name="skipNum">略过的数量</param>
        /// <param name="hasDatas">是否查询数据集合</param>
        /// <param name="hasTotal">是否查询总数</param>
        /// <returns>查询结果</returns>
        public static PageModel<T> Page<T>(this IQueryable<T> queryable, int pageNum, int pageSize, int skipNum, bool hasDatas, bool hasTotal = true)
        {
            PageModel<T> model = new PageModel<T>();
            if (hasDatas)
            {
                if (pageNum > 0 && pageSize > 0)
                {
                    model.Datas = queryable
                        .Skip((pageNum - 1) * pageSize + skipNum)
                        .Take(pageSize).ToList();
                }
                else
                {
                    model.Datas = queryable.ToList();
                }
            }
            else
            {
                model.Datas=new List<T>();
            }

            if (hasTotal)
            {
                model.Total = queryable.Count();
            }
            return model;
        }
    }
}
