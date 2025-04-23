using BecaWebService.Authorization;
using BecaWebService.Models.Communications;
using BecaWebService.Models.Users;
using BecaWebService.Services;
using Contracts;
using Entities.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BecaWebService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class UsersController(IUserService userService, IHomePageService homePageService, ILoggerManager logger) : ControllerBase
    {
        private readonly IUserService _userService = userService;
        private readonly IHomePageService _homePageService = homePageService;
        private readonly ILoggerManager _logger = logger;

        [AllowAnonymous]
        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate(AuthenticateRequest model)
        {
            try
            {
                var response = await _userService.Authenticate(model, IpAddress(), Request.Headers);
                SetTokenCookie(response.RefreshToken);
                return Ok(response);
            }
            catch (Exception ex)
            {
                string inner = ex.InnerException == null ? "(no further details)" : $" further details: {ex.InnerException.Message}";
                return BadRequest($"{ex.Message} {inner}");
            }
        }

        //[AllowAnonymous]
        [HttpGet("LoginById/{id}")]
        public IActionResult LoginById(int id)
        {
            var response = _userService.LoginById(id, IpAddress());
            SetTokenCookie(response.RefreshToken);
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public IActionResult RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken == null) return BadRequest("Autorizzazione negata");

            var response = _userService.RefreshToken(refreshToken, IpAddress());
            SetTokenCookie(response.RefreshToken);
            return Ok(response);
        }

        [HttpPost("revoke-token")]
        public IActionResult RevokeToken(RevokeTokenRequest model)
        {
            // accept refresh token in request body or cookie
            var token = model.Token ?? Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(token))
                return BadRequest(new { message = "Token is required" });

            _userService.RevokeToken(token, IpAddress());
            return Ok(new { message = "Token revoked" });
        }

        //[HttpGet]
        //public IActionResult GetAll()
        //{
        //    var users = _userService.GetAll();
        //    return Ok(users);
        //}

        //[HttpGet("{id}")]
        //public IActionResult GetById(int id)
        //{
        //    var user = _userService.GetById(id);
        //    return Ok(user);
        //}

        [HttpGet("{id}/refresh-tokens")]
        public IActionResult GetRefreshTokens(int id)
        {
            var user = _userService.GetById(id);
            //return Ok(user.RefreshTokens);
            return Ok(user.UserName);
        }

        [HttpGet("Menu/{id}")]
        public IActionResult GetMenu(int id)
        {
            _logger.LogInfo($"get menu {id}");
            var menu = _userService.GetMenuByUser(id);
            return Ok(menu);
        }

        [HttpGet("MenuProfile")]///{idCompany}/{idProfile}")]
        public IActionResult GetMenuProfile([FromQuery] int idCompany, [FromQuery] int idProfile)
        {
            List<UserMenuItem> menu = _userService.GetMenuByProfile(idProfile, idCompany);
            return Ok(menu);
        }

        [HttpGet("MenuAll")]
        public IActionResult GetMenuAll()
        {
            List<UserMenuItem> menu = _userService.GetMenuAll();
            return Ok(menu);
        }

        [HttpGet("HomePage")]
        public IActionResult GetHomePage()
        {
            var homePage = _homePageService.GetHomePageByUser();
            return Ok(homePage);
        }

        [HttpPost("GenerateUserName")]
        public async Task<IActionResult> GenerateUserName(BecaUserDTO userDto)
        {
            if (userDto == null)
                return BadRequest("Invalid user data");

            var result = await _userService.GenerateUserName(userDto);

            if (result == null)
                return StatusCode(500, "An error occurred");

            return Ok(result);
        }

        [HttpPost("AddOrUpdateUser")]
        public async Task<IActionResult> AddOrUpdateUser(BecaUserDTO userDto)
        {
            if (userDto == null)
                return BadRequest("Invalid user data");

            var result = await _userService.AddOrUpdateUserAsync(userDto);

            if (result == null)
                return StatusCode(500, "An error occurred");

            return Ok(result);
        }

        [HttpPost("changePassword")]
        public async Task<IActionResult> ChangePassword([FromBody] string pwd)
        {
            GenericResponse result = await _userService.changePassword(pwd);

            if (result.Success == false)
                return BadRequest(result.Message);

            return Ok();
        }

        [HttpGet("CreatePassword/{idUtente}")]
        public async Task<IActionResult> CreatePassword(int idUtente)
        {
            GenericResponse result = await _userService.CreatePassword(idUtente);

            if (result.Success == false)
                return BadRequest(result.Message);

            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("requestResetPassword")]
        public async Task<IActionResult> RequestResetPassword(UserResetRequest req)
        {
            _logger.LogDebug($"requestResetPassword");

            // Estrarre il dominio
            var domain = HttpContext.Request.Host.Host;
            _logger.LogDebug($"Request received from domain: {domain}");

            var result = await _userService.RequestResetPassword(req, domain);
            _logger.LogDebug($"res: {result.Message}, {result.Success}");

            if (result.Success == false) 
                return BadRequest(result.Message);

            return Ok(); // result.Message == "" ? "richiesta presa in carico dal sistema": result.Message);
        }

        [AllowAnonymous]
        [HttpGet("reset/{token}")]
        public async Task<IActionResult> Reset(string token)
        {
            var result = await _userService.ResetPassword(token);

            //if (result.Success == false)
            //    return Redirect(result.Message);
            return Redirect(result.Message); // URL della landing page
        }

        // helper methods

        private void SetTokenCookie(string token)
        {
            // append cookie with refresh token to the http response
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7)
            };
            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }

        private string IpAddress()
        {
            // get source ip address for the current request
            if (Request.Headers.TryGetValue("X-Real-IP", out Microsoft.Extensions.Primitives.StringValues value1))
                return value1.ToString();
            if (Request.Headers.TryGetValue("X-Forwarded-For", out Microsoft.Extensions.Primitives.StringValues value2))
                return value2.ToString();
            
            return HttpContext.Connection.RemoteIpAddress == null ? "" : HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
        }
    }
}
