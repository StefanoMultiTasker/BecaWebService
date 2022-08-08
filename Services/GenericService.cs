using BecaWebService.Models.Communications;
using Contracts;
using Entities.Models;
using ExtensionsLib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace BecaWebService.Services
{
    public class GenericService : IGenericService
    {
        private readonly IGenericRepository _genericRepository;

        public GenericService(IGenericRepository genericRepository)
        {
            this._genericRepository = genericRepository;
        }

        public string GetFormByView(int idView)
        {
            return _genericRepository.GetFormByView(idView);
            //if (form == null) throw new ArgumentException("La View non ha form associate");
           // return form;
        }

        public List<object> GetDataByView(int idView, List<BecaParameter> parameters)
        {
            return GetDataByForm(GetFormByView(idView), parameters);
        }

        public List<object> GetDataByForm(string Form, List<BecaParameter> parameters)
        {
            return _genericRepository.GetDataByForm<object>(Form, parameters);
        }

        public T CreateObjectFromJObject<T>(string Form, JObject jsonRecord) where T : class, new()
        {
            var obj = _genericRepository.getFormObject<T>(Form);
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
                                    DateTimeOffset.Parse(
                                        ((DateTime)jsonRecord[jproperty.Name]).ToString()
                                        ).ToLocalTime().Date);
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

        public object CreateObjectFromJSON<T>(  string jsonRecord) where T : class, new() {
            return _genericRepository.CreateObjectFromJSON<object>(jsonRecord);
        }

        public T CreateObjectFromJSON<T>(string Form, string jsonRecord) where T : class, new()
        {
            return _genericRepository.CreateObjectFromJSON<T>(Form, jsonRecord);
        }

        public async Task<GenericResponse> UpdateDataByView(int idView, object recordOld, object recordNew)
        {
            return await UpdateDataByForm(GetFormByView(idView), recordOld, recordNew);
        }

        public async Task<GenericResponse> UpdateDataByForm(string Form, object recordOld, object recordNew)
        {
            List<object> data = _genericRepository.GetDataByForm<object>(Form, recordOld);
            if (data.Count == 0) return new GenericResponse("Il record non esiste più");

            int res = (int)await _genericRepository.UpdateDataByForm<object>(Form, recordOld, recordNew);
            if (res == 0) return new GenericResponse("Il record non è stato aggiornato");

            List<object> dataNew = _genericRepository.GetDataByForm<object>(Form, recordNew);
            return new GenericResponse(dataNew);
        }

        public async Task<GenericResponse> AddDataByView(int idView, object record, bool forceInsert)
        {
            return await UpdateDataByForm(GetFormByView(idView), record, forceInsert);
        }

        public async Task<GenericResponse> AddDataByForm(string Form, object record, bool forceInsert)
        {
            if (!forceInsert)
            {
                List<object> data = _genericRepository.GetDataByForm<object>(Form, record);
                if (data.Count > 0) return new GenericResponse("Il record esiste già");
            }

            try
            {
                object res = await _genericRepository.AddDataByForm<object>(Form, record);
                if (res == null) return new GenericResponse("Il record non è stato inserito");
                return new GenericResponse(res);
            }
            catch (Exception ex)
            {
                return new GenericResponse(ex.Message);
            }
        }

        public async Task<GenericResponse> AddDataByFormChild(string form, string formChild, object parent, List<object> childElements)
        {
            try
            {
                object res = await _genericRepository.AddDataByFormChild<object>(form, formChild, parent, childElements);
                if (res == null) return new GenericResponse("Il record non è stato inserito");
                return new GenericResponse(res);
            }
            catch (Exception ex)
            {
                return new GenericResponse(ex.Message);
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
                return new GenericResponse(res);
            }
            catch (Exception ex)
            {
                return new GenericResponse(ex.Message);
            }
        }

        public List<object> GetDataByViewField(int idView, string field, List<BecaParameter> parameters)
        {
            return GetDataByFormField(GetFormByView(idView), field, parameters);
        }

        public List<object> GetDataByFormField(string Form, string field, List<BecaParameter> parameters)
        {
            return _genericRepository.GetDataByFormField(Form, field, parameters);
        }

        public List<object> GetDataByFormChildSelect(string Form, string childForm, short sqlNumber, object parent)
        {
            return _genericRepository.GetDataByFormChildSelect(Form,childForm, sqlNumber, parent);
        }

        public List<object> GetDataBySQL(string dbName, string sql, List<BecaParameter> parameters)
        {
            return _genericRepository.GetDataBySQL(dbName, sql, parameters);
        }

        public List<object> GetDataByFormLevel(string Form, int subLevel, List<BecaParameter> parameters)
        {
            return _genericRepository.GetDataByFormLevel(Form, subLevel, parameters);
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
                return new GenericResponse(ex.Message);
            }
        }
    }
}