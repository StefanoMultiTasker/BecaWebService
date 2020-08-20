using Contracts;
using Entities.Contexts;
using Entities.Models;
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
    }
}
