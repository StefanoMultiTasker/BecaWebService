using Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    public interface IGenericRepository
    {
        T getFormObject<T>(string Form);
        string GetFormByView(int idView);
        List<T> GetDataByForm<T>(string Form, List<BecaParameter> parameters) where T : class, new();
        List<T> GetDataByForm<T>(string Form, object record) where T : class, new();
        T CreateObjectFromJSON<T>(string Form, string jsonRecord) where T : class, new();
        Task<int?> UpdateDataByForm<T>(string Form, object recordOld, object recordNewd) where T : class, new();
        Task<T> AddDataByForm<T>(string Form, object record) where T : class, new();
        Task<int> DeleteDataByForm<T>(string Form, object record) where T : class, new();
        List<object> GetDataByFormField(string Form, string field, List<BecaParameter> parameters);
        List<object> GetDataBySQL(string dbName, string sql, List<BecaParameter> parameters, bool useidUtente = true);
        IDictionary<string, object> GetDataDictBySQL(string dbName, string sql, List<BecaParameter> parameters);
        List<object> GetDataByFormLevel(string Form, int subLevel, List<BecaParameter> parameters);
        object GetPanelsByForm(string Form, List<BecaParameter> parameters);
        ViewChart GetGraphByFormField(string Form, string field, List<BecaParameter> parameters);
        Task<int> ExecuteSqlCommandAsync(string dbName, string commandText, params object[] parameters);
        int ExecuteSqlCommand(string dbName, string commandText, params object[] parameters);
        Task<int> ExecuteProcedure(string dbName, string spName, List<BecaParameter> parameters);
        BecaUser GetLoggedUser();
    }
}
