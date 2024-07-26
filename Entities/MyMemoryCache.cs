using Microsoft.Extensions.Caching.Memory;

namespace Entities
{
    public interface IMyMemoryCache
    {
        public MemoryCache Cache { get; set; }
        public T GetOrSetCache<T>(string cacheKey, Func<T> getItemCallback) where T : class;
    }

    public class MyMemoryCache : IMyMemoryCache
    {
        public MemoryCache Cache { get; set; }

        public MyMemoryCache()
        {
            Cache = new MemoryCache(new MemoryCacheOptions() { });
        }

        public T GetOrSetCache<T>(string cacheKey, Func<T> getItemCallback) where T : class
        {
            if (!Cache.TryGetValue(cacheKey, out T item))
            {
                item = getItemCallback();
                Cache.Set(cacheKey, item, TimeSpan.FromMinutes(30)); // Configura il tempo di scadenza come desiderato
            }
            return item;
        }
    }
}
