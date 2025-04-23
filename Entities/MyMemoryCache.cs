using Microsoft.Extensions.Caching.Memory;

namespace Entities
{
    public interface IMyMemoryCache
    {
        public MemoryCache Cache { get; set; }
        public T GetOrSetCache<T>(string cacheKey, Func<T> getItemCallback, double duration = 30) where T : class;
    }

    public class MyMemoryCache : IMyMemoryCache
    {
        public MemoryCache Cache { get; set; }

        public MyMemoryCache()
        {
            Cache = new MemoryCache(new MemoryCacheOptions() { });
        }

        public T GetOrSetCache<T>(string cacheKey, Func<T> getItemCallback, double duration = 30) where T : class
        {
            if (!Cache.TryGetValue(cacheKey, out T item))
            {
                item = getItemCallback();
                Cache.Set(cacheKey, item, TimeSpan.FromMinutes(duration)); // Configura il tempo di scadenza come desiderato
            }
            return item;
        }
    }
}
