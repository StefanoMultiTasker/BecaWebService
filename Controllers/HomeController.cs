using BecaWebService.Authorization;
using BecaWebService.Services;
using Contracts;
using Microsoft.AspNetCore.Mvc;

namespace BecaWebService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class HomeController(IHomePageService homePageService, ILoggerManager logger) : ControllerBase
    {

        private readonly IHomePageService _homePageService = homePageService;
        private readonly ILoggerManager _logger = logger;

        [HttpPost("HomePage")]
        public IActionResult GetHomePage(HomePars pars)
        {
            if (pars.idProfiles == null || pars.idProfiles.Length == 0)
            {
                _logger.LogError("No profiles provided");
                return BadRequest("No profiles provided");
            }
            var homePage = _homePageService.GetHomeBuildByUser(pars.idProfiles);
            return Ok(homePage);
        }

        [HttpPost("HomeBrick")]
        public IActionResult GetHomeBrick(HomePars pars)
        {
            if(pars.idHomeBrick == null)
            {
                _logger.LogError("No home brick provided");
                return BadRequest("No home brick provided");
            }
            if (pars.idProfiles == null || pars.idProfiles.Length == 0)
            {
                _logger.LogError("No profiles provided");
                return BadRequest("No profiles provided");
            }
            var BrickData = _homePageService.GetHomeBrickContent(pars.idHomeBrick.Value, pars.idProfiles);
            return Ok(BrickData);
        }

    }
    public class HomePars
    {
        public int? idHomeBrick { get; set; }
        public required int[] idProfiles { get; set; }
    }
}
