using BecaWebService.ExtensionsLib;
using Contracts;
using Entities;
using Entities.Contexts;
using Entities.DataTransferObjects;
using Entities.Models;
using ExtensionsLib;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Repository
{
    public class GenericRepository : IGenericRepository
    {
        private DbBecaContext _context;
        private BecaUser _currentUser;
        private Company _activeCompany;
        private readonly FormTool _formTool;

        private Dictionary<string, DbDatiContext> _databases = new Dictionary<string, DbDatiContext>();

        public GenericRepository(IDependencies deps, IHttpContextAccessor httpContextAccessor)
        {
            _context = deps.context;
            _currentUser = (BecaUser)httpContextAccessor.HttpContext.Items["User"];
            _activeCompany = (Company)httpContextAccessor.HttpContext.Items["Company"];
            _formTool = deps.formTool;
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

        public async Task<T> AddDataByForm<T>(string Form, object record) where T : class, new()
        {
            BecaForm form = _context.BecaForm
                .FirstOrDefault(f => f.Form == Form);
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
                string idName = method.Invoke(def, null).ToString();

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

                if (idName != "")
                {
                    int key = await getContext(form.TableNameDB).InsertSqlCommandWithIdentity(sql, pars.ToArray());
                    if (key <= 0)
                    {
                        return null;
                    }
                    record.SetPropertyValue(idName, key);
                }
                else
                {
                    int ires = await getContext(form.TableNameDB).ExecuteSqlCommandAsync(sql, pars.ToArray());
                }

                List<T> inserted = this.GetDataByForm<T>(Form, record);
                if (inserted.Count == 0) return null;

                if ((form.AddProcedureName ?? "") != "" && form.AddProcedureName.Contains("#After"))
                {
                    await _context.SaveChangesAsync();
                    await UpdateDataByProcedure<T>(form.TableNameDB, form.AddProcedureName, record);
                }
                return inserted[0];
            }
            else
            {
                return null;
            }
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
                    catch (Exception ex) { isFieldPresent = false; }
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

        public List<T> GetDataByForm<T>(string Form, List<BecaParameter> parameters) where T : class, new()
        {
            BecaForm form = _context.BecaForm
                .FirstOrDefault(f => f.Form == Form);
            List<object> pars = new List<object>();
            if (form != null)
            {
                string sql = "Select * From " +
                    (form.ViewName == null || form.ViewName.ToString() == "" ? form.TableName : form.ViewName);
                string db = form.ViewName == null || form.ViewName.ToString() == "" ? form.TableNameDB : form.ViewNameDB;

                object colCheck = getContext(db).GetQueryDef<object>(Form, sql + " Where 0 = 1");
                if (form.UseDefaultParam)
                {
                    if (colCheck.GetType().GetProperty("idUtente") != null)
                    {
                        if (parameters == null) parameters = new List<BecaParameter>();
                        parameters.Add(new BecaParameter()
                        {
                            name = "idUtente",
                            value1 = _currentUser.idUtente,
                            comparison = "="
                        });
                    }
                }
                if (parameters != null && parameters.Count() > 0)
                {
                    //pars = new object[] { };
                    int numP = 0;
                    foreach (BecaParameter par in parameters)
                    {
                        sql += numP == 0 ? " Where " : " And ";
                        sql += par.name + " " + par.comparison;
                        switch (par.comparison.ToLower())
                        {
                            case "between":
                                sql += " {" + numP + "} and {" + (numP + 1).ToString() + "}";
                                numP++;
                                pars.Add(par.value1);
                                pars.Add(par.value2);
                                break;

                            case "like":
                                sql += " '%' + {" + numP + "} + '%'";
                                pars.Add(par.value1);
                                break;

                            case "is null":
                                pars.Add(null);
                                break;

                            case "in":
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

                List<T> res = getContext(db).ExecuteQuery<T>(Form, sql, subForms.Count>0, pars.ToArray());

                foreach (BecaFormLevels level in subForms)
                {
                    BecaForm childForm = _context.BecaForm
                        .FirstOrDefault(f => f.Form == level.ChildForm);

                    string parent = (form.ViewName == null || form.ViewName.ToString() == "" ? form.TableName : form.ViewName);
                    string child = (childForm.ViewName == null || childForm.ViewName.ToString() == "" ? childForm.TableName : childForm.ViewName);

                    string sqlParent = sql;
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
                                   List<object> curChildren = oParent.GetPropertyValueArray("__children");
                                   if (curChildren == null) curChildren = new List<object>();
                                   if (curChildren.Count < level.SubLevel) curChildren.Add(new List<object>());

                                   curChildren[level.SubLevel - 1] = children2;

                                   oParent.SetPropertyValuearray("__children", curChildren);
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

                return res;
            }
            else
            {
                return null;
            }
        }

        public string getRelationObjectString(string relation, object record)
        {
            string rel= string.Join("", relation.Split(",").Select(n => record.GetPropertyValue(n).ToString()));
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

        public List<T> GetDataByForm<T>(string Form, object record) where T : class, new()
        {
            BecaForm form = _context.BecaForm
                .FirstOrDefault(f => f.Form == Form);
            List<object> pars = new List<object>();
            if (form != null)
            {
                BecaParameters aPar = new BecaParameters();
                foreach (string orderField in form.PrimaryKey.Split(","))
                {
                    aPar.Add(orderField, record.GetPropertyValue(orderField));
                }
                List<T> data = this.GetDataByForm<T>(Form, aPar.parameters);
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

        public T getFormObject<T>(string Form)
        {
            BecaForm form = _context.BecaForm
                .FirstOrDefault(f => f.Form == Form);
            List<object> pars = new List<object>();
            if (form != null)
            {
                object def = getContext(form.TableNameDB).GetQueryDef<object>(Form, "Select * From " + form.TableName + " Where 0 = 1");
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

        public async Task<int?> UpdateDataByForm<T>(string Form, object recordOld, object recordNew) where T : class, new()
        {
            BecaForm form = _context.BecaForm
                .FirstOrDefault(f => f.Form == Form);
            List<object> pars = new List<object>();
            if (form != null)
            {
                if ((form.UpdateProcedureName ?? "") != "" && !form.UpdateProcedureName.Contains("#After"))
                {
                    int? resSPBefore = await UpdateDataByProcedure<T>(form.TableNameDB, form.UpdateProcedureName, recordNew);
                    if (!form.UpdateProcedureName.Contains("#Before")) return resSPBefore;
                }
                int numP = 0;
                string sql = "Update " + form.TableName + " Set ";
                PropertyInfo[] colsOld = recordOld.GetType().GetProperties();
                PropertyInfo[] colsNew = recordNew.GetType().GetProperties();
                if (colsOld.Count() > 0)
                {
                    for (int i = 0; i < colsOld.Count(); i++)
                    {
                        PropertyInfo p1 = colsOld.ElementAt(i);
                        PropertyInfo p2 = colsNew.ElementAt(i);
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
                            pars.Add(p2.GetValue(recordNew));
                            sql += (numP > 0 ? ", " : "") + p1.Name + " = {" + numP.ToString() + "}";
                            numP++;
                        }
                    }
                }
                else return null;
                if (numP == 0) return 0;

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
                    return await UpdateDataByProcedure<T>(form.TableNameDB, form.UpdateProcedureName, recordNew);
                }
                return resSave;
            }
            else
            {
                return null;
            }
        }
        #endregion "Form"

        #region "Field"

        public List<object> GetDataByFormField(string Form, string field, List<BecaParameter> parameters)
        {
            BecaFormField formField = _context.BecaFormField
                .Find(Form, field);
            //BecaFormFieldLevel formFieldCust = _context.BecaFormFieldLevel
            //    .Find(Form, field, null);
            string ddl = formField.DropDownList;
            string ddlPar = formField.Parameters;
            //string ddl = formFieldCust == null ? formField.DropDownList : formFieldCust.DropDownList;
            //string ddlPar = formFieldCust == null ? formField.Parametri : formFieldCust.Parametri;

            if (formField != null)
            {
                //List<BecaParameter> parameters = new List<BecaParameter>();
                string sql = ddl;
                string sqlChk = "Select * From " + ddl.Substring(ddl.ToUpper().IndexOf("FROM") + 5);
                if (sqlChk.ToUpper().Contains("WHERE")) sqlChk = sqlChk.Substring(0, sqlChk.ToUpper().IndexOf("WHERE") - 1);
                if (sqlChk.ToUpper().Contains("ORDER")) sqlChk = sqlChk.Substring(0, sqlChk.ToUpper().IndexOf("ORDER") - 1);
                if (ddlPar != null && ddlPar.Contains("idUtente"))
                {
                    object colCheck = getContext(formField.DropDownListDB).GetQueryDef<object>(Form + '_' + field + "_chk", sqlChk + " Where 0 = 1");
                    if (colCheck.GetType().GetProperty("idUtente") != null)
                    {
                        parameters.Add(new BecaParameter()
                        {
                            name = "idUtente",
                            value1 = _currentUser.idUtente,
                            comparison = "="
                        });
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
                List<object> pars = new List<object>();
                if (parameters != null && parameters.Count() > 0)
                {
                    int numP = 0;
                    foreach (BecaParameter par in parameters)
                    {
                        if (par.value1 != null && par.value1.ToString().IsValidDateTimeJson()) par.value1 = par.value1.ToDateTimeFromJson();
                        if (par.value2 != null && par.value2.ToString().IsValidDateTimeJson()) par.value2 = par.value2.ToDateTimeFromJson();
                        sql += numP == 0 && sql.ToUpper().IndexOf("WHERE") < 0 ? " Where " : " And ";
                        sql += par.name + " " + par.comparison;
                        switch (par.comparison)
                        {
                            case "between":
                                sql += " {" + numP + "} and {" + (numP + 1).ToString() + "}";
                                pars.Add(par.value1);
                                pars.Add(par.value2);
                                numP++;
                                break;

                            case "like":
                                sql += " '%' + {" + numP + "} + '%'";
                                pars.Add(par.value1);
                                break;

                            default:
                                sql += " {" + numP + "}";
                                pars.Add(par.value1);
                                break;
                        }
                        numP++;
                    }
                }
                sql = sql + " " + sqlGroup + " " + sqlOrd;
                return getContext(formField.DropDownListDB).ExecuteQuery<object>(Form + '_' + field, sql,false, pars.ToArray());
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
                        value1 = _currentUser.idUtente,
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
            return getContext(dbName).ExecuteQuery<object>("", sql,false, pars.ToArray());
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
            string sql = $"Exec {spName} " + string.Join(", ", names.Select((x, i) => $"{{{i}}}"));
            var pars = names.Select((x, i) =>
                parameters.Find(p => p.name == x.Replace("@", "")) == null ?
                null
                :
                parameters.Find(p => p.name == x.Replace("@", "")).value1
                ).ToArray();
            return await getContext(dbName).ExecuteSqlCommandAsync(sql, pars);
        }
        #endregion "SQL"

        private DbDatiContext getContext(int id)
        {
            return this.getContext(_activeCompany.Connections.FirstOrDefault(c => c.idConnection == id).ConnectionName);
        }

        private DbDatiContext getContext(string dbName)
        {
            if (_databases.ContainsKey(dbName)) return _databases[dbName];

            DbDatiContext db = new DbDatiContext(_formTool, _activeCompany.Connections.FirstOrDefault(c => c.ConnectionName == dbName).ConnectionString);
            _databases.Add(dbName, db);
            return db;
        }
    }
}
