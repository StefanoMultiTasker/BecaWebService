using Contracts;
using Entities.Contexts;
using Entities.Models;
using BecaWebService.ExtensionsLib;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace Repository
{
    public class BecaViewRepository : RepositoryBase<BecaView>, IBecaViewRepository
    {
        public BecaViewRepository(DbdatiContext dbdatiContext)
               : base(dbdatiContext)
        {
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

            return dbdatiContext.BecaView
                .Include(panels => panels.BecaViewPanels)
                    .ThenInclude(filters => filters.BecaPanelFilters)
                .Include(panels => panels.BecaViewPanels)
                    .ThenInclude(formula => formula.IdFormulaNavigation)
                    .ThenInclude(data => data.BecaFormulaData)
                    .ThenInclude(filters => filters.BecaFormulaDataFilters)
                .Include(data => data.BecaViewData)
                .Include(filters => filters.BecaViewFilters)
                .Include(filterValues => filterValues.BecaViewFilterValues)
                .SingleOrDefault(view => view.idBecaView == idView);
        }

        public UIform GetViewFilterUI(int idView)
        {
            List<BecaViewFilterUI> filterUI = dbdatiContext.BecaViewFilterUI
                    .Where(obj => obj.idBecaView == idView)
                    .OrderBy(obj => obj.Filter_Row)
                    .ThenBy(obj => obj.Filter_Col)
                    .ThenBy(obj => obj.Filter_SubRow)
                    .ThenBy(obj => obj.Filter_SubCol)
                    .ToList();
            UIform viewFilterUI = this.CreateFilterUI(filterUI);

            return viewFilterUI;
        }

        private UIform CreateFilterUI(List<BecaViewFilterUI> items)
        {
            if (items.Count == 0) return null;

            UIform filter = new UIform(items[0].ViewName);
            foreach (BecaViewFilterUI BecaCfgFormField in items)
            {
                if (BecaCfgFormField.Filter_Row > 0 && BecaCfgFormField.Filter_Col > 0)
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
                        field.options = opts.Replace("[", "").Replace("]", "").Split(",").ToArray();
                    }
                    field.optionDisplayed = BecaCfgFormField.DropDownDisplayField;
                    if(field.optionDisplayed != null) field.optionDisplayed = field.optionDisplayed.ToCamelCase();
                    field.DropDownList = BecaCfgFormField.DropDownList;

                    UIrow row = filter.rows.GetRow(BecaCfgFormField.Filter_Row, true);
                    if (BecaCfgFormField.Filter_SubRow > 0 && BecaCfgFormField.Filter_SubCol > 0)
                    {
                        UIcol col = row.GetCol(BecaCfgFormField.Filter_Col, true);
                        col.size = BecaCfgFormField.Filter_Size;

                        if (col.rows == null) col.rows = new UIrows();
                        UIrow subRow = col.rows.GetRow(BecaCfgFormField.Filter_SubRow, true);
                        UIcol subCol = new UIcol();
                        subCol.num = BecaCfgFormField.Filter_Col;
                        subCol.size = BecaCfgFormField.Filter_Size;
                        subCol.content = field;
                        subRow.cols.Add(subCol);
                    }
                    else
                    {
                        UIcol col = new UIcol();
                        col.num = BecaCfgFormField.Filter_Col;
                        col.size = BecaCfgFormField.Filter_Size;
                        col.content = field;

                        row.cols.Add(col);
                    }
                }
            }
            return filter;
        }
    }
}
