using Entities.Models;
using ExtensionsLib;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Entities.Contexts
{
    public partial class DbdatiContext : DbContext
    {
        public string domain;
        public int idUtente;

        private readonly IConfiguration Configuration;

        public virtual DbSet<BecaUser> BecaUsers { get; set; }
        public virtual DbSet<UserMenu> RawUserMenu { get; set; }

        public virtual DbSet<BecaView> BecaView { get; set; }
        public virtual DbSet<BecaViewData> BecaViewData { get; set; }
        public virtual DbSet<BecaViewDataUser> BecaViewDataUser { get; set; }
        public virtual DbSet<BecaViewFilterValues> BecaViewFilterValues { get; set; }
        public virtual DbSet<BecaViewFilters> BecaViewFilters { get; set; }
        public virtual DbSet<BecaViewPanels> BecaViewPanels { get; set; }
        public virtual DbSet<BecaPanelFilters> BecaPanelFilters { get; set; }
        public virtual DbSet<BecaFormula> BecaFormula { get; set; }
        public virtual DbSet<BecaFormulaData> BecaFormulaData { get; set; }
        public virtual DbSet<BecaFormulaDataFilters> BecaFormulaDataFilters { get; set; }
        public virtual DbSet<BecaViewTypes> BecaViewTypes { get; set; }
        public virtual DbSet<BecaAggregationTypes> BecaAggregationTypes { get; set; }
        public virtual DbSet<BecaViewFilterUI> BecaViewFilterUI { get; set; }
        public virtual DbSet<BecaViewDetailUI> BecaViewDetailUI { get; set; }

        public DbSet<BecaForm> BecaForm { get; set; }
        public DbSet<BecaFormLevels> BecaFormLevels { get; set; }
        public DbSet<BecaFormField> BecaFormField { get; set; }
        public DbSet<BecaFormFieldLevel> BecaFormFieldLevel { get; set; }

        //public DbdatiContext(IConfiguration configuration)
        //{
        //    Configuration = configuration;
        //}

        public FormTool _formTool;

        public DbdatiContext(DbContextOptions<DbdatiContext> options, FormTool formTool)
            : base(options)
        {
            _formTool = formTool;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region "Authenticate"

            modelBuilder.Entity<BecaUser>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.idUtente);
                entity.OwnsMany(p => p.RefreshTokens, a =>
                {
                    a.Property<int>("idUtente")
                        .HasColumnType("int");

                    a.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    a.Property<string>("Token")
                        .HasColumnType("nvarchar(max)");

                    a.Property<DateTime>("Expires")
                        .HasColumnType("datetime2");

                    a.Property<DateTime>("Created")
                        .HasColumnType("datetime2");

                    a.Property<string>("CreatedByIp")
                        .HasColumnType("nvarchar(max)");

                    a.Property<DateTime?>("Revoked")
                        .HasColumnType("datetime2");

                    a.Property<string>("RevokedByIp")
                        .HasColumnType("nvarchar(max)");

                    a.Property<string>("ReplacedByToken")
                        .HasColumnType("nvarchar(max)");

                    a.Property<string>("ReasonRevoked")
                        .HasColumnType("nvarchar(max)");

                    a.HasKey("idUtente", "Id");

                    a.ToTable("RefreshToken");

                    a.WithOwner()
                        .HasForeignKey("idUtente");

                    a.ToTable("RefreshTokens");
                });
                entity.OwnsMany(c => c.Companies, a =>
                {
                    //a.Property<int>("idUtente")
                    //    .HasColumnType("int");

                    a.Property<int>("idCompany")
                        .HasColumnType("int");

                    a.Property<string>("CompanyName")
                        .HasColumnType("nvarchar(max)");

                    a.Property<int>("isDefault")
                        .HasColumnType("int");

                    a.Property<string>("Logo1url")
                        .HasColumnType("nvarchar(max)");
                    a.Property<string>("Logo2url")
                        .HasColumnType("nvarchar(max)");
                    a.Property<string>("Logo3url")
                        .HasColumnType("nvarchar(max)");
                    a.Property<string>("Logo4url")
                        .HasColumnType("nvarchar(max)");
                    a.Property<string>("Logo5url")
                        .HasColumnType("nvarchar(max)");

                    a.Property<string>("Color1")
                        .HasColumnType("nvarchar(50)");
                    a.Property<string>("Color2")
                        .HasColumnType("nvarchar(50)");
                    a.Property<string>("Color3")
                        .HasColumnType("nvarchar(50)");
                    a.Property<string>("Color4")
                        .HasColumnType("nvarchar(50)");
                    a.Property<string>("Color5")
                        .HasColumnType("nvarchar(50)");

                    a.HasKey("idUtente", "idCompany");
                    a.WithOwner()
                        .HasForeignKey("idUtente");
                    a.ToTable("vUsersCompanies");

                    a.OwnsMany(p => p.Profiles, a =>
                    {
                        a.Property<int>("idUtente")
                            .HasColumnType("int");

                        a.Property<int>("idCompany")
                            .HasColumnType("int");

                        a.Property<int>("idProfile")
                            .HasColumnType("int");

                        a.Property<string>("Profile")
                            .HasColumnType("nvarchar(max)");

                        a.Property<bool>("PasswordChange")
                            .HasColumnType("bit");

                        a.HasKey("idUtente", "idProfile", "idCompany");
                        a.WithOwner()
                            .HasForeignKey("idUtente", "idCompany");
                        a.ToTable("vUsers");
                    });
                });
            });

            modelBuilder.Entity<UserMenu>(entity =>
            {
                entity.ToView("vMenuUser");
                entity.HasKey(e => new { e.idUtente, e.idCompany, e.idItem });
            });

            #endregion

            #region "Views"

            modelBuilder.Entity<BecaAggregationTypes>(entity =>
                {
                    entity.HasKey(e => e.IdAggregationType);
                });

            modelBuilder.Entity<BecaFormula>(entity =>
            {
                entity.HasKey(e => e.IdFormula);
            });

            modelBuilder.Entity<BecaFormulaData>(entity =>
            {
                entity.HasKey(e => e.IdFormulaData);

                entity.HasOne(d => d.IdAggregationTypeNavigation)
                    .WithMany(p => p.BecaFormulaData)
                    .HasForeignKey(d => d.IdAggregationType)
                    .HasConstraintName("FK_BecaFormulaData_BecaAggregationTypes");

                entity.HasOne(d => d.IdFormulaNavigation)
                    .WithMany(p => p.BecaFormulaData)
                    .HasForeignKey(d => d.IdFormula)
                    .HasConstraintName("FK_BecaFormulaData_BecaFormula");
            });

            modelBuilder.Entity<BecaFormulaDataFilters>(entity =>
            {
                entity.ToView("vBecaFormulaDataFilters");
                entity.HasKey(e => new { e.IdFormulaData, e.idBecaFilter });

                entity.HasOne(d => d.IdFormulaDataNavigation)
                    .WithMany(p => p.BecaFormulaDataFilters)
                    .HasForeignKey(d => d.IdFormulaData)
                    .HasConstraintName("FK_BecaFormulaDataFilters_BecaFormulaData");
            });

            modelBuilder.Entity<BecaPanelFilters>(entity =>
            {
                entity.ToView("vBecaPanelFilters");
                entity.HasKey(e => new { e.idBecaViewPanel, e.idBecaFilter });

                entity.HasOne(d => d.idBecaViewPanelNavigation)
                    .WithMany(p => p.BecaPanelFilters)
                    .HasForeignKey(d => d.idBecaViewPanel)
                    .HasConstraintName("FK_BecaPanelFilters_BecaViewPanels");
            });

            modelBuilder.Entity<BecaView>(entity =>
            {
                entity.HasKey(e => e.idBecaView);

                entity.HasOne(d => d.idBecaViewTypeNavigation)
                    .WithMany(p => p.BecaView)
                    .HasForeignKey(d => d.idBecaViewType)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BecaView_BecaViewTypes");
            });

            modelBuilder.Entity<BecaViewData>(entity =>
            {
                entity.ToView("vBecaViewData");
                entity.HasKey(e => new { e.idBecaView, e.idDataDefinition });

                entity.HasOne(d => d.idBecaViewNavigation)
                    .WithMany(p => p.BecaViewData)
                    .HasForeignKey(d => d.idBecaView)
                    .HasConstraintName("FK_BecaViewData_BecaView");
            });

            modelBuilder.Entity<BecaViewDataUser>(entity =>
            {
                entity.ToView("BecaViewDataUser");
                entity.HasKey(e => new { e.idBecaView, e.idDataDefinition, e.Domain, e.idUtente });
            });

            modelBuilder.Entity<BecaViewFilterValues>(entity =>
            {
                entity.ToView("vBecaViewFilterValues");
                entity.HasKey(e => new { e.idBecaView, e.idFilterValue })
                    .HasName("PK_BecaViewFilterValue");

                entity.HasOne(d => d.idBecaViewNavigation)
                    .WithMany(p => p.BecaViewFilterValues)
                    .HasForeignKey(d => d.idBecaView)
                    .HasConstraintName("FK_BecaViewFilterValues_BecaView");
            });

            modelBuilder.Entity<BecaViewFilters>(entity =>
            {
                entity.ToView("vBecaViewFilters");
                entity.HasKey(e => new { e.idBecaView, e.idBecaFilter });

                entity.HasOne(d => d.idBecaViewNavigation)
                    .WithMany(p => p.BecaViewFilters)
                    .HasForeignKey(d => d.idBecaView)
                    .HasConstraintName("FK_BecaViewFilters_BecaView");
            });

            modelBuilder.Entity<BecaViewPanels>(entity =>
            {
                entity.HasKey(e => e.idBecaViewPanel);

                entity.HasOne(d => d.IdAggregationTypeNavigation)
                    .WithMany(p => p.BecaViewPanels)
                    .HasForeignKey(d => d.IdAggregationType)
                    .HasConstraintName("FK_BecaViewPanels_BecaAggregationTypes");

                entity.HasOne(d => d.idBecaViewNavigation)
                    .WithMany(p => p.BecaViewPanels)
                    .HasForeignKey(d => d.idBecaView)
                    .HasConstraintName("FK_BecaViewPanels_BecaView");

                entity.HasOne(d => d.IdFormulaNavigation)
                    .WithMany(p => p.BecaViewPanels)
                    .HasForeignKey(d => d.IdFormula)
                    .HasConstraintName("FK_BecaViewPanels_BecaFormula");
            });

            modelBuilder.Entity<BecaViewTypes>(entity =>
            {
                entity.HasKey(e => e.idBecaViewType);

            });

            modelBuilder.Entity<BecaViewFilterUI>(entity =>
            {
                entity.ToView("vBecaViewFilterUI");
                entity.HasKey(e => new { e.idBecaView, e.Name });

            });
            modelBuilder.Entity<BecaViewDetailUI>(entity =>
            {
                entity.ToView("vBecaViewDetailUI");
                entity.HasKey(e => new { e.idBecaView, e.Name });

            });

            #endregion

            #region "Beca"

            modelBuilder.Entity<BecaForm>().ToTable("_dbaForms");
            modelBuilder.Entity<BecaForm>().HasKey(p => new { p.Form });

            modelBuilder.Entity<BecaFormLevels>().ToTable("_dbaFormsLevels");
            modelBuilder.Entity<BecaFormLevels>().HasKey(p => new { p.Form, p.SubLevel });

            modelBuilder.Entity<BecaFormField>().ToTable("_dbaFormsFields");
            modelBuilder.Entity<BecaFormField>().HasKey(p => new { p.Form, p.Campo });

            modelBuilder.Entity<BecaFormFieldLevel>().ToTable("_dbaFormsObjAccess");
            modelBuilder.Entity<BecaFormFieldLevel>().HasKey(p => new { p.Form, p.objName, p.idLivello });

            #endregion

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }

    public static class DbdatiContextExtension
    {
        public static List<T> ExecuteQuery<T>(this DbdatiContext db, string formName, string query, params object[] parameters) where T : class, new()
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

        public static object GetQueryDef<T>(this DbdatiContext db, string formName, string query, params object[] parameters) where T : class, new()
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

        public static async Task<int> ExecuteSqlCommandAsync(this DbdatiContext db, string commandText, params object[] parameters)
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

        public static int ExecuteSqlCommand(this DbdatiContext db, string commandText, params object[] parameters)
        {
            return db.Database.ExecuteSqlRaw(commandText, parameters);
        }

        public static async Task<int> InsertSqlCommandWithIdentity(this DbdatiContext db, string commandText, params object[] parameters)
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

        public static List<string> GetProcedureParams(this DbdatiContext db, string name)
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
