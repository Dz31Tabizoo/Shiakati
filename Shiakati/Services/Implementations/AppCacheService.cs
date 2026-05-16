using System.Collections.Concurrent;
using Shiakati.Services.Interfaces;

namespace Shiakati.Services.Implementations
{
    public class AppCacheService : ICacheService
    {
        // On stocke des Lazy<Task> pour garantir qu'un appel DB n'est exécuté qu'une seule fois, 
        // même si plusieurs threads le demandent en même temps.
        private readonly ConcurrentDictionary<string, object> _cache = new();

        public async Task<T> GetOrLoadAsync<T>(string key, Func<Task<T>> loadFromDbFunc)
        {
            var lazyTask = (Lazy<Task<T>>)_cache.GetOrAdd(key, k =>
                new Lazy<Task<T>>(() => loadFromDbFunc()));

            try
            {
                return await lazyTask.Value;
            }
            catch
            {
                // Si la requête DB échoue, on retire l'élément du cache pour pouvoir réessayer plus tard
                _cache.TryRemove(key, out _);
                throw;
            }
        }

        public void Set<T>(string key, T item, TimeSpan? expiration = null)
        {
            // On l'enveloppe dans un Lazy<Task> pour être compatible avec GetOrLoadAsync
            var lazyItem = new Lazy<Task<T>>(() => Task.FromResult(item));
            _cache.AddOrUpdate(key, lazyItem, (k, existingVal) => lazyItem);
        }

        public T Get<T>(string key)
        {
            if (_cache.TryGetValue(key, out var lazyItem) && lazyItem is Lazy<Task<T>> typedLazy)
            {
                // Attention : Get synchrone ne doit être utilisé que si on est sûr que la donnée est déjà chargée
                if (typedLazy.IsValueCreated && typedLazy.Value.IsCompletedSuccessfully)
                    return typedLazy.Value.Result;
            }
            return default;
        }

        public bool Contains(string key) => _cache.ContainsKey(key);
        public void Remove(string key) => _cache.TryRemove(key, out _);
        public void Clear() => _cache.Clear();
    }

    public static class CacheKeys
    {
        public const string Catalog = "CatalogData";
        public const string Products = "ProductsList";
        public const string StockVariants = "StockVariantsList";
    }
}