using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Contracts;
using Entities.DataTransferObjects;
using Entities.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BecaWebService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BecaViewController : ControllerBase
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private readonly IMapper _mapper;

        public BecaViewController(ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper)
        {
            _logger = logger;
            _repository = repository;
            _mapper = mapper;
        }

        [HttpGet("{idView}")]
        public async Task<IActionResult> GetViewByID(int idView)
        {
            try
            {
                BecaView dbView = await _repository.BecaView.GetViewByID(idView);
                dtoBecaView oView = _mapper.Map<BecaView, dtoBecaView>(dbView);
                _logger.LogInfo($"Returned View for id {idView}.");

                return Ok(oView);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside GetViewByID action: {ex.Message}");
                return StatusCode(500, $"Internal server error {ex.Message}");
            }
        }
    }
}
