using AutoMapper;
using Entities.Contexts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace Entities
{
    public interface IDependencies
    {
        IConfiguration configuration { get; }
        IMemoryCache memoryCache { get; }
        IMapper mapper { get; }
        DbBecaContext context { get; }
        DbMemoryContext memoryContext { get; }
        FormTool formTool { get; }
        //HttpContext httpContext { get; }
    }

    public class Dependencies : IDependencies
    {
        //private DbdatiContext _context;
        private readonly IConfiguration _config;
        private readonly IMemoryCache _cache;
        private readonly IMapper _mapper;
        private readonly DbBecaContext _context;
        private readonly DbMemoryContext _memoryContext;
        private readonly FormTool _formTool;
        //private readonly HttpContext _httpContext;

        public Dependencies(IConfiguration configuration, IMapper mapper, MyMemoryCache cache,
            DbBecaContext context, DbMemoryContext memoryContext, FormTool formTool)
        {
            _cache = cache.Cache;
            //_context = context;
            _config = configuration;
            _mapper = mapper;
            _context = context;
            _memoryContext = memoryContext;
            _formTool = formTool;
        }

        public IConfiguration configuration => _config;

        public IMemoryCache memoryCache => _cache;

        public IMapper mapper => _mapper;

        public DbBecaContext context => _context;

        public DbMemoryContext memoryContext => _memoryContext;

        public FormTool formTool => _formTool;

    }
}
