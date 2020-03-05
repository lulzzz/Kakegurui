using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Kakegurui.Core;
using YumekoJabami.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using Claim = System.Security.Claims.Claim;

namespace YumekoJabami.Controllers
{
    /// <summary>
    /// 帐户
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        /// <summary>
        /// 用户
        /// </summary>
        private readonly UserManager<IdentityUser> _userManager;

        /// <summary>
        /// 登陆
        /// </summary>
        private readonly SignInManager<IdentityUser> _signInManager;

        /// <summary>
        /// 角色
        /// </summary>
        private readonly RoleManager<IdentityRole> _roleManager;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="signInManager">登陆</param>
        /// <param name="userManager">用户</param>
        /// <param name="roleManager">角色</param>
        public AccountController(
            SignInManager<IdentityUser> signInManager, 
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        /// <summary>
        /// 登陆
        /// </summary>
        /// <param name="model">登陆信息</param>
        /// <returns>登陆结果</returns>
        [HttpPost]
        public async Task<IActionResult> Login(Login model)
        {
            IdentityUser identityUser = await _userManager.FindByNameAsync(model.UserName);
            if (identityUser != null)
            {
                Microsoft.AspNetCore.Identity.SignInResult result = await _signInManager.CheckPasswordSignInAsync(identityUser, model.Password, false);
                if (result.Succeeded)
                {
                    //基本信息
                    List<Claim> claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, identityUser.UserName),
                        new Claim(ClaimTypes.Thumbprint, identityUser.SecurityStamp)
                    };
                    //用户权限
                    IList<Claim> userClaims = await _userManager.GetClaimsAsync(identityUser);
                    claims.AddRange(userClaims);
                    //角色权限
                    IList<string> roleNames = await _userManager.GetRolesAsync(identityUser);
                    foreach (string roleName in roleNames)
                    {
                        IdentityRole identityRole = await _roleManager.FindByNameAsync(roleName);
                        IList<Claim> roleClaims = await _roleManager.GetClaimsAsync(identityRole);
                        claims.AddRange(roleClaims);
                    }
                    //去重
                    claims = claims.Distinct((c1, c2) => c1.Type == c2.Type && c1.Value == c2.Value).ToList();

                    return Ok(Token.WriteToken(claims));
                }
            }
            return BadRequest();
        }

        /// <summary>
        /// 注销
        /// </summary>
        /// <returns>注销结果</returns>
        [HttpPost]
        public IActionResult Logout()
        {
            return Ok();
        }

        /// <summary>
        /// 修改密码
        /// </summary>
        /// <param name="model">修改密码信息</param>
        /// <returns>修改结果</returns>
        [HttpPut]
        public async Task<IActionResult> ChangePassword(ChangePassword model)
        {
            IdentityUser user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null)
            {
                return NotFound();
            }
            IdentityResult changePasswordResult = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (changePasswordResult.Succeeded)
            {
                return Ok();
            }
            else
            {
                return BadRequest(changePasswordResult.Errors);
            }
        }
    }
}
