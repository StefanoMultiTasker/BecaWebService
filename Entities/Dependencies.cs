using AutoMapper;
using Entities;
using Entities.Contexts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Entities
{
    public interface IDependencies
    {
        IConfiguration configuration { get; }
        IMemoryCache memoryCache { get; }
        IMapper mapper { get; }
        DbdatiContext context { get; }
        DbMemoryContext memoryContext { get; }
        HttpContext httpContext { get; }
    }

    public class Dependencies : IDependencies
    {
        //private DbdatiContext _context;
        private readonly IConfiguration _config;
        private readonly IMemoryCache _cache;
        private readonly IMapper _mapper;
        private readonly DbdatiContext _context;
        private readonly DbMemoryContext _memoryContext;
        private readonly HttpContext _httpContext;

        public Dependencies(IConfiguration configuration, IMapper mapper, MyMemoryCache cache, 
            DbdatiContext context, DbMemoryContext memoryContext, HttpContext httpContext)
        {
            _cache = cache.Cache;
            //_context = context;
            _config = configuration;
            _mapper = mapper;
            _context = context;
            _memoryContext = memoryContext;
            _httpContext = httpContext;
        }

        public IConfiguration configuration => _config;

        public IMemoryCache memoryCache => _cache;

        public IMapper mapper => _mapper;

        public DbdatiContext context=> _context;

        public DbMemoryContext memoryContext => _memoryContext;

        public HttpContext httpContext => _httpContext;

    }
}
