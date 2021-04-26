using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Application.Data.Models;
using Application.Dto.Request;
using Application.Dto.Response;
using Application.WebApi.Extensions;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Application.WebApi.Controllers
{
    [Route("/api/v1/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
        private readonly IMapper _mapper;

        public AuthenticationController(UserManager<ApplicationUser> userManager,
            RoleManager<Role> roleManager, Microsoft.Extensions.Configuration.IConfiguration configuration, IMapper mapper)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _mapper = mapper;
        }

        // register new users
        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState.GetErrorMessages());
            try
            {
                var userExists = await _userManager.FindByNameAsync(model.Username);
                if (userExists != null)
                    return StatusCode(StatusCodes.Status500InternalServerError, new AuthResponse { Status = "Error", Message = "User already exists!" });

                ApplicationUser user = new ApplicationUser()
                {
                    Email = model.Email,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    UserName = model.Username,
                    LastName = model.LastName,
                    FirstName = model.FirstName,
                    //Gender = model.Gender,
                    //DateOfBirth = model.DateOfBirth,
                    //ColorTheme = model.ColorTheme
                };
                var result = await _userManager.CreateAsync(user, model.Password);
                if(!result.Succeeded)
                {
                    var message = "";
                    foreach (var error in result.Errors)
                    {
                        message += error.Description + ", ";
                    }

                    return StatusCode(StatusCodes.Status500InternalServerError, new AuthResponse { Status = "Error", Message = message });
                }
                return Ok(new AuthResponse { Status = "Success", Message = "User created successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // authenticate/login user
        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState.GetErrorMessages());
            try
            {
                var user = await _userManager.FindByNameAsync(model.Username);
                if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
                {
                    var userRoles = await _userManager.GetRolesAsync(user);

                    var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

                    foreach (var userRole in userRoles)
                    {
                        authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                    }

                    var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

                    var token = new JwtSecurityToken(
                        issuer: _configuration["JWT:ValidIssuer"],
                        audience: _configuration["JWT:ValidAudience"],
                        expires: DateTime.Now.AddHours(3),
                        claims: authClaims,
                        signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                        );

                    UserProfileDto currentUser = _mapper.Map<ApplicationUser, UserProfileDto>(user);

                    LoginResponse response = new LoginResponse
                    {
                        Token = new JwtSecurityTokenHandler().WriteToken(token),
                        Expiration = token.ValidTo,
                        UserProfile = currentUser
                    };

                    return Ok(response);

                }
                return Unauthorized();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        // get profile by Id
        [HttpGet]
        [Route("{id}")]
        [Authorize]
        public async Task<IActionResult> GetUserProfile(string id)
        {
            if (!ModelState.IsValid)
                return BadRequest("Provided User Id is not a valid string or Guid");
            try
            {
                //var userExists = await userManager.FindByNameAsync(model.Username);
                // add List<string> Roles property to Dto and get roles for users by Id from RoleManager and add it to the result if necessary. 
                var userExists = await _userManager.FindByIdAsync(id);
                if (userExists == null)
                    return StatusCode(StatusCodes.Status404NotFound, new AuthResponse { Status = "Error", Message = "User Not Found!" });
                var userRoles = await _userManager.GetRolesAsync(userExists);
                List<string> roles = new List<string>();
                foreach (var userRole in userRoles)
                {
                    roles.Add(userRole);
                }
                UserProfileDtoWithRoles user = _mapper.Map<ApplicationUser, UserProfileDtoWithRoles>(userExists);
                user.Roles = roles;
                return Ok(user);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Delete user by Id
        [HttpDelete]
        [Route("{id}")]
        [Authorize(Policy = "AllAccessPolicy")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                    return StatusCode(StatusCodes.Status404NotFound, new AuthResponse { Status = "Error", Message = "User Not Found" });
                var delete = await _userManager.DeleteAsync(user);
                return Ok(new AuthResponse {Status = "Success", Message = "User deleted successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //userManager.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).ToList();
        // GET ALL USERS
        [HttpGet]
        [Route("listall")]
        [Authorize(Policy = "AllAccessPolicy")]
        public async Task<IActionResult> ListAll()
        {
            try
            {
                IList<UserProfileDto> result = new List<UserProfileDto>();
                await Task.Run(async () =>
                {
                    var users = _userManager.Users;
                    foreach (ApplicationUser user in users)
                    {
                        var userRoles = await _userManager.GetRolesAsync(user);
                        List<string> roles = new List<string>();
                        foreach (var userRole in userRoles)
                        {
                            roles.Add(userRole);
                        }
                        UserProfileDtoWithRoles convertedResult = _mapper.Map<ApplicationUser, UserProfileDtoWithRoles>(user);
                        convertedResult.Roles = roles;
                        result.Add(convertedResult);
                    }
                });
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        /**
         * Create new Roles
         * @param: string roleName
         * */
        [HttpPost]
        [Route("roles")]
        [Authorize(Policy = "AllAccessPolicy")]
        public async Task<IActionResult> CreateRole(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return BadRequest("Role name should be provided.");
            }

            var newRole = new Role
            {
                Name = roleName
            };

            var roleResult = await _roleManager.CreateAsync(newRole);

            if (roleResult.Succeeded)
            {
                return Ok();
            }

            return Problem(roleResult.Errors.First().Description, null, 500);
        }

        /**
         * Add roles to user
         * @param: List<string> of role names to be added
         */
        [HttpPost]
        [Route("roles/{id}/addroles")]
        [Authorize(Policy = "AllAccessPolicy")]
        public async Task<IActionResult> AddRoles(string id, [FromBody] List<string> addRoles)
        {
            if (!ModelState.IsValid)
                return BadRequest("Provided User Id is not a valid string or Guid");
            try
            {
                var userExists = await _userManager.FindByIdAsync(id);
                if (userExists == null)
                    return StatusCode(StatusCodes.Status404NotFound, new AuthResponse { Status = "Error", Message =  "User Not Found" });
                var updatedRoles = await _userManager.AddToRolesAsync(userExists, addRoles);
                var userRoles = await _userManager.GetRolesAsync(userExists);
                List<string> roles = new List<string>();
                foreach (var userRole in userRoles)
                {
                    roles.Add(userRole);
                }
                UserProfileDtoWithRoles user = _mapper.Map<ApplicationUser, UserProfileDtoWithRoles>(userExists);
                user.Roles = roles;
                return Ok(user);
            }

            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /**
         * Remove roles to user
         * @param: List<string> of role names to be added
         */
        [HttpPost]
        [Route("roles/{id}/removeroles")]
        [Authorize(Policy = "AllAccessPolicy")]
        public async Task<IActionResult> RemoveRoles(string id, [FromBody] List<string> rolesTobeRemoved)
        {
            if (!ModelState.IsValid)
                return BadRequest("Provided User Id is not a valid string or Guid");
            try
            {
                var userExists = await _userManager.FindByIdAsync(id);
                if (userExists == null)
                    return StatusCode(StatusCodes.Status404NotFound, new AuthResponse { Status = "Error", Message =  "User Not Found"});
                var removeRoles = await _userManager.RemoveFromRolesAsync(userExists, rolesTobeRemoved);
                var userRoles = await _userManager.GetRolesAsync(userExists);
                List<string> roles = new List<string>();
                foreach (var userRole in userRoles)
                {
                    roles.Add(userRole);
                }
                UserProfileDtoWithRoles user = _mapper.Map<ApplicationUser, UserProfileDtoWithRoles>(userExists);
                user.Roles = roles;
                return Ok(user);
            }

            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}
