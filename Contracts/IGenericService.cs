using BecaWebService.Models.Communications;
using Entities.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    public interface IGenericService
    {
        T CreateObjectFromJObject<T>(string Form, JObject jsonRecord) where T : class, new();
        T CreateObjectFromJSON<T>(string Form, string jsonRecord) where T : class, new();
        List<object> GetDataByForm(string Form, List<BecaParameter> parameters);
        Task<GenericResponse> UpdateDataByForm(string Form, object recordOld, object recordNew);
        Task<GenericResponse> AddDataByForm(string Form, object record, bool forceInsert);
        Task<GenericResponse> DeleteDataByForm(string Form, object record);
        List<object> GetDataByFormField(string Form, string field, List<BecaParameter> parameters);
        List<object> GetDataBySQL(string dbName, string sql, List<BecaParameter> parameters);
        List<object> GetDataByFormLevel(string Form, int subLevel, List<BecaParameter> parameters);
        object GetPanelsByForm(string Form, List<BecaParameter> parameters);
        ViewChart GetGraphByFormField(string Form, string field, List<BecaParameter> parameters);
        Task<GenericResponse> ExecCommand(string dbName, string procName, List<BecaParameter> parameters);
    }
}
