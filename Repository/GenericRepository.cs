using BecaWebService.ExtensionsLib;
using BecaWebService.Models.Communications;
using Contracts;
using Entities;
using Entities.Contexts;
using Entities.DataTransferObjects;
using Entities.Models;
using ExtensionsLib;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.IIS.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;
using System.Linq;
using Entities.Communications;
using System.Data.Common;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Dynamic;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Repository
{
    public class GenericRepository : IGenericRepository, IDisposable
    {
        public string domain { get => _domain; }
        private readonly DbBecaContext _context;
        private readonly BecaUser? _currentUser;
        private readonly Company? _activeCompany;
        private readonly FormTool _formTool;
        //private readonly ILogger<GenericRepository> _logger;
        private readonly ILoggerManager _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly string _domain;
        private readonly string id = "";

        private readonly Dictionary<string, DbDatiContext> _databases = [];
        private readonly IServiceScopeFactory _scopeFactory;
        //private readonly ISqlConnectionExtendedFactory _factory;
        public GenericRepository(DbBecaContext context, FormTool formTool, IServiceScopeFactory scopeFactory,
            IHttpContextAccessor httpContextAccessor, ILoggerManager logger,
            IWebHostEnvironment environment) //ILogger<GenericRepository> logger) ISqlConnectionExtendedFactory factory
        {
            _context = context;
            _currentUser = httpContextAccessor.HttpContext == null || !httpContextAccessor.HttpContext.Items.ContainsKey("User")
                ? null
                : (BecaUser)httpContextAccessor.HttpContext.Items["User"]!;
            _activeCompany = httpContextAccessor.HttpContext == null || !httpContextAccessor.HttpContext.Items.ContainsKey("Company")
                ? null
                : (Company)httpContextAccessor.HttpContext.Items["Company"]!;
            _formTool = formTool;
            _logger = logger;
            _environment = environment;
            _domain = httpContextAccessor.HttpContext == null ? "" : httpContextAccessor.HttpContext.Request.Host.Host;
            _scopeFactory = scopeFactory;

            id = Guid.NewGuid().ToString();
            _logger.LogDebug($"Creato GenericRepository {id}");
            //_factory = factory;
        }

        public BecaUser? GetLoggedUser() => _currentUser;
        public Company? GetActiveCompany() => _activeCompany;

        public void Dispose()
        {
            foreach (var db in _databases.Values)
            {
                db.Dispose(); // Chiude tutte le connessioni
            }
            _databases.Clear();
            GC.SuppressFinalize(this);
            _logger.LogDebug("Distrutto GenericRepository {id}");
        }

        #region "Form"

        public string GetFormByView(int? idView, string? form)
        {
            if (idView == null) return form ?? "";
            BecaViewForm? Form = _context.BecaViewForms
                .FirstOrDefault(f => f.idBecaView == idView && f.isMain == true);
            if (Form != null) return Form.Form;
            else return "";
        }

        public async Task<T?> AddOrUpdateDataByForm<T>(string Form, object record) where T : class, new()
        {
            List<object> data = GetDataByForm<object>(Form, record);
            if (data.Count == 0)
                return await AddDataByForm<T>(Form, record);
            else
                return (await UpdateDataByForm<T>(Form, data[0], record)).data;
        }

        public async Task<T?> AddDataByForm<T>(string Form, object record) where T : class, new()
        {
            BecaForm? form = _context.BecaForm
                .FirstOrDefault(f => f.Form == Form);

            List<BecaFormField> defFieds = [.. _context.BecaFormField.Where(f => f.Form == Form && f.DefaultValue != null)];

            if (form != null)
            {
                string tryUpload = await UploadByForm(form, record);
                if (tryUpload != "") throw new InvalidOperationException(tryUpload);

                List<object?> pars = [];
                if ((form.AddProcedureName ?? "") != "")
                {
                    var (data, resCount, endOperation) = await ExecFormProcedure<T>(Form, form.AddProcedureName!, form.TableNameDB, record);
                    if (endOperation) return data;
                }
                //if ((form.AddProcedureName ?? "") != "" && form.AddProcedureName.Split(";").Count(p => p.Contains("#Before#") || !p.Contains("#")) > 0)
                //{
                //    foreach(string proc in form.AddProcedureName.Split(";").Where(p => p.Contains("#Before#") || !p.Contains("#")))
                //    {
                //        int? resSPBefore = await UpdateDataByProcedure<T>(form.TableNameDB, proc, record);
                //    }
                //    if(form.AddProcedureName.Split(";").Count(p => !p.Contains("#")) > 0)
                //    //if (!form.AddProcedureName.Contains("#Before#"))
                //    {
                //        List<T> spRes = this.GetDataByForm<T>(Form, record);
                //        return (spRes == null || spRes.Count == 0) ? null : spRes[0];
                //    }
                //}
                object def = GetContext(form.TableNameDB).GetQueryDef<object>((form.SchemaHashTableString ?? Form), "Select * From " + form.TableName + " Where 0 = 1");
                MethodInfo? method = def.GetType().GetMethod("identityName");
                string idName = method == null ? "" : method.Invoke(def, null)!.ToString() ?? "";

                int numP = 0;
                string sql = "Insert Into " + form.TableName + " (";
                string sqlF = ""; string sqlV = "";
                PropertyInfo[] colsTbl = record.GetType().GetProperties();
                Dictionary<string, PropertyInfo> colsObj = def.GetType().GetProperties().ToDictionary(c => c.Name);
                if (colsTbl.Length > 0)
                {
                    //for (int i = 0; i < colsTbl.Count(); i++)
                    foreach (PropertyInfo pTbl in colsTbl.Where(c => c.Name != idName))
                    {
                        bool colExist = colsObj.TryGetValue(pTbl.Name, out PropertyInfo? pObj);
                        if (colExist && pTbl.GetValue(record) == null && defFieds.Any(f => f.Name.Equals(pTbl.Name, StringComparison.CurrentCultureIgnoreCase)))
                        {
                            record.SetPropertyValue(pTbl.Name, defFieds.FirstOrDefault(f => f.Name.Equals(pTbl.Name, StringComparison.CurrentCultureIgnoreCase))!.DefaultValue);
                        }
                        if (colExist && pTbl.GetValue(record) != null)
                        {
                            pars.Add(pTbl.GetValue(record));
                            sqlF += (numP > 0 ? "," : "") + pTbl.Name;
                            sqlV += (numP > 0 ? "," : "") + " {" + numP.ToString() + "}";
                            numP++;
                        }
                    }
                }
                else return null;

                sql += sqlF + ") Values (" + sqlV + ")";

                int ires = 0;
                if (idName != "")
                {
                    int key = await GetContext(form.TableNameDB).InsertSqlCommandWithIdentity(sql, [.. pars]);
                    if (key <= 0)
                    {
                        return null;
                    }
                    record.SetPropertyValue(idName, key);
                    ires = 1;
                }
                else
                {
                    ires = await GetContext(form.TableNameDB).ExecuteSqlCommandAsync(sql, [.. pars]);
                }

                if (ires == 0) return null;

                if ((form.AddProcedureName ?? "") != "" && form.AddProcedureName!.Contains("#After#"))
                {
                    await _context.SaveChangesAsync();
                    await UpdateDataByProcedure<T>(form.TableNameDB, form.AddProcedureName, record);
                }

                await _context.SaveChangesAsync();
                List<T> inserted = this.GetDataByForm<T>(Form, record);
                if (inserted.Count == 0) return null;
                return inserted[0];
            }
            else
            {
                return null;
            }
        }

        public async Task<T?> AddDataByFormChild<T>(string form, string formChild, object parent, List<object> childElements) where T : class, new()
        {
            BecaForm? _form = _context.BecaForm
                .FirstOrDefault(f => f.Form == formChild);
            if (_form == null) return null;

            object def = GetContext(_form.TableNameDB).GetQueryDef<object>(_form.SchemaHashString ?? formChild, "Select * From " + _form.TableName + " Where 0 = 1");

            foreach (PropertyInfo p in def.GetType().GetProperties())
            {
                if (parent.HasPropertyValue(p.Name)) def.SetPropertyValue(p.Name, parent.GetPropertyValue(p.Name));
                foreach (var c in from JObject c in childElements
                                  where c.ContainsKey(p.Name.ToLower())
                                  select c)
                {
                    def.SetPropertyValue(p.Name, c[p.Name.ToLower()]);
                }
            }

            var res = await AddDataByForm<T>(formChild, def);
            if (res == null) return null;

            return GetDataByForm<T>(form, parent)[0];
        }

        public object? CreateObjectFromJSON<T>(string jsonRecord) where T : class, new()
        {
            dynamic? json = JsonConvert.DeserializeObject<dynamic>(jsonRecord);
            return json;
        }

        public T? CreateObjectFromJSON<T>(string Form, string jsonRecord) where T : class, new()
        {
            BecaForm? form = _context.BecaForm
                .FirstOrDefault(f => f.Form == Form);
            List<object> pars = [];

            dynamic? json = JsonConvert.DeserializeObject<dynamic>(jsonRecord);
            if (form != null && json != null)
            {
                object def = GetContext(form!.TableNameDB).GetQueryDef<object>(form.SchemaHashString ?? Form, "Select * From " + form.TableName + " Where 0 = 1");
                Type formType = def.GetType();

                PropertyInfo[] props = formType.GetProperties();
                foreach (PropertyInfo pi in props)
                {
                    bool isFieldPresent = false;
                    try
                    {
                        object v = (object)json![pi.Name.ToLowerToCamelCase()].Value;
                        isFieldPresent = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        isFieldPresent = false;
                    }
                    if (isFieldPresent)
                    {
                        def.SetPropertyValue(pi.Name, (object)json![pi.Name.ToLowerToCamelCase()].Value);
                    }
                    //pi.SetValue(def, json[pi.Name].Value, new object[] { });
                    //if (json[pi.Name].Value == DBNull.Value)
                    //{
                    //    pi.SetValue(def, null, new object[] { });
                    //}
                    //else
                    //{
                    //    pi.SetValue(def, json[pi.Name].Value, new object[] { });
                    //}
                }
                //object record = def.getObjectFromJSON<T>(jsonRecord);
                return (T)def;
            }
            return null;
        }

        public async Task<int> DeleteDataByForm<T>(string Form, object record) where T : class, new()
        {
            BecaForm? form = _context.BecaForm
                .FirstOrDefault(f => f.Form == Form);
            if (form != null)
            {
                if (!form.DeleteProcedureName.isNullOrempty())
                {
                    var (data, resCount, endOperation) = await ExecFormProcedure<T>(Form, form.DeleteProcedureName!, form.TableNameDB, record);
                    if (endOperation) return (resCount ?? 0);
                }
                //if ((form.DeleteProcedureName ?? "") != "" && !form.DeleteProcedureName.Contains("#After#"))
                //{
                //    int? resSPBefore = await UpdateDataByProcedure<T>(form.TableNameDB, form.DeleteProcedureName, record);
                //    if (!form.DeleteProcedureName.Contains("#Before#")) return (int)resSPBefore;
                //}
                int numP = 0;
                string sql = "Delete " + form.TableName + " Where ";
                List<object> pars = [];
                if (form.PrimaryKey.isNullOrempty())
                {
                    return 0;
                }
                foreach (string orderField in form.PrimaryKey!.Split(","))
                {
                    sql += (numP > 0 ? " And " : "") + orderField + " = {" + numP.ToString() + "}";
                    pars.Add(record.GetPropertyValue(orderField.Trim()));
                    numP++;
                }
                int? ires = await GetContext(form.TableNameDB).ExecuteSqlCommandAsync(sql, [.. pars]);

                if (!form.DeleteProcedureName.isNullOrempty() && form.UpdateProcedureName!.Contains("#After#"))
                {
                    await _context.SaveChangesAsync();
                    return (int)await UpdateDataByProcedure<T>(form.TableNameDB, form.DeleteProcedureName!, record);
                }
                return (int)ires;
            }
            else
            {
                return 0;
            }
        }

        public List<T> GetDataByForm<T>(string Form, List<BecaParameter> parameters, bool view = true,
            bool getChildren = true, int? pageNumber = null, int? pageSize = null, bool lowerCase = false) where T : class, new()
        {
            BecaForm? form = _context.BecaForm
                .FirstOrDefault(f => f.Form == Form);
            List<object?> pars = [];
            if (form != null)
            {
                if (!form.SelectProcedureName.isNullOrempty()) return this.GetDataBySP<T>(form.TableNameDB, form.SelectProcedureName!, parameters, 
                    lowerCase ? PropertyNaming.LowerCase : PropertyNaming.LowerCamelCase);
                string upl = string.Join(", ", _context.BecaFormField
                    .Where(f => f.Form == Form && f.FieldType == "upload")
                    .ToList().Select(n => "Null As [" + n.Name.Replace("upl", "").Trim() + "upl], Null As [" + n.Name.Replace("upl", "").Trim() + "uplName]"));
                //.ToList().Select(n => "Null As [_" + n.Name.Trim() + "_upl_], Null As [_" + n.Name.Trim() + "_uplName_]"));
                string sql = GetFormSQL(form, view, false, false);
                string sqlChk = sql;
                string db = form.ViewName.isNullOrempty() ? form.TableNameDB : form.ViewNameDB ?? "";

                object? colCheck = null;

                int numP = 0;
                if (sql.Contains('('))
                {
                    string inside = sql.inside("(", ")");
                    if (inside != "")
                    {
                        string[] funcPars = inside.Replace(" ", "").Split(",");
                        string funcPar = "";
                        string funcParChk = "";
                        foreach (string par in funcPars)
                        {
                            funcPar += "{" + numP + "}";
                            funcParChk += "Null,";
                            numP++;
                            BecaParameter? bPar = parameters.FirstOrDefault(p => p.name.Replace("+", "").Equals(par, StringComparison.CurrentCultureIgnoreCase));
                            if (bPar != null)
                            {
                                if (bPar.value1 != null && bPar.value1.ToString().IsValidDateTimeJson()) bPar.value1 = bPar.value1.ToDateTimeFromJson();
                                if (bPar.value2 != null && bPar.value2.ToString().IsValidDateTimeJson()) bPar.value2 = bPar.value2.ToDateTimeFromJson();

                                pars.Add(bPar.used == true ? bPar.value2 : bPar.value1);
                                bPar.used = true;
                            }
                            else
                            {
                                if (par == "idUtente")
                                {
                                    pars.Add(_currentUser!.idUtenteLoc(_activeCompany?.idCompany));
                                    parameters.Add(new BecaParameter()
                                    {
                                        name = "idUtente",
                                        value1 = _currentUser.idUtenteLoc(_activeCompany?.idCompany),
                                        comparison = "=",
                                        used = true
                                    });
                                    //parameters.Find(p => p.name == "idUtente").used = true;
                                }
                                else if (par == "idCompany")
                                {
                                    pars.Add(_activeCompany?.idCompany);
                                    parameters.Add(new BecaParameter()
                                    {
                                        name = "idCompany",
                                        value1 = _activeCompany?.idCompany,
                                        comparison = "=",
                                        used = true
                                    });
                                    //parameters.Find(p => p.name == "idCompany").used = true;
                                }
                                else
                                    pars.Add(null);
                            }
                        }
                        funcPar = funcPar.Replace("}{", "},{");
                        sqlChk = sqlChk.Replace(sqlChk.inside("(", ")"), funcParChk).Replace(",)", ")");
                        sql = sql.Replace(sql.inside("(", ")"), funcParChk).Replace(",)", ")");
                        sql = sql.Replace(sql.inside("(", ")"), funcPar);
                    }
                }

                colCheck ??= GetContext(db).GetQueryDef<object>((form.SchemaHashString ?? Form) + "_chk", sqlChk + " Where 0 = 1", [.. pars]);

                if (colCheck.GetType().GetProperty("idUtente") != null && form.UseDefaultParam)
                {
                    parameters ??= [];
                    if (!parameters.Any(p => p.name.Equals("idutente", StringComparison.CurrentCultureIgnoreCase)))
                    {
                        parameters.Add(new BecaParameter()
                        {
                            name = "idUtente",
                            value1 = _currentUser!.idUtenteLoc(_activeCompany?.idCompany),
                            comparison = "="
                        });
                    }
                }

                if (colCheck.GetType().GetProperty("idCompany") != null && form.UseDefaultParam)
                {
                    parameters ??= [];
                    if (!parameters.Any(p => p.name.Equals("idCompany", StringComparison.CurrentCultureIgnoreCase)))
                    {
                        parameters.Add(new BecaParameter()
                        {
                            name = "idCompany",
                            value1 = _activeCompany?.idCompany,
                            comparison = "="
                        });
                    }
                }

                if (parameters != null && parameters.Count > 0)
                {
                    foreach (BecaParameter par in parameters.Where(p => p.used == false && colCheck != null && colCheck.HasPropertyValue(p.name)))
                    {
                        if (par.value1 != null && par.value1.ToString().IsValidDateTimeJson()) par.value1 = par.value1.ToDateTimeFromJson();
                        if (par.value2 != null && par.value2.ToString().IsValidDateTimeJson()) par.value2 = par.value2.ToDateTimeFromJson();
                        sql += (numP - parameters.Count(p => p.used == true) - pars.Count(p => p == null)) == 0 && sql.IndexOf("WHERE", StringComparison.CurrentCultureIgnoreCase) < 0 ? " Where " : " And ";
                        //sql += par.name + " " + par.comparison;
                        switch (par.comparison.ToLower())
                        {
                            case "betweenFields":
                                sql += $"{{numP}} between {par.name.Replace(",", " And ")}";
                                pars.Add(par.value1);
                                numP++;
                                break;
                            case "between":
                                sql += par.name + " " + par.comparison;
                                sql += " {" + numP + "} and {" + (numP + 1).ToString() + "}";
                                pars.Add(par.value1);
                                pars.Add(par.value2);
                                numP++;
                                numP++;
                                break;

                            case "like":
                                sql += par.name + " " + par.comparison;
                                sql += " '%' + {" + numP + "} + '%'";
                                pars.Add(par.value1);
                                numP++;
                                break;

                            case "is null":
                                sql += par.name + " " + par.comparison;
                                //  pars.Add(null);
                                break;

                            case "in":
                                if (par.value1 != null)
                                {
                                    List<string> inClause = [];
                                    string[] vals = (par.value1.ToString() ?? "").Replace("(", "").Replace(")", "").Trim().Split(",");
                                    foreach (string val in vals)
                                    {
                                        inClause.Add($"{{{numP}}}");
                                        pars.Add(val);
                                        numP++;
                                    }
                                    sql += $"{par.name} In ({string.Join(",", inClause)})";
                                }
                                break;

                            default:
                                if (par.value1 == null)
                                {
                                    sql += par.name + " Is Null ";
                                }
                                else
                                {
                                    sql += par.name + " " + par.comparison;
                                    sql += " {" + numP + "}";
                                    pars.Add(par.value1);
                                    numP++;
                                }
                                break;
                        }
                        //numP++;
                    }
                }

                List<BecaFormField> fields = [.. _context.BecaFormField
                    .Where(f => f.Form == Form && f.OrderSequence != 0)
                    .OrderBy(f => Math.Abs(f.OrderSequence))];
                string sqlOrd = "";
                foreach (BecaFormField field in fields)
                {
                    sqlOrd += (sqlOrd.Length == 0 ? " Order By " : ", ") +
                        (field.OrderOnField == null ? field.Name.Trim() : field.OrderOnField.Trim()) +
                        (field.OrderSequence < 0 ? " DESC" : "");
                }
                sql += sqlOrd;

                List<BecaFormLevels> subForms = [.. _context.BecaFormLevels.Where(f => f.Form == Form)];

                if ((pageSize ?? 0) > 0) sql = sql.Replace("*", $"TOP({pageSize}) *");
                List<T> res = GetContext(db).ExecuteQuery<T>(Form, sql, subForms.Count > 0,
                    lowerCase ? PropertyNaming.LowerCase : PropertyNaming.LowerCamelCase, [.. pars]);

                if (getChildren)
                {
                    foreach (BecaFormLevels level in subForms)
                    {
                        BecaForm? childForm = _context.BecaForm
                            .FirstOrDefault(f => f.Form == level.ChildForm);
                        if (childForm != null)
                        {
                            string parent = form.getMainSource(true); // (form.ViewName == null || form.ViewName.ToString() == "" ? form.TableName : form.ViewName);
                            string child = (childForm.ViewName == null || childForm.ViewName.ToString() == "" ? childForm.TableName : childForm.ViewName);

                            db = childForm.ViewName.isNullOrempty() ? childForm.TableNameDB : childForm.ViewNameDB ?? "";

                            string sqlParent = sqlOrd == "" ? sql : sql.Replace(sqlOrd, "");
                            sql = "Select " +
                                string.Join(",", level.RelationColumn.Split(",").Select(n => parent + "." + n.Trim())) +
                                " From " + parent;
                            object objRelation = GetContext(db).GetQueryDef<object>(childForm.SchemaHashString ?? childForm.Form, sql + " Where 0 = 1");

                            List<string> orderChild = [.. _context.BecaFormField
                                .Where(f => f.Form == childForm.Form && f.OrderSequence != 0)
                                .OrderBy(f => Math.Abs(f.OrderSequence))
                                .Select(f => f.Name)];
                            string sqlOrdChild = orderChild.Count > 0 ? $" Order By {string.Join(",", orderChild)}" : "";

                            sql = "Select * From (" +
                                "Select " + child + ".*" +
                                " From (" + sqlParent + ") Parent " +
                                " Inner Join " + child +
                                " On " + string.Join(" And ", level.RelationColumn.Split(",").Select(n => "Parent." + n.Trim() + " = " + child + "." + n.Trim())) +
                                ") T" + sqlOrdChild;
                            List<object> children = this.GetDataBySQL(db, sql, (object[])[.. pars], lowerCase);

                            var groupJoin2 = res.GroupJoin(children,  //inner sequence
                                       p => GetRelationObjectString(level.RelationColumn, p), //outerKeySelector 
                                       c => GetRelationObjectString(level.RelationColumn, c),     //innerKeySelector
                                       (oParent, oChildren) =>  // resultSelector 
                                       {
                                           List<object> children2 = oChildren.ToList();
                                           List<object> curChildren = oParent.GetPropertyValueArray("children");
                                           curChildren ??= [];
                                           if (curChildren.Count < level.SubLevel) curChildren.Add(new List<object>());

                                           curChildren[level.SubLevel - 1] = children2;

                                           oParent.SetPropertyValuearray("children", curChildren);
                                           return oParent;
                                       });
                            res = groupJoin2.ToList();
                        }
                    }
                }

                return res;
            }
            else
            {
                return [];
            }
        }

        public static string GetRelationObjectString(string relation, object record)
        {
            string rel = string.Join("", relation.Split(",").Select(n => record.GetPropertyValue(n).ToString()));
            return rel;
        }

        public static object? GetRelationObject(object relation, object record)
        {
            Type tRel = relation.GetType();
            object? rel = Activator.CreateInstance(tRel);
            if (rel == null) return null;

            foreach (var prop in rel.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                rel.SetPropertyValue(prop.Name, record.GetPropertyValue(prop.Name));
                //rel.GetType().GetProperty(prop.Name).SetValue(rel, prop.GetValue(record));
            }
            return rel;
        }

        public List<T> GetDataByForm<T>(string Form, object record, bool view = true, bool getChildren = true, int? pageNumber = null, int? pageSize = null, bool lowerCase = false) where T : class, new()
        {
            BecaForm? form = _context.BecaForm
                .FirstOrDefault(f => f.Form == Form);
            List<object> pars = [];
            if (form != null)
            {
                BecaParameters aPar = new();
                if (!form.PrimaryKey.isNullOrempty())
                {
                    foreach (string orderField in form.PrimaryKey!.Split(","))
                    {
                        aPar.Add(orderField.Trim(), record.GetPropertyValue(orderField.Trim()));
                    }
                }
                List<T> data = this.GetDataByForm<T>(Form, aPar.parameters, view, getChildren, pageNumber, pageSize);
                return data;
            }
            else
            {
                return [];
            }
        }

        public List<object> GetDataByFormLevel(string Form, int subLevel, List<BecaParameter> parameters)
        {
            BecaFormLevels? childFiliali = _context.BecaFormLevels
                .Find(Form, subLevel);
            if (childFiliali == null)
            {
                return [];
            }
            return GetDataByForm<object>(childFiliali.ChildForm, parameters);
        }

        private string GetFormSQL(BecaForm form, bool view, bool uplWithoutUnderscore = false, bool noUpload = false)
        {
            string upl = noUpload ? "" : string.Join(", ", _context.BecaFormField
                .Where(f => f.Form == form.Form && f.FieldType == "upload")
                .ToList().Select(n => "'' As [" + n.Name.Replace("upl", "").Trim() + "upl], '' As [" + n.Name.Replace("upl", "").Trim() + "uplName]"));
            //.ToList().Select(n => "'' As [_" + n.Name.Trim() + "_upl_], '' As [_" + n.Name.Trim() + "_uplname_]"));
            if (uplWithoutUnderscore) upl = upl.Replace("_", "");
            string sql = "Select *" +
                (upl.Length > 0 ? ", " + upl + " " : " ") +
                "From " + form.getMainSource(view);
            //(view ? ((form.ViewName == null || form.ViewName.ToString() == "" ? form.TableName : form.ViewName)) : form.TableName);

            return sql;
        }

        private static string GetFormSQLAsync(List<BecaFormField> fields, BecaForm form, bool view, bool uplWithoutUnderscore = false, bool noUpload = false)
        {
            string upl = noUpload ? "" : string.Join(", ", fields
                .Where(f => f.Form == form.Form && f.FieldType == "upload")
                .Select(n => "'' As [" + n.Name.Replace("upl", "").Trim() + "upl], '' As [" + n.Name.Replace("upl", "").Trim() + "uplName]"));
            if (uplWithoutUnderscore) upl = upl.Replace("_", "");
            string sql = "Select *" +
                (upl.Length > 0 ? ", " + upl + " " : " ") +
                "From " + form.getMainSource(view);

            return sql;
        }

        public List<T> GetDataBySP<T>(string dbName, string spName, List<BecaParameter> parameters, PropertyNaming namingStrategy = PropertyNaming.AsIs) where T : class, new()
        {
            List<string> names = GetContext(dbName).GetProcedureParams(spName);

            if (names.Contains("@idUtente") && !parameters.Exists(p => p.name == "idUtente"))
            {
                parameters.Add(new BecaParameter()
                {
                    name = "idUtente",
                    value1 = _currentUser!.idUtenteLoc(_activeCompany?.idCompany),
                    comparison = "="
                });
            }

            string sql = $"Exec {spName} " +
                string.Join(", ", names.Where(x => parameters.Exists(p => p.name.Equals(x.Replace("@", ""), StringComparison.CurrentCultureIgnoreCase))).Select((x, i) => x +
                    @" = {" + i.ToString() + "}"));
            var pars = names.Where(x => parameters.Exists(p => p.name.Equals(x.Replace("@", ""), StringComparison.CurrentCultureIgnoreCase))).Select((x, i) =>
            {
                BecaParameter? par = parameters.Find(p => p.name.Equals(x.Replace("@", ""), StringComparison.CurrentCultureIgnoreCase));
                return par == null ?
                    null
                    :
                    (par.value1 != null && par.value1.ToString().IsValidDateTimeJson()) ?
                        par.value1.ToDateTimeFromJson()
                        :
                        par.value1;
            }).ToArray();
            return GetContext(dbName).ExecuteQuery<T>(spName, sql, false, namingStrategy, [.. pars]);
        }

        public T GetFormObject<T>(string Form, bool view, bool noUpload = false) => this.GetFormObject<T>(Form, view, [], noUpload);
        public T GetFormObject<T>(string Form, bool view, List<string> fields, bool noUpload = false)
        {
            BecaForm? form = _context.BecaForm
                .FirstOrDefault(f => f.Form == Form);
            //List<object> pars = [];
            if (form != null)
            {
                string sql = GetFormSQL(form, view, false, noUpload);
                string formName = (view ? form.SchemaHashString : form.SchemaHashTableString) ?? Form;

                object def = GetContext(form.TableNameDB).GetQueryDef<object>(formName, sql + " Where 0 = 1", fields);
                return (T)def;
            }
            else
            {
                return default!;
            }
        }

        private async Task<T> GetFormObjectAsync<T>(SqlConnection db, BecaForm form, List<BecaFormField> fields, bool view, bool noUpload = false) =>
            await this.GetFormObjectAsync<T>(db, form, fields, view, [], noUpload);
        private async Task<T> GetFormObjectAsync<T>(SqlConnection db, BecaForm form, List<BecaFormField> fields, bool view, List<string> reqFields, bool noUpload = false)
        {
            if (form != null)
            {
                string sql = GetFormSQLAsync(fields, form, view, false, noUpload);
                string formName = (view ? form.SchemaHashString : form.SchemaHashTableString) ?? form.Form;
                object def = await GetQueryDefAsync<object>(db, $"{formName}", sql + " Where 0 = 1", reqFields);
                return (T)def;
            }
            else
            {
                return default!;
            }
        }

        public object? GetPanelsByForm(string Form, List<BecaParameter> parameters)
        {
            List<object> data = this.GetDataByFormField(Form, "Panels", parameters, true);
            if (data == null || data.Count == 0) return null;
            return data[0];
        }

        public async Task<int?> UpdateDataByProcedure<T>(string dbName, string spName, object record) where T : class, new()
        {
            spName = spName.Replace("#Before##", "").Replace("#After#", "");
            List<string> names = GetContext(dbName).GetProcedureParams(spName);
            string sql = $"Exec {spName} " + string.Join(", ", names.Select((x, i) => $"{{{i}}}"));
            var pars = names.Select((x, i) => record.HasPropertyValue(x.Replace("@", ""))
                ? record.GetPropertyValue(x.Replace("@", ""))
                : record.HasPropertyValue(x.ToLower().Replace("@", "")) ? record.GetPropertyValue(x.ToLower().Replace("@", "")) : null).ToArray();
            return await GetContext(dbName).ExecuteSqlCommandAsync(sql, [.. pars]);
        }

        public async Task<(T? data, string message)> UpdateDataByForm<T>(string Form, object recordOld, object recordNew) where T : class, new()
        {
            BecaForm? form = _context.BecaForm
                .FirstOrDefault(f => f.Form == Form);

            if (form != null && form.ForceInsertOnUpdate)
            {
                if (form.ForceInsertOnUpdate)
                {
                    List<T> data = this.GetDataByForm<T>(Form, recordNew, false, false);
                    if (data.Count == 0)
                    {
                        return (await this.AddDataByForm<T>(Form, recordNew), "");
                    }
                }
            }

            if (form != null)
            {
                string tryUpload = await UploadByForm(form, recordNew);
                if (tryUpload != "") throw new InvalidOperationException(tryUpload);

                List<object?> pars = [];
                if (!form.UpdateProcedureName.isNullOrempty())
                {
                    var (data, resCount, endOperation) = await ExecFormProcedure<T>(Form, form.UpdateProcedureName!, form.TableNameDB, recordNew);
                    if (endOperation)
                    {
                        if (data == null) return (null, "Nessun record inserito dalla procedura");
                        return (data, "");
                    }
                }
                //if ((form.UpdateProcedureName ?? "") != "" && !form.UpdateProcedureName.Contains("#After#"))
                //{
                //    int? resSPBefore = await UpdateDataByProcedure<T>(form.TableNameDB, form.UpdateProcedureName, recordNew);
                //    if (!form.UpdateProcedureName.Contains("#Before#"))
                //    {
                //        List<T> inserted1 = this.GetDataByForm<T>(Form, recordNew);
                //        if (inserted1.Count == 0) return (null, "Nessun record inserito dalla procedura");
                //        return (inserted1[0], "");
                //    }
                //}

                object tblform = GetFormObject<object>(Form, false, true);
                MethodInfo? method = tblform.GetType().GetMethod("identityName");
                string idName = method == null ? "" : method.Invoke(tblform, null)!.ToString() ?? "";

                int numP = 0;
                string sql = "Update " + form.TableName + " Set ";
                PropertyInfo[] colsOld = recordOld.GetType().GetProperties().Where(p => p.Name != idName).ToArray();
                PropertyInfo[] colsNew = recordNew.GetType().GetProperties().Where(p => p.Name != idName).ToArray();
                if (colsNew.Length > 0)
                {
                    for (int i = 0; i < colsNew.Length; i++)
                    {
                        PropertyInfo p2 = colsNew.ElementAt(i);
                        if (colsOld.FirstOrDefault(p => p.Name.Equals(p2.Name, StringComparison.CurrentCultureIgnoreCase)) != null && tblform.HasPropertyValue(p2.Name))
                        {
                            PropertyInfo? p1 = colsOld.FirstOrDefault(p => p.Name.Equals(p2.Name, StringComparison.CurrentCultureIgnoreCase));
                            if (p1 != null)
                            {
                                bool update = false;
                                if (
                                    (Equals(p2.GetValue(recordNew), null) && !Equals(p1.GetValue(recordOld), null)) ||
                                    (!Equals(p2.GetValue(recordNew), null) && Equals(p1.GetValue(recordOld), null))
                                   )
                                {
                                    update = true;
                                }
                                if (!update)
                                {
                                    if (Equals(p2.GetValue(recordNew), null) && Equals(p1.GetValue(recordOld), null))
                                    {
                                        update = false;
                                    }
                                    else
                                    {
                                        if (!p2.GetValue(recordNew)!.Equals(p1.GetValue(recordOld))) update = true;
                                    }
                                }
                                if (update)
                                {
                                    object? pVal = p2.GetValue(recordNew);
                                    pars.Add(object.Equals(p2.GetValue(recordNew), null) || pVal == null ? null : (pVal.ToString().IsValidDateTimeJson() ? pVal.ToDateTimeFromJson() : pVal));
                                    sql += (numP > 0 ? ", " : "") + p2.Name + " = {" + numP.ToString() + "}";
                                    numP++;
                                }
                            }
                        }
                    }
                }
                else return (null, "Nessun record modificato");
                if (numP == 0)
                {
                    List<object> data = this.GetDataByForm<object>(Form, recordOld);
                    return ((T? data, string message))(data != null && data.Count != 0 ? (data[0], "Nessun dato modificato") : (null, "Nessun record modificato"));
                }

                string sqlW = "";
                int numPW = 0;
                int? resSave = 0;
                if (!form.PrimaryKey.isNullOrempty())
                {
                    foreach (string orderField in form.PrimaryKey!.Split(","))
                    {
                        sqlW += numPW == 0 ? " Where " : " And ";
                        sqlW += orderField + " = ";
                        sqlW += " {" + numP.ToString() + "}";
                        pars.Add(recordOld.GetPropertyValue(orderField.Trim()));
                        numP++;
                        numPW++;
                    }
                    sql += sqlW;
                    resSave = await GetContext(form.TableNameDB).ExecuteSqlCommandAsync(sql, [.. pars]);
                }
                if (!form.UpdateProcedureName.isNullOrempty() && form.UpdateProcedureName!.Contains("#After#"))
                {
                    await _context.SaveChangesAsync();
                    int? resSPAfter = await UpdateDataByProcedure<T>(form.TableNameDB, form.UpdateProcedureName, recordNew);
                }
                if (!(resSave.GetValueOrDefault() > 0 & resSave.HasValue))
                    return ((T)recordOld, "Non è stato possibile aggiornare il record");

                List<T> inserted2 = this.GetDataByForm<T>(Form, recordNew);
                if (inserted2.Count == 0) return (null, "Problemi nel trovare il nuovo record");
                return (inserted2[0], "");
            }
            else
            {
                return (null, $"L'aggiornamento è fallito: becaForm non trovata");
            }
        }

        public async Task<string> ActionByForm(int idview, string actionName, object record)
        {
            BecaViewAction? action = _context.BecaViewActions
                .FirstOrDefault(f => f.idBecaView == idview && f.ActionName == actionName);

            string error = "";
            if (action != null && !action.Command.isNullOrempty() && !action.ConnectionName.isNullOrempty())
            {
                try
                {
                    int res = 0;
                    string[] procs = action.Command!.Split(";");
                    foreach (string proc in procs)
                    {
                        res += await ExecuteProcedure(action.ConnectionName!, proc,
                            (List<BecaParameter>)record.GetType()
                                .GetProperties()
                                .Select(p => new BecaParameter { name = p.Name, value1 = p.GetValue(record), comparison = "=" })
                                .ToList());
                    }
                }
                catch (Exception ex) { error += ex.Message + Environment.NewLine; }

                await _context.SaveChangesAsync();
            }
            else
            {
                return "L'azione non ha comandi associati, rivolgersi all'amministratore di sistema";
            }
            return error;
        }

        public async Task<string> ActionByForm(int idview, string actionName, List<BecaParameter> parameters)
        {
            BecaViewAction? action = _context.BecaViewActions
                .FirstOrDefault(f => f.idBecaView == idview && f.ActionName == actionName);

            string error = "";
            if (action != null && !action.Command.isNullOrempty() && !action.ConnectionName.isNullOrempty())
            {
                try
                {
                    int res = 0;
                    string[] procs = action.Command!.Split(";");
                    foreach (string proc in procs)
                    {
                        res += await ExecuteProcedure(action.ConnectionName!, proc, parameters);
                    }
                }
                catch (Exception ex) { error += ex.Message + Environment.NewLine; }

                await _context.SaveChangesAsync();
            }
            else
            {
                return "L'azione non ha comandi associati, rivolgersi all'amministratore di sistema";
            }
            return error;
        }
        #endregion "Form"

        #region "Field"

        public List<object> GetDataByFormField(string Form, string field, List<BecaParameter> parameters, bool lowerCase)
        {
            if (_currentUser == null || _activeCompany == null) return [];

            BecaFormField? formField = _context.BecaFormField
                .Find(Form, field);
            BecaFormFieldLevel? formFieldCust = _context.BecaFormFieldLevel
                .Find(_currentUser.idProfileDef(_activeCompany.idCompany), Form, field);

            if (formField == null || formField.DropDownListDB.isNullOrempty()) return [];

            //string ddl = formField.DropDownList;
            //string ddlPar = formField.Parameters;
            string ddl = formFieldCust == null ? formField.DropDownList ?? "" : formFieldCust.DropDownList ?? "";
            string ddlPar = formFieldCust == null ? formField.Parameters ?? "" : formFieldCust.Parameters ?? "";

            object? colCheck = null;

            if (formField != null)
            {
                //List<BecaParameter> parameters = new List<BecaParameter>();
                string sql = ddl;
                string sqlChk = $"Select * From {ddl[(ddl.IndexOf("FROM", StringComparison.CurrentCultureIgnoreCase) + 5)..]}";
                if (sqlChk.Contains("WHERE", StringComparison.CurrentCultureIgnoreCase)) sqlChk = sqlChk[..(sqlChk.IndexOf("WHERE", StringComparison.CurrentCultureIgnoreCase) - 1)];
                if (sqlChk.Contains("ORDER", StringComparison.CurrentCultureIgnoreCase)) sqlChk = sqlChk[..(sqlChk.IndexOf("ORDER", StringComparison.CurrentCultureIgnoreCase) - 1)];
                if (sqlChk.Contains("GROUP", StringComparison.CurrentCultureIgnoreCase)) sqlChk = sqlChk[..(sqlChk.IndexOf("GROUP", StringComparison.CurrentCultureIgnoreCase) - 1)];
                //sqlChk += " WITH (NOLOCK)";

                List<object?> pars = [];
                int numP = 0;
                if (sqlChk.Contains('('))
                {
                    string inside = sqlChk.inside("(", ")");
                    if (inside != "")
                    {
                        string[] funcPars = inside.Replace(" ", "").Split(",");
                        string funcPar = "";
                        string funcParChk = "";
                        foreach (string par in funcPars)
                        {
                            funcPar += "{" + numP + "}";
                            funcParChk += "Null,";
                            numP++;
                            BecaParameter? bPar = parameters.FirstOrDefault(p => p.name.Replace("+", "").Equals(par, StringComparison.CurrentCultureIgnoreCase));
                            if (bPar != null)
                            {
                                if (bPar.value1 != null && bPar.value1.ToString().IsValidDateTimeJson()) bPar.value1 = bPar.value1.ToDateTimeFromJson();
                                if (bPar.value2 != null && bPar.value2.ToString().IsValidDateTimeJson()) bPar.value2 = bPar.value2.ToDateTimeFromJson();

                                pars.Add(bPar.used == true ? bPar.value2 : bPar.value1);
                                bPar.used = true;
                            }
                            else
                            {
                                if (par == "idUtente")
                                    pars.Add(_currentUser.idUtenteLoc(_activeCompany?.idCompany));
                                else if (par == "idCompany")
                                    pars.Add(_activeCompany?.idCompany);
                                else
                                    pars.Add(null);
                            }
                        }
                        funcPar = funcPar.Replace("}{", "},{");
                        sqlChk = sqlChk.Replace(sqlChk.inside("(", ")"), funcParChk).Replace(",)", ")");
                        sql = sql.Replace(sql.inside("(", ")"), funcPar);

                        if (field == "_Grafico")
                        {
                            colCheck = this.GetDataByFormField(Form, field + "Check", parameters, lowerCase);
                        }
                    }
                }
                colCheck ??= GetContext(formField.DropDownListDB!).GetQueryDef<object>((formField.SchemaHashString ?? Form) + '_' + field + "_chk", sqlChk + " Where 0 = 1", parameters: [.. pars]);

                if (((ddlPar != null && ddlPar.Contains("idUtente")) || ddl.Contains("idUtente"))
                    && colCheck.GetType().GetProperty("idUtente") != null)
                {
                    parameters ??= [];
                    if (!parameters.Any(p => p.name.Equals("idutente", StringComparison.CurrentCultureIgnoreCase)))
                    {
                        parameters.Add(new BecaParameter()
                        {
                            name = "idUtente",
                            value1 = _currentUser.idUtenteLoc(_activeCompany?.idCompany),
                            comparison = "="
                        });
                    }
                }

                if (((ddlPar != null && ddlPar.Contains("idCompany")) || ddl.Contains("idCompany"))
                    && colCheck.GetType().GetProperty("idCompany") != null)
                {
                    parameters ??= [];
                    if (!parameters.Any(p => p.name.Equals("idcompany", StringComparison.CurrentCultureIgnoreCase)))
                    {
                        parameters.Add(new BecaParameter()
                        {
                            name = "idCompany",
                            value1 = _activeCompany?.idCompany,
                            comparison = "="
                        });
                    }
                }

                string sqlOrd = "";
                if (sql.Contains("ORDER", StringComparison.CurrentCultureIgnoreCase))
                {
                    sqlOrd = sql[sql.IndexOf("ORDER", StringComparison.CurrentCultureIgnoreCase)..];
                    sql = sql[..(sql.IndexOf("ORDER", StringComparison.CurrentCultureIgnoreCase) - 1)];
                }
                string sqlGroup = "";
                if (sql.Contains("GROUP", StringComparison.CurrentCultureIgnoreCase))
                {
                    sqlGroup = sql[sql.IndexOf("GROUP", StringComparison.CurrentCultureIgnoreCase)..];
                    sql = sql[..(sql.IndexOf("GROUP", StringComparison.CurrentCultureIgnoreCase) - 1)];
                }
                string sqlWhere = "";
                if (sql.Contains("WHERE", StringComparison.CurrentCultureIgnoreCase))
                {
                    sqlWhere = sql[sql.IndexOf("WHERE", StringComparison.CurrentCultureIgnoreCase)..];
                    sql = sql[..(sql.IndexOf("WHERE", StringComparison.CurrentCultureIgnoreCase) - 1)];
                }
                sql = sql + " " + sqlWhere;
                if (parameters != null && parameters.Count > 0)
                {
                    foreach (BecaParameter par in parameters.Where(p => p.used == false && colCheck != null && colCheck.HasPropertyValue(p.name)))
                    {
                        if (par.value1 != null && par.value1.ToString().IsValidDateTimeJson()) par.value1 = par.value1.ToDateTimeFromJson();
                        if (par.value2 != null && par.value2.ToString().IsValidDateTimeJson()) par.value2 = par.value2.ToDateTimeFromJson();
                        sql += (numP - parameters.Count(p => p.used == true) - pars.Count(p => p == null)) == 0 && sql.IndexOf("WHERE", StringComparison.CurrentCultureIgnoreCase) < 0 ? " Where " : " And ";
                        //sql += par.name + " " + par.comparison;
                        switch (par.comparison.ToLower())
                        {
                            case "betweenfields":
                                sql += $"{{{numP}}} between {par.name.Replace(",", " And ")}";
                                pars.Add(par.value1);
                                numP++;
                                break;

                            case "between":
                                sql += par.name + " " + par.comparison;
                                sql += " {" + numP + "} and {" + (numP + 1).ToString() + "}";
                                pars.Add(par.value1);
                                pars.Add(par.value2);
                                numP++;
                                numP++;
                                break;

                            case "like":
                                sql += par.name + " " + par.comparison;
                                sql += " '%' + {" + numP + "} + '%'";
                                pars.Add(par.value1);
                                numP++;
                                break;

                            case "in":
                                if (par.value1 != null)
                                {
                                    List<string> inClause = [];
                                    string[] vals = (par.value1.ToString() ?? "").Replace("(", "").Replace(")", "").Trim().Split(",");
                                    foreach (string val in vals)
                                    {
                                        inClause.Add($"{{{numP}}}");
                                        pars.Add(val);
                                        numP++;
                                    }
                                    sql += $"{par.name} In ({string.Join(",", inClause)})";
                                }
                                break;

                            default:
                                if (par.value1 == null)
                                {
                                    sql += par.name + " Is Null ";
                                }
                                else
                                {
                                    sql += par.name + " " + par.comparison;
                                    sql += " {" + numP + "}";
                                    pars.Add(par.value1);
                                    numP++;
                                }
                                break;
                        }
                        //numP++;
                    }
                }
                sql = sql + " " + sqlGroup + " " + sqlOrd;
                return GetContext(formField.DropDownListDB!).ExecuteQuery<object>(Form + '_' + field, sql, false,
                    lowerCase ? PropertyNaming.LowerCase : PropertyNaming.LowerCamelCase, [.. pars]);
            }
            else
            {
                return [];
            }
        }

        public List<object> GetDataByFormChildSelect(string Form, string childForm, short sqlNumber, object parent, bool lowerCase)
        {
            if (_currentUser == null || _activeCompany == null) return [];
            BecaFormLevels? child = _context.BecaFormLevels
                .FirstOrDefault(c => c.Form == Form && c.ChildForm == childForm);
            BecaForm? form = _context.BecaForm.FirstOrDefault(f => f.Form == childForm);

            if (form == null || child == null) return [];

            string? ddl = child.GetPropertyValue("ComboAddSql" + sqlNumber.ToString())!.ToString();
            string? ddlKeys = child.GetPropertyValue("ComboAddSql" + sqlNumber.ToString() + "Keys").ToString();
            string? ddlDisplay = child.GetPropertyValue("ComboAddSql" + sqlNumber.ToString() + "Display").ToString();

            if (ddl.isNullOrempty() || ddlKeys.isNullOrempty() || ddlDisplay.isNullOrempty()) return [];

            string key = ddlKeys!.Replace(",", " + ");

            List<object?> pars = [];
            string sqlChk = "Select * From " + form.TableName;
            object colCheck = GetContext(form.TableNameDB).GetQueryDef<object>((form.SchemaHashString ?? form.Form) + "_ca" + sqlNumber.ToString() + "_chk", sqlChk + " Where 0 = 1");
            if (colCheck.GetType().GetProperty("idUtente") != null)
            {
                ddlKeys += ",idUtente";
                pars.Add(_currentUser.idUtenteLoc(_activeCompany?.idCompany));
            }
            if (colCheck.GetType().GetProperty("idCompany") != null)
            {
                ddlKeys += ",idCompany";
                pars.Add(_activeCompany!.idCompany);
            }

            string sql = ddl + " Where " + key +
                " Not In (" +
                "Select " + key + " From " + form.TableName +
                " Where " + string.Join(" And ", child.RelationColumn.Split(",").Select((n, i) => n.Trim() + " = {" + i.ToString() + "}")) +
                ") Order By " + ddlDisplay;

            if (parent != null)
            {
                foreach (string par in child.RelationColumn.Split(","))
                {
                    if (parent.HasPropertyValue(par)) pars.Add(parent.GetPropertyValue(par));
                }
            }
            return GetContext(form.TableNameDB).ExecuteQuery<object>(form + "_ca" + sqlNumber.ToString(), sql, false,
                    lowerCase ? PropertyNaming.LowerCase : PropertyNaming.LowerCamelCase, [.. pars]);
        }

        public List<object> GetDataBySQL(string dbName, string sql, List<BecaParameter> parameters, bool useidUtente = true, bool lowerCase = false)
        {
            if (_currentUser == null || _activeCompany == null) return [];

            string sqlFor = "";
            if (sql.Contains("FOR ", StringComparison.CurrentCultureIgnoreCase))
            {
                sqlFor = sql[sql.IndexOf("FOR ", StringComparison.CurrentCultureIgnoreCase)..];
                sql = sql[..(sql.IndexOf("FOR ", StringComparison.CurrentCultureIgnoreCase) - 1)];
            }
            string sqlOrd = "";
            if (sql.Contains("ORDER", StringComparison.CurrentCultureIgnoreCase))
            {
                sqlOrd = sql[sql.IndexOf("ORDER", StringComparison.CurrentCultureIgnoreCase)..];
                sql = sql[..(sql.IndexOf("ORDER", StringComparison.CurrentCultureIgnoreCase) - 1)];
            }
            string sqlGroup = "";
            if (sql.Contains("GROUP", StringComparison.CurrentCultureIgnoreCase))
            {
                sqlGroup = sql[sql.IndexOf("GROUP", StringComparison.CurrentCultureIgnoreCase)..];
                sql = sql[..(sql.IndexOf("GROUP", StringComparison.CurrentCultureIgnoreCase) - 1)];
            }
            string sqlWhere = "";
            if (sql.Contains("WHERE", StringComparison.CurrentCultureIgnoreCase))
            {
                sqlWhere = sql[sql.IndexOf("WHERE", StringComparison.CurrentCultureIgnoreCase)..];
                sql = sql[..(sql.IndexOf("WHERE", StringComparison.CurrentCultureIgnoreCase) - 1)];
            }
            string sqlTable = sql[(sql.IndexOf("FROM", StringComparison.CurrentCultureIgnoreCase) + 5)..];
            string sqlChk = "Select * From " + sqlTable;

            object colCheck = GetContext(dbName).GetQueryDef<object>(sqlChk.Replace(" ", ""), sqlChk + " Where 0 = 1");
            if (colCheck.GetType().GetProperty("idUtente") != null && useidUtente)
            {
                parameters ??= [];
                if (parameters.Find(p => p.name == "idUtente") == null)
                {
                    parameters.Add(new BecaParameter()
                    {
                        name = "idUtente",
                        value1 = _currentUser.idUtenteLoc(_activeCompany?.idCompany),
                        comparison = "="
                    });
                }
            }
            sql = sql + " " + sqlWhere;
            List<object?> pars = [];
            if (parameters != null && parameters.Count > 0)
            {
                int numP = 0;
                foreach (BecaParameter par in parameters)
                {
                    if (colCheck.HasPropertyValue(par.name))
                    {
                        sql += sql.Contains("WHERE", StringComparison.CurrentCultureIgnoreCase) ? " And " : " Where ";
                        sql += par.name + " " + par.comparison;
                        switch (par.comparison.ToLower())
                        {
                            case "between":
                                sql += " {" + numP + "} and {" + (numP + 1).ToString() + "}";
                                pars.Add(par.value1);
                                pars.Add(par.value2);
                                break;

                            case "like":
                                sql += " '%' + {" + numP + "} + '%'";
                                pars.Add(par.value1);
                                break;

                            case "in":
                                sql += par.name + " " + par.comparison;
                                string[] vals = ((string?)par.value1 ?? "").ToString().Replace("(", "").Replace(")", "").Replace(" ", "").Split(",");
                                foreach (string val in vals)
                                {
                                    pars.Add(val);
                                    numP++;
                                }
                                break;

                            default:
                                sql += " {" + numP + "}";
                                pars.Add(par.value1);
                                break;
                        }
                        numP++;
                    }
                }
            }
            sql = sql + " " + sqlGroup + " " + sqlOrd + " " + sqlFor;
            return GetContext(dbName).ExecuteQuery<object>("", sql, false,
                    lowerCase ? PropertyNaming.LowerCase : PropertyNaming.LowerCamelCase, [.. pars]);
        }

        private List<object> GetDataBySQL(string dbName, string sql, object[] parameters, bool lowerCase)
        {
            return GetContext(dbName).ExecuteQuery<object>("", sql, false,
                    lowerCase ? PropertyNaming.LowerCase : PropertyNaming.LowerCamelCase, parameters);
        }

        public IDictionary<string, object> GetDataDictBySQL(string dbName, string sql, List<BecaParameter> parameters, bool lowerCase)
        {
            List<object> data = this.GetDataBySQL(dbName, sql, parameters, lowerCase);

            sql = sql[..(sql.IndexOf("FROM", StringComparison.CurrentCultureIgnoreCase) - 1)].TrimEnd()[7..];
            string[] fields = sql.Split(",");
            string key = fields[0];
            string val = fields[1];
            Dictionary<string, object> dict = [];
            foreach (var row in data)
            {
                dict.Add(row.GetPropertyString(key).ToString(), row.GetPropertyValue(val));
            }
            return dict;
        }

        public ViewChart GetGraphByFormField(string Form, string field, List<BecaParameter> parameters)
        {
            ViewChart graph = new();

            List<object> axisX = this.GetDataByFormField(Form, field + "X", parameters, true);
            List<object> data = this.GetDataByFormField(Form, field + "Dati", parameters, true);

            foreach (object X in axisX)
            {
                ViewAxisXvalue Xval = new()
                {
                    value = X.GetPropertyValue("XValue")
                };
                dtoBecaFilterValue FV;
                if (X.GetPropertyValue("FVname1").ToString() != "")
                {
                    FV = new dtoBecaFilterValue
                    {
                        filterName = X.GetPropertyString("FVname1"),
                        value = X.GetPropertyString("FV1"),
                        Default = X.GetPropertyString("FV1"),
                        Api = false
                    };
                    Xval.filterValues.Add(FV);
                }
                if (X.GetPropertyValue("FVname1").ToString() != "")
                {
                    FV = new dtoBecaFilterValue
                    {
                        filterName = X.GetPropertyString("FVname2"),
                        value = X.GetPropertyString("FV2"),
                        Default = X.GetPropertyString("FV2"),
                        Api = false
                    };
                    Xval.filterValues.Add(FV);
                }
                graph.axisX.caption.Add(X.GetPropertyString("XValue"));
                graph.axisX.value.Add(Xval);
            }

            BecaFormField? formField = _context.BecaFormField
                .Find(Form, field + "Dati");
            if (formField == null || formField.Parameters == null) return graph;

            List<string> lines = [.. formField.Parameters.Replace(" ", "").Split(",")];
            foreach (string line in lines)
            {
                ViewChartValue val = new()
                {
                    label = line
                };
                graph.values.Add(val);
            }

            foreach (object X in data)
            {
                int il = 0;
                foreach (string line in lines)
                {
                    graph.values[il].data.Add(X.GetPropertyValue(line));
                    il++;
                }
            }
            return graph;
        }

        #endregion "Field"

        #region "SQL"
        public async Task CompleteAsync()
        {
            await _context.SaveChangesAsync();
        }

        public int ExecuteSqlCommand(string dbName, string commandText, params object[] parameters)
        {
            return GetContext(dbName).ExecuteSqlCommand(commandText, parameters);
        }

        public async Task<int> ExecuteSqlCommandAsync(string dbName, string commandText, params object[] parameters)
        {
            return await GetContext(dbName).ExecuteSqlCommandAsync(commandText, parameters);
        }

        public async Task<int> ExecuteProcedure(string dbName, string spName, List<BecaParameter> parameters)
        {
            List<string> names = GetContext(dbName).GetProcedureParams(spName);
            if (names.Contains("@idUtente") && !parameters.Any(p => p.name == "idUtente"))
            {
                parameters.Add(new BecaParameter("idUtente", _currentUser?.idUtenteLoc(_activeCompany?.idCompany)));
            }
            string sql = $"Exec {spName} " +
                string.Join(", ", names.Where(x => parameters.Exists(p => p.name.Equals(x.Replace("@", ""), StringComparison.CurrentCultureIgnoreCase))).Select((x, i) => x +
                    @" = {" + i.ToString() + "}"));
            var pars = names.Where(x => parameters.Exists(p => p.name.Equals(x.Replace("@", ""), StringComparison.CurrentCultureIgnoreCase))).Select((x, i) =>
            {
                BecaParameter? par = parameters.Find(p => p.name.Equals(x.Replace("@", ""), StringComparison.CurrentCultureIgnoreCase));
                return par == null ?
                    null
                    :
                    (par.value1 != null && par.value1.ToString().IsValidDateTimeJson()) ?
                        par.value1.ToDateTimeFromJson()
                        :
                        par.value1;
            }).ToArray();
            return await GetContext(dbName).ExecuteSqlCommandAsync(sql, [.. pars]);
        }
        #endregion "SQL"

        private DbDatiContext GetContext(int id)
        {
            return this.GetContext(_activeCompany!.Connections.FirstOrDefault(c => c.idConnection == id)!.ConnectionName);
        }

        private DbDatiContext GetContext(string dbName)
        {
            if (dbName == "")
                dbName = _activeCompany!.Connections.FirstOrDefault(c => c.Default == true)!.ConnectionName;
            string ctxName = $"{_activeCompany!.idCompany}_{dbName}";
            //if (_databases.ContainsKey(ctxName)) return _databases[ctxName];

            //DbDatiContext db = new DbDatiContext(_formTool, _activeCompany.Connections.FirstOrDefault(c => c.ConnectionName == dbName).ConnectionString);
            //_databases.Add(ctxName, db);
            //return db;
            if (_databases.TryGetValue(ctxName, out DbDatiContext? value))
                return value;

            var connectionString = _activeCompany.Connections
                .FirstOrDefault(c => c.ConnectionName == dbName)?.ConnectionString ?? "";

            var optionsBuilder = new DbContextOptionsBuilder<DbDatiContext>();
            optionsBuilder.UseSqlServer(connectionString);

            DbDatiContext db = new(_formTool, connectionString);
            _databases.Add(ctxName, db);
            return db;
        }

        private async Task<string> UploadByForm(BecaForm form, object record)
        {
            try
            {
                List<BecaFormField> upl = [.. _context.BecaFormField.Where(f => f.Form == form.Form && f.FieldType == "upload")];
                //foreach (BecaFormField field in upl.Where(f => record.HasPropertyValue($"_{f.Name}_upl_") && record.GetPropertyString($"_{f.Name}_upl_").ToString() != ""))
                foreach (BecaFormField field in upl.Where(f => record.HasPropertyValue($"{f.Name.Replace("upl", "")}upl") && record.GetPropertyString($"{f.Name.Replace("upl", "")}upl").ToString() != ""))
                {
                    string res = await SaveFileByField(form, field, record);
                    if (res.Contains("ERR: ")) return res.Replace("ERR: ", "");
                    if (field.Name.right(3) == "upl")
                        record.SetPropertyValue(field.Name.leftExcept(3), res); //field.Parameters.Split("|").Last()
                    else
                        record.SetPropertyValue(field.Name, res); //field.Parameters.Split("|").Last()
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UploadByForm: {ex.Message}");
            }
            return "";
        }

        private async Task<string> SaveFileByField(BecaForm form, BecaFormField field, object record)
        {
            try
            {
                //Cerco i parametri x salvare il file
                // il primo è la tabella/vista in cui cercare il tipo documento
                // il secondo il nome del campo codice documento
                // il terzo il nome del campo folder in cui salvare
                // il quarto il nome del campo name da usare eventualmente per rinominare il file
                // il quinto le estensioni permesse
                // il sesto la dimensione massima
                // il settimo il codice documento da usare o il nome del campo da cui prendere il nome
                if (field.Parameters.isNullOrempty()) return "";
                string[] pars = field.Parameters!.Split("|");

                if (pars.Length < 6) return "";

                //se il 6° parametro è fra apici allora mi viene fornito il valore da cercare
                //altrimento mi viene fornito il nome del campo in cui cercare il tipo documento che sto caricando
                string cod = pars[6].Contains('\'')
                    ? pars[6].Replace("'", "")
                    : record.HasPropertyValue(pars[6]) ? record.GetPropertyString(pars[6]) : "";

                string sql = "Select " + pars[1] + " as Cod, " + pars[2] + " As Fld, " + pars[3] + " As Name, " + pars[4] + " As ext, " + pars[5] + " As MB " +
                    "From " + pars[0] + " Where " + pars[1] + " = {0}";
                List<BecaParameter> parameters = [ new BecaParameter()
                    {
                        name = pars[1],
                        value1 = (object)cod,
                        comparison = "="
                    }
                ];
                List<object> data = this.GetDataBySQL(form.TableNameDB, sql, parameters);
                if (data.Count < 1)
                    return "ERR: C'è stato un problema nel reperimento del tipo documento (" + field.Name + "). Contattare il fornitore";

                object tipoDoc = data[0];
                string folderNameSub = GetSaveName(tipoDoc.GetPropertyString("Fld"), record).Replace("/", @"\");
                string folderName = folderNameSub.Contains('\\') || folderNameSub.Contains(@":\")
                    ? @"\\192.168.0.207\BecaWeb\Web\Upload\" + _activeCompany!.MainFolder + @"\" + folderNameSub
                    : @"E:\BecaWeb\Web\Upload\" + _activeCompany!.MainFolder + @"\" + folderNameSub;
                folderName = @"\\192.168.0.207\BecaWeb\Web\Upload\" + _activeCompany.MainFolder + @"\" + folderNameSub;
                // Percorso relativo basato sulla directory virtuale configurata

                // Costruisci il percorso completo
                string physicalPath = folderName;
                string fileName = GetSaveName(tipoDoc.GetPropertyString("Name"), record);

                _logger.LogDebug($"Salvo il file {fileName} in {physicalPath}");

                if (physicalPath == "")
                {
                    return "ERR: C'è stato un problema nella definizione della cartella di destinazione (" + field.Name + "). Contattare il fornitore";
                }
                if (fileName == "" && tipoDoc.GetPropertyValue("Name").ToString() != "")
                {
                    return "ERR: C'è stato un problema nella definizione del nome del file (" + field.Name + "). Contattare il fornitore";
                }

                if (!Directory.Exists(physicalPath)) Directory.CreateDirectory(physicalPath);

                string fileUploaded = record.GetPropertyString($"{field.Name.Replace("upl", "")}upl");
                //string fileUploaded = record.GetPropertyValue($"_{field.Name}_upl_").ToString();
                //string fileUploaded = record.GetPropertyValue($"_{field.Name}_upl_").ToString();
                string fileUploadedName = record.HasPropertyValue($"{field.Name.Replace("upl", "")}uplName")
                    ? record.GetPropertyString($"{field.Name.Replace("upl", "")}uplName")
                    : record.HasPropertyValue($"{field.Name.Replace("upl", "")}") ? record.GetPropertyString($"{field.Name}") : "";

                if (fileName == "") fileName = fileUploadedName;
                if (fileUploaded.Length > 0)
                {
                    string sFileExtension = fileUploadedName.Remove(0, fileUploadedName.LastIndexOf('.') + 1).ToLower();
                    if (tipoDoc.GetPropertyValue("MB").ToString() != "" && tipoDoc.GetPropertyString("MB") != "0" && GetBase64Dimension(fileUploaded) > int.Parse(tipoDoc.GetPropertyString("MB")))
                    {
                        return "ERR: Il file " + fileUploadedName + " eccede la dimensione permessa (" + Math.Round((decimal)(int.Parse(tipoDoc.GetPropertyString("MB")) / 1024 / 1024), 2).ToString() + "MB)";
                    }
                    if (!tipoDoc.GetPropertyString("ext").Contains(sFileExtension, StringComparison.CurrentCultureIgnoreCase) && tipoDoc.GetPropertyString("ext") != "")
                    {
                        return "ERR: Il tipo di file (" + sFileExtension + ") non è ammesso. Seleziona uno tra questi tipi: " + tipoDoc.GetPropertyString("ext");
                    }
                    else
                    {
                        fileName = fileName.Replace("." + sFileExtension, "") + "." + sFileExtension;

                        _logger.LogDebug($"Scrivo il file {fileName} in {physicalPath}");

                        string filePath = Path.Combine(physicalPath, fileName);
                        if (File.Exists(filePath)) System.IO.File.Delete(filePath);
                        await System.IO.File.WriteAllBytesAsync(filePath, Convert.FromBase64String(fileUploaded));
                        return fileName;
                    }
                }
                else { return ""; }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore nel salvataggio del file: {ex.Message}");
                return "ERR: " + ex.Message;
            }
        }

        private static string GetSaveName(string Name, object record)
        {
            int p1, p2;
            while (Name.Contains('#'))
            {
                p1 = Name.IndexOf('#');
                p2 = Name.IndexOf('#', p1 + 1);
                string ph = Name.Substring(p1 + 1, p2 - p1 - 1);
                if (record.HasPropertyValue(ph))
                {
                    if (record.GetPropertyValue(ph).GetType() == typeof(DateTime))
                        Name = Name.Replace("#" + ph + "#", ((DateTime)record.GetPropertyValue(ph)).ToString("yyyyMMdd"));
                    else if (record.GetPropertyValue(ph).ToString().isDate())
                        Name = Name.Replace("#" + ph + "#", DateTime.Parse(record.GetPropertyString(ph)).ToString("yyyyMMdd"));
                    else
                        Name = Name.Replace("#" + ph + "#", record.GetPropertyValue(ph).ToString());
                }
                else
                    return "";
            }
            return Name;
        }

        private static double GetBase64Dimension(string base64String)
        {
            bool applyPaddingsRules = true;

            // Remove MIME-type from the base64 if exists
            int base64Length = base64String.AsSpan()[(base64String.IndexOf(',') + 1)..].Length;

            double fileSizeInByte = Math.Ceiling((double)base64Length / 4) * 3;

            if (applyPaddingsRules && base64Length >= 2)
            {
                var paddings = base64String[^2..];
                fileSizeInByte = paddings.Equals("==") ? fileSizeInByte - 2 : paddings[1].Equals('=') ? fileSizeInByte - 1 : fileSizeInByte;
            }
            return fileSizeInByte > 0 ? fileSizeInByte / 1_048_576 : 0;
        }

        private async Task<(T? data, int? resCount, Boolean endOperation)> ExecFormProcedure<T>(string Form, string procedure, string tblDB, object record) where T : class, new()
        {
            int? resSPBefore = null;
            if (!procedure.isNullOrempty() && procedure.Split(";").Any(p => p.Contains("#Before#") || !p.Contains('#')))
            {
                foreach (string proc in procedure.Split(";").Where(p => p.Contains("#Before#") || !p.Contains('#')))
                {
                    resSPBefore = (resSPBefore ?? 0) + await UpdateDataByProcedure<T>(tblDB, proc, record);
                }
                if (procedure.Split(";").Any(p => !p.Contains('#')))
                //if (!form.AddProcedureName.Contains("#Before#"))
                {
                    List<T> spRes = this.GetDataByForm<T>(Form, record);
                    return (spRes.Count == 0 ? null : spRes[0], spRes.Count, true);
                }
                var res = new T();
                return (res, resSPBefore, false);
            }
            return (null, null, false);
        }

        public async Task<(List<BecaForm> forms, List<BecaFormLevels> children, string message)> GetBecaFormsByRequest(DataFormPostParameters req)
        {
            var view_forms = req.RequestList.Select(r => new { r.idView, form = GetFormByView(r.idView, r.Form) }).ToList();
            if (view_forms.Any(f => f.form.isNullOrempty()))
            {
                var errorMessage = string.Join("\n",
                    view_forms
                        .Where(f => string.IsNullOrEmpty(f.form))
                        .Select(f => $"Alla view {f.idView} non è associata alcuna form")
                );
                return ([], [], errorMessage);
            }

            var forms = view_forms.Select(o => o.form).Distinct().ToList();
            var children = await _context.BecaFormLevels.Where(f => forms.Contains(f.Form)).Distinct().ToListAsync();
            var childForms = children.Select(c => c.ChildForm).Distinct();
            forms = forms.Concat(childForms).Distinct().ToList();

            var becaForms = await _context.BecaForm
                .Where(f => forms.Contains(f.Form)) // Filtro per selezionare solo i BecaForm con Form presente in forms
                .ToListAsync(); // Async per interrogare il DB in modo efficiente

            // Troviamo le form mancanti confrontando quelle richieste con quelle effettivamente recuperate
            var foundForms = becaForms.Select(bf => bf.Form).ToHashSet(); // Per lookup veloce
            var missingForms = forms.Where(f => !foundForms.Contains(f!)).ToList();

            if (missingForms.Count != 0)
            {
                return ([], [], $"Le seguenti form non sono presenti nel database: {string.Join(", ", missingForms)}");
            }

            return (becaForms, children, "");
        }

        public async Task<(List<BecaFormField> fields, List<BecaFormFieldLevel> custFields, string message)> GetBecaFormFieldsByRequest(DataFormPostParameters req)
        {
            if (_currentUser == null || _activeCompany == null) return ([], [], "Token non valido o header mancanti");

            var view_forms = req.RequestList.Select(r => new { r.idView, form = GetFormByView(r.idView, r.Form) }).ToList();
            var forms = view_forms.Select(o => o.form).Distinct().ToList();
            var childForms = await _context.BecaFormLevels.Where(f => forms.Contains(f.Form)).Select(c => c.ChildForm).Distinct().ToListAsync();
            forms.AddRange(childForms);
            forms = forms.Distinct().ToList();

            // Lista delle richieste di form+campo (se presenti)
            var reqFields = req.RequestList
                .Where(r => !string.IsNullOrEmpty(r.FormField))
                .Select(r => $"{(GetFormByView(r.idView, r.Form)).ToLower()}_{(r.FormField ?? "").ToLower()}")
                .Distinct()
                .ToHashSet(); // Ottimizzazione per lookup veloce

            // Selezione di tutti i campi validi dal DB
            var fields = await _context.BecaFormField
                .Where(f => (forms.Contains(f.Form) && (f.OrderSequence != 0 || f.FieldType == "upload")) || reqFields.Contains(f.Form.ToLower() + "_" + f.Name.ToLower()))
                .ToListAsync();

            // Troviamo i campi mancanti confrontando quelli richiesti con quelli effettivamente trovati
            var foundFields = fields.Select(f => $"{f.Form.ToLower()}_{f.Name.ToLower()}").ToHashSet();
            var missingFields = reqFields.Except(foundFields).ToList(); // Differenza tra richiesti e trovati

            if (missingFields.Count != 0)
            {
                return ([], [], $"I seguenti campi non sono presenti nel database: {string.Join(", ", missingFields)}");
            }

            List<BecaFormFieldLevel> formFieldCust = await _context.BecaFormFieldLevel
                .Where(f => f.idProfile == _currentUser.idProfileDef(_activeCompany.idCompany) &&
                    (forms.Contains(f.Form) || reqFields.Contains(f.Form.ToLower() + "_" + f.Name.ToLower())))
                .ToListAsync();

            return (fields, formFieldCust, "");
        }

        public async Task<(BecaForm? forms, List<BecaFormField> fields, string message)> GetBecaFormObjects4Save(DataFormPostParameter req)
        {
            if (_currentUser == null || _activeCompany == null) return (null, [], "Token non valido o header mancanti");

            BecaForm? form = await _context.BecaForm
                .FirstOrDefaultAsync(f => f.Form == GetFormByView(req.idView, req.Form));
            List<BecaFormField> fields = await _context.BecaFormField
                .Where(f => f.Form == GetFormByView(req.idView, req.Form))
                .ToListAsync();

            if (form == null) return (null, [], "View non associata o form non definita");
            return (form, fields, "");
        }


        public Dictionary<string, SqlConnection> GetConnectionsByRequest(List<BecaForm> forms, List<BecaFormField> fields, List<BecaFormFieldLevel>? customFields)
        {
            var c1 = forms.Select(f => f.ViewNameDB ?? f.TableNameDB ?? "").Distinct();
            var c2 = fields.Where(f => f.FieldType == "dropdown" && !f.DropDownList.isNullOrempty()).Select(f => f.DropDownListDB ?? "").Distinct();
            var c3 = customFields == null ? [] : customFields.Where(f => f.FieldType == "dropdown" && !f.DropDownList.isNullOrempty()).Select(f => f.DropDownListDB ?? "").Distinct();

            var connectionStrings = (c1.Concat(c2).Concat(c3).Distinct()).ToList();

            return _activeCompany!.Connections
                .Where(c => connectionStrings.Contains(c.ConnectionName))
                .ToDictionary(c => c.ConnectionName, c => new SqlConnection(c.ConnectionString));
            //.ToDictionary(c => c.ConnectionName, c => _factory.Create(c.ConnectionString).Connection);
        }

        public async Task<List<object>> GetDataByFormFieldAsync(Dictionary<string, SqlConnection> connections,
            List<BecaForm> forms, List<BecaFormField> fields, List<BecaFormFieldLevel> customFields,
            string Form, string field, List<BecaParameter> parameters, bool lowerCase)
        {
            BecaFormField formField = fields.FirstOrDefault(f =>
                string.Equals(f.Form, Form, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(f.Name, field, StringComparison.OrdinalIgnoreCase))!;
            BecaFormFieldLevel? formFieldCust = customFields.FirstOrDefault(f =>
                string.Equals(f.Form, Form, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(f.Name, field, StringComparison.OrdinalIgnoreCase));

            if (formField.DropDownListDB.isNullOrempty()) return [];

            string ddl = formFieldCust == null ? formField.DropDownList ?? "" : formFieldCust.DropDownList ?? "";
            string ddlPar = formFieldCust == null ? formField.Parameters ?? "" : formFieldCust.Parameters ?? "";

            object? colCheck = null;

            if (formField != null)
            {
                SqlConnection cnn = connections.First(c => c.Key == formField.DropDownListDB!).Value;

                //List<BecaParameter> parameters = new List<BecaParameter>();
                string sql = ddl;
                string sqlChk = $"Select * From {ddl[(ddl.IndexOf("FROM", StringComparison.CurrentCultureIgnoreCase) + 5)..]}";
                if (sqlChk.Contains("WHERE", StringComparison.CurrentCultureIgnoreCase)) sqlChk = sqlChk[..(sqlChk.IndexOf("WHERE", StringComparison.CurrentCultureIgnoreCase) - 1)];
                if (sqlChk.Contains("ORDER", StringComparison.CurrentCultureIgnoreCase)) sqlChk = sqlChk[..(sqlChk.IndexOf("ORDER", StringComparison.CurrentCultureIgnoreCase) - 1)];
                if (sqlChk.Contains("GROUP", StringComparison.CurrentCultureIgnoreCase)) sqlChk = sqlChk[..(sqlChk.IndexOf("GROUP", StringComparison.CurrentCultureIgnoreCase) - 1)];
                //sqlChk += " WITH (NOLOCK)";

                List<object?> pars = [];
                int numP = 0;
                if (sqlChk.Contains('('))
                {
                    string inside = sqlChk.inside("(", ")");
                    if (inside != "")
                    {
                        string[] funcPars = inside.Replace(" ", "").Split(",");
                        string funcPar = "";
                        string funcParChk = "";
                        foreach (string par in funcPars)
                        {
                            funcPar += "{" + numP + "}";
                            funcParChk += "Null,";
                            numP++;
                            BecaParameter? bPar = parameters.FirstOrDefault(p => p.name.Replace("+", "").Equals(par, StringComparison.CurrentCultureIgnoreCase));
                            if (bPar != null)
                            {
                                if (bPar.value1 != null && bPar.value1.ToString().IsValidDateTimeJson()) bPar.value1 = bPar.value1.ToDateTimeFromJson();
                                if (bPar.value2 != null && bPar.value2.ToString().IsValidDateTimeJson()) bPar.value2 = bPar.value2.ToDateTimeFromJson();

                                pars.Add(bPar.used == true ? bPar.value2 : bPar.value1);
                                bPar.used = true;
                            }
                            else
                            {
                                if (par == "idUtente")
                                    pars.Add(_currentUser!.idUtenteLoc(_activeCompany?.idCompany));
                                else if (par == "idCompany")
                                    pars.Add(_activeCompany?.idCompany);
                                else
                                    pars.Add(null);
                            }
                        }
                        funcPar = funcPar.Replace("}{", "},{");
                        sqlChk = sqlChk.Replace(sqlChk.inside("(", ")"), funcParChk).Replace(",)", ")");
                        sql = sql.Replace(sql.inside("(", ")"), funcPar);

                        if (field == "_Grafico")
                        {
                            colCheck = this.GetDataByFormField(Form, field + "Check", parameters, lowerCase);
                        }
                    }
                }

                colCheck ??= await GetQueryDefAsync<object>(cnn, formField.SchemaHashString + "_chk", sqlChk + " Where 0 = 1", [], parameters: [.. pars]);

                if (((ddlPar != null && ddlPar.Contains("idUtente")) || ddl.Contains("idUtente"))
                    && colCheck.GetType().GetProperty("idUtente") != null)
                {
                    parameters ??= [];
                    if (!parameters.Any(p => p.name.Equals("idutente", StringComparison.CurrentCultureIgnoreCase)))
                    {
                        parameters.Add(new BecaParameter()
                        {
                            name = "idUtente",
                            value1 = _currentUser!.idUtenteLoc(_activeCompany?.idCompany),
                            comparison = "="
                        });
                    }
                }

                if (((ddlPar != null && ddlPar.Contains("idCompany")) || ddl.Contains("idCompany"))
                    && colCheck.GetType().GetProperty("idCompany") != null)
                {
                    parameters ??= [];
                    if (!parameters.Any(p => p.name.Equals("idcompany", StringComparison.CurrentCultureIgnoreCase)))
                    {
                        parameters.Add(new BecaParameter()
                        {
                            name = "idCompany",
                            value1 = _activeCompany?.idCompany,
                            comparison = "="
                        });
                    }
                }

                string sqlOrd = "";
                if (sql.Contains("ORDER", StringComparison.CurrentCultureIgnoreCase))
                {
                    sqlOrd = sql[sql.IndexOf("ORDER", StringComparison.CurrentCultureIgnoreCase)..];
                    sql = sql[..(sql.IndexOf("ORDER", StringComparison.CurrentCultureIgnoreCase) - 1)];
                }
                string sqlGroup = "";
                if (sql.Contains("GROUP", StringComparison.CurrentCultureIgnoreCase))
                {
                    sqlGroup = sql[sql.IndexOf("GROUP", StringComparison.CurrentCultureIgnoreCase)..];
                    sql = sql[..(sql.IndexOf("GROUP", StringComparison.CurrentCultureIgnoreCase) - 1)];
                }
                string sqlWhere = "";
                if (sql.Contains("WHERE", StringComparison.CurrentCultureIgnoreCase))
                {
                    sqlWhere = sql[sql.IndexOf("WHERE", StringComparison.CurrentCultureIgnoreCase)..];
                    sql = sql[..(sql.IndexOf("WHERE", StringComparison.CurrentCultureIgnoreCase) - 1)];
                }
                sql = sql + " " + sqlWhere;
                if (parameters != null && parameters.Count > 0)
                {
                    foreach (BecaParameter par in parameters.Where(p => p.used == false && colCheck != null && colCheck.HasPropertyValue(p.name)))
                    {
                        if (par.value1 != null && par.value1.ToString().IsValidDateTimeJson()) par.value1 = par.value1.ToDateTimeFromJson();
                        if (par.value2 != null && par.value2.ToString().IsValidDateTimeJson()) par.value2 = par.value2.ToDateTimeFromJson();
                        sql += (numP - parameters.Count(p => p.used == true) - pars.Count(p => p == null)) == 0 && sql.IndexOf("WHERE", StringComparison.CurrentCultureIgnoreCase) < 0 ? " Where " : " And ";
                        //sql += par.name + " " + par.comparison;
                        switch (par.comparison.ToLower())
                        {
                            case "betweenfields":
                                sql += $"{{{numP}}} between {par.name.Replace(",", " And ")}";
                                pars.Add(par.value1);
                                numP++;
                                break;

                            case "between":
                                sql += par.name + " " + par.comparison;
                                sql += " {" + numP + "} and {" + (numP + 1).ToString() + "}";
                                pars.Add(par.value1);
                                pars.Add(par.value2);
                                numP++;
                                numP++;
                                break;

                            case "like":
                                sql += par.name + " " + par.comparison;
                                sql += " '%' + {" + numP + "} + '%'";
                                pars.Add(par.value1);
                                numP++;
                                break;

                            case "in":
                                sql += par.name + " " + par.comparison;
                                string[] vals = ((string?)par.value1 ?? "").ToString().Replace("(", "").Replace(")", "").Replace(" ", "").Split(",");
                                foreach (string val in vals)
                                {
                                    pars.Add(val);
                                    numP++;
                                }
                                break;

                            default:
                                if (par.value1 == null)
                                {
                                    sql += par.name + " Is Null ";
                                }
                                else
                                {
                                    sql += par.name + " " + par.comparison;
                                    sql += " {" + numP + "}";
                                    pars.Add(par.value1);
                                    numP++;
                                }
                                break;
                        }
                        //numP++;
                    }
                }
                sql = sql + " " + sqlGroup + " " + sqlOrd;

                return await ExecuteQueryAsync<object>(cnn, Form + '_' + field, sql, false,
                    lowerCase ? PropertyNaming.LowerCase : PropertyNaming.LowerCamelCase, [.. pars]);
            }
            else
            {
                return [];
            }
        }

        public async Task<List<T>> GetDataByFormAsync<T>(Dictionary<string, SqlConnection> connections,
            BecaForm form, List<BecaFormField> fields, object record,
            bool view = true, bool getChildren = true, int? pageNumber = null, int? pageSize = null, bool lowerCase = false) where T : class, new()
        {
            List<object> pars = [];
            if (form != null)
            {
                BecaParameters aPar = new();
                if (!form.PrimaryKey.isNullOrempty())
                {
                    foreach (string orderField in form.PrimaryKey!.Split(","))
                    {
                        aPar.Add(orderField.Trim(), record.GetPropertyValue(orderField.Trim()));
                    }
                }
                List<T> data = await this.GetDataByFormAsync<T>(connections, [form], [], fields, form.Form, aPar.parameters, view, getChildren, pageNumber, pageSize, lowerCase);
                return data;
            }
            else
            {
                return [];
            }
        }
        public async Task<List<T>> GetDataByFormAsync<T>(Dictionary<string, SqlConnection> connections,
            List<BecaForm> forms, List<BecaFormLevels> children, List<BecaFormField> fields,
            string Form, List<BecaParameter> parameters, bool view = true, bool getChildren = true,
            int? pageNumber = null, int? pageSize = null, bool lowerCase = false) where T : class, new()
        {
            BecaForm form = forms.FirstOrDefault(f =>
                string.Equals(f.Form, Form, StringComparison.OrdinalIgnoreCase))!;
            List<object?> pars = [];
            if (form != null)
            {
                SqlConnection cnn = connections.First(c => c.Key == (form.ViewNameDB ?? form.TableNameDB)).Value;

                if (!form.SelectProcedureName.isNullOrempty()) return await this.GetDataBySPAsync<T>(cnn, form.SelectProcedureName!, parameters, 
                        lowerCase ? PropertyNaming.LowerCase : PropertyNaming.LowerCamelCase);
                string upl = string.Join(", ", fields
                    .Where(f => f.Form == Form && f.FieldType == "upload")
                    .Select(n => "Null As [" + n.Name.Replace("upl", "").Trim() + "upl], Null As [" + n.Name.Replace("upl", "").Trim() + "uplName]"));
                string sql = GetFormSQLAsync(fields, form, view, false, false);
                string sqlChk = sql;
                string db = form.ViewName.isNullOrempty() ? form.TableNameDB : form.ViewNameDB ?? "";

                object? colCheck = null;

                int numP = 0;
                if (sql.Contains('('))
                {
                    string inside = sql.inside("(", ")");
                    if (inside != "")
                    {
                        string[] funcPars = inside.Replace(" ", "").Split(",");
                        string funcPar = "";
                        string funcParChk = "";
                        foreach (string par in funcPars)
                        {
                            funcPar += "{" + numP + "}";
                            funcParChk += "Null,";
                            numP++;
                            BecaParameter? bPar = parameters.FirstOrDefault(p => p.name.Replace("+", "").Equals(par, StringComparison.CurrentCultureIgnoreCase));
                            if (bPar != null)
                            {
                                if (bPar.value1 != null && bPar.value1.ToString().IsValidDateTimeJson()) bPar.value1 = bPar.value1.ToDateTimeFromJson();
                                if (bPar.value2 != null && bPar.value2.ToString().IsValidDateTimeJson()) bPar.value2 = bPar.value2.ToDateTimeFromJson();

                                pars.Add(bPar.used == true ? bPar.value2 : bPar.value1);
                                bPar.used = true;
                            }
                            else
                            {
                                if (par == "idUtente")
                                {
                                    pars.Add(_currentUser!.idUtenteLoc(_activeCompany?.idCompany));
                                    parameters.Add(new BecaParameter()
                                    {
                                        name = "idUtente",
                                        value1 = _currentUser.idUtenteLoc(_activeCompany?.idCompany),
                                        comparison = "=",
                                        used = true
                                    });
                                    //parameters.Find(p => p.name == "idUtente").used = true;
                                }
                                else if (par == "idCompany")
                                {
                                    pars.Add(_activeCompany?.idCompany);
                                    parameters.Add(new BecaParameter()
                                    {
                                        name = "idCompany",
                                        value1 = _activeCompany?.idCompany,
                                        comparison = "=",
                                        used = true
                                    });
                                    //parameters.Find(p => p.name == "idCompany").used = true;
                                }
                                else
                                    pars.Add(null);
                            }
                        }
                        funcPar = funcPar.Replace("}{", "},{");
                        sqlChk = sqlChk.Replace(sqlChk.inside("(", ")"), funcParChk).Replace(",)", ")");
                        sql = sql.Replace(sql.inside("(", ")"), funcParChk).Replace(",)", ")");
                        sql = sql.Replace(sql.inside("(", ")"), funcPar);
                    }
                }

                colCheck ??= await GetQueryDefAsync<object>(cnn, Form + "_chk", sqlChk + " Where 0 = 1", [], parameters: [.. pars]);

                if (colCheck.GetType().GetProperty("idUtente") != null && form.UseDefaultParam)
                {
                    parameters ??= [];
                    if (!parameters.Any(p => p.name.Equals("idutente", StringComparison.CurrentCultureIgnoreCase)))
                    {
                        parameters.Add(new BecaParameter()
                        {
                            name = "idUtente",
                            value1 = _currentUser!.idUtenteLoc(_activeCompany?.idCompany),
                            comparison = "="
                        });
                    }
                }

                if (colCheck.GetType().GetProperty("idCompany") != null && form.UseDefaultParam)
                {
                    parameters ??= [];
                    if (!parameters.Any(p => p.name.Equals("idCompany", StringComparison.CurrentCultureIgnoreCase)))
                    {
                        parameters.Add(new BecaParameter()
                        {
                            name = "idCompany",
                            value1 = _activeCompany?.idCompany,
                            comparison = "="
                        });
                    }
                }

                if (parameters != null && parameters.Count > 0)
                {
                    foreach (BecaParameter par in parameters.Where(p => p.used == false && colCheck != null && colCheck.HasPropertyValue(p.name)))
                    {
                        if (par.value1 != null && par.value1.ToString().IsValidDateTimeJson()) par.value1 = par.value1.ToDateTimeFromJson();
                        if (par.value2 != null && par.value2.ToString().IsValidDateTimeJson()) par.value2 = par.value2.ToDateTimeFromJson();
                        sql += (numP - parameters.Count(p => p.used == true) - pars.Count(p => p == null)) == 0 && sql.IndexOf("WHERE", StringComparison.CurrentCultureIgnoreCase) < 0 ? " Where " : " And ";
                        //sql += par.name + " " + par.comparison;
                        switch (par.comparison.ToLower())
                        {
                            case "betweenFields":
                                sql += $"{{numP}} between {par.name.Replace(",", " And ")}";
                                pars.Add(par.value1);
                                numP++;
                                break;
                            case "between":
                                sql += par.name + " " + par.comparison;
                                sql += " {" + numP + "} and {" + (numP + 1).ToString() + "}";
                                pars.Add(par.value1);
                                pars.Add(par.value2);
                                numP++;
                                numP++;
                                break;

                            case "like":
                                sql += par.name + " " + par.comparison;
                                sql += " '%' + {" + numP + "} + '%'";
                                pars.Add(par.value1);
                                numP++;
                                break;

                            case "is null":
                                sql += par.name + " " + par.comparison;
                                //  pars.Add(null);
                                break;

                            case "in":
                                if (par.value1 != null)
                                {
                                    List<string> inClause = [];
                                    string[] vals = (par.value1.ToString() ?? "").Replace("(", "").Replace(")", "").Trim().Split(",");
                                    foreach (string val in vals)
                                    {
                                        inClause.Add($"{{{numP}}}");
                                        pars.Add(val);
                                        numP++;
                                    }
                                    sql += $"{par.name} In ({string.Join(",", inClause)})";
                                }
                                break;

                            default:
                                if (par.value1 == null)
                                {
                                    sql += par.name + " Is Null ";
                                }
                                else
                                {
                                    sql += par.name + " " + par.comparison;
                                    sql += " {" + numP + "}";
                                    pars.Add(par.value1);
                                    numP++;
                                }
                                break;
                        }
                        //numP++;
                    }
                }

                List<BecaFormField> orderFields = [.. fields
                    .Where(f => f.Form == Form && f.OrderSequence != 0)
                    .OrderBy(f => Math.Abs(f.OrderSequence))];
                string sqlOrd = "";
                foreach (BecaFormField field in orderFields)
                {
                    sqlOrd += (sqlOrd.Length == 0 ? " Order By " : ", ") +
                        (field.OrderOnField == null ? field.Name.Trim() : field.OrderOnField.Trim()) +
                        (field.OrderSequence < 0 ? " DESC" : "");
                }
                sql += sqlOrd;

                List<BecaFormLevels> subForms = [.. children.Where(f => f.Form == Form)];

                if ((pageSize ?? 0) > 0) sql = sql.Replace("*", $"TOP({pageSize}) *");
                List<T> res = await ExecuteQueryAsync<T>(cnn, Form, sql, subForms.Count > 0,
                    lowerCase ? PropertyNaming.LowerCase : PropertyNaming.LowerCamelCase, [.. pars]);

                if (getChildren)
                {
                    foreach (BecaFormLevels level in subForms)
                    {
                        BecaForm? childForm = forms.FirstOrDefault(f => f.Form == level.ChildForm);
                        if (childForm != null)
                        {
                            string parent = form.getMainSource(true);
                            string child = (childForm.ViewName.isNullOrempty() ? childForm.TableName : childForm.ViewName ?? "");

                            db = childForm.ViewName.isNullOrempty() ? childForm.TableNameDB : childForm.ViewNameDB ?? "";

                            string sqlParent = sqlOrd == "" ? sql : sql.Replace(sqlOrd, "");
                            sql = "Select " +
                                string.Join(",", level.RelationColumn.Split(",").Select(n => parent + "." + n.Trim())) +
                                " From " + parent;
                            object objRelation = await GetQueryDefAsync<object>(cnn, "", sql + " Where 0 = 1", [], parameters: [.. pars]);

                            List<string> orderChild = [.. fields
                                .Where(f => f.Form == childForm.Form && f.OrderSequence != 0)
                                .OrderBy(f => Math.Abs(f.OrderSequence))
                                .Select(f => f.Name)];
                            string sqlOrdChild = orderChild.Count > 0 ? $" Order By {string.Join(",", orderChild)}" : "";

                            sql = "Select * From (" +
                                "Select " + child + ".*" +
                                " From (" + sqlParent + ") Parent " +
                                " Inner Join " + child +
                                " On " + string.Join(" And ", level.RelationColumn.Split(",").Select(n => "Parent." + n.Trim() + " = " + child + "." + n.Trim())) +
                                ") T" + sqlOrdChild;
                            List<object> childData = await ExecuteQueryAsync<object>(cnn, $"{Form}_{child}", sql, false,
                                lowerCase ? PropertyNaming.LowerCase : PropertyNaming.LowerCamelCase, [.. pars]);

                            var groupJoin2 = res.GroupJoin(childData,  //inner sequence
                                       p => GetRelationObjectString(level.RelationColumn, p), //outerKeySelector 
                                       c => GetRelationObjectString(level.RelationColumn, c),     //innerKeySelector
                                       (oParent, oChildren) =>  // resultSelector 
                                       {
                                           List<object> children2 = oChildren.ToList();
                                           List<object> curChildren = oParent.GetPropertyValueArray("children");
                                           curChildren ??= [];
                                           if (curChildren.Count < level.SubLevel) curChildren.Add(new List<object>());

                                           curChildren[level.SubLevel - 1] = children2;

                                           oParent.SetPropertyValuearray("children", curChildren);
                                           return oParent;
                                       });
                            res = groupJoin2.ToList();
                        }
                    }
                }

                return res;
            }
            else
            {
                return [];
            }
        }

        public async Task<List<T>> GetDataBySPAsync<T>(SqlConnection cnn, string spName, List<BecaParameter> parameters, PropertyNaming namingStrategy = PropertyNaming.AsIs) where T : class, new()
        {
            List<string> names = await GetProcedureParamsAsync(cnn, spName);

            if (names.Contains("@idUtente") && !parameters.Exists(p => p.name == "idUtente"))
            {
                parameters.Add(new BecaParameter()
                {
                    name = "idUtente",
                    value1 = _currentUser!.idUtenteLoc(_activeCompany?.idCompany),
                    comparison = "="
                });
            }

            string sql = $"Exec {spName} " +
                string.Join(", ", names.Where(x => parameters.Exists(p => p.name.Equals(x.Replace("@", ""), StringComparison.CurrentCultureIgnoreCase))).Select((x, i) => x +
                    @" = {" + i.ToString() + "}"));
            var pars = names.Where(x => parameters.Exists(p => p.name.Equals(x.Replace("@", ""), StringComparison.CurrentCultureIgnoreCase))).Select((x, i) =>
            {
                BecaParameter? par = parameters.Find(p => p.name.Equals(x.Replace("@", ""), StringComparison.CurrentCultureIgnoreCase));
                return par == null ?
                    null
                    :
                    (par.value1 != null && par.value1.ToString().IsValidDateTimeJson()) ?
                        par.value1.ToDateTimeFromJson()
                        :
                        par.value1;
            }).ToArray();
            return await ExecuteQueryAsync<T>(cnn, "", sql, false, namingStrategy, [.. pars]);
        }

        public async Task<List<object>> GetDataBySQLAsync(SqlConnection connection, string sql, List<BecaParameter> parameters, bool useidUtente = true, bool lowerCase = false)
        {
            if (_currentUser == null || _activeCompany == null) return [];

            string sqlFor = "";
            if (sql.Contains("FOR ", StringComparison.CurrentCultureIgnoreCase))
            {
                sqlFor = sql[sql.IndexOf("FOR ", StringComparison.CurrentCultureIgnoreCase)..];
                sql = sql[..(sql.IndexOf("FOR ", StringComparison.CurrentCultureIgnoreCase) - 1)];
            }
            string sqlOrd = "";
            if (sql.Contains("ORDER", StringComparison.CurrentCultureIgnoreCase))
            {
                sqlOrd = sql[sql.IndexOf("ORDER", StringComparison.CurrentCultureIgnoreCase)..];
                sql = sql[..(sql.IndexOf("ORDER", StringComparison.CurrentCultureIgnoreCase) - 1)];
            }
            string sqlGroup = "";
            if (sql.Contains("GROUP", StringComparison.CurrentCultureIgnoreCase))
            {
                sqlGroup = sql[sql.IndexOf("GROUP", StringComparison.CurrentCultureIgnoreCase)..];
                sql = sql[..(sql.IndexOf("GROUP", StringComparison.CurrentCultureIgnoreCase) - 1)];
            }
            string sqlWhere = "";
            if (sql.Contains("WHERE", StringComparison.CurrentCultureIgnoreCase))
            {
                sqlWhere = sql[sql.IndexOf("WHERE", StringComparison.CurrentCultureIgnoreCase)..];
                sql = sql[..(sql.IndexOf("WHERE", StringComparison.CurrentCultureIgnoreCase) - 1)];
            }
            string sqlTable = sql[(sql.IndexOf("FROM", StringComparison.CurrentCultureIgnoreCase) + 5)..];
            string sqlChk = "Select * From " + sqlTable;

            object colCheck = await GetQueryDefAsync<object>(connection, sqlChk.Replace(" ", ""), sqlChk + " Where 0 = 1", []);
            if (colCheck.GetType().GetProperty("idUtente") != null && useidUtente)
            {
                parameters ??= [];
                if (parameters.Find(p => p.name == "idUtente") == null)
                {
                    parameters.Add(new BecaParameter()
                    {
                        name = "idUtente",
                        value1 = _currentUser.idUtenteLoc(_activeCompany?.idCompany),
                        comparison = "="
                    });
                }
            }
            sql = sql + " " + sqlWhere;
            List<object?> pars = [];
            if (parameters != null && parameters.Count > 0)
            {
                int numP = 0;
                foreach (BecaParameter par in parameters)
                {
                    if (colCheck.GetType().GetProperty(par.name) != null)
                    {
                        sql += sql.Contains("WHERE", StringComparison.CurrentCultureIgnoreCase) ? " And " : " Where ";
                        sql += par.name + " " + par.comparison;
                        switch (par.comparison.ToLower())
                        {
                            case "between":
                                sql += " {" + numP + "} and {" + (numP + 1).ToString() + "}";
                                pars.Add(par.value1);
                                pars.Add(par.value2);
                                break;

                            case "like":
                                sql += " '%' + {" + numP + "} + '%'";
                                pars.Add(par.value1);
                                break;

                            case "in":
                                List<string> inClause = [];
                                string[] vals = ((string?)par.value1.ToString() ?? "").Replace("(", "").Replace(")", "").Trim().Split(",");
                                foreach (string val in vals)
                                {
                                    inClause.Add($"{{{numP}}}");
                                    pars.Add(val);
                                    numP++;
                                }
                                sql += $"{par.name} In ({string.Join(",", inClause)})";
                                break;

                            default:
                                sql += " {" + numP + "}";
                                pars.Add(par.value1);
                                break;
                        }
                        numP++;
                    }
                }
            }
            sql = sql + " " + sqlGroup + " " + sqlOrd + " " + sqlFor;
            return await ExecuteQueryAsync<object>(connection, "", sql, false,
                    lowerCase ? PropertyNaming.LowerCase : PropertyNaming.LowerCamelCase, [.. pars]);
        }

        public async Task<T?> AddDataByFormAsync<T>(SqlConnection connection, BecaForm form, List<BecaFormField> fields, object record, bool GetRecordAfterInsert) where T : class, new()
        {
            List<BecaFormField> defFieds = fields.Where(f => f.DefaultValue != null).ToList();
            if (form != null)
            {
                string tryUpload = await UploadByFormAsync(connection, form, fields, record);
                if (tryUpload != "") throw new InvalidOperationException(tryUpload);

                List<object?> pars = [];
                if ((form.AddProcedureName ?? "") != "")
                {
                    var (data, resCount, endOperation) = await ExecFormProcedureAsync<T>(connection, form, fields, form.AddProcedureName!, record);
                    if (endOperation) return data;
                }

                object def = await GetQueryDefAsync<object>(connection, (form.SchemaHashTableString ?? form.Form) + "_Add", "Select * From " + form.TableName + " Where 0 = 1", []);
                MethodInfo? method = def.GetType().GetMethod("identityName");
                string idName = method == null ? "" : method.Invoke(def, null)!.ToString() ?? "";

                int numP = 0;
                string sql = "Insert Into " + form.TableName + " (";
                string sqlF = ""; string sqlV = "";
                PropertyInfo[] colsTbl = record.GetType().GetProperties();
                Dictionary<string, PropertyInfo> colsObj = def.GetType().GetProperties().ToDictionary(c => c.Name);
                if (colsTbl.Length > 0)
                {
                    //for (int i = 0; i < colsTbl.Count(); i++)
                    foreach (PropertyInfo pTbl in colsTbl.Where(c => c.Name != idName))
                    {
                        bool colExist = colsObj.TryGetValue(pTbl.Name, out PropertyInfo? pObj);
                        bool isUploadField = (pTbl.Name.EndsWith("upl") && def.HasPropertyValue(pTbl.Name.Replace("upl",""))) 
                            || (pTbl.Name.EndsWith("uplName") && def.HasPropertyValue(pTbl.Name.Replace("uplName", "")));
                        if (colExist && !isUploadField && pTbl.GetValue(record) == null && defFieds.Any(f => f.Name.Equals(pTbl.Name, StringComparison.CurrentCultureIgnoreCase)))
                        {
                            record.SetPropertyValue(pTbl.Name, defFieds.FirstOrDefault(f => f.Name.Equals(pTbl.Name, StringComparison.CurrentCultureIgnoreCase))!.DefaultValue);
                        }
                        if (colExist && !isUploadField && pTbl.GetValue(record) != null)
                        {
                            pars.Add(pTbl.GetValue(record));
                            sqlF += (numP > 0 ? "," : "") + pTbl.Name;
                            sqlV += (numP > 0 ? "," : "") + " {" + numP.ToString() + "}";
                            numP++;
                        }
                    }
                }
                else return null;

                sql += sqlF + ") Values (" + sqlV + ")";

                int ires = 0;
                if (idName != "")
                {
                    int key = await InsertSqlCommandWithIdentityAsync(connection, sql, [.. pars]);
                    if (key <= 0)
                    {
                        return null;
                    }
                    record.SetPropertyValue(idName, key);
                    ires = 1;
                }
                else
                {
                    ires = await ExecuteSqlCommandAsync(connection, sql, [.. pars]);
                }

                if (ires == 0) return null;

                if ((form.AddProcedureName ?? "") != "" && form.AddProcedureName!.Contains("#After#"))
                {
                    //await _context.SaveChangesAsync();
                    await UpdateDataByProcedureAsync<T>(connection, form.AddProcedureName, record);
                }

                //await _context.SaveChangesAsync();
                if (GetRecordAfterInsert)
                {
                    List<T> inserted = await this.GetDataByFormAsync<T>(new Dictionary<string, SqlConnection>
                    {
                        { form.TableNameDB, connection }
                    }, form, fields, record);
                    if (inserted.Count == 0) return null;
                    return inserted[0];
                } else
                {
                    return record as T;
                }
            }
            else
            {
                return null;
            }
        }

        public async Task<(T? data, string message)> UpdateDataByFormAsync<T>(SqlConnection connection, BecaForm form, List<BecaFormField> fields,
            object recordOld, object recordNew, bool GetRecordAfterInsert) where T : class, new()
        {
            if (form != null && form.ForceInsertOnUpdate)
            {
                if (form.ForceInsertOnUpdate)
                {
                    List<T> data = await this.GetDataByFormAsync<T>(new Dictionary<string, SqlConnection>
                        {
                            { form.TableNameDB, connection }
                        }, form, fields, recordNew);
                    if (data.Count == 0)
                    {
                        return (await this.AddDataByFormAsync<T>(connection, form, fields, recordNew, GetRecordAfterInsert), "");
                    }
                }
            }

            if (form != null)
            {
                string tryUpload = await UploadByFormAsync(connection, form, fields, recordNew);
                if (tryUpload != "") throw new InvalidOperationException(tryUpload);

                List<object?> pars = [];
                if (!form.UpdateProcedureName.isNullOrempty())
                {
                    var (data, resCount, endOperation) = await ExecFormProcedureAsync<T>(connection, form, fields, form.UpdateProcedureName!, recordNew);
                    if (endOperation)
                    {
                        if (data == null) return (null, "Nessun record inserito dalla procedura");
                        return (data, "");
                    }
                }

                object tblform = await GetFormObjectAsync<object>(connection, form, fields, false, true);
                MethodInfo? method = tblform.GetType().GetMethod("identityName");
                string idName = method == null ? "" : method.Invoke(tblform, null)!.ToString() ?? "";

                int numP = 0;
                string sql = "Update " + form.TableName + " Set ";
                PropertyInfo[] colsOld = recordOld.GetType().GetProperties().Where(p => p.Name != idName).ToArray();
                PropertyInfo[] colsNew = recordNew.GetType().GetProperties().Where(p => p.Name != idName).ToArray();
                if (colsNew.Length > 0)
                {
                    for (int i = 0; i < colsNew.Length; i++)
                    {
                        PropertyInfo p2 = colsNew.ElementAt(i);
                        if (tblform.HasPropertyValue(p2.Name) && colsOld.FirstOrDefault(p => p.Name.Equals(p2.Name, StringComparison.CurrentCultureIgnoreCase)) != null)
                        {
                            PropertyInfo? p1 = colsOld.FirstOrDefault(p => p.Name.Equals(p2.Name, StringComparison.CurrentCultureIgnoreCase));
                            if (p1 != null)
                            {
                                bool update = false;
                                if (
                                    (Equals(p2.GetValue(recordNew), null) && !Equals(p1.GetValue(recordOld), null)) ||
                                    (!Equals(p2.GetValue(recordNew), null) && Equals(p1.GetValue(recordOld), null))
                                   )
                                {
                                    update = true;
                                }
                                if (!update)
                                {
                                    if (Equals(p2.GetValue(recordNew), null) && Equals(p1.GetValue(recordOld), null))
                                    {
                                        update = false;
                                    }
                                    else
                                    {
                                        if (!p2.GetValue(recordNew)!.Equals(p1.GetValue(recordOld))) update = true;
                                    }
                                }
                                if (update)
                                {
                                    object? pVal = p2.GetValue(recordNew);
                                    pars.Add(object.Equals(p2.GetValue(recordNew), null) || pVal == null ? null : (pVal.ToString().IsValidDateTimeJson() ? pVal.ToDateTimeFromJson() : pVal));
                                    sql += (numP > 0 ? ", " : "") + p2.Name + " = {" + numP.ToString() + "}";
                                    numP++;
                                }
                            }
                        }
                    }
                }
                else return (null, "Nessun record modificato");
                if (numP == 0)
                {
                    List<object> data = await this.GetDataByFormAsync<object>(new Dictionary<string, SqlConnection>
                        {
                            { form.TableNameDB, connection }
                        }, form, fields, recordOld);
                    return ((T? data, string message))(data != null && data.Count != 0 ? (data[0], "Nessun dato modificato") : (null, "Nessun record modificato"));
                }

                string sqlW = "";
                int numPW = 0;
                int? resSave = 0;
                if (!form.PrimaryKey.isNullOrempty())
                {
                    foreach (string orderField in form.PrimaryKey!.Split(","))
                    {
                        sqlW += numPW == 0 ? " Where " : " And ";
                        sqlW += orderField + " = ";
                        sqlW += " {" + numP.ToString() + "}";
                        pars.Add(recordOld.GetPropertyValue(orderField.Trim()));
                        numP++;
                        numPW++;
                    }
                    sql += sqlW;
                    resSave = await ExecuteSqlCommandAsync(connection, sql, [.. pars]);
                }
                if (!form.UpdateProcedureName.isNullOrempty() && form.UpdateProcedureName!.Contains("#After#"))
                {
                    //await _context.SaveChangesAsync();
                    int? resSPAfter = await UpdateDataByProcedureAsync<T>(connection, form.UpdateProcedureName, recordNew);
                }
                if (!(resSave.GetValueOrDefault() > 0 & resSave.HasValue))
                    return ((T)recordOld, "Non è stato possibile aggiornare il record");

                if(GetRecordAfterInsert)
                {
                    List<T> inserted2 = await this.GetDataByFormAsync<T>(new Dictionary<string, SqlConnection>
                        {
                            { form.TableNameDB, connection }
                        }, form, fields, recordNew);
                    if (inserted2.Count == 0) return (null, "Problemi nel trovare il nuovo record");
                    return (inserted2[0], "");
                } else
                {
                    return ((T)recordNew, "");
                }
            }
            else
            {
                return (null, $"L'aggiornamento è fallito: becaForm non trovata");
            }
        }

        private async Task<(T? data, int? resCount, Boolean endOperation)> ExecFormProcedureAsync<T>(SqlConnection connection,
            BecaForm form, List<BecaFormField> fieds, string procedure, object record) where T : class, new()
        {
            int? resSPBefore = null;
            if (!procedure.isNullOrempty() && procedure.Split(";").Any(p => p.Contains("#Before#") || !p.Contains('#')))
            {
                foreach (string proc in procedure.Split(";").Where(p => p.Contains("#Before#") || !p.Contains('#')))
                {
                    resSPBefore = (resSPBefore ?? 0) + await UpdateDataByProcedureAsync<T>(connection, proc, record);
                }
                if (procedure.Split(";").Any(p => !p.Contains('#')))
                //if (!form.AddProcedureName.Contains("#Before#"))
                {
                    List<T> spRes = await this.GetDataByFormAsync<T>(new Dictionary<string, SqlConnection>
                    {
                        { form.TableNameDB, connection }
                    }, form, fieds, record);
                    return (spRes.Count == 0 ? null : spRes[0], spRes.Count, true);
                }
                var res = new T();
                return (res, resSPBefore, false);
            }
            return (null, null, false);
        }

        private async Task<int?> UpdateDataByProcedureAsync<T>(SqlConnection connection, string spName, object record) where T : class, new()
        {
            spName = spName.Replace("#Before##", "").Replace("#After#", "");
            List<string> names = await GetProcedureParamsAsync(connection, spName);
            string sql = $"Exec {spName} " + string.Join(", ", names.Select((x, i) => $"{{{i}}}"));
            var pars = names.Select((x, i) => record.HasPropertyValue(x.Replace("@", ""))
                ? record.GetPropertyValue(x.Replace("@", ""))
                : record.HasPropertyValue(x.ToLower().Replace("@", "")) ? record.GetPropertyValue(x.ToLower().Replace("@", "")) : null).ToArray();
            return await ExecuteSqlCommandAsync(connection, sql, [.. pars]);
        }

        private async Task<string> UploadByFormAsync(SqlConnection connection, BecaForm form, List<BecaFormField> upl, object record)
        {
            try
            {
                foreach (BecaFormField field in upl.Where(f => record.HasPropertyValue($"{f.Name.Replace("upl", "")}upl") && record.GetPropertyString($"{f.Name.Replace("upl", "")}upl").ToString() != "" && !f.Parameters.isNullOrempty()))
                {
                    string res = await SaveFileByFieldAsync(connection, form, field, record);
                    if (res.Contains("ERR: ")) return res.Replace("ERR: ", "");
                    if (field.Name.right(3) == "upl")
                        record.SetPropertyValue(field.Name.leftExcept(3), res); //field.Parameters.Split("|").Last()
                    else
                        record.SetPropertyValue(field.Name, res); //field.Parameters.Split("|").Last()
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UploadByForm: {ex.Message}");
            }
            return "";
        }

        private async Task<string> SaveFileByFieldAsync(SqlConnection connection, BecaForm form, BecaFormField field, object record)
        {
            try
            {
                //Cerco i parametri x salvare il file
                // il primo è la tabella/vista in cui cercare il tipo documento
                // il secondo il nome del campo codice documento
                // il terzo il nome del campo folder in cui salvare
                // il quarto il nome del campo name da usare eventualmente per rinominare il file
                // il quinto le estensioni permesse
                // il sesto la dimensione massima
                // il settimo il codice documento da usare o il nome del campo da cui prendere il nome
                if (field.Parameters.isNullOrempty()) return "";
                string[] pars = field.Parameters!.Split("|");

                if (pars.Length < 6) return "";

                //se il 6° parametro è fra apici allora mi viene fornito il valore da cercare
                //altrimento mi viene fornito il nome del campo in cui cercare il tipo documento che sto caricando
                string cod = pars[6].Contains('\'')
                    ? pars[6].Replace("'", "")
                    : record.HasPropertyValue(pars[6]) ? record.GetPropertyString(pars[6]) : "";

                string sql = "Select " + pars[1] + " as Cod, " + pars[2] + " As Fld, " + pars[3] + " As Name, " + pars[4] + " As ext, " + pars[5] + " As MB " +
                    "From " + pars[0] + " Where " + pars[1] + " = {0}";
                List<BecaParameter> parameters = [ new BecaParameter()
                    {
                        name = pars[1],
                        value1 = (object)cod,
                        comparison = "="
                    }
                ];
                List<object> data = await this.GetDataBySQLAsync(connection, sql, parameters, false, false);
                if (data.Count < 1)
                    return "ERR: C'è stato un problema nel reperimento del tipo documento (" + field.Name + "). Contattare il fornitore";

                object tipoDoc = data[0];
                string folderNameSub = GetSaveName(tipoDoc.GetPropertyString("Fld"), record).Replace("/", @"\");
                string folderName = folderNameSub.Contains('\\') || folderNameSub.Contains(@":\")
                    ? @"\\192.168.0.207\BecaWeb\Web\Upload\" + _activeCompany!.MainFolder + @"\" + folderNameSub
                    : @"E:\BecaWeb\Web\Upload\" + _activeCompany!.MainFolder + @"\" + folderNameSub;
                folderName = @"\\192.168.0.207\BecaWeb\Web\Upload\" + _activeCompany.MainFolder + @"\" + folderNameSub;
                // Percorso relativo basato sulla directory virtuale configurata

                // Costruisci il percorso completo
                string physicalPath = folderName;
                string fileName = GetSaveName(tipoDoc.GetPropertyString("Name"), record);

                _logger.LogDebug($"Salvo il file {fileName} in {physicalPath}");

                if (physicalPath == "")
                {
                    return "ERR: C'è stato un problema nella definizione della cartella di destinazione (" + field.Name + "). Contattare il fornitore";
                }
                if (fileName == "" && tipoDoc.GetPropertyValue("Name").ToString() != "")
                {
                    return "ERR: C'è stato un problema nella definizione del nome del file (" + field.Name + "). Contattare il fornitore";
                }

                if (!Directory.Exists(physicalPath)) Directory.CreateDirectory(physicalPath);

                string fileUploaded = record.GetPropertyString($"{field.Name.Replace("upl", "")}upl");
                //string fileUploaded = record.GetPropertyValue($"_{field.Name}_upl_").ToString();
                //string fileUploaded = record.GetPropertyValue($"_{field.Name}_upl_").ToString();
                string fileUploadedName = record.HasPropertyValue($"{field.Name.Replace("upl", "")}uplName")
                    ? record.GetPropertyString($"{field.Name.Replace("upl", "")}uplName")
                    : record.HasPropertyValue($"{field.Name.Replace("upl", "")}") ? record.GetPropertyString($"{field.Name}") : "";

                if (fileName == "") fileName = fileUploadedName;
                if (fileUploaded.Length > 0)
                {
                    string sFileExtension = fileUploadedName.Remove(0, fileUploadedName.LastIndexOf('.') + 1).ToLower();
                    if (tipoDoc.GetPropertyValue("MB").ToString() != "" && tipoDoc.GetPropertyString("MB") != "0" && GetBase64Dimension(fileUploaded) > int.Parse(tipoDoc.GetPropertyString("MB")))
                    {
                        return "ERR: Il file " + fileUploadedName + " eccede la dimensione permessa (" + Math.Round((decimal)(int.Parse(tipoDoc.GetPropertyString("MB")) / 1024 / 1024), 2).ToString() + "MB)";
                    }
                    if (!tipoDoc.GetPropertyString("ext").Contains(sFileExtension, StringComparison.CurrentCultureIgnoreCase) && tipoDoc.GetPropertyString("ext") != "")
                    {
                        return "ERR: Il tipo di file (" + sFileExtension + ") non è ammesso. Seleziona uno tra questi tipi: " + tipoDoc.GetPropertyString("ext");
                    }
                    else
                    {
                        fileName = fileName.Replace("." + sFileExtension, "") + "." + sFileExtension;

                        _logger.LogDebug($"Scrivo il file {fileName} in {physicalPath}");

                        string filePath = Path.Combine(physicalPath, fileName);
                        if (File.Exists(filePath)) System.IO.File.Delete(filePath);
                        await System.IO.File.WriteAllBytesAsync(filePath, Convert.FromBase64String(fileUploaded));
                        return fileName;
                    }
                }
                else { return ""; }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore nel salvataggio del file: {ex.Message}");
                return "ERR: " + ex.Message;
            }
        }

        private async Task<object> GetQueryDefAsync<T>(SqlConnection db, string formName, string query, List<string> fields, params object[] parameters) where T : class, new()
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

        private async Task<List<T>> ExecuteQueryAsync<T>(SqlConnection db, string formName, string query, bool hasChildren = false,
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

        private static async Task<int> ExecuteSqlCommandAsync(SqlConnection db, string query, params object[] parameters)
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
        private async Task<List<string>> GetProcedureParamsAsync(SqlConnection cnn, string name)
        {
            List<object> pars = [name];
            string commandText = $"SELECT PARAMETER_NAME, DATA_TYPE FROM information_schema.parameters WHERE specific_name = '{name}'";
            List<object> names = await ExecuteQueryAsync<object>(cnn, "", commandText, false, PropertyNaming.AsIs, [.. pars]);
            if (name == null || names.Count == 0) return [];
            return names.Select(n => n.GetPropertyString("PARAMETER_NAME")).ToList();
        }

        private async Task<int> InsertSqlCommandWithIdentityAsync(SqlConnection connection, string commandText, params object[] parameters)
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
