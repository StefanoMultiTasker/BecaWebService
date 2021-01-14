using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
//using System.Web.Http;
using AutoMapper;
using Contracts;
using Entities.DataTransferObjects;
using Entities.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BecaWebService.Controllers
{
    [Route("[controller]")]
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
        //public async Task<IActionResult> GetViewByID(int idView)
        public IActionResult GetViewByID(int idView, [FromHeader] string Authorization)
        {
            try
            {
                if (Authorization != null) _repository.ReadToken(Authorization);
                BecaView dbView = _repository.BecaView.GetViewByID(idView);
                dtoBecaView oView = _mapper.Map<BecaView, dtoBecaView>(dbView);
                UIform viewFilterUI = _repository.BecaView.GetViewUI(idView, "F");
                UIform viewDetailUI = _repository.BecaView.GetViewUI(idView, "D");
                oView.FilterUI = viewFilterUI;
                oView.DetailUI = viewDetailUI;
                _logger.LogInfo($"Returned View for id {idView}.");

                return Ok(oView);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside GetViewByID action: {ex.Message}");
                return StatusCode(500, $"Internal server error {ex.Message}");
            }
        }
        ///{idView} 
        [HttpPost("CustomizeCols")]
        public IActionResult PostCustomizeCols([FromQuery] int idView, List<dtoBecaData> cols, [FromHeader] string Authorization)
        {
            try
            {
                _repository.ReadToken(Authorization);
                _repository.BecaView.CustomizeColumnsByUser(idView, cols);
                _logger.LogInfo($"Saved Cusom cols for view {idView.ToString()}: " + cols.ToArray().ToString());
                return Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside CustomizeCols action: {ex.Message}");
                return BadRequest($"Internal server error {ex.Message}");
            }
        }
    }
}
