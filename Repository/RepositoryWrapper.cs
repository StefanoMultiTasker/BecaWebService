using AutoMapper;
using Contracts;
using Entities;
using Entities.Contexts;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository
{
    public class RepositoryWrapper : IRepositoryWrapper
    {
        private IDependencies _deps;
        private DbBecaContext _repoContext;
        private IBecaViewRepository _BecaView;
        private readonly IMapper _mapper;

        public IBecaViewRepository BecaView
        {
            get
            {
                if (_BecaView == null)
                {
                    _BecaView = new BecaViewRepository(_deps);
                }
                return _BecaView;
            }
        }

        public RepositoryWrapper(IDependencies deps)
        {
            _deps = deps;
            _repoContext = _deps.context;
        }

        public async Task SaveAsync()
        {
            await _repoContext.SaveChangesAsync();
        }

    }
}
