using BecaWebService.Authorization;
using BecaWebService.Helpers;
using BecaWebService.Models.Communications;
using Contracts;
using Entities.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Collections;
using System.IO.Compression;

namespace BecaWebService.Controllers
{
    [Authorize]
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
                GenericResponse res = _genericService.GetDataByForm(form, parameters);
                if (!res.Success) return BadRequest(res.Message);

                //return Ok(res);
                return await getContent(res._extraLoads, data.lowerCase, cancel);
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
                GenericResponse res = _genericService.GetDataByForm(form, parameters);
                if (!res.Success) return BadRequest(res.Message);

                return Ok(res._extraLoads);
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
                GenericResponse res = _genericService.GetDataByFormField(form, FormField, parameters);
                if (!res.Success) return BadRequest(res.Message);
                //string Form = data["Form"].ToString();
                //string FormField = data["FormField"].ToString();
                //List<BecaParameter> parameters = data["Parameters"].ToObject<BecaParameters>().parameters.ToList<BecaParameter>();
                //IEnumerable<object> res = _genericService.GetDataByFormField(Form, FormField, parameters);
                //return Ok(res);
                return await getContent(res._extraLoads, data.lowerCase, cancel);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("DataFields")]
        public async Task<IActionResult> DataFields([FromBody] dataFormFieldsPostParameter req, System.Threading.CancellationToken cancel)
        {
            List<List<object>> res = new List<List<object>>();
            foreach (dataFormPostParameter data in req.RequestList)
            {
                try
                {
                    string form = data.idView == null ? data.Form : getFormByView(data.idView.Value);
                    if ((form ?? "") == "")
                        return BadRequest("La View non ha form associate");
                    string FormField = data.FormField;

                    List<BecaParameter> parameters = data.Parameters.parameters;
                    GenericResponse _res = _genericService.GetDataByFormField(form, FormField, parameters);
                    res.Add(_res.Success ? _res._extraLoads: []);
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }
            return await getContent(res, req.RequestList[0].lowerCase, cancel);
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
                object record = _genericService.CreateObjectFromJObject<object>(form, data.parentData, true);

                GenericResponse res = _genericService.GetDataByFormChildSelect(form, formChild, sqlNumber, record);
                if (!res.Success) return BadRequest(res.Message);
                return await getContent(res._extraLoads, true, cancel);
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
                //var modelData = JsonConvert.DeserializeObject<dataFormPostParameter>(Request.Form["data"]);
                string form = data.idView == null ? data.Form : getFormByView(data.idView.Value);
                if ((form ?? "") == "")
                    return BadRequest("La View non ha form associate");

                if(data.newData == null && data.newListData == null)
                {
                    return BadRequest(new GenericResponse("Nessun dato fornito"));
                }

                if (data.newData != null && data.originalData == null)
                {
                    return BadRequest(new GenericResponse("Nessun dato di confronto fornito"));
                }

                if (data.newListData != null && data.originalListData == null)
                {
                    return BadRequest(new GenericResponse("Nessun dato di confronto fornito"));
                }

                GenericResponse result = new GenericResponse(true);
                if (data.newData != null)
                {
                    object recordNew = _genericService.CreateObjectFromJObject<object>(form, data.newData, true);
                    object recordOld = _genericService.CreateObjectFromJObject<object>(form, data.originalData, true);

                    result = await _genericService.UpdateDataByForm(form, recordOld, recordNew);
                }
                if (data.newListData != null)
                {
                    List<object> recordsNew = _genericService.CreateObjectsFromJArray<object>(form, data.newListData, true);
                    List<object> recordsOld = _genericService.CreateObjectsFromJArray<object>(form, data.originalListData, true);

                    string resultMessage = "";
                    bool resultSuccess = false;
                    List<object> resultObj = new List<object>();
                    for (int i = 0; i < recordsNew.Count; i++)
                    {
                        GenericResponse singleResult = await _genericService.UpdateDataByForm(form, recordsOld[i], recordsNew[i]);
                        resultMessage = String.Join(";", resultMessage, singleResult.Message);
                        resultObj.Add(singleResult._extraLoad);
                        if (singleResult.Success) resultSuccess = true;
                    }
                    result = new GenericResponse(resultObj, resultMessage);
                }

                _logger.LogDebug($"DataFormUpdate: {form}");

                if (!result.Success)
                    return BadRequest(result); //.Message);

                // Verifica se `lowercase` è `true` e applica la trasformazione
                if (data.lowerCase)
                {
                    result._extraLoad = ConvertToLowercaseRecursive(result._extraLoad);
                }

                return Ok(result);
                //return Ok(result._extraLoad);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message.toResponse());
            }
        }

        [HttpPost("DataFormAdd")]
        public async Task<IActionResult> DataFormAdd([FromBody] dataFormPostParameter data)
        {
            try
            {
                string form = data.idView == null ? data.Form : getFormByView(data.idView.Value);
                if ((form ?? "") == "")
                    return BadRequest("La View non ha form associate");

                if (data.newData == null && data.newListData == null)
                {
                    return BadRequest(new GenericResponse("Nessun dato fornito"));
                }

                GenericResponse result = new GenericResponse(true);
                if (data.newData != null)
                {

                    object recordNew = _genericService.CreateObjectFromJObject<object>(form, data.newData, false);

                    result = await _genericService.AddDataByForm(form, recordNew, data.force.Value);
                }
                if (data.newListData != null)
                {
                    List<object> recordsNew = _genericService.CreateObjectsFromJArray<object>(form, data.newListData, true);

                    string resultMessage = "";
                    bool resultSuccess = false;
                    List<object> resultObj = new List<object>();
                    for (int i = 0; i < recordsNew.Count; i++)
                    {
                        GenericResponse singleResult = await _genericService.AddDataByForm(form, recordsNew[i], data.force.Value);
                        resultMessage = String.Join(";", resultMessage, singleResult.Message);
                        resultObj.Add(singleResult._extraLoad);
                        if (singleResult.Success) resultSuccess = true;
                    }
                    result = new GenericResponse(resultObj, resultMessage);
                }

                if (!result.Success)
                    return BadRequest(result);//.Message);

                // Verifica se `lowercase` è `true` e applica la trasformazione
                if (data.lowerCase)
                {
                    result._extraLoad = ConvertToLowercaseRecursive(result._extraLoad);
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message.toResponse());
            }
        }

        [HttpPost("DataFormAddOrUpdate")]
        public async Task<IActionResult> DataFormAddOrUpdate([FromBody] dataFormPostParameter data)
        {
            string form = data.idView == null ? data.Form : getFormByView(data.idView.Value);
            if ((form ?? "") == "")
                return BadRequest("La View non ha form associate");
            //Log.Information($"FormAddOrUpdate {form}");

            if (data.newData == null && data.newListData == null)
            {
                return BadRequest(new GenericResponse("Nessun dato fornito"));
            }

            GenericResponse result = new GenericResponse(true);
            if (data.newData != null)
            {
                object recordNew = _genericService.CreateObjectFromJObject<object>(form, data.newData, false, true);
            //Log.Information($"FormAddOrUpdate {form} object generated");

                result = await _genericService.AddOrUpdateDataByForm(form, recordNew);
            }
            if (data.newListData != null)
            {
                List<object> recordsNew = _genericService.CreateObjectsFromJArray<object>(form, data.newListData, true);

                string resultMessage = "";
                bool resultSuccess = false;
                List<object> resultObj = new List<object>();
                for (int i = 0; i < recordsNew.Count; i++)
                {
                    GenericResponse singleResult = await _genericService.AddOrUpdateDataByForm(form, recordsNew[i]);
                    resultMessage = String.Join(";", resultMessage, singleResult.Message); 
                    if (data.lowerCase && singleResult._extraLoad != null)
                    {
                        singleResult._extraLoad = ConvertToLowercaseRecursive(singleResult._extraLoad);
                        resultObj.Add(singleResult._extraLoad);
                    } else
                    if (singleResult.Success) resultSuccess = true;
                }
                result = new GenericResponse(resultObj, resultMessage);
            }

            //Log.Information($"FormAddOrUpdate done");
            if (!result.Success)
                return BadRequest(result); //.Message);

            // Verifica se `lowercase` è `true` e applica la trasformazione
            if (data.lowerCase)
            {
                result._extraLoad = ConvertToLowercaseRecursive(result._extraLoad);
            }
            return Ok(result);
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

                object recordNew = _genericService.CreateObjectFromJObject<object>(form, data.newData, true);

                GenericResponse result = await _genericService.DeleteDataByForm(form, recordNew);
                if (!result.Success)
                    return BadRequest(result); //.Message);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message.toResponse());
            }
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
                object record = _genericService.CreateObjectFromJObject<object>(form, data.parentData, true);

                List<object> childElements = new List<object>();
                if (data.child1 != null) childElements.Add(data.child1);
                if (data.child2 != null) childElements.Add(data.child2);
                if (data.child3 != null) childElements.Add(data.child3);

                GenericResponse result = await _genericService.AddDataByFormChild(form, formChild, record, childElements);
                if (!result.Success)
                    return BadRequest(result.Message);

                // Verifica se `lowercase` è `true` e applica la trasformazione
                if (data.lowerCase)
                {
                    result._extraLoad = ConvertToLowercaseRecursive(result._extraLoad);
                }

                return Ok(result._extraLoad);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("DataFormAction")]
        public async Task<IActionResult> DataFormAction([FromBody] dataFormPostParameter data)
        //[FromForm] string form, [FromForm] string Record)
        {
            try
            {
                int idView = (int)data.idView;
                string form = data.idView == null ? data.Form : getFormByView(data.idView.Value);
                if ((form ?? "") == "")
                    return BadRequest("La View non ha form associate");
                string actionName = data.FormField;

                List<BecaParameter> parameters = data.Parameters.parameters;

                GenericResponse result = await _genericService.ActionByForm(idView, form, actionName, parameters);
                if (!result.Success)
                    return BadRequest(result.Message);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("DataFormActionRow")]
        public async Task<IActionResult> DataFormActionRow([FromBody] dataFormPostParameter data)
        //[FromForm] string form, [FromForm] string Record)
        {
            try
            {
                int idView = (int)data.idView;
                string form = data.idView == null ? data.Form : getFormByView(data.idView.Value);
                if ((form ?? "") == "")
                    return BadRequest("La View non ha form associate");
                string actionName = data.FormField;

                object record = _genericService.CreateObjectFromJObject<object>(form, data.newData, true);

                GenericResponse result = await _genericService.ActionByForm(idView, form, actionName, record);
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
            if (data == null) return BadRequest("Nessuna informazione fornita");

            string dbName = data["DbName"] == null ? "" : data["DbName"].ToString();
            string procName = data["ProcedureName"].ToString();
            List<BecaParameter> parameters = data["Parameters"].ToObject<BecaParameters>().parameters.ToList<BecaParameter>();                                                                                                                //var recordNew = _genericService.CreateObjectFromJSON<object>(form, Record);

            GenericResponse result = await _genericService.ExecCommand(dbName, procName, parameters);
            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result);
        }

        private async Task<IActionResult> getContent(IEnumerable<object> res, bool lowerCase, System.Threading.CancellationToken cancel)
        {
            DefaultContractResolver contractResolver = new DefaultContractResolver
            {
                NamingStrategy = lowerCase 
                    ? new Extensions.ServiceExtensions.LowerNamingStrategy() { OverrideSpecifiedNames = false }
                    : new Extensions.ServiceExtensions.LowerCamelCaseNamingStrategy() { OverrideSpecifiedNames = false }
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
        private object ConvertToLowercaseRecursive(object obj)
        {
            if (obj == null) return null;

            // Se è una collezione (array o lista)
            if (obj is IEnumerable enumerable && !(obj is string))
            {
                var list = new List<object>();
                foreach (var item in enumerable)
                {
                    list.Add(ConvertToLowercaseRecursive(item));
                }
                return list;
            }

            // Se è un oggetto con proprietà
            var type = obj.GetType();
            if (type.IsClass && type != typeof(string))
            {
                var dictionary = new Dictionary<string, object>();
                foreach (var property in type.GetProperties())
                {
                    var value = property.GetValue(obj);
                    dictionary[property.Name.ToLower()] = ConvertToLowercaseRecursive(value);
                }
                return dictionary;
            }

            // Per tutti gli altri tipi primitivi
            return obj;
        }



    }

    public class dataFormPostParameters
    {
        public List<dataFormPostParameter> RequestList { get; set; }
    }
    public class dataFormPostParameter
    {
        public string? Form { get; set; }
        public int? idView { get; set; }
        public string? FormField { get; set; }
        public string? DbName { get; set; }
        public string? ProcedureName { get; set; }
        public BecaParameters? Parameters { get; set; }
        public bool? force { get; set; }
        public JObject? newData { get; set; }
        public JObject? originalData { get; set; }
        public JArray? newListData { get; set; }
        public JArray? originalListData { get; set; }
        public bool lowerCase { get; set; }
    }

    public class dataFormFieldsPostParameter
    {
        public required List<dataFormPostParameter> RequestList { get; set; }
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
        public bool lowerCase { get; set; }
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
