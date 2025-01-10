using BecaWebService.Authorization;
using BecaWebService.Models.Communications;
using BecaWebService.Services;
using Contracts.Custom;
using Entities.Models.Custom;
using ExtensionsLib;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace BecaWebService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class MiscController : Controller
    {
        private readonly IMiscService _service;
        private readonly ILogger<MiscController> _logger;

        public MiscController(IMiscService service, ILogger<MiscController> logger)
        {
            _logger = logger;
            _service = service;
        }

        [HttpPost("UploadPresenze")]
        public async Task<IActionResult> PostUploadPresenze([FromForm] int idOrologio, [FromForm] string aaco, [FromForm] string mmco, Microsoft.AspNetCore.Http.IFormFile file) // uploadFile upl)  
        {
            try
            {
                var result = await _service.UploadPresenze(idOrologio, aaco, mmco, file);

                if (!result.Success)
                    return BadRequest(result.Message);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("ImportaPresenze")]
        public async Task<IActionResult> ImportaPresenze([FromForm] int idOrologio)
        {
            try
            {
                var result = await _service.ImportaPresenze(idOrologio);

                if (!result.Success)
                    return BadRequest(result.Message);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("PrintPresenze")]
        public IActionResult PostPrintPresenze(
            [FromForm] string aaco, [FromForm] string mmco,
            [FromForm] string cdff, [FromForm] string aact, [FromForm] string cdnn, [FromForm] string cdmt,
            [FromForm] string ffcl, [FromForm] string codc, [FromForm] string cdc,
            [FromForm] string nome
            )
        {
            string step = "start";
            try
            {
                step = $"aaco: {aaco}, mmco: {mmco}, " +
                    $"cdff: {cdff}, aact: {aact}, cdnn: {cdnn}, cdmt: {cdmt}, " +
                    $"ffcl: {ffcl}, codc: {codc}, cdc: {cdc}, " +
                    $"nome: {nome}";
                var result = _service.PrintPresenze(aaco, mmco, cdff, aact, cdnn, cdmt, ffcl, codc, cdc, nome);
                step = "invio pdf";
                if (!result.Success)
                    return BadRequest(result.Message);

                return File(((MemoryStream)result._extraLoad.GetPropertyValue("pdf")).ToArray(), "application/pdf");
            }
            catch (Exception ex)
            {
                string err = $"controller error: {step}, {ex.Message}";
                if (ex.InnerException != null) err += " - " + ex.InnerException.Message;
                return BadRequest(err);
            }
        }

        [HttpGet("ListCUByCodFisc")]
        public IActionResult ListCUByCodFisc()
        {
            try
            {
                var result = _service.ListCUByCodFisc();

                if (!result.Success)
                    return BadRequest(result.Message);

                return Ok(result._extraLoad);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("LavorSendRequestMail")]
        public IActionResult LavorSendRequestMail([FromForm] string subject, [FromForm] string text)
        {
            try
            {
                var result = _service.LavorSendRequestMail(subject, text);

                if (!result.Success)
                    return BadRequest(result.Message);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("SavinoOTP")]
        public IActionResult SavinoOTP([FromBody] SavinoOTP data)
        {
            string logName = @$"E:\BecaWeb\uploadlocal\{DateTime.Today.Year}{DateTime.Today.Month.ToString().PadLeft(2, '0')}{DateTime.Today.Day.ToString().PadLeft(2, '0')}__savinoOTP.txt";
            StreamWriter sw = System.IO.File.AppendText(logName);

            try
            {
                string json = JsonConvert.SerializeObject(data);

                sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: {json}");
                sw.WriteLine("");
            }
            catch (Exception)
            {
            }
            if (data.id == 0)
            {
                sw.Flush();
                sw.Close();
                return Ok(new { testSuccess = true });
            }
            GenericResponse res = _service.SavinoOTP(data, sw);
            sw.WriteLine("");
            sw.Flush();
            sw.Close();
            return res.Success ? (IActionResult)Ok(new { success = true }) : (IActionResult)NotFound(data);
        }

        [HttpPost("SavinoFirma")]
        public async Task<IActionResult> SavinoFirma([FromBody] SavinoFirma data)
        {
            string logName = @$"E:\BecaWeb\uploadlocal\{DateTime.Today.Year}{DateTime.Today.Month.ToString().PadLeft(2, '0')}{DateTime.Today.Day.ToString().PadLeft(2, '0')}__savinoFirma.txt";
            StreamWriter sw = System.IO.File.AppendText(logName);

            try
            {
                string json = JsonConvert.SerializeObject(data);

                sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: {json}");
                sw.WriteLine("");
            }
            catch (Exception)
            {
            }

            if (data.id == 0)
            {
                sw.Flush();
                sw.Close();
                return Ok(new { testSuccess = true });
            }
            bool res = await _service.SavinoFirma(data, sw);
            sw.WriteLine("");
            sw.Flush();
            sw.Close();
            return res ? (IActionResult)Ok(new { success = true }) : (IActionResult)NotFound(data);
        }

        [HttpPost("pms")]
        public async Task<IActionResult> pms([FromBody] pmsJson data)
        {
            string logName = @$"E:\BecaWeb\uploadlocal\{DateTime.Today.Year}{DateTime.Today.Month.ToString().PadLeft(2, '0')}{DateTime.Today.Day.ToString().PadLeft(2, '0')}_PMS.txt";
            StreamWriter sw = System.IO.File.AppendText(logName);
            try
            {
                string json = JsonConvert.SerializeObject(data);
                sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: {json}");
                sw.WriteLine("");

                bool res = await _service.pms(data, json, sw);
            }
            catch (Exception)
            {
            }
            sw.Flush();
            sw.Close();
            return Ok();
        }

        [HttpPost("pmsAvvia")]
        public async Task<IActionResult> pmsAvvia([FromBody] pmsAvviaProcesso data)
        {
            GenericResponse result = await _service.AvviaProcesso(data);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result);
        }

        [HttpPost("pmsInvalida")]
        public async Task<IActionResult> pmsInvalida([FromBody] pmsInvalidaFasi data)
        {
            GenericResponse result = await _service.InvalidaFasi(data);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result);
        }

        [HttpPost("PreparaEbitemp")]
        public IActionResult PreparaDocs([FromBody] PreparaEbitemp data)
        {
            try
            {
                var result = _service.PreparaEbitemp(data.matricole4Ebitemp);

                if (!result.Success)
                    return BadRequest(result.Message);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("PreparaDocs")]
        public async Task<IActionResult> PreparaDocs([FromBody] PreparaDocs data)
        {
            try
            {
                var result = await _service.PreparaDocumenti($"{data.AnnoInizio}{data.MeseInizio}", $"{data.AnnoFine}{data.MeseFine}",data.Matricole, data.IncludeCU, data.folder);

                if (!result.Success)
                    return BadRequest(result.Message);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

    //public class SavinoOTP
    //{
    //    public int id { get; set; }
    //    public string otp { get; set; }
    //    public Int64 dueDate { get; set; }
    //    public string? phone { get; set; }
    //    public string? email { get; set; }
    //    public string? pec { get; set; }
    //}
    //public class SavinoFirma
    //{
    //    public int id { get; set; }
    //    public int root { get; set; }
    //    public Int64 date { get; set; }
    //    public string? fullname { get; set; }
    //    public string? ip { get; set; }
    //}
}
