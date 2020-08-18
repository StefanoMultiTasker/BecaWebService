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

        public async Task<BecaView> GetViewByID(int idView)
        {
            return await dbdatiContext.BecaView
                .Include(panels => panels.BecaViewPanels)
                    .ThenInclude(filters => filters.BecaPanelFilters)
                .Include(panels => panels.BecaViewPanels)
                    .ThenInclude(formula => formula.IdFormulaNavigation)
                    .ThenInclude(data => data.BecaFormulaData)
                    .ThenInclude(filters => filters.BecaFormulaDataFilters)
                .Include(data => data.BecaViewData)
                .Include(filters => filters.BecaViewFilters)
                .Include(filterValues => filterValues.BecaViewFilterValues)
                .SingleOrDefaultAsync(view => view.idBecaView == idView);
        }
    }
}
