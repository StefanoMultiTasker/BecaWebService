using BecaWebService.Controllers;
using BecaWebService.ExtensionsLib;
using BecaWebService.Helpers;
using BecaWebService.Models.Communications;
using BecaWebService.Services.Custom;
using Contracts;
using Contracts.Custom;
using Entities.Communications;
using Entities.Models;
using Entities.Models.Custom;
using ExtensionsLib;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Ocsp;
using Repository;
using System.Diagnostics;
using System.Reflection;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BecaWebService.Services
{
    public class GenericService : IGenericService, IDisposable
    {
        private readonly IGenericRepository _genericRepository;
        private readonly IBecaRepository _becaRepository;
        private readonly IMailService _mailService;
        private readonly ILoggerManager _logger;
        private readonly string id = "";
        private readonly IWebHostEnvironment _env;

        public GenericService(IGenericRepository genericRepository, IBecaRepository becaRepository, IMailService mailService, ILoggerManager logger, IWebHostEnvironment env)
        {
            this._genericRepository = genericRepository;
            _becaRepository = becaRepository;
            _mailService = mailService;
            _logger = logger;

            id = Guid.NewGuid().ToString();
            _logger.LogDebug($"Creato genericSerive {id}");
            _env = env;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _logger.LogDebug("Distrutto genericService {id}");
        }

        public int? GetUserId() => _genericRepository.GetLoggedUser() == null ? null : _genericRepository.GetLoggedUser()!.idUtente;
        public int? GetCompanyId() => _genericRepository.GetActiveCompany() == null ? null : _genericRepository.GetActiveCompany()!.idCompany;


        public string GetFormByView(int idView)
        {
            return _genericRepository.GetFormByView(idView, null);
            //if (form == null) throw new ArgumentException("La View non ha form associate");
            // return form;
        }

        public GenericResponse GetDataByView(int idView, List<BecaParameter> parameters, int? pageNumber = null, int? pageSize = null, bool lowerCase = false)
        {
            try
            {
                return GetDataByForm(GetFormByView(idView), parameters, pageNumber, pageSize, lowerCase);
            }
            catch (Exception ex)
            {
                return ex.Message.toResponse();
            }
        }

        public GenericResponse GetDataByForm(string Form, List<BecaParameter> parameters, int? pageNumber = null, int? pageSize = null, bool lowerCase = false)
        {
            try
            {
                return _genericRepository.GetDataByForm<object>(Form, parameters, true, true, pageNumber, pageSize, lowerCase).toResponse();
            }
            catch (Exception ex)
            {
                return ex.Message.toResponse();
            }
        }

        //public GenericResponse GetDataBySP(string dbName, string Form, List<BecaParameter> parameters)
        //{
        //    try
        //    {
        //        return _genericRepository.GetDataBySP<object>(dbName, Form, parameters).toResponse();
        //    }
        //    catch (Exception ex)
        //    {
        //        return ex.Message.toResponse();
        //    }
        //}

        public T CreateObjectFromJObject<T>(string Form, JObject jsonRecord, bool view) where T : class, new() =>
            this.CreateObjectFromJObject<T>(Form, jsonRecord, view, false);

        public T CreateObjectFromJObject<T>(string Form, JObject jsonRecord, bool view, bool partial) where T : class, new()
        {
            var obj = _genericRepository.GetFormObject<T>(Form, view, (partial ? jsonRecord.Properties().Select(p => p.Name.ToLower()).ToList() : []));
            var properties = obj.GetType().GetProperties(); // Ottieni le proprietà una sola volta //
            foreach (JProperty jproperty in jsonRecord.Properties())
            {
                foreach (PropertyInfo property in properties)
                {
                    if (jproperty.Name.Equals(property.Name, StringComparison.CurrentCultureIgnoreCase))
                    {
                        if ((property.PropertyType.FullName ?? "").Contains("date", StringComparison.CurrentCultureIgnoreCase))
                        {
                            if (jsonRecord[jproperty.Name]!.Type.ToString() == "Null")
                            {
                                obj.SetPropertyValue(property.Name, null);
                            }
                            else
                            {
                                obj.SetPropertyValue(
                                    property.Name,
                                    DateTimeOffset.Parse((string)jsonRecord[jproperty.Name]!).UtcDateTime);
                            }
                        }
                        else
                        {
                            if (!jsonRecord[jproperty.Name].IsNullOrEmpty()) obj.SetPropertyValue(property.Name, jsonRecord[jproperty.Name]);
                        }
                        break;
                    }
                }
            }
            //var res = jsonRecord.ToAnonymousType(obj);
            return obj;
        }
        public T CreateObjectFromExpando<T>(string Form, dynamic jsonRecord, bool view, bool partial) where T : class, new()
        {
            var dict = (IDictionary<string, object?>)jsonRecord;

            var partialList = partial
                ? dict.Keys.Select(k => k.ToLower()).ToList()
                : new List<string>();

            var obj = _genericRepository.GetFormObject<T>(Form, view, partialList);
            var properties = obj.GetType().GetProperties();

            foreach (var kvp in dict)
            {
                var propName = kvp.Key;
                var value = kvp.Value;

                foreach (var property in properties)
                {
                    if (string.Equals(property.Name, propName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        if ((property.PropertyType.FullName ?? "").Contains("date", StringComparison.CurrentCultureIgnoreCase))
                        {
                            if (value == null)
                            {
                                obj.SetPropertyValue(property.Name, null);
                            }
                            else
                            {
                                obj.SetPropertyValue(
                                    property.Name,
                                    DateTimeOffset.TryParse(value.ToString(), out var dto)
                                        ? dto.UtcDateTime
                                        : value);
                            }
                        }
                        else
                        {
                            if (value != null)
                                obj.SetPropertyValue(property.Name, value);
                        }

                        break;
                    }
                }
            }

            return obj;
        }

        public List<T> CreateObjectsFromJArray<T>(string Form, JArray jsonRecord, bool view) where T : class, new() =>
            this.CreateObjectsFromJArray<T>(Form, jsonRecord, view, false);

        public List<T> CreateObjectsFromJArray<T>(string Form, JArray jsonRecords, bool view, bool partial) where T : class, new()
        {
            var objects = new List<T>();

            foreach (JObject jsonRecord in jsonRecords.Cast<JObject>())
            {
                var obj = _genericRepository.GetFormObject<T>(Form, view,
                    (partial ? jsonRecord.Properties().Select(p => p.Name.ToLower()).ToList() : []));

                foreach (JProperty jproperty in jsonRecord.Properties())
                {
                    foreach (PropertyInfo property in obj.GetType().GetProperties())
                    {
                        if (jproperty.Name.Equals(property.Name, StringComparison.CurrentCultureIgnoreCase))
                        {
                            if ((property.PropertyType.FullName ?? "").Contains("date", StringComparison.CurrentCultureIgnoreCase))
                            {
                                if (jsonRecord[jproperty.Name]!.Type.ToString() == "Null")
                                {
                                    obj.SetPropertyValue(property.Name, null);
                                }
                                else
                                {
                                    obj.SetPropertyValue(
                                        property.Name,
                                        DateTimeOffset.Parse((string)jsonRecord[jproperty.Name]!).UtcDateTime);
                                }
                            }
                            else
                            {
                                if (!jsonRecord[jproperty.Name].IsNullOrEmpty())
                                {
                                    obj.SetPropertyValue(property.Name, jsonRecord[jproperty.Name]);
                                }
                            }
                            break;
                        }
                    }
                }
                objects.Add(obj);
            }
            return objects;
        }


        public object? CreateObjectFromJSON<T>(string jsonRecord) where T : class, new()
        {
            return _genericRepository.CreateObjectFromJSON<object>(jsonRecord);
        }

        public T? CreateObjectFromJSON<T>(string Form, string jsonRecord) where T : class, new()
        {
            return _genericRepository.CreateObjectFromJSON<T>(Form, jsonRecord);
        }

        public async Task<GenericResponse> AddOrUpdateDataByForm(string Form, object record)
        {
            try
            {
                var res = await _genericRepository.AddOrUpdateDataByForm<object>(Form, record);
                return res == null ? new GenericResponse("Il record non è stata salvato") : res.toResponse();
            }
            catch (Exception ex)
            {
                return ex.Message.toResponse();
            }
        }

        public async Task<GenericResponse> UpdateDataByView(int idView, object recordOld, object recordNew)
        {
            return await UpdateDataByForm(GetFormByView(idView), recordOld, recordNew);
        }

        public async Task<GenericResponse> UpdateDataByForm(string Form, object recordOld, object recordNew)
        {
            try
            {
                List<object> data = _genericRepository.GetDataByForm<object>(Form, recordOld);
                if (data.Count == 0) return new GenericResponse("Il record non esiste più");

                var res = await _genericRepository.UpdateDataByForm<object>(Form, recordOld, recordNew);
                if (res.data == null) return new GenericResponse(res.message ?? "Il record non è stato aggiornato");

                return new GenericResponse(res.data, res.message); // res.toResponse();    
                //return _genericRepository.GetDataByForm<object>(Form, recordNew).toResponse();
            }
            catch (Exception ex)
            {
                return ex.Message.toResponse();
            }
        }

        public async Task<GenericResponse> AddDataByView(int idView, object record, bool forceInsert)
        {
            return await UpdateDataByForm(GetFormByView(idView), record, forceInsert);
        }

        public async Task<GenericResponse> AddDataByForm(string Form, object record, bool forceInsert)
        {
            try
            {
                if (!forceInsert)
                {
                    List<object> data = _genericRepository.GetDataByForm<object>(Form, record);
                    if (data.Count > 0) return new GenericResponse("Il record esiste già");
                }

                object? res = await _genericRepository.AddDataByForm<object>(Form, record);
                if (res == null) return new GenericResponse("Il record non è stato inserito");
                return res.toResponse();
            }
            catch (Exception ex)
            {
                return ex.Message.toResponse();
            }
        }

        public async Task<GenericResponse> AddDataByFormChild(string form, string formChild, object parent, List<object> childElements)
        {
            try
            {
                object? res = await _genericRepository.AddDataByFormChild<object>(form, formChild, parent, childElements);
                if (res == null) return new GenericResponse("Il record non è stato inserito");
                return res.toResponse();
            }
            catch (Exception ex)
            {
                return ex.Message.toResponse();
            }
        }

        public async Task<GenericResponse> DeleteDataByView(int idView, object record)
        {
            return await DeleteDataByForm(GetFormByView(idView), record);
        }

        public async Task<GenericResponse> DeleteDataByForm(string Form, object record)
        {
            //List<object> data = _genericRepository.GetDataByForm<object>(Form, record);
            //if (data.Count == 0) return new GenericResponse("Il record non esiste più");

            try
            {
                int res = await _genericRepository.DeleteDataByForm<object>(Form, record);
                if (res == 0) return new GenericResponse("Il record non è stato eliminato");
                return res.toResponse();
            }
            catch (Exception ex)
            {
                return ex.Message.toResponse();
            }
        }

        public async Task<GenericResponse> ActionByForm(int idview, string form, string actionName, object record)
        {
            try
            {
                string res = await _genericRepository.ActionByForm(idview, actionName, record);
                if (res != "") return res.toResponse();
                //if (form != "")
                //{
                //    List<object> data = _genericRepository.GetDataByForm<object>(form, record);
                //    if (data.Count() > 0)
                //        return data[0].toResponse();
                //    else
                //        return "Non trovo più il record originale, qualcosa deve essere andato male".toResponse();
                //}
                else return true.toResponse();
            }
            catch (Exception ex) { return ex.Message.toResponse(); }
        }

        public async Task<GenericResponse> ActionByForm(int idview, string form, string actionName, List<BecaParameter> parameters)
        {
            try
            {
                BecaViewAction? action = _becaRepository.BecaViewActions(actionName);
                if (action == null) return $"L'azione {actionName} non esiste".toResponse();

                if (!action.Command.isNullOrempty() && !action.ConnectionName.isNullOrempty())
                {
                    string res = await _genericRepository.ActionByForm(idview, actionName, parameters);
                    if (res != "") return res.toResponse();
                }
                if ((action.sqlEmailOptions ?? "") != "")
                {
                    List<object> invii = [];
                    if (action.sqlEmailOptions!.Contains("select", StringComparison.CurrentCultureIgnoreCase))
                    {
                        invii = _genericRepository.GetDataBySQL(action.ConnectionName!, action.Command!, parameters);
                    }
                    else
                    {
                        invii = _genericRepository.GetDataBySP<object>(action.ConnectionName!, action.sqlEmailOptions, parameters);
                    }
                    foreach (object invio in invii)
                    {
                        SendMailOptions options = new()
                        {
                            Sender = new SendMailOptionsOrigin() { Type = "EMail", Value = invio.GetPropertyString("emailFrom") },
                            Dest = new SendMailOptionsOrigin() { Type = "EMail", Value = invio.GetPropertyString("emailTo") },
                            Subject = new SendMailOptionsOrigin() { Type = "*", Value = invio.GetPropertyString("emailSubject") },
                            Text = new SendMailOptionsOrigin() { Type = "*", Value = invio.GetPropertyString("emailText") },
                        };
                        if (invio.HasPropertyValue("emailCC") && (invio.GetPropertyString("emailCC") ?? "") != "") options.CC = new SendMailOptionsOrigin() { Type = "EMail", Value = invio.GetPropertyString("emailCC") };
                        if (invio.HasPropertyValue("emailCCN") && (invio.GetPropertyString("emailCCN") ?? "") != "") options.CCN = new SendMailOptionsOrigin() { Type = "EMail", Value = invio.GetPropertyString("emailCCN") };
                        _mailService.Send(options);
                    }
                }

                //if (form != "" )
                //{
                //    List<object> data = _genericRepository.GetDataByForm<object>(form, record);
                //    if (data.Count() > 0)
                //        return data[0].toResponse();
                //    else
                //        return "Non trovo più il record originale, qualcosa deve essere andato male".toResponse();
                //}
                return true.toResponse();
            }
            catch (Exception ex) { return ex.Message.toResponse(); }
        }

        public GenericResponse GetDataByViewField(int idView, string field, List<BecaParameter> parameters, bool lowerCase)
        {
            return GetDataByFormField(GetFormByView(idView), field, parameters, lowerCase);
        }

        public GenericResponse GetDataByFormField(string Form, string field, List<BecaParameter> parameters, bool lowerCase)
        {
            try
            {
                return _genericRepository.GetDataByFormField(Form, field, parameters, lowerCase).toResponse();
            }
            catch (Exception ex)
            {
                return ex.Message.toResponse();
            }
        }

        public async Task<GenericResponse> GetDataPackAsync(DataFormPostParameters req, bool lowerCase)
        {
            //Dictionary<string, long> timers = [];
            //var totalwatch = System.Diagnostics.Stopwatch.StartNew(); // Avvia il timer
            //var stopwatch = System.Diagnostics.Stopwatch.StartNew(); // Avvia il timer

            var (forms, children, messageForms) = await _genericRepository.GetBecaFormsByRequest(req);
            if (!messageForms.isNullOrempty()) return messageForms.toResponse();
            //stopwatch.Stop();
            //timers.Add("forms", stopwatch.ElapsedMilliseconds);

            //stopwatch.Restart();
            var (fields, customFields, messageFields) = await _genericRepository.GetBecaFormFieldsByRequest(req);
            if (!messageFields.isNullOrempty()) return messageFields.toResponse();
            //stopwatch.Stop();
            //timers.Add("fields", stopwatch.ElapsedMilliseconds);

            //stopwatch.Restart();
            var connections = _genericRepository.GetConnectionsByRequest(forms, fields, customFields);
            foreach (var cnn in connections)
            {
                cnn.Value.Open();
            }
            //stopwatch.Stop();
            //timers.Add("open connections", stopwatch.ElapsedMilliseconds);

            string errorMsg = "";
            var tasks = req.RequestList.Select(async data =>
            {
                //var taskTimer = System.Diagnostics.Stopwatch.StartNew();
                string taskID = Guid.NewGuid().ToString();
                try
                {
                    string form = data.idView == null ? data.Form ?? "" : GetFormByView(data.idView.Value);
                    if (data.FormField == null)
                    {
                        int? pageNumber = data.pageNumber;
                        int? pageSize = data.pageSize;

                        List<BecaParameter> parameters = data.Parameters!.parameters;
                        var _res = await _genericRepository.GetDataByFormAsync<object>(connections, forms, children, fields, form, parameters, true, true, pageNumber, pageSize, lowerCase);
                        return _res;
                    }
                    else
                    {
                        string FormField = data.FormField;
                        List<BecaParameter> parameters = data.Parameters!.parameters;

                        var _res = await _genericRepository.GetDataByFormFieldAsync(connections, forms, fields, customFields, form, FormField, parameters, lowerCase);
                        return _res;
                    }
                }
                catch (Exception ex)
                {
                    errorMsg += $"Il reperimento dei dati per {data.Form}, {data.FormField} è fallito: {ex.Message}\n";
                    return []; // Mantieni un posto nella sequenza
                }
                //finally
                //{
                //    taskTimer.Stop();
                //    timers.Add($"Task per {data.Form},{data.FormField} ({taskID}):", taskTimer.ElapsedMilliseconds);
                //}
            }).ToList();

            // Aspetta che tutte le operazioni siano terminate PRIMA di restituire il risultato
            var results = await Task.WhenAll(tasks);

            //stopwatch.Restart();
            foreach (var cnn in connections)
            {
                cnn.Value.Close();
                cnn.Value.Dispose();
            }
            //stopwatch.Stop();
            //timers.Add("close connections", stopwatch.ElapsedMilliseconds);
            //totalwatch.Stop();
            //timers.Add("TOTALE ", totalwatch.ElapsedMilliseconds);
            //List<object> _timers = timers.Select(t => (object)new { t.Key, t.Value }).ToList();
            //results = [.. results, _timers];

            return new GenericResponse(results, errorMsg);
        }

        private object? isObjectInList(List<object> list, object record, BecaForm form)
        {
            string[] keys = form.PrimaryKey!.Replace(" ", "").Split(",", StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in list)
            {
                int keyOk = 0;
                foreach (var field in keys)
                {
                    if (item.HasPropertyValue(field) && record.HasPropertyValue(field) && item.GetPropertyString(field) == record.GetPropertyString(field))
                    {
                        keyOk++;
                    }
                }
                if (keyOk == keys.Length) return item;
            }
            return null;
        }

        public async Task<GenericResponse> DataFormSaveAsync(DataFormPostParameter data, DataFormSaveActions action, bool lowerCase)
        {
            var (form, fields, message) = await _genericRepository.GetBecaFormObjects4Save(data);
            if (!message.isNullOrempty()) return message.toResponse();

            var connections = _genericRepository.GetConnectionsByRequest([form], fields, null);
            SqlConnection cnn = connections.First().Value;
            cnn.Open();

            bool forceInsert = data.force ?? false;
            bool isFromview = action switch
            {
                DataFormSaveActions.Add => false,
                DataFormSaveActions.Update => true,
                DataFormSaveActions.AddOrUpdate => false,
                _ => false
            };
            bool isPartialFields = action switch
            {
                DataFormSaveActions.Add => false,
                DataFormSaveActions.Update => false,
                DataFormSaveActions.AddOrUpdate => true,
                _ => false
            };


            List<object> recordsNew = data.newData != null
                ? [CreateObjectFromJObject<object>(form!.Form, data.newData, isFromview, isPartialFields)]
                : CreateObjectsFromJArray<object>(form!.Form, data.newListData!, isFromview, isPartialFields);

            List<object> recordsOld = data.originalData != null
                ? [CreateObjectFromJObject<object>(form!.Form, data.originalData, isFromview)]
                : data.originalListData != null ? CreateObjectsFromJArray<object>(form!.Form, data.originalListData!, isFromview) : [];

            List<object>? data2Check = data.Parameters == null
                ? null
                : await _genericRepository.GetDataByFormAsync<object>(connections, new List<BecaForm>() { form }, [], fields, form.Form, data.Parameters!.parameters, true, false, null, null, lowerCase);
            //data2Check = null;
            bool GetRecordAfterInsert = data2Check == null;

            var tasks = recordsNew.Select(async (recordNew, i) =>
            //var tasks = recordsNew.Select((recordNew, i) =>
            //{
            //    return Task.Run(async () =>
                {
                    try
                    {
                        GenericResponse singleResult = new(true);
                        switch (action)
                        {
                            case DataFormSaveActions.Add:
                                if (!forceInsert)
                                {
                                    if (data2Check == null)
                                    {
                                        List<object> data1 = await _genericRepository.GetDataByFormAsync<object>(connections, form, fields, recordNew);
                                        if (data1.Count > 0) return new GenericResponse("Il record esiste già");
                                    }
                                    else
                                    {
                                        if (isObjectInList(data2Check, recordNew, form) != null) return new GenericResponse("Il record esiste già");
                                    }
                                }

                                object? res1 = await _genericRepository.AddDataByFormAsync<object>(cnn, form!, fields, recordNew, GetRecordAfterInsert);
                                if (res1 == null) return new GenericResponse("Il record non è stato inserito");
                                singleResult = res1.toResponse();
                                break;
                            case DataFormSaveActions.Update:
                                var recordOld = recordsOld[i];

                                if (data2Check == null)
                                {
                                    List<object> data2 = await _genericRepository.GetDataByFormAsync<object>(connections, form, fields, recordOld);
                                    if (data2.Count == 0) return new GenericResponse("Il record non esiste più");
                                }
                                else
                                {
                                    if (isObjectInList(data2Check, recordNew, form) == null) return new GenericResponse("Il record non esiste più");
                                }

                                var res2 = await _genericRepository.UpdateDataByFormAsync<object>(cnn, form!, fields, recordOld, recordNew, GetRecordAfterInsert);
                                if (res2.data == null) return new GenericResponse(res2.message ?? "Il record non è stato aggiornato");

                                singleResult = new GenericResponse(res2.data, res2.message);
                                break;
                            case DataFormSaveActions.AddOrUpdate:
                                object? current = null;
                                if (data2Check == null)
                                {
                                    List<object> data3 = await _genericRepository.GetDataByFormAsync<object>(connections, form, fields, recordNew);
                                    if (data3.Count > 0)
                                    {
                                        current = data3[0];
                                    }
                                }
                                else
                                {
                                    current = isObjectInList(data2Check, recordNew, form);
                                }

                                object? res3 = null;
                                if (current == null)
                                    res3 = await _genericRepository.AddDataByFormAsync<object>(cnn, form!, fields, recordNew, GetRecordAfterInsert);
                                else
                                {
                                    object recordOld3 = CreateObjectFromExpando<object>(form.Form, current, true, false);
                                    res3 = (await _genericRepository.UpdateDataByFormAsync<object>(cnn, form!, fields, recordOld3, recordNew, GetRecordAfterInsert)).data;
                                }
                                singleResult = new GenericResponse(res3 ?? "Il record non è stata salvato");
                                break;
                        }
                        return singleResult;
                    }
                    catch (Exception ex)
                    {
                        return $"Il salvataggio del {i + 1}° record per {form.Form} è fallito: {ex.Message}".toResponse();
                    }
                }).ToList();
            //}).ToList();

            // Aspetta che tutte le operazioni siano terminate PRIMA di restituire il risultato
            var results = await Task.WhenAll(tasks);

            if (!GetRecordAfterInsert) {
                data2Check = await _genericRepository.GetDataByFormAsync<object>(connections, new List<BecaForm>() { form }, [], fields, form.Form, data.Parameters!.parameters, true, false, null, null, lowerCase);
                foreach(var result in results)
                {
                    if (result is GenericResponse response && response.Success)
                    {
                        var extraLoad = isObjectInList(data2Check, result._extraLoad!, form);
                        if (extraLoad != null)
                        {
                            response._extraLoad = extraLoad;
                        }
                    }
                }
            }

            cnn.Close();

            GenericResponse finalResult = new(results.Any(r => r.Success), string.Join("\n", results.Select(r => r.Message).Where(m => !string.IsNullOrWhiteSpace(m))));

            if (data.newListData == null)
            {
                finalResult._extraLoad = results.FirstOrDefault()?._extraLoad;
            }
            else
            {
                finalResult._extraLoads = results.Select(r => r._extraLoad).ToList();
            }

            return finalResult;
        }

        public GenericResponse GetDataByFormChildSelect(string Form, string childForm, short sqlNumber, object parent, bool lowerCase)
        {
            try
            {
                return _genericRepository.GetDataByFormChildSelect(Form, childForm, sqlNumber, parent, lowerCase).toResponse();
            }
            catch (Exception ex)
            {
                return ex.Message.toResponse();
            }
        }

        public GenericResponse GetDataBySQL(string dbName, string sql, List<BecaParameter> parameters)
        {
            try
            {
                return _genericRepository.GetDataBySQL(dbName, sql, parameters).toResponse();
            }
            catch (Exception ex)
            {
                return ex.Message.toResponse();
            }
        }

        public GenericResponse GetDataByFormLevel(string Form, int subLevel, List<BecaParameter> parameters)
        {
            try
            {
                return _genericRepository.GetDataByFormLevel(Form, subLevel, parameters).toResponse();
            }
            catch (Exception ex)
            {
                return ex.Message.toResponse();
            }
        }

        public ViewChart GetGraphByFormField(string Form, string field, List<BecaParameter> parameters)
        {
            return _genericRepository.GetGraphByFormField(Form, field, parameters);
        }

        public object? GetPanelsByForm(string Form, List<BecaParameter> parameters)
        {
            return _genericRepository.GetPanelsByForm(Form, parameters);
        }

        public async Task<GenericResponse> ExecCommand(string dbName, string procName, List<BecaParameter> parameters)
        {
            try
            {
                int res = await _genericRepository.ExecuteProcedure(dbName, procName, parameters);
                return new GenericResponse(true);
            }
            catch (Exception ex)
            {
                return ex.Message.toResponse();
            }
        }
    }
}