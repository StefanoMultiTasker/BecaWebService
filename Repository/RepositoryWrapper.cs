using AutoMapper;
using Contracts;
using Entities;
using Entities.Contexts;
using Microsoft.AspNetCore.Http;

namespace Repository
{
    public class RepositoryWrapper : IRepositoryWrapper
    {
        private IDependencies _deps;
        private DbBecaContext _repoContext;
        private IBecaViewRepository _BecaView;
        private readonly IMapper _mapper;
        private HttpContext _httpContext;

        //public IBecaViewRepository BecaView
        //{
        //    get
        //    {
        //        if (_BecaView == null)
        //        {
        //            _BecaView = new BecaViewRepository(_deps, _httpContext);
        //        }
        //        return _BecaView;
        //    }
        //}

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
