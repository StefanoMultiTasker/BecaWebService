using ExtensionsLib;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Contexts
{
    public partial class DbDatiContext : DbContext
    {
        private string _connection;
        public FormTool _formTool;

        public DbDatiContext(string connectionString)
        {
            _connection = connectionString;
        }

        public DbDatiContext(FormTool formTool)
        {
            _formTool = formTool;
        }
        //public DbDatiContext(DbContextOptions<DbDatiContext> options,string connection, FormTool formTool)
        //    : base(options)
        //{
        //    _connection = connection;
        //    _formTool = formTool;
        //}

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_connection);
        }
    }

    public static class DbdatiContextExtension
    {
        public static List<T> ExecuteQuery<T>(this DbDatiContext db, string formName, string query, params object[] parameters) where T : class, new()
        {
            using (var command = db.Database.GetDbConnection().CreateCommand())
            {
                var rawSqlCommand = db.Database
                     .GetService<IRawSqlCommandBuilder>()
                     .Build(query, parameters);

                command.CommandText = query;
                command.CommandType = CommandType.Text;

                db.Database.OpenConnection();
                var paramObject = new RelationalCommandParameterObject(db.Database.GetService<IRelationalConnection>(), rawSqlCommand.ParameterValues, null, db, null);

                //using (var reader = command.ExecuteReader())
                using (var reader = rawSqlCommand
                    .RelationalCommand
                    .ExecuteReader(paramObject)
                    .DbDataReader
                    )
                {
                    var lst = new List<T>();
                    var lstColumns = new T().GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToList();
                    if (lstColumns.Count() == 0)
                    {
                        Type generatedType = db._formTool.GetFormCfg(formName, reader);
                        PropertyInfo[] props = generatedType.GetProperties();
                        while (reader.Read())
                        {
                            var generatedObject = Activator.CreateInstance(generatedType);
                            int i = 0;
                            foreach (PropertyInfo pi in props)
                            {
                                if (reader.GetValue(pi.Name) == DBNull.Value)
                                {
                                    pi.SetValue(generatedObject, null, new object[] { });
                                }
                                else
                                {
                                    pi.SetValue(generatedObject, reader.GetValue(pi.Name), new object[] { });
                                }

                                i += 1;
                            }
                            lst.Add((T)generatedObject);
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
                    return lst;
                }
            }
        }

        public static object GetQueryDef<T>(this DbDatiContext db, string formName, string query, params object[] parameters) where T : class, new()
        {
            using (var command = db.Database.GetDbConnection().CreateCommand())
            {
                var rawSqlCommand = db.Database
                     .GetService<IRawSqlCommandBuilder>()
                     .Build(query, parameters);

                command.CommandText = query;
                command.CommandType = CommandType.Text;

                db.Database.OpenConnection();
                var paramObject = new RelationalCommandParameterObject(db.Database.GetService<IRelationalConnection>(), rawSqlCommand.ParameterValues, null, db, null);

                //using (var reader = command.ExecuteReader())
                using (var reader = rawSqlCommand
                    .RelationalCommand
                    .ExecuteReader(paramObject)
                    .DbDataReader
                    )
                {
                    var lstColumns = new T().GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToList();
                    if (lstColumns.Count() == 0)
                    {
                        Type generatedType = db._formTool.GetFormCfg(formName, reader);
                        var generatedObject = Activator.CreateInstance(generatedType);
                        string identityName = "";
                        foreach (DbColumn col in reader.GetColumnSchema())
                        {
                            if ((bool)col.IsAutoIncrement)
                            {
                                identityName = col.ColumnName;
                                break;
                            }
                        }
                        MethodInfo method = generatedObject.GetType().GetMethod("set_identityName");
                        method.Invoke(generatedObject, new object[] { identityName });
                        return generatedObject;
                    }
                    else
                    {
                        return new T();
                    }
                }
            }
        }

        public static async Task<int> ExecuteSqlCommandAsync(this DbDatiContext db, string commandText, params object[] parameters)
        {
            //using (var command = db.Database.GetDbConnection().CreateCommand())
            //{
            //    var rawSqlCommand = db.Database
            //         .GetService<IRawSqlCommandBuilder>()
            //         .Build(commandText, parameters);

            //    command.CommandText = commandText;
            //    command.CommandType = CommandType.Text;

            //    db.Database.OpenConnection();
            //    var paramObject = new RelationalCommandParameterObject(db.Database.GetService<IRelationalConnection>(), rawSqlCommand.ParameterValues, null, null);

            //    return await rawSqlCommand
            //        .RelationalCommand
            //        .ExecuteNonQueryAsync(paramObject);
            //}
            return await db.Database.ExecuteSqlRawAsync(commandText, parameters);
        }

        public static int ExecuteSqlCommand(this DbDatiContext db, string commandText, params object[] parameters)
        {
            return db.Database.ExecuteSqlRaw(commandText, parameters);
        }

        public static async Task<int> InsertSqlCommandWithIdentity(this DbDatiContext db, string commandText, params object[] parameters)
        {
            commandText += "; SELECT CAST(scope_identity() AS int)";
            using (var command = db.Database.GetDbConnection().CreateCommand())
            {
                var rawSqlCommand = db.Database
                     .GetService<IRawSqlCommandBuilder>()
                     .Build(commandText, parameters);

                command.CommandText = commandText;
                command.CommandType = CommandType.Text;

                db.Database.OpenConnection();
                var paramObject = new RelationalCommandParameterObject(db.Database.GetService<IRelationalConnection>(), rawSqlCommand.ParameterValues, null, db, null);

                return (int)await rawSqlCommand
                    .RelationalCommand
                    .ExecuteScalarAsync(paramObject);
            }
        }

        public static List<string> GetProcedureParams(this DbDatiContext db, string name)
        {
            List<object> pars = new List<object>();
            pars.Add(name);
            string commandText = $"SELECT PARAMETER_NAME, DATA_TYPE FROM information_schema.parameters WHERE specific_name = '{name}'";
            List<object> names = ExecuteQuery<object>(db, "", commandText, pars.ToArray());
            if (name == null || names.Count == 0) return new List<string>();
            return names.Select(n => n.GetPropertyValue("PARAMETER_NAME").ToString()).ToList();
        }
    }
}
