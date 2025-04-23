using BecaWebService.Authorization;
using BecaWebService.ExtensionsLib;
using BecaWebService.Helpers;
using BecaWebService.Models.Communications;
using Contracts;
using Entities.Communications;
using Entities.Models;
using ExtensionsLib;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Collections;
using System.IO.Compression;
using static BecaWebService.Extensions.ServiceExtensions;
using static iText.IO.Image.Jpeg2000ImageData;

namespace BecaWebService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class DataFormController(IGenericService service, ILoggerManager logger) : ControllerBase
    {
        private readonly IGenericService _genericService = service;
        private readonly ILoggerManager _logger = logger;

        private string GetFormByView(int idView)
        {
            return _genericService.GetFormByView(idView);
        }

        [HttpPost()]
        //[DeflateCompression]
        public async Task<IActionResult> Post([FromBody] DataFormPostParameter data, System.Threading.CancellationToken cancel)
        {
            string form = "";
            try
            {
                string check = CheckDataFormPostParameter(data, false, out form);
                if (check != "") return BadRequest(check);
                //form = data!.idView == null ? data.Form! : GetFormByView(data.idView.Value);
                if ((form ?? "") == "") return BadRequest("La View non ha form associate");

                int? pageNumber = data.pageNumber;
                int? pageSize = data.pageSize;

                //_logger.LogInfo($"User {_genericService.GetUserId()} - Company {_genericService.GetCompanyId()} - DataForm {form}");

                List<BecaParameter> parameters = data.Parameters!.parameters;
                GenericResponse res = _genericService.GetDataByForm(form!, parameters, pageNumber, pageSize, data.lowerCase);
                if (!res.Success) return BadRequest(res.Message);


                return await GetContent(res._extraLoads, data.lowerCase, cancel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"User {_genericService.GetUserId()} - Company {_genericService.GetCompanyId()} - DataForm {form} - errore: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("DataFormNoZip")]
        public IActionResult DataFormNoZip([FromBody] DataFormPostParameter data)
        {
            string form = "";
            try
            {
                string check = CheckDataFormPostParameter(data, false, out form);
                if (check != "") return BadRequest(check);
                //form = data!.idView == null ? data.Form! : GetFormByView(data.idView.Value);
                if ((form ?? "") == "") return BadRequest("La View non ha form associate");

                //_logger.LogInfo($"User {_genericService.GetUserId()} - Company {_genericService.GetCompanyId()} - DataForm {form}");

                List<BecaParameter> parameters = data.Parameters!.parameters;
                GenericResponse res = _genericService.GetDataByForm(form!, parameters);
                if (!res.Success) return BadRequest(res.Message);

                return Ok(res._extraLoads);
            }
            catch (Exception ex)
            {
                _logger.LogError($"User {_genericService.GetUserId()} - Company {_genericService.GetCompanyId()} - DataForm {form} - errore: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("DataField")]
        public async Task<IActionResult> DataField([FromBody] DataFormPostParameter data, System.Threading.CancellationToken cancel)
        {
            string form = "";
            string FormField = "";
            try
            {
                string check = CheckDataFormPostParameter(data, true, out form);
                if (check != "") return BadRequest(check);
                //form = data!.idView == null ? data.Form! : GetFormByView(data.idView.Value);
                if ((form ?? "") == "") return BadRequest("La View non ha form associate");

                FormField = data.FormField!;
                //_logger.LogInfo($"User {_genericService.GetUserId()} - Company {_genericService.GetCompanyId()} - DataField {form} - {FormField}");

                List<BecaParameter> parameters = data.Parameters!.parameters;
                GenericResponse res = _genericService.GetDataByFormField(form!, FormField, parameters, data.lowerCase);
                if (!res.Success) return BadRequest(res.Message);
                return await GetContent(res._extraLoads, data.lowerCase, cancel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"User {_genericService.GetUserId()} - Company {_genericService.GetCompanyId()} - DataField {form} - {FormField} - errore: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("DataFields")]
        public async Task<IActionResult> DataFields([FromBody] DataFormFieldsPostParameter req, System.Threading.CancellationToken cancel)
        {
            List<List<object>> res = [];
            foreach (DataFormPostParameter data in req.RequestList)
            {
                try
                {
                    if (data != null)
                    {
                        string check = CheckDataFormPostParameter(data, true, out string form);
                        if (check != "") return BadRequest(check);
                        //form = data.idView == null ? (data.Form ?? "") : GetFormByView(data.idView.Value);
                        if ((form ?? "") == "") return BadRequest("La View non ha form associate");
                        string FormField = data.FormField!;

                        List<BecaParameter> parameters = data.Parameters!.parameters ?? [];
                        GenericResponse _res = _genericService.GetDataByFormField(form!, FormField, parameters, req.RequestList[0].lowerCase);
                        res.Add(_res.Success ? _res._extraLoads : []);
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }
            return await GetContent(res, req.RequestList[0].lowerCase, cancel);
        }

        [HttpPost("DataPack")]
        public async Task<IActionResult> DataPack([FromBody] DataFormPostParameters req, System.Threading.CancellationToken cancel)
        {
            List<List<object>> res = [];
            foreach (DataFormPostParameter data in req.RequestList)
            {
                try
                {
                    string check = CheckDataFormPostParameter(data, false, out string form);
                    if (check != "") return BadRequest(check);
                    //string form = data!.idView == null ? data.Form! : GetFormByView(data.idView.Value);
                    if ((form ?? "") == "") return BadRequest("La View non ha form associate");

                    if (data.FormField == null)
                    {
                        int? pageNumber = data.pageNumber;
                        int? pageSize = data.pageSize;

                        List<BecaParameter> parameters = data.Parameters!.parameters;
                        GenericResponse _res = _genericService.GetDataByForm(form!, parameters, pageNumber, pageSize, req.RequestList[0].lowerCase);
                        res.Add(_res.Success ? _res._extraLoads : []);
                    }
                    else
                    {
                        if (data.FormField.isNullOrempty()) return BadRequest("Non hai specificato il campo");
                        string FormField = data.FormField;

                        List<BecaParameter> parameters = data.Parameters!.parameters;
                        GenericResponse _res = _genericService.GetDataByFormField(form!, FormField, parameters, req.RequestList[0].lowerCase);
                        res.Add(_res.Success ? _res._extraLoads : []);
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }
            return await GetContent(res, req.RequestList[0].lowerCase, cancel);
        }

        [HttpPost("DataPack2")]
        public async Task<IActionResult> DataPack2([FromBody] DataFormPostParameters req, System.Threading.CancellationToken cancel)
        {
            try
            {
                GenericResponse res = await _genericService.GetDataPackAsync(req, req.RequestList[0].lowerCase);
                if (!res.Success) return BadRequest(res.Message);
                return await GetContent(res._extraLoads, req.RequestList[0].lowerCase, cancel);
            }
            catch (Exception ex)
            {
                return BadRequest($"DataPack2 è fallito: {ex.Message}");
            }
        }

        [HttpPost("DataFormChildSelect")]
        public async Task<IActionResult> DataFormChildSelect([FromBody] DataFormChildElem data, System.Threading.CancellationToken cancel)
        {
            try
            {
                string form = data.idView == null ? data.Form ?? "" : GetFormByView(data.idView.Value) ?? "";
                if (form.isNullOrempty()) return BadRequest("La View non ha form associate");
                if (data.FormChild.isNullOrempty()) return BadRequest("Non ha specificato il child");
                if (data.parentData == null) return BadRequest("Non ha specificato il record parent");

                string formChild = data.FormChild!;
                short sqlNumber = data.sqlNumber;
                object record = _genericService.CreateObjectFromJObject<object>(form, data.parentData, true);

                GenericResponse res = _genericService.GetDataByFormChildSelect(form, formChild, sqlNumber, record, true);
                if (!res.Success) return BadRequest(res.Message);
                return await GetContent(res._extraLoads, true, cancel);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("DataGraph")]
        public IActionResult DataGraph([FromBody] JObject data)
        {
            if (data["Form"] == null) return BadRequest("Non hai specificato il form");
            if (data["FormField"] == null) return BadRequest("Non hai specificato il campo");
            if (data["Parameters"] == null) return BadRequest("Non hai specificato i parametri");

            string Form = data["Form"]!.ToString();
            string FormField = data["FormField"]!.ToString();
            List<BecaParameter> parameters = [.. data["Parameters"]!.ToObject<BecaParameters>()!.parameters];
            ViewChart res = _genericService.GetGraphByFormField(Form, FormField, parameters);
            return Ok(res);
        }

        [HttpPost("DataPanels")]
        public IActionResult DataPanels([FromBody] JObject data)
        {
            if (data["Form"] == null) return BadRequest("Non hai specificato il form");
            if (data["Parameters"] == null) return BadRequest("Non hai specificato i parametri");

            string Form = data["Form"]!.ToString();
            List<BecaParameter> parameters = [.. data["Parameters"]!.ToObject<BecaParameters>()!.parameters];
            object? res = _genericService.GetPanelsByForm(Form, parameters);
            if (res == null) return BadRequest();
            return Ok(res);
        }

        [HttpPost("DataFormUpdate")]
        public async Task<IActionResult> DataFormUpdate([FromBody] DataFormPostParameter data)
        //[FromForm] string form, [FromForm] string Record, [FromForm] string oldRecord)
        {
            //try
            //{
            //    //var modelData = JsonConvert.DeserializeObject<dataFormPostParameter>(Request.Form["data"]);
            //    string form = data.idView == null ? data.Form : GetFormByView(data.idView.Value);
            //    if ((form ?? "") == "")
            //        return BadRequest("La View non ha form associate");

            //    if (data.newData == null && data.newListData == null)
            //    {
            //        return BadRequest(new GenericResponse("Nessun dato fornito"));
            //    }

            //    if (data.newData != null && data.originalData == null)
            //    {
            //        return BadRequest(new GenericResponse("Nessun dato di confronto fornito"));
            //    }

            //    if (data.newListData != null && data.originalListData == null)
            //    {
            //        return BadRequest(new GenericResponse("Nessun dato di confronto fornito"));
            //    }

            //    GenericResponse result = new GenericResponse(true);
            //    if (data.newData != null)
            //    {
            //        object recordNew = _genericService.CreateObjectFromJObject<object>(form, data.newData, true);
            //        object recordOld = _genericService.CreateObjectFromJObject<object>(form, data.originalData, true);

            //        result = await _genericService.UpdateDataByForm(form, recordOld, recordNew);
            //    }
            //    if (data.newListData != null)
            //    {
            //        List<object> recordsNew = _genericService.CreateObjectsFromJArray<object>(form, data.newListData, true);
            //        List<object> recordsOld = _genericService.CreateObjectsFromJArray<object>(form, data.originalListData, true);

            //        string resultMessage = "";
            //        bool resultSuccess = false;
            //        List<object> resultObj = new List<object>();
            //        for (int i = 0; i < recordsNew.Count; i++)
            //        {
            //            GenericResponse singleResult = await _genericService.UpdateDataByForm(form, recordsOld[i], recordsNew[i]);
            //            resultMessage = String.Join(";", resultMessage, singleResult.Message);
            //            resultObj.Add(singleResult._extraLoad);
            //            if (singleResult.Success) resultSuccess = true;
            //        }
            //        result = new GenericResponse(resultObj, resultMessage);
            //    }

            //    //_logger.LogDebug($"DataFormUpdate: {form}");

            //    if (!result.Success)
            //        return BadRequest(result); //.Message);

            //    // Verifica se `lowercase` è `true` e applica la trasformazione
            //    if (data.lowerCase)
            //    {
            //        result._extraLoad = ConvertToLowercaseRecursive(result._extraLoad);
            //    }

            //    return Ok(result);
            //    //return Ok(result._extraLoad);
            //}
            //catch (Exception ex)
            //{
            //    return BadRequest(ex.Message.toResponse());
            //}
            try
            {
                string check = CheckDataFormSaveParameter(data, true);
                if (check != "") return BadRequest(check);

                GenericResponse result = await _genericService.DataFormSaveAsync(data, DataFormSaveActions.Update, data.lowerCase);

                if (!result.Success)
                    return BadRequest(result);//.Message);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message.toResponse());
            }
        }

        [HttpPost("DataFormAdd")]
        public async Task<IActionResult> DataFormAdd([FromBody] DataFormPostParameter data)
        {
            //try
            //{
            //    string form = data.idView == null ? data.Form : GetFormByView(data.idView.Value);
            //    if ((form ?? "") == "")
            //        return BadRequest("La View non ha form associate");

            //    if (data.newData == null && data.newListData == null)
            //    {
            //        return BadRequest(new GenericResponse("Nessun dato fornito"));
            //    }

            //    GenericResponse result = new GenericResponse(true);
            //    if (data.newData != null)
            //    {

            //        object recordNew = _genericService.CreateObjectFromJObject<object>(form, data.newData, false);

            //        result = await _genericService.AddDataByForm(form, recordNew, data.force.Value);
            //    }
            //    if (data.newListData != null)
            //    {
            //        List<object> recordsNew = _genericService.CreateObjectsFromJArray<object>(form, data.newListData, true);

            //        List<string> resultMessage = [];
            //        bool resultSuccess = false;
            //        List<object> resultObj = new List<object>();
            //        for (int i = 0; i < recordsNew.Count; i++)
            //        {
            //            GenericResponse singleResult = await _genericService.AddDataByForm(form, recordsNew[i], data.force.Value);
            //            resultMessage.Add(singleResult.Message);// = String.Join(";", resultMessage, singleResult.Message);
            //            resultObj.Add(singleResult._extraLoad);
            //            if (singleResult.Success) resultSuccess = true;
            //        }
            //        result = new GenericResponse(resultObj, String.Join(";", resultMessage.Where(m => m != "")));
            //    }

            //    if (!result.Success)
            //        return BadRequest(result);//.Message);

            //    // Verifica se `lowercase` è `true` e applica la trasformazione
            //    if (data.lowerCase)
            //    {
            //        result._extraLoad = ConvertToLowercaseRecursive(result._extraLoad);
            //    }
            //    return Ok(result);
            //}
            //catch (Exception ex)
            //{
            //    return BadRequest(ex.Message.toResponse());
            //}
            try
            {
                string check = CheckDataFormSaveParameter(data, false);
                if (check != "") return BadRequest(check);

                GenericResponse result = await _genericService.DataFormSaveAsync(data, DataFormSaveActions.Add, data.lowerCase);

                if (!result.Success)
                    return BadRequest(result);//.Message);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message.toResponse());
            }
        }

        [HttpPost("DataFormAddOrUpdate")]
        public async Task<IActionResult> DataFormAddOrUpdate([FromBody] DataFormPostParameter data)
        {
            //bool resultSuccess = false;
            //string form = data.idView == null ? data.Form ?? "" : GetFormByView(data.idView.Value);
            //if ((form ?? "") == "")
            //    return BadRequest("La View non ha form associate");
            ////Log.Information($"FormAddOrUpdate {form}");

            //if (data.newData == null && data.newListData == null)
            //{
            //    return BadRequest(new GenericResponse("Nessun dato fornito"));
            //}

            //GenericResponse result = new(true);
            //if (data.newData != null)
            //{
            //    object recordNew = _genericService.CreateObjectFromJObject<object>(form!, data.newData, false, true);
            //    //Log.Information($"FormAddOrUpdate {form} object generated");

            //    result = await _genericService.AddOrUpdateDataByForm(form!, recordNew);
            //    resultSuccess = result.Success;
            //}
            //if (data.newListData != null)
            //{
            //    List<object> recordsNew = _genericService.CreateObjectsFromJArray<object>(form!, data.newListData, true);

            //    string resultMessage = "";
            //    List<object?> resultObj = [];
            //    for (int i = 0; i < recordsNew.Count; i++)
            //    {
            //        GenericResponse singleResult = await _genericService.AddOrUpdateDataByForm(form!, recordsNew[i]);
            //        resultMessage = String.Join(";", resultMessage, singleResult.Message);
            //        if (data.lowerCase && singleResult._extraLoad != null)
            //        {
            //            singleResult._extraLoad = ConvertToLowercaseRecursive(singleResult._extraLoad);
            //            resultObj.Add(singleResult._extraLoad);
            //        }
            //        else
            //        if (singleResult.Success) resultSuccess = true;
            //    }
            //    result = new GenericResponse(resultObj!, resultMessage);
            //}

            ////Log.Information($"FormAddOrUpdate done");
            //if (!resultSuccess)
            //    return BadRequest(result); //.Message);

            //// Verifica se `lowercase` è `true` e applica la trasformazione
            //if (data.lowerCase)
            //{
            //    result._extraLoad = ConvertToLowercaseRecursive(result._extraLoad ?? "");
            //    result._extraLoads = ConvertToLowercaseRecursive(result._extraLoads);
            //}
            //return Ok(result);
            try
            {
                string check = CheckDataFormSaveParameter(data, false);
                if (check != "") return BadRequest(check);

                GenericResponse result = await _genericService.DataFormSaveAsync(data, DataFormSaveActions.AddOrUpdate, data.lowerCase);

                if (!result.Success)
                    return BadRequest(result);//.Message);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message.toResponse());
            }
        }

        [HttpPost("DataFormDelete")]
        public async Task<IActionResult> DataFormDelete([FromBody] DataFormPostParameter data)
        //[FromForm] string form, [FromForm] string Record)
        {
            try
            {
                string form = data.idView == null ? data.Form ?? "" : GetFormByView(data.idView.Value);
                if ((form ?? "") == "")
                    return BadRequest("La View non ha form associate");

                object recordNew = _genericService.CreateObjectFromJObject<object>(form!, data.newData!, true);

                GenericResponse result = await _genericService.DeleteDataByForm(form!, recordNew);
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
        public async Task<IActionResult> DataFormChildAdd([FromBody] DataFormChildElem data)
        {
            try
            {
                string form = data.idView == null ? data.Form ?? "" : GetFormByView(data.idView.Value);

                if (form.isNullOrempty()) return BadRequest("La View non ha form associate");
                if (data.FormChild.isNullOrempty()) return BadRequest("Non hai specificato il child");
                if (data.parentData == null) return BadRequest("Non hai fornito il record parent");

                string formChild = data.FormChild!;
                object record = _genericService.CreateObjectFromJObject<object>(form, data.parentData!, true);

                List<object> childElements = [];
                if (data.child1 != null) childElements.Add(data.child1);
                if (data.child2 != null) childElements.Add(data.child2);
                if (data.child3 != null) childElements.Add(data.child3);

                GenericResponse result = await _genericService.AddDataByFormChild(form, formChild, record, childElements);
                if (!result.Success)
                    return BadRequest(result.Message);

                // Verifica se `lowercase` è `true` e applica la trasformazione
                if (data.lowerCase)
                {
                    result._extraLoad = ConvertToLowercaseRecursive(result._extraLoad ?? "");
                }

                return Ok(result._extraLoad);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("DataFormAction")]
        public async Task<IActionResult> DataFormAction([FromBody] DataFormPostParameter data)
        //[FromForm] string form, [FromForm] string Record)
        {
            try
            {
                if (data.idView == null) return BadRequest("Non hai specificato la view");
                int idView = (int)data.idView!;
                string form = data.idView == null ? data.Form ?? "" : GetFormByView(data.idView.Value);
                if (form.isNullOrempty()) return BadRequest("La View non ha form associate");
                if (data.FormField.isNullOrempty()) return BadRequest("Non hai specificato l'azione");
                if (data.Parameters == null || data.Parameters.parameters == null) return BadRequest("Non hai specificato i parametri");

                string actionName = data.FormField!;

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
        public async Task<IActionResult> DataFormActionRow([FromBody] DataFormPostParameter data)
        //[FromForm] string form, [FromForm] string Record)
        {
            try
            {
                if (data.idView == null) return BadRequest("Non hai specificato la view");
                int idView = (int)data.idView!;
                string form = data.idView == null ? data.Form ?? "" : GetFormByView(data.idView.Value);
                if (form.isNullOrempty()) return BadRequest("La View non ha form associate");
                if (data.FormField.isNullOrempty()) return BadRequest("Non hai specificato l'azione");
                if (data.newData == null) return BadRequest("Non hai fornito il recod associato");

                string actionName = data.FormField!;
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

            string dbName = data["DbName"] == null ? "" : data["DbName"]!.ToString();
            string procName = data["ProcedureName"] == null ? "" : data["ProcedureName"]!.ToString();
            List<BecaParameter> parameters = data["Parameters"] == null ? [] : [.. data["Parameters"]!.ToObject<BecaParameters>()!.parameters];

            if (procName == "") return BadRequest("Non hai indicato la procecure e/o il DB");

            GenericResponse result = await _genericService.ExecCommand(dbName, procName, parameters);
            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result);
        }

        private async Task<IActionResult> GetContent(IEnumerable<object> res, bool lowerCase, System.Threading.CancellationToken cancel)
        {
            //DefaultContractResolver contractResolver = new()
            //{
            //    NamingStrategy = lowerCase  
            //        ? new Extensions.ServiceExtensions.LowerNamingStrategy() { OverrideSpecifiedNames = false }
            //        : new Extensions.ServiceExtensions.LowerCamelCaseNamingStrategy() { OverrideSpecifiedNames = false }
            //};

            //var json = Newtonsoft.Json.JsonConvert.SerializeObject(res, new Newtonsoft.Json.JsonSerializerSettings
            //{
            //    ContractResolver = contractResolver,
            //    Formatting = Newtonsoft.Json.Formatting.Indented
            //});
            var settings = lowerCase ? LowerCaseSettings : LowerCamelCaseSettings;
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(res, settings);
            //var json = System.Text.Json.JsonSerializer.Serialize(res);
            if (!string.IsNullOrEmpty(Request.Headers.AcceptEncoding))
            {
                var encodings = Request.Headers.AcceptEncoding.ToString().Split(',', StringSplitOptions.TrimEntries);
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
        private static List<object?> ConvertToLowercaseRecursive(List<object?> obj)
        {
            List<object?> result = [];
            foreach (var item in obj) { result.Add(ConvertToLowercaseRecursive(item)); }
            return result;
        }

        private static object? ConvertToLowercaseRecursive(object? obj)
        {
            //DefaultContractResolver contractResolver = new()
            //{
            //    NamingStrategy = new Extensions.ServiceExtensions.LowerNamingStrategy() { OverrideSpecifiedNames = false }
            //};

            //var json = Newtonsoft.Json.JsonConvert.SerializeObject(obj, new Newtonsoft.Json.JsonSerializerSettings
            //{
            //    ContractResolver = contractResolver,
            //    Formatting = Newtonsoft.Json.Formatting.Indented
            //});
            //var json = Newtonsoft.Json.JsonConvert.SerializeObject(obj, LowerCaseSettings);
            //return json;
            if (obj is null) return null;

            // Se è un dizionario, converte le chiavi in minuscolo e ricorsivamente i valori
            if (obj is IDictionary dict)
            {
                var newDict = new Dictionary<string, object?>();
                foreach (DictionaryEntry entry in dict)
                {
                    if (entry.Key is string key)
                        newDict[key.ToLower()] = ConvertToLowercaseRecursive(entry.Value);
                }
                return newDict;
            }

            // Se è una collezione (array o lista)
            if (obj is IEnumerable enumerable && obj is not string)
            {
                var list = new List<object?>();
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
                var dictionary = new Dictionary<string, object?>();
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

        private string CheckDataFormPostParameter(DataFormPostParameter data, bool isField, out string form)
        {
            form = data.idView == null ? (data.Form ?? "") : GetFormByView(data.idView.Value);
            if (data.idView == null && data.Form == null) return ("Non hai specificato nè la view nè la form");
            if (form.isNullOrempty()) return ("La view non ha form associate");
            if (isField && data.FormField.isNullOrempty()) return ("Non hai specificato il campo");
            if (data.Parameters == null) return ("Manca la sezione parametri");
            return "";
        }

        private string CheckDataFormSaveParameter(DataFormPostParameter data, bool requiresOldData = false)
        {
            string form = data.idView == null ? data.Form ?? "" : GetFormByView(data.idView.Value);
            if (string.IsNullOrWhiteSpace(form))
                return "La View non ha form associate";

            if (data.newData == null && data.newListData == null)
                return "Nessun dato fornito";

            if (requiresOldData)
            {
                if (data.newData != null && data.originalData == null)
                    return "Nessun dato di confronto fornito";
                if (data.newListData != null && data.originalListData == null)
                    return "Nessun dato di confronto fornito";
            }
            return "";
        }

        private static readonly DefaultContractResolver LowerNamingResolver = new()
        {
            NamingStrategy = new LowerNamingStrategy() { OverrideSpecifiedNames = false }
        };

        private static readonly DefaultContractResolver LowerCamelCaseNamingResolver = new()
        {
            NamingStrategy = new LowerCamelCaseNamingStrategy() { OverrideSpecifiedNames = true }
        };

        private static readonly JsonSerializerSettings LowerCaseSettings = new JsonSerializerSettings
        {
            ContractResolver = LowerNamingResolver,
            Formatting = Formatting.Indented
        };

        private static readonly JsonSerializerSettings LowerCamelCaseSettings = new JsonSerializerSettings
        {
            ContractResolver = LowerCamelCaseNamingResolver,
            Formatting = Formatting.Indented
        };

    }


    internal class Compressor
    {
        public static async Task<byte[]> BrotliCompressBytesAsync(byte[] bytes, System.Threading.CancellationToken cancel)
        {
            using var outputStream = new MemoryStream();
            using (var compressionStream = new BrotliStream(outputStream, CompressionLevel.Optimal))
            {
                await compressionStream.WriteAsync(bytes, cancel);
            }
            return outputStream.ToArray();
        }
        public static async Task<byte[]> GZipCompressBytesAsync(byte[] bytes, System.Threading.CancellationToken cancel)
        {
            using var outputStream = new MemoryStream();
            using (var compressionStream = new GZipStream(outputStream, CompressionLevel.Optimal))
            {
                await compressionStream.WriteAsync(bytes, cancel);
            }
            return outputStream.ToArray();
        }
    }
}
