using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Services.Interfaces
{
    public interface ICacheService
    {
        void Set<T>(string key, T value, TimeSpan? expiration = null);
        T Get<T>(string key);
        bool Contains(string key);
        void Remove(string key);
        void Clear();
        Task<T> GetOrLoadAsync<T>(string key, Func<Task<T>> loadFromDbFunc);
    }
}
