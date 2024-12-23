using BecaWebService.Controllers;
using BecaWebService.Helpers;
using BecaWebService.Models.Communications;
using Contracts;
using Entities.Models;
using ExtensionsLib;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace BecaWebService.Services
{
    public class GenericService : IGenericService
    {
        private readonly IGenericRepository _genericRepository;
        private readonly ILogger<GenericService> _logger;

        public GenericService(IGenericRepository genericRepository, ILogger<GenericService> logger)
        {
            this._genericRepository = genericRepository;
            _logger = logger;
        }

        public string GetFormByView(int idView)
        {
            return _genericRepository.GetFormByView(idView);
            //if (form == null) throw new ArgumentException("La View non ha form associate");
            // return form;
        }

        public GenericResponse GetDataByView(int idView, List<BecaParameter> parameters)
        {
            try
            {
                return GetDataByForm(GetFormByView(idView), parameters);
            }
            catch (Exception ex)
            {
                return ex.Message.toResponse();
            }
        }

        public GenericResponse GetDataByForm(string Form, List<BecaParameter> parameters)
        {
            try
            {
                return _genericRepository.GetDataByForm<object>(Form, parameters).toResponse();
            }
            catch (Exception ex)
            {
                return ex.Message.toResponse();
            }
        }

        public GenericResponse GetDataBySP(string dbName, string Form, List<BecaParameter> parameters)
        {
            try
            {
                return _genericRepository.GetDataBySP<object>(dbName, Form, parameters).toResponse();
            }
            catch (Exception ex)
            {
                return ex.Message.toResponse();
            }
        }

        public T CreateObjectFromJObject<T>(string Form, JObject jsonRecord, bool view) where T : class, new() =>
            this.CreateObjectFromJObject<T>(Form, jsonRecord, view, false);

        public T CreateObjectFromJObject<T>(string Form, JObject jsonRecord, bool view, bool partial) where T : class, new()
        {
            var obj = _genericRepository.getFormObject<T>(Form, view, (partial ? jsonRecord.Properties().Select(p => p.Name.ToLower()).ToList() : new List<string>()));
            foreach (JProperty jproperty in jsonRecord.Properties())
            {
                foreach (PropertyInfo property in obj.GetType().GetProperties())
                {
                    if (jproperty.Name.ToLower() == property.Name.ToLower())
                    {
                        if (property.PropertyType.FullName.ToLower().Contains("date"))
                        {
                            if (jsonRecord[jproperty.Name].Type.ToString() == "Null")
                            {
                                obj.SetPropertyValue(property.Name, null);
                            }
                            else
                            {
                                obj.SetPropertyValue(
                                    property.Name,
                                    DateTimeOffset.Parse((string)jsonRecord[jproperty.Name]).UtcDateTime);
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

        public List<T> CreateObjectsFromJArray<T>(string Form, JArray jsonRecord, bool view) where T : class, new() =>
            this.CreateObjectsFromJArray<T>(Form, jsonRecord, view, false);

        public List<T> CreateObjectsFromJArray<T>(string Form, JArray jsonRecords, bool view, bool partial) where T : class, new()
        {
            var objects = new List<T>();

            foreach (JObject jsonRecord in jsonRecords)
            {
                var obj = _genericRepository.getFormObject<T>(Form, view,
                    (partial ? jsonRecord.Properties().Select(p => p.Name.ToLower()).ToList() : new List<string>()));

                foreach (JProperty jproperty in jsonRecord.Properties())
                {
                    foreach (PropertyInfo property in obj.GetType().GetProperties())
                    {
                        if (jproperty.Name.ToLower() == property.Name.ToLower())
                        {
                            if (property.PropertyType.FullName.ToLower().Contains("date"))
                            {
                                if (jsonRecord[jproperty.Name].Type.ToString() == "Null")
                                {
                                    obj.SetPropertyValue(property.Name, null);
                                }
                                else
                                {
                                    obj.SetPropertyValue(
                                        property.Name,
                                        DateTimeOffset.Parse((string)jsonRecord[jproperty.Name]).UtcDateTime);
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


        public object CreateObjectFromJSON<T>(string jsonRecord) where T : class, new()
        {
            return _genericRepository.CreateObjectFromJSON<object>(jsonRecord);
        }

        public T CreateObjectFromJSON<T>(string Form, string jsonRecord) where T : class, new()
        {
            return _genericRepository.CreateObjectFromJSON<T>(Form, jsonRecord);
        }

        public async Task<GenericResponse> AddOrUpdateDataByForm(string Form, object record)
        {
            try
            {
                return (await _genericRepository.AddOrUpdateDataByForm<object>(Form, record)).toResponse();
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

                object res = await _genericRepository.AddDataByForm<object>(Form, record);
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
                object res = await _genericRepository.AddDataByFormChild<object>(form, formChild, parent, childElements);
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
            try {
                string res =  await _genericRepository.ActionByForm(idview, actionName, record);
                if (res!="") return res.toResponse();
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
                string res = await _genericRepository.ActionByForm(idview, actionName, parameters);
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

        public GenericResponse GetDataByViewField(int idView, string field, List<BecaParameter> parameters)
        {
            return GetDataByFormField(GetFormByView(idView), field, parameters);
        }

        public GenericResponse GetDataByFormField(string Form, string field, List<BecaParameter> parameters)
        {
            try
            {
                return _genericRepository.GetDataByFormField(Form, field, parameters).toResponse();
            }
            catch (Exception ex)
            {
                return ex.Message.toResponse();
            }
        }

        public GenericResponse GetDataByFormChildSelect(string Form, string childForm, short sqlNumber, object parent)
        {
            try
            {
                return _genericRepository.GetDataByFormChildSelect(Form, childForm, sqlNumber, parent).toResponse();
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

        public object GetPanelsByForm(string Form, List<BecaParameter> parameters)
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