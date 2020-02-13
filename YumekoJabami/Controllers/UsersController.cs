using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using YumekoJabami.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using YumekoJabami.Data;

namespace YumekoJabami.Controllers
{
    /// <summary>
    /// 用户
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        /// <summary>
        /// 用户
        /// </summary>
        private readonly UserManager<IdentityUser> _userManager;

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
        /// <param name="userManager">用户</param>
        /// <param name="roleManager">角色</param>
        /// <param name="context">数据库实例</param>
        public UsersController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager,SystemContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        /// <summary>
        /// 查询用户集合
        /// </summary>
        /// <returns>查询结果</returns>
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            List<IdentityUser> identityUsers = _userManager.Users.ToList();
            List<UserRole> users = new List<UserRole>();
            foreach (IdentityUser identityUser in identityUsers)
            {
                UserRole userRoleRoles = new UserRole
                {
                    UserName = identityUser.UserName,
                    Roles = new List<TrafficRole>()
                };
                IList<string> roleNames = await _userManager.GetRolesAsync(identityUser);
                foreach (string roleName in roleNames)
                {
                    IdentityRole identityRole = await _roleManager.FindByNameAsync(roleName);
                    IList<Claim> roleClaims = await _roleManager.GetClaimsAsync(identityRole);
                    userRoleRoles.Roles.Add(new TrafficRole { Name = roleName, Claims = roleClaims.Select(c => new TrafficClaim { Type = c.Type, Value = c.Value }).ToList() });
                }
                users.Add(userRoleRoles);
            }
            return Ok(users);
        }

        /// <summary>
        /// 按用户查询用户角色集合
        /// </summary>
        /// <param name="userName">用户名</param>
        /// <returns>查询结果</returns>
        [HttpGet("{userName}")]
        public async Task<IActionResult> GetUserRoles(string userName)
        {
            IdentityUser identityUser = await _userManager.FindByNameAsync(userName);
            if (identityUser == null)
            {
                return NotFound();
            }

            IList<string> roleNames = await _userManager.GetRolesAsync(identityUser);
            UserRole userRoleRoles = new UserRole
            {
                UserName = userName,
                Roles = new List<TrafficRole>()
            };
            foreach (string roleName in roleNames)
            {
                IdentityRole role = await _roleManager.FindByNameAsync(roleName);
                IList<Claim> roleClaims = await _roleManager.GetClaimsAsync(role);
                userRoleRoles.Roles.Add(new TrafficRole { Name = roleName, Claims = roleClaims.Select(c => new TrafficClaim { Type = c.Type, Value = c.Value }).ToList() });
            }
            return Ok(userRoleRoles);
        }

        /// <summary>
        /// 添加用户
        /// </summary>
        /// <param name="userPassword">用户</param>
        /// <returns>添加结果</returns>
        [HttpPost]
        public async Task<IActionResult> PostUser(UserPassword userPassword)
        {
            IdentityUser identityUser = new IdentityUser { UserName = userPassword.UserName };
            IdentityResult result = await _userManager.CreateAsync(identityUser, userPassword.Password);
            if (result.Succeeded)
            {
                return Ok(userPassword.UserName);
            }
            else
            {
                if (result.Errors.Any(e => e.Code == "DuplicateUserName"))
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
        /// 更新用户
        /// </summary>
        /// <param name="userPassword">用户</param>
        /// <returns>更新结果</returns>
        [HttpPut]
        public async Task<IActionResult> PutUser(UserPassword userPassword)
        {
            IdentityUser identityUser = await _userManager.FindByNameAsync(userPassword.UserName);
            if (identityUser == null)
            {
                return NotFound();
            }
            string code = await _userManager.GeneratePasswordResetTokenAsync(identityUser);
            IdentityResult result = await _userManager.ResetPasswordAsync(identityUser, code, userPassword.Password);
            if (result.Succeeded)
            {
                return Ok();
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        /// <summary>
        /// 更新用户角色
        /// </summary>
        /// <param name="userRoleRoles">用户角色</param>
        /// <returns>更新结果</returns>
        [HttpPut("roles")]
        public async Task<IActionResult> PutUserRoles(UserRole userRoleRoles)
        {
            IdentityUser identityUser = await _userManager.FindByNameAsync(userRoleRoles.UserName);
            if (identityUser == null)
            {
                return NotFound();
            }

            IList<string> roleNames = await _userManager.GetRolesAsync(identityUser);

            foreach (TrafficRole role in userRoleRoles.Roles)
            {
                if (!roleNames.Contains(role.Name))
                {
                    await _userManager.AddToRoleAsync(identityUser, role.Name);
                }
            }

            foreach (string roleName in roleNames)
            {
                if (userRoleRoles.Roles.All(c => c.Name != roleName))
                {
                    await _userManager.RemoveFromRoleAsync(identityUser, roleName);
                }
            }
            return Ok();
        }


        /// <summary>
        /// 删除用户
        /// </summary>
        /// <param name="userName">用户名</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{userName}")]
        public async Task<IActionResult> DeleteUser([FromRoute] string userName)
        {
            if ("admin" == userName)
            {
                return BadRequest();
            }

            IdentityUser user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                return NotFound();
            }

            IdentityResult result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                _context.RemoveRange(_context.UserClaims.Where(u => u.UserId == user.Id));
                _context.RemoveRange(_context.UserRoles.Where(u => u.UserId == user.Id));
                return Ok(userName);
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

    }
}
