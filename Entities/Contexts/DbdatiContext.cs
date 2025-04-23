using BecaWebService.ExtensionsLib;
using ExtensionsLib;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Newtonsoft.Json.Serialization;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Reflection;

namespace Entities.Contexts
{
    public interface IDbDatiContextFactory
    {
        DbDatiContext Create(string connectionString);
    }
    public class DbDatiContextFactory(FormTool formTool) : IDbDatiContextFactory
    {
        private readonly FormTool _formTool = formTool;

        public DbDatiContext Create(string connectionString) => new(_formTool, connectionString);
    }

    public partial class DbDatiContext(FormTool? formTool, string connectionString) : DbContext
    {
        private readonly string _connection = connectionString;
        public FormTool _formTool = formTool;

        public DbSet<object> generic { get; set; }

        //public DbDatiContext(DbContextOptions<DbDatiContext> options,string connection, FormTool formTool)
        //    : base(options)
        //{
        //    _connection = connection;
        //    _formTool = formTool;
        //}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<object>().HasNoKey();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_connection);
        }
    }
    public enum PropertyNaming
    {
        AsIs,
        LowerCase,
        LowerCamelCase
    }

    public static class DbdatiContextExtension
    {
        public static List<T> ExecuteQuery<T>(this DbDatiContext db, string formName, string query, bool hasChildren = false, 
            PropertyNaming namingStrategy = PropertyNaming.AsIs, params object[] parameters) where T : class, new()
        {
            DbConnection connection = db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            using var command = connection.CreateCommand();
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

            //db.Database.OpenConnection();
            //var paramObject = new RelationalCommandParameterObject(db.Database.GetService<IRelationalConnection>(), rawSqlCommand.ParameterValues, null, db, null);

            //using (var reader = command.ExecuteReader())
            //using (var reader = rawSqlCommand
            //    .RelationalCommand
            //    .ExecuteReader(paramObject)
            //    .DbDataReader
            //    )
            //using var reader = command.ExecuteReader();
            var lst = new List<T>();
            DbDataReader? reader = null;
            try
            {
                reader = command.ExecuteReader();
                var lstColumns = new T().GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToList();
                if (lstColumns.Count == 0)
                {
                    //Type generatedType = db._formTool.GetFormCfg(formName, reader, false, hasChildren);
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
                        //lst.Add((T)generatedObject);
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
                            PropertyInfo prop = lstColumns.FirstOrDefault(a => a.Name.ToLower().Equals(name.ToLower()));
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
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                {
                    reader.Close();
                }
                if (connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }
            }
            return lst;
        }

        public static object GetQueryDef<T>(this DbDatiContext db, string formName, string query, params object[] parameters) where T : class, new() =>
            db.GetQueryDef<T>(formName, query, [], parameters);

        public static object GetQueryDef<T>(this DbDatiContext db, string formName, string query, List<string> fields, params object[] parameters) where T : class, new()
        {
            // Sostituisci i parametri null con DBNull.Value
            var sanitizedParameters = parameters.Select(p => p ?? DBNull.Value).ToArray();
            DbConnection connection = db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"{query}";
            command.CommandType = CommandType.Text;
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@p{i}"; // Assegna un nome unico al parametro
                parameter.Value = parameters[i] ?? DBNull.Value; // Usa DBNull.Value per i valori nulli
                command.Parameters.Add(parameter);
            }

            //db.Database.OpenConnection();
            //var paramObject = new RelationalCommandParameterObject(db.Database.GetService<IRelationalConnection>(), rawSqlCommand.ParameterValues, null, db, null);

            //using (var reader = command.ExecuteReader())
            //using (var reader = rawSqlCommand
            //    .RelationalCommand
            //    .ExecuteReader(paramObject)
            //    .DbDataReader
            //    )
            using var reader = command.ExecuteReader();
            var lstColumns = new T().GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToList();
            if (lstColumns.Count == 0)
            {
                string identityName = "";
                DbColumn idntityCol = reader.GetColumnSchema().FirstOrDefault(c => c.IsAutoIncrement == true);
                if (idntityCol != null) identityName = idntityCol.ColumnName;

                Type generatedType = db._formTool.GetFormCfg(formName, reader, fields, (identityName != ""));
                var generatedObject = Activator.CreateInstance(generatedType);

                if (identityName != "")
                {
                    MethodInfo method = generatedObject.GetType().GetMethod("set_identityName");
                    method.Invoke(generatedObject, [identityName]);
                }
                if (connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }
                return generatedObject;
            }
            else
            {
                if (connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }
                return new T();
            }
        }

        public static async Task<int> ExecuteSqlCommandAsync(this DbDatiContext db, string commandText, params object[] parameters)
        {
            return await db.Database.ExecuteSqlRawAsync(commandText, parameters);
        }

        public static int ExecuteSqlCommand(this DbDatiContext db, string commandText, params object[] parameters)
        {
            return db.Database.ExecuteSqlRaw(commandText, parameters);
        }

        public static async Task<int> InsertSqlCommandWithIdentity(this DbDatiContext db, string commandText, params object[] parameters)
        {
            commandText += "; SELECT CAST(scope_identity() AS int)";
            DbConnection connection = db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            using var command = connection.CreateCommand();
            var rawSqlCommand = db.Database
                 .GetService<IRawSqlCommandBuilder>()
                 .Build(commandText, parameters);

            command.CommandText = commandText;
            command.CommandType = CommandType.Text;

            db.Database.OpenConnection();
            var paramObject = new RelationalCommandParameterObject(db.Database.GetService<IRelationalConnection>(), rawSqlCommand.ParameterValues, null, db, null);

            int identity = 0;
            try
            {
                identity = (int)await rawSqlCommand
                    .RelationalCommand
                    .ExecuteScalarAsync(paramObject);
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }
            }
            return identity;
        }

        public static List<string> GetProcedureParams(this DbDatiContext db, string name)
        {
            List<object> pars = [name];
            string commandText = $"SELECT PARAMETER_NAME, DATA_TYPE FROM information_schema.parameters WHERE specific_name = '{name}'";
            List<object> names = ExecuteQuery<object>(db, "", commandText, false, PropertyNaming.AsIs,[.. pars]);
            if (name == null || names.Count == 0) return [];
            return names.Select(n => n.GetPropertyValue("PARAMETER_NAME").ToString()).ToList();
        }
    }
}
