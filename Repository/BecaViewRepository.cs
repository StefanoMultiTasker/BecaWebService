using Contracts;
using Entities.Contexts;
using Entities.Models;
using BecaWebService.ExtensionsLib;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using System.Linq;
using Entities.DataTransferObjects;
using Entities;
using Microsoft.AspNetCore.Http;

namespace Repository
{
    public class BecaViewRepository : RepositoryBase<BecaView>, IBecaViewRepository
    {
        private readonly IMapper _mapper;

        public BecaViewRepository(IDependencies deps, IHttpContextAccessor httpContextAccessor)
               : base(deps, httpContextAccessor)
        {
            _mapper = deps.mapper;
        }

        //public async Task<BecaView> GetViewByID(int idView)
        public BecaView GetViewByID(int idView)
        {
            BecaView view = dbBecaContext.BecaView
                        .SingleOrDefault(view => view.idBecaView == idView);
            //.Include(data => data.BecaViewData)
            //.Include(filters => filters.BecaViewFilters)
            //.Include(filterValues => filterValues.BecaViewFilterValues)

            List<BecaViewData> cols = dbBecaContext.BecaViewData
                        .Where(view => view.idBecaView == idView)
                        .ToList();
            this.SetCustomizedCols(idView, ref cols);
            List<BecaViewFilters> vFilters = dbBecaContext.BecaViewFilters
                        .Where(view => view.idBecaView == idView)
                        .ToList();
            List<BecaViewFilterValues> vFilterVals = dbBecaContext.BecaViewFilterValues
                        .Where(view => view.idBecaView == idView)
                        .ToList();

            view.BecaViewData = cols;
            view.BecaViewFilters = vFilters;
            view.BecaViewFilterValues = vFilterVals;

            List<BecaViewPanels> panels = dbBecaContext.BecaViewPanels
                    .Where(panel => panel.idBecaView == idView)
                    .Include(filters => filters.BecaPanelFilters)
                    .Include(formula => formula.IdFormulaNavigation)
                        .ThenInclude(data => data.BecaFormulaData)
                            .ThenInclude(dfilters => dfilters.BecaFormulaDataFilters)
                    .ToList();
            //List<BecaViewPanels> panels = dbdatiContext.BecaViewPanels
            //        .Where(panel => panel.idBecaView == idView)
            //            .Include(filters => filters.BecaPanelFilters)
            //            .Include(formula => formula.IdFormulaNavigation)
            //        .ToList();
            //foreach(BecaViewPanels panel in panels)
            //{

            //}
            //List<BecaFormulaDataFilters> dFilters = dbdatiContext.BecaFormulaDataFilters
            //            .Where(view => view.idBecaView == idView)
            //            .ToList();


            //            .ThenInclude(data => data.BecaFormulaData)
            //                .ThenInclude(dfilters => dfilters.BecaFormulaDataFilters)
            //        .ToList();
            view.BecaViewPanels = panels;

            return view;
        }

        private void SetCustomizedCols(int idView, ref List<BecaViewData> cols)
        {
            int idUtente = CurrentUser().idUtente;
            int idCompany = CurrentCompany().idCompany;
            List<BecaViewDataUser> customCols = dbBecaContext.BecaViewDataUser
                        .Where(view => view.idBecaView == idView &&
                            view.idUtente == idUtente &&
                            view.idCompany == idCompany)
                        .ToList();
            foreach (BecaViewDataUser customCol in customCols)
            {
                BecaViewData col = cols.FirstOrDefault(c => c.Field == customCol.field);
                if (col != null) col.isGridVisible = customCol.isGridVisible;
            }
        }

        public UIform GetViewUI(int idView, string tipoUI)
        {
            switch (tipoUI)
            {
                case "F":
                    List<BecaViewFilterUI> filterUI = dbBecaContext.BecaViewFilterUI
                            .Where(obj => obj.idBecaView == idView)
                            .OrderBy(obj => obj.Row)
                            .ThenBy(obj => obj.Col)
                            .ThenBy(obj => obj.SubRow)
                            .ThenBy(obj => obj.SubCol)
                            .ToList();
                    UIform viewFilterUI = this.CreateFilterUI(this._mapper.Map<List<BecaViewFilterUI>, List<BecaViewUI>>(filterUI));
                    return viewFilterUI;
                case "D":
                    List<BecaViewDetailUI> detailUI = dbBecaContext.BecaViewDetailUI
                            .Where(obj => obj.idBecaView == idView)
                            .OrderBy(obj => obj.Row)
                            .ThenBy(obj => obj.Col)
                            .ThenBy(obj => obj.SubRow)
                            .ThenBy(obj => obj.SubCol)
                            .ToList();
                    UIform viewDetailUI = this.CreateFilterUI(this._mapper.Map<List<BecaViewDetailUI>, List<BecaViewUI>>(detailUI));
                    return viewDetailUI;
                default: return null;
            }
        }

        private UIform CreateFilterUI(List<BecaViewUI> items)
        {
            if (items.Count == 0) return null;

            UIform filter = new UIform(items[0].ViewName);
            foreach (BecaViewUI BecaCfgFormField in items)
            {
                if (BecaCfgFormField.Row > 0 && BecaCfgFormField.Col > 0)
                {
                    FieldConfig field = new FieldConfig();
                    field.label = BecaCfgFormField.Title;
                    field.name = BecaCfgFormField.Name.ToCamelCase();
                    field.placeholder = BecaCfgFormField.HelpShort;
                    field.fieldType = BecaCfgFormField.FieldType;
                    field.inputType = BecaCfgFormField.FieldInput;
                    field.format = BecaCfgFormField.Format;
                    field.reference = BecaCfgFormField.Filter_Reference;
                    field.filter_API = BecaCfgFormField.Filter_API;
                    string opts = BecaCfgFormField.Filter_options ?? "";
                    if (opts.StartsWith("[") && opts.EndsWith("]"))
                    {
                        field.options = opts.Replace("[", "").Replace("]", "").Replace("'", "").Replace(" ", "").Split(",").ToArray();
                    }
                    field.optionDisplayed = BecaCfgFormField.DropDownDisplayField.ToCamelCase();
                    if (field.optionDisplayed != null) field.optionDisplayed = field.optionDisplayed.ToLowerToCamelCase();
                    //field.DropDownList = BecaCfgFormField.DropDownList;
                    field.DropDownKeyFields = BecaCfgFormField.DropDownKeyFields.ToCamelCase();
                    field.row = BecaCfgFormField.Row;
                    field.col = BecaCfgFormField.Col;
                    field.subRow = BecaCfgFormField.SubRow;
                    field.subCol = BecaCfgFormField.SubCol;
                    field.ColSize = BecaCfgFormField.ColSize;
                    field.SubColSize = BecaCfgFormField.SubColSize;

                    filter.fields.Add(field);
                    //UIrow row = filter.rows.GetRow(BecaCfgFormField.Row, true);
                    //if (BecaCfgFormField.SubRow > 0 && BecaCfgFormField.SubCol > 0)
                    //{
                    //    UIcol col = row.GetCol(BecaCfgFormField.Col, true);
                    //    col.size = BecaCfgFormField.ColSize;

                    //    if (col.rows == null) col.rows = new UIrows();
                    //    UIrow subRow = col.rows.GetRow(BecaCfgFormField.SubRow, true);
                    //    UIcol subCol = new UIcol();
                    //    subCol.num = BecaCfgFormField.Col;
                    //    subCol.size = BecaCfgFormField.SubColSize;
                    //    subCol.content = field;
                    //    subRow.cols.Add(subCol);
                    //}
                    //else
                    //{
                    //    UIcol col = new UIcol();
                    //    col.num = BecaCfgFormField.Col;
                    //    col.size = BecaCfgFormField.ColSize;
                    //    col.content = field;

                    //    row.AddCol(col);
                    //}
                }
            }
            return filter;
        }

        public bool CustomizeColumnsByUser(int idView, List<dtoBecaData> cols)
        {
            List<BecaViewDataUser> customCols = new List<BecaViewDataUser>();
            List<BecaViewData> viewCols = dbBecaContext.BecaViewData
                        .Where(view => view.idBecaView == idView)
                        .ToList();
            foreach (dtoBecaData col in cols)
            {
                if (col.isGridVisible)
                {
                    BecaViewDataUser customCol = new BecaViewDataUser();
                    customCol.idBecaView = idView;
                    foreach (BecaViewData viewCol in viewCols)
                    {
                        if (viewCol.Field.ToLower() == col.Name.ToLower())
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
                List<BecaViewDataUser> userData = dbBecaContext.BecaViewDataUser
                            .Where(view => view.idBecaView == idView &&
                                view.idUtente == CurrentUser().idUtente &&
                                view.idCompany == CurrentCompany().idCompany)
                            .ToList();
                dbBecaContext.RemoveRange(userData);
                dbBecaContext.SaveChanges();
            }
            catch (Exception ex)
            {
            }
            try
            {
                dbBecaContext.AddRange(customCols.FindAll(col => col.isGridVisible == true));
                dbBecaContext.SaveChanges();
            }
            catch (Exception ex)
            {
            }
            return true;
        }
    }
}
