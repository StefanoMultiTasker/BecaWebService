using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BecaWebService.Tools
{
    public interface IMyMemoryCache
    {
        public MemoryCache Cache { get; set; }
    }

    public class MyMemoryCache : IMyMemoryCache
    {
        public MemoryCache Cache { get; set; }

        public MyMemoryCache()
        {
            Cache = new MemoryCache(new MemoryCacheOptions() { });
        }
    }
}
