using BecaWebService.Authorization;
using BecaWebService.Models.Communications;
using BecaWebService.Models.Users;
using BecaWebService.Services;
using Entities.Models;
using Microsoft.AspNetCore.Mvc;

namespace BecaWebService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class UsersController : ControllerBase
    {
        private IUserService _userService;
        private IHomePageService _homePageService;

        public UsersController(IUserService userService, IHomePageService homePageService)
        {
            _userService = userService;
            _homePageService = homePageService;
        }

        [AllowAnonymous]
        [HttpPost("authenticate")]
        public IActionResult Authenticate(AuthenticateRequest model)
        {
            var response = _userService.Authenticate(model, ipAddress());
            setTokenCookie(response.RefreshToken);
            return Ok(response);
        }

        //[AllowAnonymous]
        [HttpGet("LoginById/{id}")]
        public IActionResult LoginById(int id)
        {
            var response = _userService.LoginById(id, ipAddress());
            setTokenCookie(response.RefreshToken);
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public IActionResult RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            var response = _userService.RefreshToken(refreshToken, ipAddress());
            setTokenCookie(response.RefreshToken);
            return Ok(response);
        }

        [HttpPost("revoke-token")]
        public IActionResult RevokeToken(RevokeTokenRequest model)
        {
            // accept refresh token in request body or cookie
            var token = model.Token ?? Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(token))
                return BadRequest(new { message = "Token is required" });

            _userService.RevokeToken(token, ipAddress());
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
            var menu = _userService.GetMenuByUser(id);
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

        [HttpGet("CreatePassword/{idUtente}")]
        public async Task<IActionResult> CreatePassword(int idUtente)
        {
            GenericResponse result = await _userService.CreatePassword(idUtente);

            if (result.Success == false)
                return BadRequest(result.Message);

            return Ok();
        }

        // helper methods

        private void setTokenCookie(string token)
        {
            // append cookie with refresh token to the http response
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7)
            };
            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }

        private string ipAddress()
        {
            // get source ip address for the current request
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"];
            else
                return HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
        }
    }
}
