using Entities.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts
{
    public interface IdbaFunzioniAreeRepository : IRepositoryBase<DbaFunzioniAree>
    {
    }

    public interface IdbaFunzioniGruppiRepository : IRepositoryBase<DbaFunzioniGruppi>
    {
    }

    public interface IdbaFunzioniRepository : IRepositoryBase<DbaFunzioni>
    {
    }

    public interface IdbaFunzioniCfgRepository : IRepositoryBase<DbaFunzioniCfg>
    {
    }
}
