using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using YumekoJabami.Data;
using YumekoJabami.Models;

namespace YumekoJabami.Controllers
{
    /// <summary>
    /// 角色
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        /// <summary>
        /// 角色
        /// </summary>
        private readonly RoleManager<IdentityRole> _roleManager;

        /// <summary>
        /// 数据库实例
        /// </summary>
        private readonly SystemContext _context;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="roleManager">角色</param>
        /// <param name="context">数据库实例</param>
        public RolesController(RoleManager<IdentityRole> roleManager, SystemContext context)
        {
            _roleManager = roleManager;
            _context = context;
        }

        /// <summary>
        /// 查询角色集合
        /// </summary>
        /// <returns>查询结果</returns>
        [HttpGet]
        public async Task<IActionResult> GetRoles()
        {
            List<IdentityRole> identityRoles= _roleManager.Roles.ToList();
            List<TrafficRole> roles = new List<TrafficRole>();
            foreach (IdentityRole identityRole in identityRoles)
            {
                IList<Claim> claims = await _roleManager.GetClaimsAsync(identityRole);
                TrafficRole trafficRole = new TrafficRole
                {
                    Name = identityRole.Name,
                    Claims = claims
                        .Select(c => new TrafficClaim { Type = c.Type, Value = c.Value })
                        .ToList()
                };
                roles.Add(trafficRole);
            }
            return Ok(roles);
        }

        /// <summary>
        /// 查询角色
        /// </summary>
        /// <param name="name">角色名</param>
        /// <returns>查询结果</returns>
        [HttpGet("{name}")]
        public async Task<IActionResult> GetRole([FromRoute] string name)
        {
            IdentityRole identityRole = await _roleManager.FindByNameAsync(name);
            if (identityRole == null)
            {
                return NotFound();
            }
            IList<Claim> claims = await _roleManager.GetClaimsAsync(identityRole);
            return Ok(new TrafficRole
            {
                Name = identityRole.Name,
                Claims = claims
                    .Select(c => new TrafficClaim {Type = c.Type, Value = c.Value})
                    .ToList()
            });
        }

        /// <summary>
        /// 添加角色
        /// </summary>
        /// <param name="trafficRole">角色</param>
        /// <returns>添加结果</returns>
        [HttpPost]
        public async Task<IActionResult> PostRole([FromBody] TrafficRole trafficRole)
        {
            IdentityResult result = await _roleManager.CreateAsync(new IdentityRole{Name = trafficRole.Name});
            if (result.Succeeded)
            {
                return Ok(trafficRole);
            }
            else
            {
                if (result.Errors.Any(e => e.Code == "DuplicateRoleName"))
                {
                    return Conflict();
                }
                else
                {
                    return BadRequest(result.Errors);
                }
            }
        }

        /// <summary>
        /// 更新角色权限
        /// </summary>
        /// <param name="trafficRole">角色权限</param>
        /// <returns>更新结果</returns>
        [HttpPut("claims")]
        public async Task<IActionResult> PutRoleClaims(TrafficRole trafficRole)
        {
            IdentityRole identityRole = await _roleManager.FindByNameAsync(trafficRole.Name);
            if (identityRole == null)
            {
                return NotFound();
            }

            IList<Claim> claims = await _roleManager.GetClaimsAsync(identityRole);

            foreach (TrafficClaim claim in trafficRole.Claims)
            {
                if (!claims.Any(c => c.Type == claim.Type && c.Value == claim.Value))
                {
                    await _roleManager.AddClaimAsync(identityRole, new Claim(claim.Type, claim.Value));
                }
            }

            foreach (Claim claim in claims)
            {
                if (!trafficRole.Claims.Any(c => c.Type == claim.Type && c.Value == claim.Value))
                {
                    await _roleManager.RemoveClaimAsync(identityRole, claim);
                }
            }
            return Ok();
        }

        /// <summary>
        /// 删除角色
        /// </summary>
        /// <param name="name">角色名</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{name}")]
        public async Task<IActionResult> DeleteRole([FromRoute] string name)
        {
            IdentityRole role = await _roleManager.FindByNameAsync(name);
            if (role == null)
            {
                return NotFound();
            }

            IdentityResult result=await _roleManager.DeleteAsync(role);
            if (result.Succeeded)
            {
                _context.RemoveRange(_context.RoleClaims.Where(r => r.RoleId == role.Id));
                _context.RemoveRange(_context.UserRoles.Where(r => r.RoleId == role.Id));
                await _context.SaveChangesAsync();
                return Ok(role);
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }
    }
}


