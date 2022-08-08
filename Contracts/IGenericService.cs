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
        object CreateObjectFromJSON<T>(string jsonRecord) where T : class, new();
        T CreateObjectFromJObject<T>(string Form, JObject jsonRecord) where T : class, new();
        T CreateObjectFromJSON<T>(string Form, string jsonRecord) where T : class, new();

        string GetFormByView(int idView);

        List<object> GetDataByView(Int32 idView, List<BecaParameter> parameters);
        List<object> GetDataByForm(string Form, List<BecaParameter> parameters);

        Task<GenericResponse> UpdateDataByView(Int32 idView, object recordOld, object recordNew);
        Task<GenericResponse> UpdateDataByForm(string Form, object recordOld, object recordNew);

        Task<GenericResponse> AddDataByView(Int32 idView, object record, bool forceInsert);
        Task<GenericResponse> AddDataByForm(string Form, object record, bool forceInsert);
        Task<GenericResponse> AddDataByFormChild(string form, string formChild, object parent, List<object> childElements);

        Task<GenericResponse> DeleteDataByView(Int32 idView, object record);
        Task<GenericResponse> DeleteDataByForm(string Form, object record);

        List<object> GetDataByViewField(Int32 idView, string field, List<BecaParameter> parameters);
        List<object> GetDataByFormField(string Form, string field, List<BecaParameter> parameters);

        List<object> GetDataByFormChildSelect(string Form, string childForm, short sqlNumber, object parent);

        List<object> GetDataBySQL(string dbName, string sql, List<BecaParameter> parameters);
        List<object> GetDataByFormLevel(string Form, int subLevel, List<BecaParameter> parameters);

        object GetPanelsByForm(string Form, List<BecaParameter> parameters);
        ViewChart GetGraphByFormField(string Form, string field, List<BecaParameter> parameters);
        Task<GenericResponse> ExecCommand(string dbName, string procName, List<BecaParameter> parameters);
    }
}
