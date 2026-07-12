using System.Collections.Concurrent;
using Shiakati.Services.Interfaces;

namespace Shiakati.Services.Implementations
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    namespace Shiakati.Services.Implementations
    {
        public class AppCacheService : ICacheService
        {
            // ─── Internal wrapper to store value + expiry ──────────────────
            private class CacheEntry<T>
            {
                public Lazy<Task<T>> LazyValue { get; }
                public DateTime Expiry { get; }

                public CacheEntry(Lazy<Task<T>> lazyValue, DateTime expiry)
                {
                    LazyValue = lazyValue;
                    Expiry = expiry;
                }
            }

            // ─── ConcurrentDictionary ──────────────────────────────────────
            private readonly ConcurrentDictionary<string, object> _cache = new();

            // ─── GetOrLoad with optional expiration ──────────────────────
            public async Task<T> GetOrLoadAsync<T>(string key, Func<Task<T>> loadFromDbFunc, TimeSpan? expiration = null)
            {
                // Try to get existing entry
                if (_cache.TryGetValue(key, out var cachedItem))
                {
                    // If the item is a valid CacheEntry<T>, check expiry
                    if (cachedItem is CacheEntry<T> entry)
                    {
                        // If not expired, return the cached value
                        if (DateTime.UtcNow < entry.Expiry)
                        {
                            return await entry.LazyValue.Value;
                        }
                        else
                        {
                            // Expired → remove it
                            _cache.TryRemove(key, out _);
                        }
                    }
                    else
                    {
                        // In case of type mismatch (should not happen), remove and reload
                        _cache.TryRemove(key, out _);
                    }
                }

                // ─── Cache miss or expired ────────────────────────────────────
                // Calculate expiry: if expiration is provided, use it; otherwise, never expires (DateTime.MaxValue)
                var expiryTime = expiration.HasValue
                    ? DateTime.UtcNow.Add(expiration.Value)
                    : DateTime.MaxValue;

                // Create the Lazy<Task<T>> (ensures the load function runs only once per key, even under concurrent requests)
                var newLazy = new Lazy<Task<T>>(() => loadFromDbFunc());
                var newEntry = new CacheEntry<T>(newLazy, expiryTime);

                // Try to add atomically
                var added = _cache.TryAdd(key, newEntry);
                if (!added)
                {
                    // Another thread might have added it in the meantime → retrieve and return that one
                    if (_cache.TryGetValue(key, out var existing) && existing is CacheEntry<T> existingEntry)
                    {
                        return await existingEntry.LazyValue.Value;
                    }
                    // Fallback: just load again (should be extremely rare)
                    return await loadFromDbFunc();
                }

                // Return the newly loaded value
                return await newLazy.Value;
            }

            // ─── Set (with optional expiration) ──────────────────────────────
            public void Set<T>(string key, T item, TimeSpan? expiration = null)
            {
                var expiryTime = expiration.HasValue
                    ? DateTime.UtcNow.Add(expiration.Value)
                    : DateTime.MaxValue;

                var lazyItem = new Lazy<Task<T>>(() => Task.FromResult(item));
                var entry = new CacheEntry<T>(lazyItem, expiryTime);

                _cache.AddOrUpdate(key, entry, (k, existing) => entry);
            }

            // ─── Get (synchronous) ────────────────────────────────────────────
            public T Get<T>(string key)
            {
                if (_cache.TryGetValue(key, out var cachedItem) && cachedItem is CacheEntry<T> entry)
                {
                    if (DateTime.UtcNow < entry.Expiry && entry.LazyValue.IsValueCreated && entry.LazyValue.Value.IsCompletedSuccessfully)
                    {
                        return entry.LazyValue.Value.Result;
                    }
                }
                return default;
            }

            // ─── Contains ─────────────────────────────────────────────────────
            public bool Contains(string key)
            {
                if (_cache.TryGetValue(key, out var cachedItem) && cachedItem is CacheEntry<object> entry)
                {
                    return DateTime.UtcNow < entry.Expiry;
                }
                return false;
            }

            // ─── Remove ──────────────────────────────────────────────────────
            public void Remove(string key) => _cache.TryRemove(key, out _);

            // ─── Clear ───────────────────────────────────────────────────────
            public void Clear() => _cache.Clear();
        }
    }

    public static class CacheKeys
    {
        // ─── Reference Data (Static, rarely changes) ─────────────────────
        public const string Catalog = "CatalogData";
        public const string Products = "ProductsList";
        
        // ─── Transactional Data (Changes frequently) ─────────────────────
        public const string StockVariants = "StockVariantsList";
        public const string Clients = "ClientsList";
        public const string Suppliers = "SuppliersList";
        public const string Reservations = "ReservationsList";

        // ─── Stock & Dashboard ────────────────────────────────────────────
        public const string StockAlerts = "StockAlertsList";
        public const string DashboardStats = "DashboardStats";
    }
}