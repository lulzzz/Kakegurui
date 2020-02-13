using System.Linq;
using ItsukiSumeragi.Cache;
using ItsukiSumeragi.Data;
using ItsukiSumeragi.Models;
using Kakegurui.WebExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ItsukiSumeragi.Controller
{
    /// <summary>
    /// 违法标签
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class TagsController:ControllerBase
    {
        /// <summary>
        /// 数据库实例
        /// </summary>
        private readonly DeviceContext _context;

        /// <summary>
        /// 缓存
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">数据库实例</param>
        /// <param name="memoryCache">缓存</param>
        public TagsController(DeviceContext context,IMemoryCache memoryCache)
        {
            _context = context;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// 查询标签集合
        /// </summary>
        /// <param name="pageNum">页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <returns>查询结果</returns>
        [HttpGet]
        public PageModel<TrafficTag> GetTags([FromQuery] int pageNum, [FromQuery] int pageSize)
        {
            var model= _context.Tags
                .Select(t=>_memoryCache.FillTag(t))
                .Page(pageNum, pageSize);
            return model;
        }

        /// <summary>
        /// 查询标签
        /// </summary>
        /// <param name="tagName">标签名称</param>
        /// <returns>查询结果</returns>
        [HttpGet("{tagName}")]
        public IActionResult GetTag([FromRoute] string tagName)
        {
            TrafficTag tag = _context.Tags
                .Select(t => _memoryCache.FillTag(t))
                .SingleOrDefault(t => t.TagName == tagName);
            if (tag == null)
            {
                return NotFound();
            }
            else
            {
                return Ok(tag);
            }
        }

        /// <summary>
        /// 更新标签
        /// </summary>
        /// <param name="tag">标签</param>
        /// <returns>更新结果</returns>
        [HttpPut]
        public IActionResult PutTag([FromBody] TrafficTag tag)
        {
            _context.Entry(tag).State = EntityState.Modified;
            _context.Entry(tag).Property(t => t.TagName).IsModified = false;
            _context.Entry(tag).Property(t => t.TagType).IsModified = false;
            _context.Entry(tag).Property(t => t.EnglishName).IsModified = false;

            try
            {
                _context.SaveChanges();
                return Ok();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (_context.Tags.Count(t => t.TagName == tag.TagName) == 0)
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

    }
}
