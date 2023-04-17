using Microsoft.Extensions.Caching.Memory;

namespace Entities
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
