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

namespace Repository
{
    public class BecaViewRepository : RepositoryBase<BecaView>, IBecaViewRepository
    {
        private readonly IMapper _mapper;

        public BecaViewRepository(DbdatiContext dbdatiContext, IMapper mapper)
               : base(dbdatiContext)
        {
            _mapper = mapper;
        }

        //public async Task<BecaView> GetViewByID(int idView)
        public BecaView GetViewByID(int idView)
        {
            BecaView view = dbdatiContext.BecaView
                        .SingleOrDefault(view => view.idBecaView == idView);
            //.Include(data => data.BecaViewData)
            //.Include(filters => filters.BecaViewFilters)
            //.Include(filterValues => filterValues.BecaViewFilterValues)

            List<BecaViewData> cols = dbdatiContext.BecaViewData
                        .Where(view => view.idBecaView == idView)
                        .ToList();
            this.SetCustomizedCols(idView, ref cols);
            List<BecaViewFilters> vFilters = dbdatiContext.BecaViewFilters
                        .Where(view => view.idBecaView == idView)
                        .ToList();
            List<BecaViewFilterValues> vFilterVals = dbdatiContext.BecaViewFilterValues
                        .Where(view => view.idBecaView == idView)
                        .ToList();

            view.BecaViewData = cols;
            view.BecaViewFilters = vFilters;
            view.BecaViewFilterValues = vFilterVals;

            List<BecaViewPanels> panels = dbdatiContext.BecaViewPanels
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
            if (dbdatiContext.domain == null) return;
            int idUtente = dbdatiContext.idUtente;
            string domain = dbdatiContext.domain.ToLower();
            List<BecaViewDataUser> customCols = dbdatiContext.BecaViewDataUser
                        .Where(view => view.idBecaView == idView &&
                            view.idUtente == idUtente &&
                            view.Domain == domain)
                        .ToList();
            foreach (BecaViewDataUser customCol in customCols)
            {
                foreach (BecaViewData col in cols)
                {
                    if (col.idDataDefinition == customCol.idDataDefinition)
                    {
                        col.isGridVisible = customCol.isGridVisible;
                        break;
                    }
                }
            }
        }

        public UIform GetViewUI(int idView, string tipoUI)
        {
            switch (tipoUI)
            {
                case "F":
                    List<BecaViewFilterUI> filterUI = dbdatiContext.BecaViewFilterUI
                            .Where(obj => obj.idBecaView == idView)
                            .OrderBy(obj => obj.Row)
                            .ThenBy(obj => obj.Col)
                            .ThenBy(obj => obj.SubRow)
                            .ThenBy(obj => obj.SubCol)
                            .ToList();
                    UIform viewFilterUI = this.CreateFilterUI(this._mapper.Map<List<BecaViewFilterUI>, List<BecaViewUI>>(filterUI));
                    return viewFilterUI;
                case "D":
                    List<BecaViewDetailUI> detailUI = dbdatiContext.BecaViewDetailUI
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
                    field.name = BecaCfgFormField.Name;
                    field.placeholder = BecaCfgFormField.HelpShort;
                    field.type = BecaCfgFormField.FieldType;
                    field.inputType = BecaCfgFormField.FieldInput;
                    field.format = BecaCfgFormField.Format;
                    string opts = BecaCfgFormField.Filter_options ?? "";
                    if (opts.StartsWith("[") && opts.EndsWith("]"))
                    {
                        field.options = opts.Replace("[", "").Replace("]", "").Replace("'", "").Replace(" ", "").Split(",").ToArray();
                    }
                    field.optionDisplayed = BecaCfgFormField.DropDownDisplayField;
                    if (field.optionDisplayed != null) field.optionDisplayed = field.optionDisplayed.ToCamelCase();
                    field.DropDownList = BecaCfgFormField.DropDownList;

                    UIrow row = filter.rows.GetRow(BecaCfgFormField.Row, true);
                    if (BecaCfgFormField.SubRow > 0 && BecaCfgFormField.SubCol > 0)
                    {
                        UIcol col = row.GetCol(BecaCfgFormField.Col, true);
                        col.size = BecaCfgFormField.ColSize;

                        if (col.rows == null) col.rows = new UIrows();
                        UIrow subRow = col.rows.GetRow(BecaCfgFormField.SubRow, true);
                        UIcol subCol = new UIcol();
                        subCol.num = BecaCfgFormField.Col;
                        subCol.size = BecaCfgFormField.SubColSize;
                        subCol.content = field;
                        subRow.cols.Add(subCol);
                    }
                    else
                    {
                        UIcol col = new UIcol();
                        col.num = BecaCfgFormField.Col;
                        col.size = BecaCfgFormField.ColSize;
                        col.content = field;

                        row.cols.Add(col);
                    }
                }
            }
            return filter;
        }

        public bool CustomizeColumnsByUser(int idView, List<dtoBecaData> cols)
        {
            List<BecaViewDataUser> customCols = new List<BecaViewDataUser>();
            List<BecaViewData> viewCols = dbdatiContext.BecaViewData
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
                        if (viewCol.Name.ToLower() == col.Name.ToLower())
                        {
                            customCol.idDataDefinition = viewCol.idDataDefinition;
                            break;
                        }
                    }
                    customCol.Domain = dbdatiContext.domain;
                    customCol.idUtente = dbdatiContext.idUtente;
                    customCol.isGridVisible = col.isGridVisible;
                    customCols.Add(customCol);
                }
            }
            try
            {
                int idUtente = dbdatiContext.idUtente;
                string domain = dbdatiContext.domain.ToLower();
                List<BecaViewDataUser> userData = dbdatiContext.BecaViewDataUser
                            .Where(view => view.idBecaView == idView &&
                                view.idUtente == idUtente &&
                                view.Domain == domain)
                            .ToList();
                dbdatiContext.RemoveRange(userData);
                dbdatiContext.SaveChanges();
            }
            catch (Exception ex)
            {
            }
            try
            {
                dbdatiContext.AddRange(customCols.FindAll(col => col.isGridVisible == true));
                dbdatiContext.SaveChanges();
            }
            catch (Exception ex)
            {
            }
            return true;
        }
    }
}
