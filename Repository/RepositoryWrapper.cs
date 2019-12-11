using Contracts;
using Entities.Contexts;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Repository
{
    public class RepositoryWrapper : IRepositoryWrapper
    {
        private DbdatiContext _repoContext;
        private IdbaFunzioniAreeRepository _Area;
        private IdbaFunzioniGruppiRepository _Gruppo;
        private IdbaFunzioniRepository _Voce;
        private IdbaFunzioniCfgRepository _VoceConfig;
        private IMenuLivelloRepository _MenuLivello;

        public IdbaFunzioniAreeRepository Area
        {
            get
            {
                if (_Area == null)
                {
                    _Area = new DbaFunzioniAreeRepository(_repoContext);
                }
                return _Area;
            }
        }

        public IdbaFunzioniGruppiRepository Gruppo
        {
            get
            {
                if (_Gruppo == null)
                {
                    _Gruppo = new DbaFunzioniGruppiRepository(_repoContext);
                }
                return _Gruppo;
            }
        }

        public IdbaFunzioniRepository Voce
        {
            get
            {
                if (_Voce == null)
                {
                    _Voce = new DbaFunzioniRepository(_repoContext);
                }
                return _Voce;
            }
        }

        public IdbaFunzioniCfgRepository VoceConfig
        {
            get
            {
                if (_VoceConfig == null)
                {
                    _VoceConfig = new DbaFunzioniCfgRepository(_repoContext);
                }
                return _VoceConfig;
            }
        }

        public IMenuLivelloRepository MenuLivello
        {
            get
            {
                if (_MenuLivello == null)
                {
                    _MenuLivello = new MenuLivelloRepository(_repoContext);
                }
                return _MenuLivello;
            }
        }

        public RepositoryWrapper(DbdatiContext repositoryContext)
        {
            _repoContext = repositoryContext;
        }

        public async Task SaveAsync()
        {
            await _repoContext.SaveChangesAsync();
        }
    }
}
