//using System.Web.Http;
using AutoMapper;
using BecaWebService.Authorization;
using Contracts;
using Entities.DataTransferObjects;
using Entities.Models;
using Microsoft.AspNetCore.Mvc;

namespace BecaWebService.Controllers
{
    [Authorize]
    [Route("[controller]")]
    [ApiController]
    public class BecaViewController : ControllerBase
    {
        private ILoggerManager _logger;
        private IBecaViewRepository _repository;
        private readonly IMapper _mapper;

        public BecaViewController(ILoggerManager logger, IBecaViewRepository repository, IMapper mapper)
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
                BecaView dbView = _repository.GetViewByID(idView);
                dtoBecaView oView = _mapper.Map<BecaView, dtoBecaView>(dbView);
                UIform viewFilterUI = _repository.GetViewUI(idView, "F");
                UIform viewDetailUI = _repository.GetViewUI(idView, "D");
                oView.FilterUI = viewFilterUI;
                oView.DetailUI = viewDetailUI;
                foreach(dtoBecaViewChild child in oView.ViewDefinition.childrenForm) { 
                    UIform childDetailUI = _repository.GetViewUI(child.form);
                    child.DetailUI= childDetailUI;
                }
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
                _repository.CustomizeColumnsByUser(idView, cols);
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
