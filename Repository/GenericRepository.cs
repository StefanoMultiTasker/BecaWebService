using BecaWebService.ExtensionsLib;
using BecaWebService.Models.Communications;
using Contracts;
using Entities;
using Entities.Contexts;
using Entities.DataTransferObjects;
using Entities.Models;
using ExtensionsLib;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.IIS.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace Repository
{
    public class GenericRepository : IGenericRepository
    {
        private DbBecaContext _context;
        private BecaUser _currentUser;
        private Company _activeCompany;
        private readonly FormTool _formTool;
        private readonly ILogger<GenericRepository> _logger;

        private Dictionary<string, DbDatiContext> _databases = new Dictionary<string, DbDatiContext>();

        public GenericRepository(IDependencies deps, IHttpContextAccessor httpContextAccessor, ILogger<GenericRepository> logger)
        {
            _context = deps.context;
            _currentUser = (BecaUser)httpContextAccessor.HttpContext.Items["User"];
            _activeCompany = (Company)httpContextAccessor.HttpContext.Items["Company"];
            _formTool = deps.formTool;
            _logger = logger;
        }

        public BecaUser GetLoggedUser() => _currentUser;

        #region "Form"

        public string GetFormByView(int idView)
        {
            BecaViewForm form = _context.BecaViewForms
                .FirstOrDefault(f => f.idBecaView == idView && f.isMain == true);
            if (form != null) return form.Form;
            else return null;
        }

        public async Task<T> AddOrUpdateDataByForm<T>(string Form, object record) where T : class, new()
        {
            List<object> data = GetDataByForm<object>(Form, record);
            if (data.Count == 0)
                return await AddDataByForm<T>(Form, record);
            else
                return (await UpdateDataByForm<T>(Form, data[0], record)).data;
        }

        public async Task<T> AddDataByForm<T>(string Form, object record) where T : class, new()
        {
            BecaForm form = _context.BecaForm
                .FirstOrDefault(f => f.Form == Form);

            string tryUpload = await UploadByForm(form, record);
            if (tryUpload != "") throw new InvalidOperationException(tryUpload);

            List<object> pars = new List<object>();
            if (form != null)
            {
                if ((form.AddProcedureName ?? "") != "" && !form.AddProcedureName.Contains("#After"))
                {
                    int? resSPBefore = await UpdateDataByProcedure<T>(form.TableNameDB, form.AddProcedureName, record);
                    if (!form.AddProcedureName.Contains("#Before"))
                    {
                        List<T> spRes = this.GetDataByForm<T>(Form, record);
                        return (spRes == null || spRes.Count == 0) ? null : spRes[0];
                    }
                }
                object def = getContext(form.TableNameDB).GetQueryDef<object>(Form, "Select * From " + form.TableName + " Where 0 = 1");
                MethodInfo method = def.GetType().GetMethod("identityName");
                string idName = method == null ? "" : method.Invoke(def, null).ToString();

                int numP = 0;
                string sql = "Insert Into " + form.TableName + " (";
                string sqlF = ""; string sqlV = "";
                PropertyInfo[] colsTbl = record.GetType().GetProperties();
                Dictionary<string, PropertyInfo> colsObj = def.GetType().GetProperties().ToDictionary(c => c.Name);
                if (colsTbl.Count() > 0)
                {
                    //for (int i = 0; i < colsTbl.Count(); i++)
                    foreach (PropertyInfo pTbl in colsTbl.Where(c => c.Name != idName))
                    {
                        PropertyInfo pObj;
                        bool colExist = colsObj.TryGetValue(pTbl.Name, out pObj);
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
                    int key = await getContext(form.TableNameDB).InsertSqlCommandWithIdentity(sql, pars.ToArray());
                    if (key <= 0)
                    {
                        return null;
                    }
                    record.SetPropertyValue(idName, key);
                    ires = 1;
                }
                else
                {
                    ires = await getContext(form.TableNameDB).ExecuteSqlCommandAsync(sql, pars.ToArray());
                }

                if (ires == 0) return null;

                if ((form.AddProcedureName ?? "") != "" && form.AddProcedureName.Contains("#After"))
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

        public async Task<T> AddDataByFormChild<T>(string form, string formChild, object parent, List<object> childElements) where T : class, new()
        {
            BecaForm _form = _context.BecaForm
                .FirstOrDefault(f => f.Form == formChild);
            object def = getContext(_form.TableNameDB).GetQueryDef<object>(formChild, "Select * From " + _form.TableName + " Where 0 = 1");

            foreach (PropertyInfo p in def.GetType().GetProperties())
            {
                if (parent.HasPropertyValue(p.Name)) def.SetPropertyValue(p.Name, parent.GetPropertyValue(p.Name));
                foreach (JObject c in childElements)
                {
                    if (c.ContainsKey(p.Name.ToLower())) def.SetPropertyValue(p.Name, c[p.Name.ToLower()]);
                }
            }

            var res = await AddDataByForm<T>(formChild, def);
            if (res == null) return null;

            return GetDataByForm<T>(form, parent)[0];
        }

        public object CreateObjectFromJSON<T>(string jsonRecord) where T : class, new()
        {
            dynamic json = JsonConvert.DeserializeObject<dynamic>(jsonRecord);
            return (object)json;
        }

        public T CreateObjectFromJSON<T>(string Form, string jsonRecord) where T : class, new()
        {
            BecaForm form = _context.BecaForm
                .FirstOrDefault(f => f.Form == Form);
            List<object> pars = new List<object>();
            if (form != null)
            {
                object def = getContext(form.TableNameDB).GetQueryDef<object>(Form, "Select * From " + form.TableName + " Where 0 = 1");
                Type formType = def.GetType();
                dynamic json = JsonConvert.DeserializeObject<dynamic>(jsonRecord);

                PropertyInfo[] props = formType.GetProperties();
                foreach (PropertyInfo pi in props)
                {
                    bool isFieldPresent = false;
                    try
                    {
                        object v = (object)json[pi.Name.ToLowerToCamelCase()].Value;
                        isFieldPresent = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        isFieldPresent = false;
                    }
                    if (isFieldPresent)
                    {
                        def.SetPropertyValue(pi.Name, (object)json[pi.Name.ToLowerToCamelCase()].Value);
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
            BecaForm form = _context.BecaForm
                .FirstOrDefault(f => f.Form == Form);
            if (form != null)
            {
                if ((form.DeleteProcedureName ?? "") != "" && !form.DeleteProcedureName.Contains("#After"))
                {
                    int? resSPBefore = await UpdateDataByProcedure<T>(form.TableNameDB, form.DeleteProcedureName, record);
                    if (!form.DeleteProcedureName.Contains("#Before")) return (int)resSPBefore;
                }
                int numP = 0;
                string sql = "Delete " + form.TableName + " Where ";
                List<object> pars = new List<object>();
                foreach (string orderField in form.PrimaryKey.Split(","))
                {
                    sql += (numP > 0 ? " And " : "") + orderField + " = {" + numP.ToString() + "}";
                    pars.Add(record.GetPropertyValue(orderField.Trim()));
                    numP++;
                }
                int? ires = await getContext(form.TableNameDB).ExecuteSqlCommandAsync(sql, pars.ToArray());
                if ((form.DeleteProcedureName ?? "") != "" && form.UpdateProcedureName.Contains("#After"))
                {
                    await _context.SaveChangesAsync();
                    return (int)await UpdateDataByProcedure<T>(form.TableNameDB, form.DeleteProcedureName, record);
                }
                return (int)ires;
            }
            else
            {
                return 0;
            }
        }

        public List<T> GetDataByForm<T>(string Form, List<BecaParameter> parameters, bool view = true, bool getChildren = true) where T : class, new()
        {
            BecaForm form = _context.BecaForm
                .FirstOrDefault(f => f.Form == Form);
            List<object> pars = new List<object>();
            if (form != null)
            {
                if ((form.SelectProcedureName ?? "") != "") return this.GetDataBySP<T>(form.TableNameDB, form.SelectProcedureName, parameters);
                string upl = string.Join(", ", _context.BecaFormField
                    .Where(f => f.Form == Form && f.FieldType == "upload")
                    .ToList().Select(n => "Null As [_" + n.Name.Trim() + "_upl_], Null As [_" + n.Name.Trim() + "_uplName_]"));
                string sql = getFormSQL(form, view);
                string db = form.ViewName.isNullOrempty() ? form.TableNameDB : form.ViewNameDB;

                int numP = 0;
                if (sql.Contains("("))
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
                            funcParChk += "Null, ";
                            numP++;
                            BecaParameter bPar = parameters.FirstOrDefault(p => p.name.ToLower().Replace("+", "") == par.ToLower());
                            if (bPar != null)
                            {
                                if (bPar.value1 != null && bPar.value1.ToString().IsValidDateTimeJson()) bPar.value1 = bPar.value1.ToDateTimeFromJson();
                                if (bPar.value2 != null && bPar.value2.ToString().IsValidDateTimeJson()) bPar.value2 = bPar.value2.ToDateTimeFromJson();

                                pars.Add(bPar.used == true ? bPar.value2 : bPar.value1);
                                bPar.used = true;
                            }
                            else
                            {
                                pars.Add(par == "idUtente" ? _currentUser.idUtenteLoc(_activeCompany == null ? null : _activeCompany.idCompany) : null);
                            }
                        }
                        funcPar = funcPar.Replace("}{", "},{");
                        sql = sql.Replace(sql.inside("(", ")"), funcParChk).Replace(",)", ")");
                        sql = sql.Replace(sql.inside("(", ")"), funcPar);
                    }
                }

                if (!sql.Contains("("))
                {
                    object colCheck = getContext(db).GetQueryDef<object>(Form, sql + " Where 0 = 1", pars.ToArray());
                    if (colCheck.GetType().GetProperty("idUtente") != null && form.UseDefaultParam)
                    {
                        if (parameters == null) parameters = new List<BecaParameter>();
                        parameters.Add(new BecaParameter()
                        {
                            name = "idUtente",
                            value1 = _currentUser.idUtenteLoc(_activeCompany == null ? null : _activeCompany.idCompany),
                            comparison = "="
                        });
                    }
                    if (parameters != null && parameters.Count() > 0)
                    {
                        foreach (BecaParameter par in parameters.Where(p => p.used == false && colCheck != null && colCheck.HasPropertyValue(p.name)))
                        {
                            if (par.value1 != null && par.value1.ToString().IsValidDateTimeJson()) par.value1 = par.value1.ToDateTimeFromJson();
                            if (par.value2 != null && par.value2.ToString().IsValidDateTimeJson()) par.value2 = par.value2.ToDateTimeFromJson();
                            sql += (numP - parameters.Count(p => p.used == true) - pars.Count(p => p == null)) == 0 && sql.ToUpper().IndexOf("WHERE") < 0 ? " Where " : " And ";
                            //sql += par.name + " " + par.comparison;
                            switch (par.comparison)
                            {
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
                                    sql += par.name + " " + par.comparison;
                                    string[] vals = par.value1.ToString().Replace("(", "").Replace(")", "").Replace(" ", "").Split(",");
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
                }

                List<BecaFormField> fields = _context.BecaFormField
                    .Where(f => f.Form == Form && f.OrderSequence != 0)
                    .OrderBy(f => Math.Abs(f.OrderSequence))
                    .ToList();
                string sqlOrd = "";
                foreach (BecaFormField field in fields)
                {
                    sqlOrd += (sqlOrd.Length == 0 ? " Order By " : ", ") +
                        (field.OrderOnField == null ? field.Name : field.OrderOnField) +
                        (field.OrderSequence < 0 ? " DESC" : "");
                }
                sql += sqlOrd;

                List<BecaFormLevels> subForms = _context.BecaFormLevels
                    .Where(f => f.Form == Form)
                    .ToList();

                List<T> res = getContext(db).ExecuteQuery<T>(Form, sql, subForms.Count > 0, pars.ToArray());

                if (getChildren)
                {
                    foreach (BecaFormLevels level in subForms)
                    {
                        BecaForm childForm = _context.BecaForm
                            .FirstOrDefault(f => f.Form == level.ChildForm);

                        string parent = (form.ViewName == null || form.ViewName.ToString() == "" ? form.TableName : form.ViewName);
                        string child = (childForm.ViewName == null || childForm.ViewName.ToString() == "" ? childForm.TableName : childForm.ViewName);

                        string sqlParent = sqlOrd == "" ? sql : sql.Replace(sqlOrd, "");
                        sql = "Select " +
                            string.Join(",", level.RelationColumn.Split(",").Select(n => parent + "." + n.Trim())) +
                            " From " + parent;
                        object objRelation = getContext(db).GetQueryDef<object>("", sql + " Where 0 = 1");

                        sql = "Select * From (" +
                            "Select " + child + ".*" +
                            " From (" + sqlParent + ") Parent " +
                            " Inner Join " + child +
                            " On " + string.Join(" And ", level.RelationColumn.Split(",").Select(n => "Parent." + n.Trim() + " = " + child + "." + n.Trim())) +
                            ") T";
                        List<object> children = this.GetDataBySQL(db, sql, pars.ToArray());

                        var groupJoin2 = res.GroupJoin(children,  //inner sequence
                                   p => this.getRelationObjectString(level.RelationColumn, p), //outerKeySelector 
                                   c => this.getRelationObjectString(level.RelationColumn, c),     //innerKeySelector
                                   (oParent, oChildren) =>  // resultSelector 
                                   {
                                       List<object> children2 = oChildren.ToList();
                                       //List<object> children2 = new List<object>();
                                       //foreach (var oChild in oChildren)
                                       //{
                                       //    children2.Add(oChild);
                                       //}
                                       List<object> curChildren = oParent.GetPropertyValueArray("children");
                                       if (curChildren == null) curChildren = new List<object>();
                                       if (curChildren.Count < level.SubLevel) curChildren.Add(new List<object>());

                                       curChildren[level.SubLevel - 1] = children2;

                                       oParent.SetPropertyValuearray("children", curChildren);
                                       return oParent;
                                   });
                        //var groupJoin = res.GroupJoin(children,  //inner sequence
                        //            p => new {FFCL=p.GetPropertyValue("FFCL").ToString(),CODC= p.GetPropertyValue("CODC").ToString()}, //outerKeySelector 
                        //            c => new { FFCL = c.GetPropertyValue("FFCL").ToString(), CODC = c.GetPropertyValue("CODC").ToString() },     //innerKeySelector
                        //            (oParent, oChildren) => new // resultSelector 
                        //            {
                        //                data = oParent,
                        //                _children = oChildren
                        //            });
                        //string ttt = "";
                        //foreach (var item in groupJoin)
                        //{
                        //    string ss = "";
                        //}
                        //foreach (var item in groupJoin2) {
                        //    string rr = "";
                        //}
                        res = groupJoin2.ToList();
                        //List<T> res2 = (List<T>)groupJoin;
                    }
                }

                return res;
            }
            else
            {
                return null;
            }
        }

        public string getRelationObjectString(string relation, object record)
        {
            string rel = string.Join("", relation.Split(",").Select(n => record.GetPropertyValue(n).ToString()));
            return rel;
        }

        public object getRelationObject(object relation, object record)
        {
            Type tRel = relation.GetType();
            object rel = Activator.CreateInstance(tRel);
            foreach (var prop in rel.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                rel.SetPropertyValue(prop.Name, record.GetPropertyValue(prop.Name));
                //rel.GetType().GetProperty(prop.Name).SetValue(rel, prop.GetValue(record));
            }
            return rel;
        }

        public List<T> GetDataByForm<T>(string Form, object record, bool view = true, bool getChildren = true) where T : class, new()
        {
            BecaForm form = _context.BecaForm
                .FirstOrDefault(f => f.Form == Form);
            List<object> pars = new List<object>();
            if (form != null)
            {
                BecaParameters aPar = new BecaParameters();
                foreach (string orderField in form.PrimaryKey.Split(","))
                {
                    aPar.Add(orderField.Trim(), record.GetPropertyValue(orderField.Trim()));
                }
                List<T> data = this.GetDataByForm<T>(Form, aPar.parameters, view, getChildren);
                return data;
            }
            else
            {
                return null;
            }
        }

        public List<object> GetDataByFormLevel(string Form, int subLevel, List<BecaParameter> parameters)
        {
            BecaFormLevels childFiliali = _context.BecaFormLevels
                .Find(Form, subLevel);
            if (childFiliali == null)
            {
                return null;
            }
            return GetDataByForm<object>(childFiliali.ChildForm, parameters);
        }

        private string getFormSQL(BecaForm form, bool view, bool uplWithoutUnderscore = false, bool noUpload = false)
        {
            string upl = noUpload ? "" : string.Join(", ", _context.BecaFormField
                .Where(f => f.Form == form.Form && f.FieldType == "upload")
                .ToList().Select(n => "'' As [_" + n.Name.Trim() + "_upl_], '' As [_" + n.Name.Trim() + "_uplName_]"));
            if (uplWithoutUnderscore) upl = upl.Replace("_", "");
            string sql = "Select *" +
                (upl.Length > 0 ? ", " + upl + " " : " ") +
                "From " +
                (view ? ((form.ViewName == null || form.ViewName.ToString() == "" ? form.TableName : form.ViewName)) : form.TableName);

            return sql;
        }

        public List<T> GetDataBySP<T>(string dbName, string spName, List<BecaParameter> parameters) where T : class, new()
        {
            List<string> names = getContext(dbName).GetProcedureParams(spName);

            if (names.Contains("@idUtente") && !parameters.Exists(p => p.name == "idUtente"))
            {
                parameters.Add(new BecaParameter()
                {
                    name = "idUtente",
                    value1 = _currentUser.idUtenteLoc(_activeCompany == null ? null : _activeCompany.idCompany),
                    comparison = "="
                });
            }

            string sql = $"Exec {spName} " +
                string.Join(", ", names.Where(x => parameters.Exists(p => p.name.ToLower() == x.ToLower().Replace("@", ""))).Select((x, i) => x +
                    @" = {" + i.ToString() + "}"));
            var pars = names.Where(x => parameters.Exists(p => p.name.ToLower() == x.ToLower().Replace("@", ""))).Select((x, i) =>
            {
                BecaParameter par = parameters.Find(p => p.name.ToLower() == x.ToLower().Replace("@", ""));
                return par == null ?
                    null
                    :
                    (par.value1 != null && par.value1.ToString().IsValidDateTimeJson()) ?
                        par.value1.ToDateTimeFromJson()
                        :
                        par.value1;
            }).ToArray();
            return getContext(dbName).ExecuteQuery<T>(spName, sql, false, pars.ToArray());
        }

        public T getFormObject<T>(string Form, bool view, bool noUpload = false) => this.getFormObject<T>(Form, view, new List<string>(), noUpload);
        public T getFormObject<T>(string Form, bool view, List<string> fields, bool noUpload = false)
        {
            BecaForm form = _context.BecaForm
                .FirstOrDefault(f => f.Form == Form);
            List<object> pars = new List<object>();
            if (form != null)
            {
                string sql = getFormSQL(form, view, true, noUpload);
                object def = getContext(form.TableNameDB).GetQueryDef<object>(Form, sql + " Where 0 = 1", fields);
                return (T)def;
            }
            else
            {
                return default(T);
            }
        }

        public object GetPanelsByForm(string Form, List<BecaParameter> parameters)
        {
            List<object> data = this.GetDataByFormField(Form, "Panels", parameters);
            if (data == null || data.Count == 0) return null;
            return data[0];
        }

        public async Task<int?> UpdateDataByProcedure<T>(string dbName, string spName, object record) where T : class, new()
        {
            spName = spName.Replace("#Before", "").Replace("#After", "");
            List<string> names = getContext(dbName).GetProcedureParams(spName);
            string sql = $"Exec {spName} " + string.Join(", ", names.Select((x, i) => $"{{{i}}}"));
            var pars = names.Select((x, i) => record.HasPropertyValue(x.Replace("@", "")) ? record.GetPropertyValue(x.Replace("@", "")) : null).ToArray();
            return await getContext(dbName).ExecuteSqlCommandAsync(sql, pars);
        }

        public async Task<(T data, string message)> UpdateDataByForm<T>(string Form, object recordOld, object recordNew) where T : class, new()
        {
            BecaForm form = _context.BecaForm
                .FirstOrDefault(f => f.Form == Form);

            if (form != null && form.ForceInsertOnUpdate)
            {
                List<T> data = this.GetDataByForm<T>(Form, recordNew, false, false);
                if (data.Count == 0)
                {
                    return (await this.AddDataByForm<T>(Form, recordNew), "");
                }
            }

            string tryUpload = await UploadByForm(form, recordNew);
            if (tryUpload != "") throw new InvalidOperationException(tryUpload);

            List<object> pars = new List<object>();
            if (form != null)
            {
                if ((form.UpdateProcedureName ?? "") != "" && !form.UpdateProcedureName.Contains("#After"))
                {
                    int? resSPBefore = await UpdateDataByProcedure<T>(form.TableNameDB, form.UpdateProcedureName, recordNew);
                    if (!form.UpdateProcedureName.Contains("#Before"))
                    {
                        List<T> inserted1 = this.GetDataByForm<T>(Form, recordNew);
                        if (inserted1.Count == 0) return (null, "Nessun record inserito dalla procedura");
                        return (inserted1[0], "");
                    }
                }

                object tblform = getFormObject<object>(Form, false, true);
                MethodInfo method = tblform.GetType().GetMethod("identityName");
                string idName = method == null ? "" : method.Invoke(tblform, null).ToString();


                int numP = 0;
                string sql = "Update " + form.TableName + " Set ";
                PropertyInfo[] colsOld = recordOld.GetType().GetProperties().Where(p => p.Name != idName).ToArray();
                PropertyInfo[] colsNew = recordNew.GetType().GetProperties().Where(p => p.Name != idName).ToArray();
                if (colsNew.Count() > 0)
                {
                    for (int i = 0; i < colsNew.Count(); i++)
                    {
                        PropertyInfo p2 = colsNew.ElementAt(i);
                        if (colsOld.FirstOrDefault(p => p.Name.ToLower() == p2.Name.ToLower()) != null && tblform.HasPropertyValue(p2.Name))
                        {
                            PropertyInfo p1 = colsOld.ElementAt(i);
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
                                    if (!p2.GetValue(recordNew).Equals(p1.GetValue(recordOld))) update = true;
                                }
                            }
                            if (update)
                            {
                                object pVal = p2.GetValue(recordNew);
                                pars.Add(object.Equals(p2.GetValue(recordNew), null) ? null : (pVal.ToString().IsValidDateTimeJson() ? pVal.ToDateTimeFromJson() : pVal));
                                sql += (numP > 0 ? ", " : "") + p2.Name + " = {" + numP.ToString() + "}";
                                numP++;
                            }
                        }
                    }
                }
                else return (null, "Nessun record modificato");
                if (numP == 0)
                {
                    List<object> data = this.GetDataByForm<object>(Form, recordOld);
                    return ((T data, string message))(data != null && data.Count != 0 ? (data[0], "Nessun dato modificato") : (null, "Nessun record modificato"));
                }

                string sqlW = "";
                int numPW = 0;
                foreach (string orderField in form.PrimaryKey.Split(","))
                {
                    sqlW += numPW == 0 ? " Where " : " And ";
                    sqlW += orderField + " = ";
                    sqlW += " {" + numP.ToString() + "}";
                    pars.Add(recordOld.GetPropertyValue(orderField.Trim()));
                    numP++;
                    numPW++;
                }
                sql += sqlW;
                int? resSave = await getContext(form.TableNameDB).ExecuteSqlCommandAsync(sql, pars.ToArray());
                if ((form.UpdateProcedureName ?? "") != "" && form.UpdateProcedureName.Contains("#After"))
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
            BecaViewAction action = _context.BecaViewActions
                .FirstOrDefault(f => f.idBecaView == idview && f.ActionName == actionName);

            string error = "";
            if (action != null)
            {
                try
                {
                    int res = 0;
                    string[] procs = action.Command.Split(";");
                    foreach (string proc in procs)
                    {
                        res += await ExecuteProcedure(action.ConnectionName, proc,
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
            BecaViewAction action = _context.BecaViewActions
                .FirstOrDefault(f => f.idBecaView == idview && f.ActionName == actionName);

            string error = "";
            if (action != null)
            {
                try
                {
                    int res = 0;
                    string[] procs = action.Command.Split(";");
                    foreach (string proc in procs)
                    {
                        res += await ExecuteProcedure(action.ConnectionName, proc, parameters);
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

        public List<object> GetDataByFormField(string Form, string field, List<BecaParameter> parameters)
        {
            BecaFormField formField = _context.BecaFormField
                .Find(Form, field);
            BecaFormFieldLevel formFieldCust = _context.BecaFormFieldLevel
                .Find(_currentUser.idProfileDef(_activeCompany.idCompany), Form, field);
            //string ddl = formField.DropDownList;
            //string ddlPar = formField.Parameters;
            string ddl = formFieldCust == null ? formField.DropDownList : formFieldCust.DropDownList;
            string ddlPar = formFieldCust == null ? formField.Parameters : formFieldCust.Parameters;

            object colCheck = null;

            if (formField != null)
            {
                //List<BecaParameter> parameters = new List<BecaParameter>();
                string sql = ddl;
                string sqlChk = "Select * From " + ddl.Substring(ddl.ToUpper().IndexOf("FROM") + 5);
                if (sqlChk.ToUpper().Contains("WHERE")) sqlChk = sqlChk.Substring(0, sqlChk.ToUpper().IndexOf("WHERE") - 1);
                if (sqlChk.ToUpper().Contains("ORDER")) sqlChk = sqlChk.Substring(0, sqlChk.ToUpper().IndexOf("ORDER") - 1);
                if (sqlChk.ToUpper().Contains("GROUP")) sqlChk = sqlChk.Substring(0, sqlChk.ToUpper().IndexOf("GROUP") - 1);

                List<object> pars = new List<object>();
                int numP = 0;
                if (sqlChk.Contains("("))
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
                            funcParChk += "Null, ";
                            numP++;
                            BecaParameter bPar = parameters.FirstOrDefault(p => p.name.ToLower().Replace("+", "") == par.ToLower());
                            if (bPar != null)
                            {
                                if (bPar.value1 != null && bPar.value1.ToString().IsValidDateTimeJson()) bPar.value1 = bPar.value1.ToDateTimeFromJson();
                                if (bPar.value2 != null && bPar.value2.ToString().IsValidDateTimeJson()) bPar.value2 = bPar.value2.ToDateTimeFromJson();

                                pars.Add(bPar.used == true ? bPar.value2 : bPar.value1);
                                bPar.used = true;
                            }
                            else
                            {
                                pars.Add(par == "idUtente" ? _currentUser.idUtenteLoc(_activeCompany == null ? null : _activeCompany.idCompany) : null);
                            }
                        }
                        //foreach (BecaParameter par in parameters.Where(p => funcPars.Contains(p.name)))
                        //{
                        //    funcPar += "{" + numP + "}";
                        //    pars.Add(par.value1);
                        //    numP++;
                        //    par.used = true;
                        //}
                        funcPar = funcPar.Replace("}{", "},{");
                        sqlChk = sqlChk.Replace(sqlChk.inside("(", ")"), funcParChk).Replace(",)", ")");
                        sql = sql.Replace(sql.inside("(", ")"), funcPar);

                        if (field == "_Grafico")
                        {
                            colCheck = this.GetDataByFormField(Form, field + "Check", parameters);
                        }
                    }
                }
                else
                {
                    colCheck = getContext(formField.DropDownListDB).GetQueryDef<object>(Form + '_' + field + "_chk", sqlChk + " Where 0 = 1", pars.ToArray());
                    if (ddlPar != null && ddlPar.Contains("idUtente"))
                    {
                        if (colCheck.GetType().GetProperty("idUtente") != null)
                        {
                            parameters.Add(new BecaParameter()
                            {
                                name = "idUtente",
                                value1 = _currentUser.idUtenteLoc(_activeCompany == null ? null : _activeCompany.idCompany),
                                comparison = "="
                            });
                        }
                    }
                }

                string sqlOrd = "";
                if (sql.ToUpper().Contains("ORDER"))
                {
                    sqlOrd = sql.Substring(sql.ToUpper().IndexOf("ORDER"));
                    sql = sql.Substring(0, sql.ToUpper().IndexOf("ORDER") - 1);
                }
                string sqlGroup = "";
                if (sql.ToUpper().Contains("GROUP"))
                {
                    sqlGroup = sql.Substring(sql.ToUpper().IndexOf("GROUP"));
                    sql = sql.Substring(0, sql.ToUpper().IndexOf("GROUP") - 1);
                }
                string sqlWhere = "";
                if (sql.ToUpper().Contains("WHERE"))
                {
                    sqlWhere = sql.Substring(sql.ToUpper().IndexOf("WHERE"));
                    sql = sql.Substring(0, sql.ToUpper().IndexOf("WHERE") - 1);
                }
                sql = sql + " " + sqlWhere;
                if (parameters != null && parameters.Count() > 0)
                {
                    foreach (BecaParameter par in parameters.Where(p => p.used == false && colCheck != null && colCheck.HasPropertyValue(p.name)))
                    {
                        if (par.value1 != null && par.value1.ToString().IsValidDateTimeJson()) par.value1 = par.value1.ToDateTimeFromJson();
                        if (par.value2 != null && par.value2.ToString().IsValidDateTimeJson()) par.value2 = par.value2.ToDateTimeFromJson();
                        sql += (numP - parameters.Count(p => p.used == true) - pars.Count(p => p == null)) == 0 && sql.ToUpper().IndexOf("WHERE") < 0 ? " Where " : " And ";
                        //sql += par.name + " " + par.comparison;
                        switch (par.comparison)
                        {
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
                                string[] vals = par.value1.ToString().Replace("(", "").Replace(")", "").Replace(" ", "").Split(",");
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
                return getContext(formField.DropDownListDB).ExecuteQuery<object>(Form + '_' + field, sql, false, pars.ToArray());
            }
            else
            {
                return null;
            }
        }

        public List<object> GetDataByFormChildSelect(string Form, string childForm, short sqlNumber, object parent)
        {
            BecaFormLevels child = _context.BecaFormLevels
                .FirstOrDefault(c => c.Form == Form && c.ChildForm == childForm);
            BecaForm form = _context.BecaForm.FirstOrDefault(f => f.Form == childForm);

            string ddl = child.GetPropertyValue("ComboAddSql" + sqlNumber.ToString()).ToString();
            string ddlKeys = child.GetPropertyValue("ComboAddSql" + sqlNumber.ToString() + "Keys").ToString();
            string ddlDisplay = child.GetPropertyValue("ComboAddSql" + sqlNumber.ToString() + "Display").ToString();

            if (child != null && form != null)
            {
                string key = ddlKeys.Replace(",", " + ");

                List<object> pars = new List<object>();
                string sqlChk = "Select * From " + form.TableName;
                object colCheck = getContext(form.TableNameDB).GetQueryDef<object>(form + "_ca" + sqlNumber.ToString() + "_chk", sqlChk + " Where 0 = 1");
                if (colCheck.GetType().GetProperty("idUtente") != null)
                {
                    ddlKeys += ",idUtente";
                    pars.Add(_currentUser.idUtenteLoc(_activeCompany == null ? null : _activeCompany.idCompany));
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
                return getContext(form.TableNameDB).ExecuteQuery<object>(form + "_ca" + sqlNumber.ToString(), sql, false, pars.ToArray());
            }
            else
            {
                return null;
            }
        }

        public List<object> GetDataBySQL(string dbName, string sql, List<BecaParameter> parameters, bool useidUtente = true)
        {
            string sqlOrd = "";
            if (sql.ToUpper().Contains("ORDER"))
            {
                sqlOrd = sql.Substring(sql.ToUpper().IndexOf("ORDER"));
                sql = sql.Substring(0, sql.ToUpper().IndexOf("ORDER") - 1);
            }
            string sqlGroup = "";
            if (sql.ToUpper().Contains("GROUP"))
            {
                sqlGroup = sql.Substring(sql.ToUpper().IndexOf("GROUP"));
                sql = sql.Substring(0, sql.ToUpper().IndexOf("GROUP") - 1);
            }
            string sqlWhere = "";
            if (sql.ToUpper().Contains("WHERE"))
            {
                sqlWhere = sql.Substring(sql.ToUpper().IndexOf("WHERE"));
                sql = sql.Substring(0, sql.ToUpper().IndexOf("WHERE") - 1);
            }
            string sqlTable = sql.Substring(sql.ToUpper().IndexOf("FROM") + 5);
            string sqlChk = "Select * From " + sqlTable;

            object colCheck = getContext(dbName).GetQueryDef<object>("", sqlChk + " Where 0 = 1");
            if (colCheck.GetType().GetProperty("idUtente") != null && useidUtente)
            {
                if (parameters == null) parameters = new List<BecaParameter>();
                if (parameters.Find(p => p.name == "idUtente") == null)
                {
                    parameters.Add(new BecaParameter()
                    {
                        name = "idUtente",
                        value1 = _currentUser.idUtenteLoc(_activeCompany == null ? null : _activeCompany.idCompany),
                        comparison = "="
                    });
                }
            }
            sql = sql + " " + sqlWhere;
            List<object> pars = new List<object>();
            if (parameters != null && parameters.Count() > 0)
            {
                int numP = 0;
                foreach (BecaParameter par in parameters)
                {
                    if (colCheck.GetType().GetProperty(par.name) != null)
                    {
                        sql += sql.ToUpper().Contains("WHERE") ? " And " : " Where ";
                        sql += par.name + " " + par.comparison;
                        switch (par.comparison)
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
                                string[] vals = par.value1.ToString().Replace("(", "").Replace(")", "").Replace(" ", "").Split(",");
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
            sql = sql + " " + sqlGroup + " " + sqlOrd;
            return getContext(dbName).ExecuteQuery<object>("", sql, false, pars.ToArray());
        }

        public List<object> GetDataBySQL(string dbName, string sql, object[] parameters)
        {
            return getContext(dbName).ExecuteQuery<object>("", sql, false, parameters);
        }

        public IDictionary<string, object> GetDataDictBySQL(string dbName, string sql, List<BecaParameter> parameters)
        {
            List<object> data = this.GetDataBySQL(dbName, sql, parameters);

            sql = sql.Substring(0, sql.ToUpper().IndexOf("FROM") - 1).TrimEnd().Substring(7);
            string[] fields = sql.Split(",");
            string key = fields[0];
            string val = fields[1];
            IDictionary<string, object> dict = new Dictionary<string, object>();
            foreach (var row in data)
            {
                dict.Add(row.GetPropertyValue(key).ToString(), row.GetPropertyValue(val));
            }
            return dict;
        }

        public ViewChart GetGraphByFormField(string Form, string field, List<BecaParameter> parameters)
        {
            ViewChart graph = new ViewChart();

            List<object> axisX = this.GetDataByFormField(Form, field + "X", parameters);
            List<object> data = this.GetDataByFormField(Form, field + "Dati", parameters);

            foreach (object X in axisX)
            {
                ViewAxisXvalue Xval = new ViewAxisXvalue();
                Xval.value = X.GetPropertyValue("XValue");
                dtoBecaFilterValue FV;
                if (X.GetPropertyValue("FVname1").ToString() != "")
                {
                    FV = new dtoBecaFilterValue();
                    FV.filterName = X.GetPropertyValue("FVname1").ToString();
                    FV.value = X.GetPropertyValue("FV1").ToString();
                    FV.Default = X.GetPropertyValue("FV1").ToString();
                    FV.Api = false;
                    Xval.filterValues.Add(FV);
                }
                if (X.GetPropertyValue("FVname1").ToString() != "")
                {
                    FV = new dtoBecaFilterValue();
                    FV.filterName = X.GetPropertyValue("FVname2").ToString();
                    FV.value = X.GetPropertyValue("FV2").ToString();
                    FV.Default = X.GetPropertyValue("FV2").ToString();
                    FV.Api = false;
                    Xval.filterValues.Add(FV);
                }
                graph.axisX.caption.Add(X.GetPropertyValue("XValue").ToString());
                graph.axisX.value.Add(Xval);
            }

            BecaFormField formField = _context.BecaFormField
                .Find(Form, field + "Dati");
            List<string> lines = formField.Parameters.Replace(" ", "").Split(",").ToList();
            foreach (string line in lines)
            {
                ViewChartValue val = new ViewChartValue();
                val.label = line;
                graph.values.Add(val);
            }
            int il = 0;
            foreach (object X in data)
            {
                il = 0;
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

        public int ExecuteSqlCommand(string dbName, string commandText, params object[] parameters)
        {
            return getContext(dbName).ExecuteSqlCommand(commandText, parameters);
        }

        public async Task<int> ExecuteSqlCommandAsync(string dbName, string commandText, params object[] parameters)
        {
            return await getContext(dbName).ExecuteSqlCommandAsync(commandText, parameters);
        }

        public async Task<int> ExecuteProcedure(string dbName, string spName, List<BecaParameter> parameters)
        {
            List<string> names = getContext(dbName).GetProcedureParams(spName);
            if (names.Contains("@idUtente") && parameters.Count(p => p.name == "idUtente") == 0)
            {
                parameters.Add(new BecaParameter("idUtente", _currentUser.idUtenteLoc(_activeCompany == null ? null : _activeCompany.idCompany)));
            }
            string sql = $"Exec {spName} " +
                string.Join(", ", names.Where(x => parameters.Exists(p => p.name.ToLower() == x.ToLower().Replace("@", ""))).Select((x, i) => x +
                    @" = {" + i.ToString() + "}"));
            var pars = names.Where(x => parameters.Exists(p => p.name.ToLower() == x.ToLower().Replace("@", ""))).Select((x, i) =>
            {
                BecaParameter par = parameters.Find(p => p.name.ToLower() == x.ToLower().Replace("@", ""));
                return par == null ?
                    null
                    :
                    (par.value1 != null && par.value1.ToString().IsValidDateTimeJson()) ?
                        par.value1.ToDateTimeFromJson()
                        :
                        par.value1;
            }).ToArray();
            return await getContext(dbName).ExecuteSqlCommandAsync(sql, pars);
        }
        #endregion "SQL"

        private DbDatiContext getContext(int id)
        {
            return this.getContext(_activeCompany.Connections.FirstOrDefault(c => c.idConnection == id).ConnectionName);
        }

        private DbDatiContext getContext(string dbName)
        {
            if (dbName == "")
                dbName = _activeCompany.Connections.FirstOrDefault(c => c.Default == true).ConnectionName;
            if (_databases.ContainsKey(dbName)) return _databases[dbName];

            DbDatiContext db = new DbDatiContext(_formTool, _activeCompany.Connections.FirstOrDefault(c => c.ConnectionName == dbName).ConnectionString);
            _databases.Add(dbName, db);
            return db;
        }

        private async Task<string> UploadByForm(BecaForm form, object record)
        {
            try
            {
                List<BecaFormField> upl = _context.BecaFormField
                .Where(f => f.Form == form.Form && f.FieldType == "upload")
                .ToList();
                foreach (BecaFormField field in upl.Where(f => record.HasPropertyValue(f.Name + "upl") && record.GetPropertyString(f.Name + "upl").ToString() != ""))
                {
                    string res = await SaveFileByField(form, field, record);
                    if (res.Contains("ERR: ")) return res.Replace("ERR: ", "");
                    record.SetPropertyValue(field.Name, res); //field.Parameters.Split("|").Last()
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"UploadByForm: {ex.Message}");
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
                string[] pars = field.Parameters.Split("|");

                //se il 6° parametro è fra apici allora mi viene fornito il valore da cercare
                //altrimento mi viene fornito il nome del campo in cui cercare il tipo documento che sto caricando
                string cod = pars[6].Contains("'")
                    ? pars[6].Replace("'", "")
                    : record.HasPropertyValue(pars[6]) ? record.GetPropertyValue(pars[6]).ToString() : "";
                //if (cod.Contains("'"))
                //{
                //    //se è fra apici allora mi viene fornito il valore da cercare
                //    cod = cod.Replace("'", "");
                //}
                //else
                //{
                //    //altrimento mi viene fornito il nome del campo in cui cercare il tipo documento che sto caricando
                //    if (record.HasPropertyValue(cod)) cod = record.GetPropertyValue(cod).ToString(); else cod = "";
                //}

                string sql = "Select " + pars[1] + " as Cod, " + pars[2] + " As Fld, " + pars[3] + " As Name, " + pars[4] + " As ext, " + pars[5] + " As MB " +
                    "From " + pars[0] + " Where " + pars[1] + " = {0}";
                List<BecaParameter> parameters = new List<BecaParameter>() { new BecaParameter()
                    {
                        name = pars[1],
                        value1 = (object)cod,
                        comparison = "="
                    }
                };
                List<object> data = this.GetDataBySQL(form.TableNameDB, sql, parameters);
                if (data.Count < 1)
                    return "ERR: C'è stato un problema nel reperimento del tipo documento (" + field.Name + "). Contattare il fornitore";

                object tipoDoc = data[0];
                string folderNameSub = GetSaveName(tipoDoc.GetPropertyValue("Fld").ToString(), record).Replace("/", @"\");
                string folderName = folderNameSub.Contains(@"\\") || folderNameSub.Contains(@":\")
                    ? @"\\192.168.0.207\BecaWeb\Web\Upload\" + _activeCompany.MainFolder + @"\" + folderNameSub
                    : @"E:\BecaWeb\Web\Upload\" + _activeCompany.MainFolder + @"\" + folderNameSub;
                folderName = @"\\192.168.0.207\BecaWeb\Web\Upload\" + _activeCompany.MainFolder + @"\" + folderNameSub;
                string fileName = GetSaveName(tipoDoc.GetPropertyValue("Name").ToString(), record);

                _logger.LogDebug($"Salvo il file {fileName} in {folderName}");

                if (folderName == "")
                {
                    return "ERR: C'è stato un problema nella definizione della cartella di destinazione (" + field.Name + "). Contattare il fornitore";
                }
                if (fileName == "" && tipoDoc.GetPropertyValue("Name").ToString() != "")
                {
                    return "ERR: C'è stato un problema nella definizione del nome del file (" + field.Name + "). Contattare il fornitore";
                }

                if (!Directory.Exists(folderName)) Directory.CreateDirectory(folderName);


                string fileUploaded = record.GetPropertyValue(field.Name + "upl").ToString();
                string fileUploadedName = record.GetPropertyValue(field.Name + "uplName").ToString();

                if (fileName == "") fileName = fileUploadedName;
                if (fileUploaded.Length > 0)
                {
                    string sFileExtension = fileUploadedName.Remove(0, fileUploadedName.LastIndexOf(".") + 1).ToLower();
                    if (tipoDoc.GetPropertyValue("MB").ToString() != "" && tipoDoc.GetPropertyString("MB") != "0" && getBase64Dimension(fileUploaded) > int.Parse(tipoDoc.GetPropertyString("MB")))
                    {
                        return "ERR: Il file " + fileUploadedName + " eccede la dimensione permessa (" + Math.Round((decimal)(int.Parse(tipoDoc.GetPropertyString("MB")) / 1024 / 1024), 2).ToString() + "MB)";
                    }
                    if (!tipoDoc.GetPropertyString("ext").ToLower().Contains(sFileExtension) && tipoDoc.GetPropertyString("ext") != "")
                    {
                        return "ERR: Il tipo di file (" + sFileExtension + ") non è ammesso. Seleziona uno tra questi tipi: " + tipoDoc.GetPropertyString("ext");
                    }
                    else
                    {
                        fileName = fileName.Replace("." + sFileExtension, "") + "." + sFileExtension;
                        if (File.Exists(folderName + @"\" + fileName))
                            System.IO.File.Delete(folderName + @"\" + fileName + "." + sFileExtension);
                        await System.IO.File.WriteAllBytesAsync(folderName + @"\" + fileName, Convert.FromBase64String(fileUploaded));
                        return fileName;
                    }
                }
                else { return ""; }
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Errore nel salvataggio del file: {ex.Message}");
                return "ERR: " + ex.Message;
            }
        }

        private string GetSaveName(string Name, object record)
        {
            int p1, p2;
            string ph = "";

            while (Name.Contains("#"))
            {
                p1 = Name.IndexOf("#");
                p2 = Name.IndexOf("#", p1 + 1);
                ph = Name.Substring(p1 + 1, p2 - p1 - 1);
                if (record.HasPropertyValue(ph))
                {
                    if (record.GetPropertyValue(ph).GetType() == typeof(DateTime))
                        Name = Name.Replace("#" + ph + "#", ((DateTime)record.GetPropertyValue(ph)).ToString("yyyyMMdd"));
                    else if (record.GetPropertyValue(ph).ToString().isDate())
                        Name = Name.Replace("#" + ph + "#", DateTime.Parse(record.GetPropertyValue(ph).ToString()).ToString("yyyyMMdd"));
                    else
                        Name = Name.Replace("#" + ph + "#", record.GetPropertyValue(ph).ToString());
                }
                else
                    return "";
            }
            return Name;
        }

        private double getBase64Dimension(string base64String)
        {
            bool applyPaddingsRules = true;

            // Remove MIME-type from the base64 if exists
            int base64Length = base64String.AsSpan().Slice(base64String.IndexOf(',') + 1).Length;

            double fileSizeInByte = Math.Ceiling((double)base64Length / 4) * 3;

            if (applyPaddingsRules && base64Length >= 2)
            {
                var paddings = base64String[^2..];
                fileSizeInByte = paddings.Equals("==") ? fileSizeInByte - 2 : paddings[1].Equals('=') ? fileSizeInByte - 1 : fileSizeInByte;
            }
            return fileSizeInByte > 0 ? fileSizeInByte / 1_048_576 : 0;
        }

    }
}
