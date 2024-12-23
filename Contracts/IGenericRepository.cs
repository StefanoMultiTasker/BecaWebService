using Entities.Models;

namespace Contracts
{
    public interface IGenericRepository
    {
        T getFormObject<T>(string Form, bool view, bool noUpload = false);
        T getFormObject<T>(string Form, bool view, List<string> fields, bool noUpload = false);
        string GetFormByView(int idView);
        object CreateObjectFromJSON<T>(string jsonRecord) where T : class, new();
        T CreateObjectFromJSON<T>(string Form, string jsonRecord) where T : class, new();

        List<T> GetDataByForm<T>(string Form, List<BecaParameter> parameters, bool view = true, bool getChildren = true) where T : class, new();
        List<T> GetDataByForm<T>(string Form, object record, bool view = true, bool getChildren = true) where T : class, new();
        List<T> GetDataBySP<T>(string dbName, string spName, List<BecaParameter> parameters) where T : class, new();

        Task<T> AddOrUpdateDataByForm<T>(string Form, object record) where T : class, new();
        Task<(T data, string message)> UpdateDataByForm<T>(string Form, object recordOld, object recordNewd) where T : class, new();
        Task<T> AddDataByForm<T>(string Form, object record) where T : class, new();
        Task<T> AddDataByFormChild<T>(string form, string formChild, object parent, List<object> childElements) where T : class, new();
        Task<int> DeleteDataByForm<T>(string Form, object record) where T : class, new();
        Task<string> ActionByForm(int idview, string actionName, object record);
        Task<string> ActionByForm(int idview, string actionName, List<BecaParameter> parameters);

        List<object> GetDataByFormField(string Form, string field, List<BecaParameter> parameters);
        List<object> GetDataByFormChildSelect(string Form, string childForm, short sqlNumber, object parent);
        Task<int> ExecuteSqlCommandAsync(string dbName, string commandText, params object[] parameters);
        int ExecuteSqlCommand(string dbName, string commandText, params object[] parameters);
        Task<int> ExecuteProcedure(string dbName, string spName, List<BecaParameter> parameters);
        Task CompleteAsync();
        List<object> GetDataBySQL(string dbName, string sql, List<BecaParameter> parameters, bool useidUtente = true);
        IDictionary<string, object> GetDataDictBySQL(string dbName, string sql, List<BecaParameter> parameters);

        List<object> GetDataByFormLevel(string Form, int subLevel, List<BecaParameter> parameters);

        object GetPanelsByForm(string Form, List<BecaParameter> parameters);
        ViewChart GetGraphByFormField(string Form, string field, List<BecaParameter> parameters);

        BecaUser GetLoggedUser();
        Company GetActiveCompany();
        string domain { get; }
    }
}
