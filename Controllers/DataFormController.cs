using BecaWebService.Helpers;
using BecaWebService.Models.Communications;
using Contracts;
using Entities.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
        //[DeflateCompression]
        public async Task<IActionResult> Post([FromBody] dataFormPostParameter data, System.Threading.CancellationToken cancel)
        {
            try
            {
                string form = data.idView == null ? data.Form : getFormByView(data.idView.Value);
                if ((form ?? "") == "")
                    return BadRequest("La View non ha form associate");

                List<BecaParameter> parameters = data.Parameters.parameters;
                IEnumerable<object> res = _genericService.GetDataByForm(form, parameters);

                //return Ok(res);
                return await getContent(res, cancel);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            //string Form = data["Form"].ToString();
            //List<BecaParameter> parameters = data["Parameters"].ToObject<BecaParameters>().parameters.ToList<BecaParameter>();
            //IEnumerable<object> res = _genericService.GetDataByView(Form, parameters);
        }

        [HttpPost("DataFormNoZip")]
        public IActionResult DataFormNoZip([FromBody] dataFormPostParameter data, System.Threading.CancellationToken cancel)
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
        public async Task<IActionResult> DataField([FromBody] dataFormPostParameter data, System.Threading.CancellationToken cancel)
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
                //return Ok(res);
                return await getContent(res, cancel);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("DataFormChildSelect")]
        public async Task<IActionResult> DataFormChildSelect([FromBody] dataFormChildElem data, System.Threading.CancellationToken cancel)
        {
            try
            {
                string form = data.idView == null ? data.Form : getFormByView(data.idView.Value);
                if ((form ?? "") == "")
                    return BadRequest("La View non ha form associate");
                string formChild = data.FormChild;
                short sqlNumber = data.sqlNumber;
                object record = _genericService.CreateObjectFromJObject<object>(form, data.parentData);

                IEnumerable<object> res = _genericService.GetDataByFormChildSelect(form, formChild, sqlNumber, record);
                //string Form = data["Form"].ToString();
                //string FormField = data["FormField"].ToString();
                //List<BecaParameter> parameters = data["Parameters"].ToObject<BecaParameters>().parameters.ToList<BecaParameter>();
                //IEnumerable<object> res = _genericService.GetDataByFormField(Form, FormField, parameters);
                //return Ok(res);
                return await getContent(res, cancel);
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

                GenericResponse result = await _genericService.DeleteDataByForm(form, recordNew);
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

        [HttpPost("DataFormChildAdd")]
        public async Task<IActionResult> DataFormChildAdd([FromBody] dataFormChildElem data)
        {
            try
            {
                string form = data.idView == null ? data.Form : getFormByView(data.idView.Value);
                if ((form ?? "") == "")
                    return BadRequest("La View non ha form associate");
                string formChild = data.FormChild;
                object record = _genericService.CreateObjectFromJObject<object>(form, data.parentData);

                List<object> childElements = new List<object>();
                if (data.child1 != null) childElements.Add(data.child1);
                if (data.child2 != null) childElements.Add(data.child2);
                if (data.child3 != null) childElements.Add(data.child3);

                GenericResponse result = await _genericService.AddDataByFormChild(form, formChild, record, childElements);
                if (!result.Success)
                    return BadRequest(result.Message);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
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

        private async Task<IActionResult> getContent(IEnumerable<object> res, System.Threading.CancellationToken cancel)
        {
            DefaultContractResolver contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new Extensions.ServiceExtensions.LowerCamelCaseNamingStrategy()
                {
                    OverrideSpecifiedNames = false
                }
            };

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(res, new Newtonsoft.Json.JsonSerializerSettings
            {
                ContractResolver = contractResolver,
                Formatting = Newtonsoft.Json.Formatting.Indented
            });
            //var json = System.Text.Json.JsonSerializer.Serialize(res);
            if (!string.IsNullOrEmpty(Request.Headers["Accept-Encoding"]))
            {
                var encodings = Request.Headers["Accept-Encoding"].ToString().Split(',', StringSplitOptions.TrimEntries);
                if (Array.IndexOf(encodings, "gzip") > -1)
                {
                    Response.Headers.Append("Content-Encoding", "gzip");
                    var compressedBytes = await Compressor.GZipCompressBytesAsync(System.Text.Encoding.UTF8.GetBytes(json), cancel);
                    return File(compressedBytes, "application/json");
                }
                if (Array.IndexOf(encodings, "br") > -1)
                {
                    Response.Headers.Append("Content-Encoding", "br");
                    var compressedBytes = await Compressor.BrotliCompressBytesAsync(System.Text.Encoding.UTF8.GetBytes(json), cancel);
                    return File(compressedBytes, "application/json");
                }
            }
            Response.ContentType = "application/json"; // ADD THE CONTENT TYPE
            return Content(json); // return non-compressed data

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

    public class dataFormChildElem
    {
        public int? idView { get; set; }
        public string? Form { get; set; }
        public string? FormChild { get; set; }
        public short sqlNumber { get; set; }
        public JObject? parentData { get; set; }
        public JObject? child1 { get; set; }
        public JObject? child2 { get; set; }
        public JObject? child3 { get; set; }
    }

    internal class Compressor
    {
        public static async Task<byte[]> BrotliCompressBytesAsync(byte[] bytes, System.Threading.CancellationToken cancel)
        {
            using (var outputStream = new MemoryStream())
            {
                using (var compressionStream = new BrotliStream(outputStream, CompressionLevel.Optimal))
                {
                    await compressionStream.WriteAsync(bytes, 0, bytes.Length, cancel);
                }
                return outputStream.ToArray();
            }
        }
        public static async Task<byte[]> GZipCompressBytesAsync(byte[] bytes, System.Threading.CancellationToken cancel)
        {
            using (var outputStream = new MemoryStream())
            {
                using (var compressionStream = new GZipStream(outputStream, CompressionLevel.Optimal))
                {
                    await compressionStream.WriteAsync(bytes, 0, bytes.Length, cancel);
                }
                return outputStream.ToArray();
            }
        }
    }
}
