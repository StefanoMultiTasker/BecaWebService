using AutoMapper;
using BecaWebService.ExtensionsLib;
using Contracts;
using Entities;
using Entities.Contexts;
using Entities.DataTransferObjects;
using Entities.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Repository
{
    public class BecaViewRepository(DbBecaContext context, IHttpContextAccessor httpContextAccessor, IMapper mapper) : IBecaViewRepository
    {
        private readonly DbBecaContext dbBecaContext = context;
        private readonly BecaUser _currentUser= (BecaUser)httpContextAccessor.HttpContext!.Items["User"]!;
        private readonly Company _currentCompany = (Company)httpContextAccessor.HttpContext.Items["Company"]!;
        private readonly IMapper _mapper = mapper;

        private BecaUser CurrentUser() => _currentUser;
        private Company CurrentCompany() => _currentCompany;

        //public async Task<BecaView> GetViewByID(int idView)
        public BecaView? GetViewByID(int idView)
        {
            BecaView? view = dbBecaContext.BecaView
                        .SingleOrDefault(view => view.idBecaView == idView);
            List<BecaViewData> cols = [.. dbBecaContext.BecaViewData.Where(view => view.idBecaView == idView)];

            if (view == null || cols.Count == 0) return null;

            this.SetCustomizedCols(idView, ref cols);
            List<BecaViewFilters> vFilters = [.. dbBecaContext.BecaViewFilters.Where(view => view.idBecaView == idView)];
            List<BecaViewFilterValues> vFilterVals = [.. dbBecaContext.BecaViewFilterValues.Where(view => view.idBecaView == idView)];
            List<BecaViewChild> children = [.. dbBecaContext.BecaViewChildren
                        .Include(c => c.BecaFormChildData)
                        .Where(view => view.idBecaView == idView)];

            view.BecaViewData = cols;
            view.BecaViewFilters = vFilters;
            view.BecaViewFilterValues = vFilterVals;
            view.BecaViewChildren = children;

            List<BecaViewPanels> panels = [.. dbBecaContext.BecaViewPanels
                    .Where(panel => panel.idBecaView == idView)
                    .Include(filters => filters.BecaPanelFilters)
                    .Include(formula => formula.IdFormulaNavigation)
                        .ThenInclude(data => data.BecaFormulaData)
                            .ThenInclude(dfilters => dfilters.BecaFormulaDataFilters)];
            view.BecaViewPanels = panels;

            List<BecaViewAction> actions = [.. dbBecaContext.BecaViewActions.Where(action => action.idBecaView == idView)];
            view.Actions = actions;

            return view;
        }

        private void SetCustomizedCols(int idView, ref List<BecaViewData> cols)
        {
            int idUtente = CurrentUser().idUtente;
            int idCompany = CurrentCompany().idCompany;
            List<BecaViewDataUser> customCols = [.. dbBecaContext.BecaViewDataUser
                        .Where(view => view.idBecaView == idView &&
                            view.idUtente == idUtente &&
                            view.idCompany == idCompany)];
            foreach (BecaViewDataUser customCol in customCols)
            {
                BecaViewData? col = cols.FirstOrDefault(c => c.Field == customCol.field);
                if (col != null) col.isGridVisible = customCol.isGridVisible;
            }
        }

        public UIform? GetViewUI(int idView, string tipoUI)
        {
            List<BecaViewUIProfile> profileUI = [.. dbBecaContext.BecaViewUIProfile
                .Where(obj => obj.idBecaView == idView && obj.idProfile == CurrentUser().idProfileDef(CurrentCompany().idCompany))];

            switch (tipoUI)
            {
                case "F":
                    List<BecaViewFilterUI> filterUI = [.. dbBecaContext.BecaViewFilterUI
                            .Where(obj => obj.idBecaView == idView)
                            .OrderBy(obj => obj.Row)
                            .ThenBy(obj => obj.Col)
                            .ThenBy(obj => obj.Col_Order)
                            .ThenBy(obj => obj.SubRow)
                            .ThenBy(obj => obj.SubCol)
                            .ThenBy(obj => obj.SubCol_Order)];
                    if (filterUI == null || filterUI.Count == 0) return null;
                    UIform? viewFilterUI = CreateFormSectionUI(this._mapper.Map<List<BecaViewFilterUI>, List<BecaViewUI>>(filterUI), profileUI);
                    return viewFilterUI;
                case "D":
                    List<BecaViewDetailUI> detailUI = [.. dbBecaContext.BecaViewDetailUI
                            .Where(obj => obj.idBecaView == idView)
                            .OrderBy(obj => obj.Row)
                            .ThenBy(obj => obj.Col)
                            .ThenBy(obj => obj.Col_Order)
                            .ThenBy(obj => obj.SubRow)
                            .ThenBy(obj => obj.SubCol)
                            .ThenBy(obj => obj.SubCol_Order)];
                    if (detailUI == null || detailUI.Count == 0) return null;
                    UIform? viewDetailUI = CreateFormSectionUI(this._mapper.Map<List<BecaViewDetailUI>, List<BecaViewUI>>(detailUI), profileUI);
                    return viewDetailUI;
                default: return null;
            }
        }

        public UIform? GetViewUI(string form)
        {
            List<BecaViewUIProfile> profileUI = [.. dbBecaContext.BecaViewUIProfile
                .Where(obj => obj.Form == form && obj.idProfile == CurrentUser().idProfileDef(CurrentCompany().idCompany))];

            List<BecaViewDetailUI> detailUI = [.. dbBecaContext.BecaViewDetailUI
                    .Where(obj => obj.Form == form)
                    .OrderBy(obj => obj.Row)
                    .ThenBy(obj => obj.Col)
                    .ThenBy(obj => obj.Col_Order)
                    .ThenBy(obj => obj.SubRow)
                    .ThenBy(obj => obj.SubCol)
                    .ThenBy(obj => obj.SubCol_Order)];
            if (detailUI == null || detailUI.Count == 0) return null;
            UIform? viewDetailUI = CreateFormSectionUI(this._mapper.Map<List<BecaViewDetailUI>, List<BecaViewUI>>(detailUI), profileUI);
            return viewDetailUI;
        }

        private static UIform? CreateFormSectionUI(List<BecaViewUI> items, List<BecaViewUIProfile> profileUI)
        {
            UIform filter = new(items[0].ViewName ?? "");
            foreach (BecaViewUI BecaCfgFormField in items)
            {
                if (BecaCfgFormField.Row > 0 && BecaCfgFormField.Col > 0)
                {
                    BecaViewUIProfile? fieldProfile = profileUI.Find(f => f.Field.Equals(BecaCfgFormField.Name, StringComparison.CurrentCultureIgnoreCase));
                    FieldConfig field = new()
                    {
                        label = fieldProfile != null ? fieldProfile.Title ?? BecaCfgFormField.Title ?? "" : BecaCfgFormField.Title ?? "",
                        name = BecaCfgFormField.Name.ToLower(),//.ToCamelCase();
                        placeholder = BecaCfgFormField.HelpShort ?? "",
                        fieldType = fieldProfile != null ? fieldProfile.FieldType ?? BecaCfgFormField.FieldType ?? "" : BecaCfgFormField.FieldType ?? "",
                        inputType = fieldProfile != null ? fieldProfile.FieldInput ?? BecaCfgFormField.FieldInput ?? "" : BecaCfgFormField.FieldInput ?? "",
                        format = BecaCfgFormField.Format ?? "",
                        reference = BecaCfgFormField.Filter_Reference,
                        filterAPI = BecaCfgFormField.Filter_API
                    };
                    string opts = BecaCfgFormField.Filter_options ?? "";
                    if (opts.StartsWith('[') && opts.EndsWith(']'))
                    {
                        field.options = [.. opts.Replace("[", "").Replace("]", "").Replace("'", "").Replace(" ", "").Split(",")];
                    }
                    field.optionDisplayed = (fieldProfile != null 
                        ? fieldProfile.DropDownDisplayField ?? BecaCfgFormField.DropDownDisplayField 
                        : BecaCfgFormField.DropDownDisplayField)?.ToLower(); //.ToCamelCase();
                    if (field.optionDisplayed != null) field.optionDisplayed = field.optionDisplayed.ToLowerToCamelCase();
                    //field.DropDownList = BecaCfgFormField.DropDownList;
                    field.DropDownKeyFields = (fieldProfile != null 
                        ? fieldProfile.DropDownKeyFields ?? BecaCfgFormField.DropDownKeyFields 
                        : BecaCfgFormField.DropDownKeyFields)?.ToLower(); // ToCamelCase();
                    field.DropDownItems = BecaCfgFormField.DropDownItems;
                    field.DropDownListAll = fieldProfile != null ? fieldProfile.DropDownListAll : BecaCfgFormField.DropDownListAll;
                    field.DropDownListNull = fieldProfile != null ? fieldProfile.DropDownListNull : BecaCfgFormField.DropDownListNull;
                    field.row = BecaCfgFormField.Row;
                    field.col = BecaCfgFormField.Col;
                    field.subRow = BecaCfgFormField.SubRow;
                    field.subCol = BecaCfgFormField.SubCol;
                    field.ColClass = BecaCfgFormField.ColClass;
                    field.ColSize = BecaCfgFormField.ColSize;
                    field.SubColSize = BecaCfgFormField.SubColSize;
                    field.disabled = BecaCfgFormField.Locked;
                    field.required = BecaCfgFormField.Required;

                    filter.fields.Add(field);
                }
            }
            return filter;
        }

        public bool CustomizeColumnsByUser(int idView, List<dtoBecaData> cols)
        {
            List<BecaViewDataUser> customCols = [];
            List<BecaViewData> viewCols = [.. dbBecaContext.BecaViewData.Where(view => view.idBecaView == idView)];
            foreach (dtoBecaData col in cols)
            {
                if (col.isGridVisible)
                {
                    BecaViewDataUser customCol = new()
                    {
                        idBecaView = idView
                    };
                    foreach (BecaViewData viewCol in viewCols)
                    {
                        if (viewCol.Field.Equals(col.Name, StringComparison.CurrentCultureIgnoreCase))
                        {
                            customCol.field = viewCol.Field;
                            break;
                        }
                    }
                    customCol.idCompany = CurrentCompany().idCompany;
                    customCol.idUtente = CurrentUser().idUtente;
                    customCol.isGridVisible = col.isGridVisible;
                    customCols.Add(customCol);
                }
            }
            try
            {
                List<BecaViewDataUser> userData = [.. dbBecaContext.BecaViewDataUser
                            .Where(view => view.idBecaView == idView &&
                                view.idUtente == CurrentUser().idUtente &&
                                view.idCompany == CurrentCompany().idCompany)];
                dbBecaContext.RemoveRange(userData);
                dbBecaContext.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            try
            {
                dbBecaContext.AddRange(customCols.FindAll(col => col.isGridVisible == true));
                dbBecaContext.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return true;
        }
    }
}
