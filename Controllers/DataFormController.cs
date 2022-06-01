using BecaWebService.Models.Communications;
using Contracts;
using Entities.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BecaWebService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DataFormController : ControllerBase
    {
        private readonly IGenericService _genericService;
        private readonly ILogger<DataFormController> _logger;

        public DataFormController(IGenericService service, ILogger<DataFormController> logger)
        {
            _logger = logger;
            _genericService = service;
        }

        private string getFormByView(int idView)
        {
            return _genericService.GetFormByView(idView);
        }

        [HttpPost()]
        public IActionResult Post([FromBody] dataFormPostParameter data)
        {
            try
            {
                string form = data.idView == null ? data.Form : getFormByView(data.idView.Value);
                if ((form ?? "") == "")
                    return BadRequest("La View non ha form associate");

                List<BecaParameter> parameters = data.Parameters.parameters;
                IEnumerable<object> res = _genericService.GetDataByForm(form, parameters);

                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            //string Form = data["Form"].ToString();
            //List<BecaParameter> parameters = data["Parameters"].ToObject<BecaParameters>().parameters.ToList<BecaParameter>();
            //IEnumerable<object> res = _genericService.GetDataByView(Form, parameters);
        }

        [HttpPost("DataField")]
        public IActionResult DataField([FromBody] dataFormPostParameter data)
        {
            try
            {
                string form = data.idView == null ? data.Form : getFormByView(data.idView.Value);
                if ((form ?? "") == "")
                    return BadRequest("La View non ha form associate");
                string FormField = data.FormField;

                List<BecaParameter> parameters = data.Parameters.parameters;
                IEnumerable<object> res = _genericService.GetDataByFormField(form, FormField, parameters);
                //string Form = data["Form"].ToString();
                //string FormField = data["FormField"].ToString();
                //List<BecaParameter> parameters = data["Parameters"].ToObject<BecaParameters>().parameters.ToList<BecaParameter>();
                //IEnumerable<object> res = _genericService.GetDataByFormField(Form, FormField, parameters);
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("DataGraph")]
        public IActionResult DataGraph([FromBody] JObject data)
        {
            string Form = data["Form"].ToString();
            string FormField = data["FormField"].ToString();
            List<BecaParameter> parameters = data["Parameters"].ToObject<BecaParameters>().parameters.ToList<BecaParameter>();
            ViewChart res = _genericService.GetGraphByFormField(Form, FormField, parameters);
            return Ok(res);
        }

        [HttpPost("DataPanels")]
        public IActionResult DataPanels([FromBody] JObject data)
        {
            string Form = data["Form"].ToString();
            List<BecaParameter> parameters = data["Parameters"].ToObject<BecaParameters>().parameters.ToList<BecaParameter>();
            object res = _genericService.GetPanelsByForm(Form, parameters);
            return Ok(res);
        }

        [HttpPost("DataFormUpdate")]
        public async Task<IActionResult> DataFormUpdate([FromBody] dataFormPostParameter data)
        //[FromForm] string form, [FromForm] string Record, [FromForm] string oldRecord)
        {
            try
            {
                string form = data.idView == null ? data.Form : getFormByView(data.idView.Value);
                if ((form ?? "") == "")
                    return BadRequest("La View non ha form associate");

                object recordNew = _genericService.CreateObjectFromJObject<object>(form, data.newData);
                object recordOld = _genericService.CreateObjectFromJObject<object>(form, data.originalData);

                GenericResponse result = await _genericService.UpdateDataByForm(form, recordOld, recordNew);
                if (!result.Success)
                    return BadRequest(result.Message);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            //string form = data["form"].ToString();  //data.form; // data["form"].ToString();
            //object recordNew = _genericService.CreateObjectFromJObject<object>(form, data["newData"].ToObject<JObject>());  //data.newData; //data["newData"].FromObject<object>();
            //object recordOld = _genericService.CreateObjectFromJObject<object>(form, data["originalData"].ToObject<JObject>());  //data.newData; //data["newData"].FromObject<object>();

            ////var recordNew = _genericService.CreateObjectFromJSON<object>(form, Record);
            ////var recordOld = _genericService.CreateObjectFromJSON<object>(form, oldRecord);
            //GenericResponse result = await _genericService.UpdateDataByForm(form, recordOld, recordNew);
            //if (!result.Success)
            //    return BadRequest(result.Message);

            //return Ok(result);
        }

        [HttpPost("DataFormAdd")]
        public async Task<IActionResult> DataFormAdd([FromBody] dataFormPostParameter data)
        {
            try
            {
                string form = data.idView == null ? data.Form : getFormByView(data.idView.Value);
                if ((form ?? "") == "")
                    return BadRequest("La View non ha form associate");

                object recordNew = _genericService.CreateObjectFromJObject<object>(form, data.newData);

                GenericResponse result = await _genericService.AddDataByForm(form, recordNew, data.force.Value);
                if (!result.Success)
                    return BadRequest(result.Message);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            //string form = data["form"].ToString();  //data.form; // data["form"].ToString();
            //string force = data["force"].ToString();  //data.form; // data["form"].ToString();
            //object recordNew = _genericService.CreateObjectFromJObject<object>(form, data["newData"].ToObject<JObject>());  //data.newData; //data["newData"].FromObject<object>();
            ////var recordNew = _genericService.CreateObjectFromJSON<object>(form, Record);
            //GenericResponse result = await _genericService.AddDataByForm(form, recordNew, (force == "0" ? false : true));
            //if (!result.Success)
            //    return BadRequest(result.Message);

            //return Ok(result);
        }

        [HttpPost("DataFormDelete")]
        public async Task<IActionResult> DataFormDelete([FromBody] dataFormPostParameter data)
        //[FromForm] string form, [FromForm] string Record)
        {
            try
            {
                string form = data.idView == null ? data.Form : getFormByView(data.idView.Value);
                if ((form ?? "") == "")
                    return BadRequest("La View non ha form associate");

                object recordNew = _genericService.CreateObjectFromJObject<object>(form, data.newData);

                GenericResponse result = await _genericService.DeleteDataByForm(form, recordNew );
                if (!result.Success)
                    return BadRequest(result.Message);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            //string form = data["form"].ToString();  //data.form; // data["form"].ToString();
            //object recordNew = _genericService.CreateObjectFromJObject<object>(form, data["newData"].ToObject<JObject>());  //data.newData; //data["newData"].FromObject<object>();
            //                                                                                                                //var recordNew = _genericService.CreateObjectFromJSON<object>(form, Record);
            //GenericResponse result = await _genericService.DeleteDataByForm(form, recordNew);
            //if (!result.Success)
            //    return BadRequest(result.Message);

            //return Ok(result);
        }

        [HttpPost("ExecProcedure")]
        public async Task<IActionResult> ExecProcedure([FromBody] JObject data)
        {
            string dbName = data["DbName"].ToString();
            string procName = data["ProcedureName"].ToString();
            List<BecaParameter> parameters = data["Parameters"].ToObject<BecaParameters>().parameters.ToList<BecaParameter>();                                                                                                                //var recordNew = _genericService.CreateObjectFromJSON<object>(form, Record);

            object res = await _genericService.ExecCommand(dbName, procName, parameters);
            return Ok(res);
        }
    }

    public class dataFormPostParameter
    {
        public string? Form { get; set; }
        public int? idView { get; set; }
        public string? FormField { get; set; }
        public BecaParameters Parameters { get; set; }
        public Boolean? force { get; set; }
        public JObject? newData { get; set; }
        public JObject? originalData { get; set; }
    }
}
