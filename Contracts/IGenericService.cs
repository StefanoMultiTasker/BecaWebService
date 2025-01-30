using BecaWebService.Models.Communications;
using Entities.Models;
using Newtonsoft.Json.Linq;

namespace Contracts
{
    public interface IGenericService
    {
        int GetUserId();
        int GetCompanyId();
        object CreateObjectFromJSON<T>(string jsonRecord) where T : class, new();
        T CreateObjectFromJObject<T>(string Form, JObject jsonRecord, bool view) where T : class, new();
        T CreateObjectFromJObject<T>(string Form, JObject jsonRecord, bool view, bool partial) where T : class, new();
        List<T> CreateObjectsFromJArray<T>(string Form, JArray jsonRecord, bool view) where T : class, new();
        List<T> CreateObjectsFromJArray<T>(string Form, JArray jsonRecord, bool view, bool partial) where T : class, new();
        T CreateObjectFromJSON<T>(string Form, string jsonRecord) where T : class, new();

        string GetFormByView(int idView);

        GenericResponse GetDataByView(Int32 idView, List<BecaParameter> parameters);
        GenericResponse GetDataByForm(string Form, List<BecaParameter> parameters);
        GenericResponse GetDataBySP(string dbName, string Form, List<BecaParameter> parameters);

        Task<GenericResponse> AddOrUpdateDataByForm(string Form, object record);

        Task<GenericResponse> UpdateDataByView(Int32 idView, object recordOld, object recordNew);
        Task<GenericResponse> UpdateDataByForm(string Form, object recordOld, object recordNew);

        Task<GenericResponse> AddDataByView(Int32 idView, object record, bool forceInsert);
        Task<GenericResponse> AddDataByForm(string Form, object record, bool forceInsert);
        Task<GenericResponse> AddDataByFormChild(string form, string formChild, object parent, List<object> childElements);

        Task<GenericResponse> DeleteDataByView(Int32 idView, object record);
        Task<GenericResponse> DeleteDataByForm(string Form, object record);

        Task<GenericResponse> ActionByForm(int idview, string form, string actionName, object record);
        Task<GenericResponse> ActionByForm(int idview, string form, string actionName, List<BecaParameter> parameters);

        GenericResponse GetDataByViewField(Int32 idView, string field, List<BecaParameter> parameters);
        GenericResponse GetDataByFormField(string Form, string field, List<BecaParameter> parameters);

        GenericResponse GetDataByFormChildSelect(string Form, string childForm, short sqlNumber, object parent);

        GenericResponse GetDataBySQL(string dbName, string sql, List<BecaParameter> parameters);
        GenericResponse GetDataByFormLevel(string Form, int subLevel, List<BecaParameter> parameters);

        object GetPanelsByForm(string Form, List<BecaParameter> parameters);
        ViewChart GetGraphByFormField(string Form, string field, List<BecaParameter> parameters);
        Task<GenericResponse> ExecCommand(string dbName, string procName, List<BecaParameter> parameters);
    }
}
