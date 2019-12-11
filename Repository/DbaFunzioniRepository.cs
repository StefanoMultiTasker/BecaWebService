using Contracts;
using Entities.Contexts;
using Entities.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Repository
{
    class DbaFunzioniAreeRepository : RepositoryBase<DbaFunzioniAree>, IdbaFunzioniAreeRepository
    {
        public DbaFunzioniAreeRepository(DbdatiContext dbdatiContext)
            : base(dbdatiContext)
        {
        }
    }

    class DbaFunzioniGruppiRepository : RepositoryBase<DbaFunzioniGruppi>, IdbaFunzioniGruppiRepository
    {
        public DbaFunzioniGruppiRepository(DbdatiContext dbdatiContext)
            : base(dbdatiContext)
        {
        }
    }

    class DbaFunzioniRepository : RepositoryBase<DbaFunzioni>, IdbaFunzioniRepository
    {
        public DbaFunzioniRepository(DbdatiContext dbdatiContext)
            : base(dbdatiContext)
        {
        }
    }

    class DbaFunzioniCfgRepository : RepositoryBase<DbaFunzioniCfg>, IdbaFunzioniCfgRepository
    {
        public DbaFunzioniCfgRepository(DbdatiContext dbdatiContext)
            : base(dbdatiContext)
        {
        }
    }
}
