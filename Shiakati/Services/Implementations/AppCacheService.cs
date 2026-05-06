using System.Collections.Concurrent;
using Shiakati.Services.Interfaces;

namespace Shiakati.Services.Implementations
{
    public class AppCacheService : ICacheService
    {
        private readonly ConcurrentDictionary<string, object> _cache = new();
        public void Set<T>(string key,T item, TimeSpan? expiration = null)
        {
            _cache.AddOrUpdate(key, item, (k, existingVal) => item);

        }
        public T Get<T>(string key)
        {
            if (_cache.TryGetValue(key, out var item))
            {
                return (T)item;
            }
            // Retourne null (ou la valeur par défaut) si la clé n'existe pas
            return default;
        }
        public bool Contains(string key) => _cache.ContainsKey(key);

        public void Remove(string key) => _cache.TryRemove(key, out _);

        public void Clear() => _cache.Clear();
    }
}
