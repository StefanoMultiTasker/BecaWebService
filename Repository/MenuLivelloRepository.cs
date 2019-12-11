using Contracts;
using Entities.Contexts;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository
{
    public class MenuLivelloRepository : RepositoryBase<VMenuLivello>, IMenuLivelloRepository
    {
        public MenuLivelloRepository(DbdatiContext dbdatiContext)
               : base(dbdatiContext)
        {
        }

        public async Task<IEnumerable<VMenuLivello>> GetAllByLivello(int idLivello)
        {
            return await GetByCondition(menu => menu.IdLivello.Equals(idLivello))
                .OrderBy(m => m.AreaOrdine)
                .OrderBy(m => m.GruppoOrdine)
                .OrderBy(m => m.SottoGruppo)
                .OrderBy(m => m.Posizione)
                .ToListAsync();
        }
    }
}
