using BecaWebService.ExtensionsLib;
using ExtensionsLib;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Contracts;
using Entities;
using Entities.Contexts;

namespace Repository
{
    public class SqlConnectionExtendedFactory : ISqlConnectionExtendedFactory
    {
        private readonly FormTool _formTool;
        private readonly ILoggerManager _logger;

        public SqlConnectionExtendedFactory(FormTool formTool, ILoggerManager logger)
        {
            _formTool = formTool;
            _logger = logger;
        }

        public async Task<TResult> UseConnectionAsync<TResult>(string connStr, Func<ISqlConnectionExtended, Task<TResult>> action)
        {
            var conn = Create(connStr);
            try
            {
                await conn.OpenAsync();
                return await action(conn);
            }
            finally
            {
                conn.Close();
                conn.Dispose();
            }
        }

        public ISqlConnectionExtended Create(string connectionString)
        {
            return new SqlConnectionExtended(_formTool, _logger, connectionString);
        }
    }

    // Since SqlConnection is sealed, we cannot inherit from it.
    // Instead, we can use composition to wrap SqlConnection and expose its functionality.
    public class SqlConnectionExtended : ISqlConnectionExtended
    {
        private readonly FormTool _formTool;
        private readonly ILoggerManager _logger;
        private readonly SqlConnection _sqlConnection;

        public SqlConnectionExtended(FormTool formTool, ILoggerManager logger, string connectionString)
        {
            _formTool = formTool;
            _logger = logger;
            _sqlConnection = new SqlConnection(connectionString);
        }


        public void Dispose()
        {
            _sqlConnection?.Dispose();
        }
        
        // Expose methods and properties of SqlConnection as needed
        public SqlConnection Connection => _sqlConnection;
        public void Open()=> _sqlConnection.Open();
        public async Task OpenAsync()=> await _sqlConnection.OpenAsync();

        public void Close()=>  _sqlConnection.Close();
        public async Task CloseAsync()=>  await _sqlConnection.CloseAsync();

        public string ConnectionString
        {
            get => _sqlConnection.ConnectionString;
            set => _sqlConnection.ConnectionString = value;
        }

        public System.Data.ConnectionState State => _sqlConnection.State;

        public async Task<object> GetQueryDefAsync<T>(SqlConnection db, string formName, string query, List<string> fields, params object[] parameters) where T : class, new()
        {
            // Sostituisci i parametri null con DBNull.Value
            var sanitizedParameters = parameters.Select(p => p ?? DBNull.Value).ToArray();

            using var command = db.CreateCommand();
            command.CommandText = $"{query}";
            command.CommandType = CommandType.Text;
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@p{i}"; // Assegna un nome unico al parametro
                parameter.Value = parameters[i] ?? DBNull.Value; // Usa DBNull.Value per i valori nulli
                command.Parameters.Add(parameter);
            }

            using var reader = await command.ExecuteReaderAsync();
            var lstColumns = new T().GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToList();
            if (lstColumns.Count == 0)
            {
                string identityName = "";
                DbColumn? idntityCol = reader.GetColumnSchema().FirstOrDefault(c => c.IsAutoIncrement == true);
                if (idntityCol != null) identityName = idntityCol.ColumnName;

                Type generatedType = _formTool.GetFormCfg(formName, reader, fields, (identityName != ""));
                var generatedObject = Activator.CreateInstance(generatedType);

                if (identityName != "")
                {
                    MethodInfo? method = generatedObject!.GetType().GetMethod("set_identityName");
                    method?.Invoke(generatedObject, [identityName]);
                }
                return generatedObject!;
            }
            else
            {
                return new T();
            }
        }

        public async Task<List<T>> ExecuteQueryAsync<T>(SqlConnection db, string formName, string query, bool hasChildren = false,
            PropertyNaming namingStrategy = PropertyNaming.AsIs, params object[] parameters) where T : class, new()
        {
            using var command = db.CreateCommand();

            for (int i = 0; i < parameters.Length; i++)
            {
                query = query.Replace($"{{{i}}}", $"@p{i}");
            }
            command.CommandText = $"{query}";
            command.CommandType = CommandType.Text;
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@p{i}"; // Assegna un nome unico al parametro
                parameter.Value = parameters[i] ?? DBNull.Value; // Usa DBNull.Value per i valori nulli
                command.Parameters.Add(parameter);
            }

            using var reader = await command.ExecuteReaderAsync();
            var lst = new List<T>();
            var lstColumns = new T().GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToList();
            if (lstColumns.Count == 0)
            {
                //Type generatedType = _formTool.GetFormCfg(formName, reader, false, hasChildren);
                //PropertyInfo[] props = generatedType.GetProperties().Where(p => p.Name != "children").ToArray();
                while (reader.Read())
                {
                    dynamic row = new ExpandoObject();
                    var rowDictionary = (IDictionary<string, object?>)row;

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string dataName = reader.GetName(i);
                        string columnName = namingStrategy switch
                        {
                            PropertyNaming.LowerCase => dataName.ToLowerInvariant(),
                            PropertyNaming.LowerCamelCase => dataName.ToLowerToCamelCase(),
                            _ => dataName
                        };
                        rowDictionary[columnName] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    if (hasChildren) rowDictionary["children"] = new List<object>();
                    lst.Add((T)row);
                    //var generatedObject = Activator.CreateInstance(generatedType);
                    //int i = 0;
                    //foreach (PropertyInfo pi in props)
                    //{
                    //    if (reader.GetValue(pi.Name) == DBNull.Value)
                    //    {
                    //        pi.SetValue(generatedObject, null, []);
                    //    }
                    //    else
                    //    {
                    //        pi.SetValue(generatedObject, reader.GetValue(pi.Name), []);
                    //    }

                    //    i += 1;
                    //}
                    //lst.Add((T)generatedObject!);
                }
            }
            else
            {
                while (reader.Read())
                {
                    var newObject = new T();
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        var name = reader.GetName(i);
                        PropertyInfo? prop = lstColumns.FirstOrDefault(a => a.Name.ToLower().Equals(name.ToLower()));
                        if (prop == null)
                        {
                            continue;
                        }
                        var val = reader.IsDBNull(i) ? null : reader[i];
                        prop.SetValue(newObject, val, null);
                    }
                    lst.Add(newObject);
                }
            }
            return lst;
        }

        public async Task<int> ExecuteSqlCommandAsync(SqlConnection db, string query, params object[] parameters)
        {
            using var command = db.CreateCommand();

            for (int i = 0; i < parameters.Length; i++)
            {
                query = query.Replace($"{{{i}}}", $"@p{i}");
            }
            command.CommandText = $"{query}";
            command.CommandType = CommandType.Text;
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@p{i}"; // Assegna un nome unico al parametro
                parameter.Value = parameters[i] ?? DBNull.Value; // Usa DBNull.Value per i valori nulli
                command.Parameters.Add(parameter);
            }
            return await command.ExecuteNonQueryAsync();
        }
        public async Task<List<string>> GetProcedureParamsAsync(SqlConnection cnn, string name)
        {
            List<object> pars = [name];
            string commandText = $"SELECT PARAMETER_NAME, DATA_TYPE FROM information_schema.parameters WHERE specific_name = '{name}'";
            List<object> names = await ExecuteQueryAsync<object>(cnn, "", commandText, false, PropertyNaming.AsIs, [.. pars]);
            if (name == null || names.Count == 0) return [];
            return names.Select(n => n.GetPropertyString("PARAMETER_NAME")).ToList();
        }

        public async Task<int> InsertSqlCommandWithIdentityAsync(SqlConnection connection, string commandText, params object[] parameters)
        {
            commandText += "; SELECT CAST(scope_identity() AS int) As newIdentity";

            using var command = connection.CreateCommand();

            for (int i = 0; i < parameters.Length; i++)
            {
                commandText = commandText.Replace($"{{{i}}}", $"@p{i}");
            }
            command.CommandText = $"{commandText}";
            command.CommandType = CommandType.Text;
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@p{i}"; // Assegna un nome unico al parametro
                parameter.Value = parameters[i] ?? DBNull.Value; // Usa DBNull.Value per i valori nulli
                command.Parameters.Add(parameter);
            }

            int identity = 0;
            try
            {
                object? res = await command.ExecuteScalarAsync();
                identity = res == null ? 0 : (int)res;
            }
            catch (Exception ex)
            {
                _logger.LogError($"InsertSqlCommandWithIdentityAsync: {commandText} non ha funzionato: {ex.Message}");
            }
            return identity;
        }
    }
}
