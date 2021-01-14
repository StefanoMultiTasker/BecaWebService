using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    public interface IRepositoryWrapper
    {
        IdbaFunzioniAreeRepository Area { get; }
        IdbaFunzioniGruppiRepository Gruppo { get; }
        IdbaFunzioniRepository Voce { get; }
        IdbaFunzioniCfgRepository VoceConfig { get; }
        IMenuLivelloRepository MenuLivello { get; }
        IBecaViewRepository BecaView { get;  }
        void ReadToken(string token);
        Task SaveAsync();
    }
}
