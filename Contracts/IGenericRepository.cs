using Entities.Communications;
using Entities.Contexts;
using Entities.Models;
using Microsoft.Data.SqlClient;

namespace Contracts
{
    public interface IGenericRepository
    {
        T GetFormObject<T>(string Form, bool view, bool noUpload = false);
        T GetFormObject<T>(string Form, bool view, List<string> fields, bool noUpload = false);
        string GetFormByView(int? idView, string? form);
        object? CreateObjectFromJSON<T>(string jsonRecord) where T : class, new();
        T? CreateObjectFromJSON<T>(string Form, string jsonRecord) where T : class, new();

        List<T> GetDataByForm<T>(string Form, List<BecaParameter> parameters, bool view = true, bool getChildren = true, int? pageNumber = null, int? pageSize = null, bool lowerCase = false) where T : class, new();
        List<T> GetDataByForm<T>(string Form, object record, bool view = true, bool getChildren = true, int? pageNumber = null, int? pageSize = null, bool lowerCase = false) where T : class, new();
        List<T> GetDataBySP<T>(string dbName, string spName, List<BecaParameter> parameters, PropertyNaming namingStrategy = PropertyNaming.AsIs) where T : class, new();

        Task<T?> AddOrUpdateDataByForm<T>(string Form, object record) where T : class, new();
        Task<(T? data, string message)> UpdateDataByForm<T>(string Form, object recordOld, object recordNewd) where T : class, new();
        Task<T?> AddDataByForm<T>(string Form, object record) where T : class, new();
        Task<T?> AddDataByFormChild<T>(string form, string formChild, object parent, List<object> childElements) where T : class, new();
        Task<int> DeleteDataByForm<T>(string Form, object record) where T : class, new();
        Task<string> ActionByForm(int idview, string actionName, object record);
        Task<string> ActionByForm(int idview, string actionName, List<BecaParameter> parameters);

        List<object> GetDataByFormField(string Form, string field, List<BecaParameter> parameters, bool lowerCase);
        List<object> GetDataByFormChildSelect(string Form, string childForm, short sqlNumber, object parent, bool lowerCase);
        Task<int> ExecuteSqlCommandAsync(string dbName, string commandText, params object[] parameters);
        int ExecuteSqlCommand(string dbName, string commandText, params object[] parameters);
        Task<int> ExecuteProcedure(string dbName, string spName, List<BecaParameter> parameters);
        Task CompleteAsync();
        List<object> GetDataBySQL(string dbName, string sql, List<BecaParameter> parameters, bool useidUtente = true, bool lowerCase = false);
        IDictionary<string, object> GetDataDictBySQL(string dbName, string sql, List<BecaParameter> parameters, bool lowerCase);

        List<object> GetDataByFormLevel(string Form, int subLevel, List<BecaParameter> parameters);

        object? GetPanelsByForm(string Form, List<BecaParameter> parameters);
        ViewChart GetGraphByFormField(string Form, string field, List<BecaParameter> parameters);

        Task<(List<BecaForm> forms, List<BecaFormLevels> children, string message)> GetBecaFormsByRequest(DataFormPostParameters req);
        Task<(List<BecaFormField> fields, List<BecaFormFieldLevel> custFields, string message)> GetBecaFormFieldsByRequest(DataFormPostParameters req);
        Task<(BecaForm? forms, List<BecaFormField> fields, string message)> GetBecaFormObjects4Save(DataFormPostParameter req);
        Dictionary<string, SqlConnection> GetConnectionsByRequest(List<BecaForm> forms, List<BecaFormField> fields, List<BecaFormFieldLevel>? customFields);

        Task<List<T>> GetDataByFormAsync<T>(Dictionary<string, SqlConnection> connections,
            List<BecaForm> forms, List<BecaFormLevels> children, List<BecaFormField> fields,
            string Form, List<BecaParameter> parameters, bool view = true, bool getChildren = true, 
            int? pageNumber = null, int? pageSize = null, bool lowerCase = false) where T : class, new();
        Task<List<T>> GetDataByFormAsync<T>(Dictionary<string, SqlConnection> connections,
            BecaForm form, List<BecaFormField> fields, object record,
            bool view = true, bool getChildren = true, int? pageNumber = null, int? pageSize = null, bool lowerCase = false) where T : class, new();

        Task<List<object>> GetDataByFormFieldAsync(Dictionary<string, SqlConnection> connections,
            List<BecaForm> forms, List<BecaFormField> fields, List<BecaFormFieldLevel> customFields,
            string Form, string field, List<BecaParameter> parameters, bool lowerCase);

        Task<List<T>> GetDataBySPAsync<T>(SqlConnection cnn, string spName, List<BecaParameter> parameters, PropertyNaming namingStrategy = PropertyNaming.AsIs) where T : class, new();

        Task<T?> AddDataByFormAsync<T>(SqlConnection connection, BecaForm form, List<BecaFormField> fields, object record, bool GetRecordAfterInsert) where T : class, new();
        Task<(T? data, string message)> UpdateDataByFormAsync<T>(SqlConnection connection, BecaForm form, List<BecaFormField> fields,
            object recordOld, object recordNew, bool GetRecordAfterInsert) where T : class, new();

        BecaUser? GetLoggedUser();
        Company? GetActiveCompany();
        string domain { get; }
    }
}
