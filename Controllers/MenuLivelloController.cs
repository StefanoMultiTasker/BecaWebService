using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Contracts;
using Entities.DataTransferObjects;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace BecaWebService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenuLivelloController : ControllerBase
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;

        public MenuLivelloController(ILoggerManager logger, IRepositoryWrapper repository)
        {
            _logger = logger;
            _repository = repository;
        }

        [HttpGet("{idLivello}")]
        public async Task<IActionResult> GetMenuLivello(int idLivello)
        {
            try
            {
                var menu = await _repository.MenuLivello.GetAllByLivello(idLivello);
                var menuLivello = new MenuLivelloDTO(menu);
                _logger.LogInfo($"Returned menu for profile {idLivello}.");

                return Ok(menuLivello);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside GetAllByLivello action: {ex.Message}");
                return StatusCode(500, $"Internal server error {ex.Message}");
            }
        }
    }
}