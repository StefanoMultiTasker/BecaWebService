using Entities.Contexts;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    public interface ISqlConnectionExtendedFactory
    {
        ISqlConnectionExtended Create(string connectionString);
        Task<TResult> UseConnectionAsync<TResult>(string connStr, Func<ISqlConnectionExtended, Task<TResult>> action);
    }

    public interface ISqlConnectionExtended : IDisposable
    {
        SqlConnection Connection { get; }
        System.Data.ConnectionState State { get; }
        string ConnectionString { get; set; }
        void Open();
        Task OpenAsync();
        void Close();
        Task CloseAsync();
        Task<object> GetQueryDefAsync<T>(SqlConnection db, string formName, string query, List<string> fields, params object[] parameters) where T : class, new();
        Task<List<T>> ExecuteQueryAsync<T>(SqlConnection db, string formName, string query, bool hasChildren = false,
            PropertyNaming namingStrategy = PropertyNaming.AsIs, params object[] parameters) where T : class, new();
        Task<int> ExecuteSqlCommandAsync(SqlConnection db, string query, params object[] parameters);
        Task<List<string>> GetProcedureParamsAsync(SqlConnection cnn, string name);
        Task<int> InsertSqlCommandWithIdentityAsync(SqlConnection connection, string commandText, params object[] parameters);
    }
}
