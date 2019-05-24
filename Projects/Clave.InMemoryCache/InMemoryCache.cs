namespace Clave.InMemoryCache
{
    using System;
    using System.Threading.Tasks;
  using Microsoft.Extensions.Caching.Memory;

  public class InMemoryCache : IInMemoryCache
    {
        private readonly Action<Func<Task>> _doInBackground;

        private readonly IMemoryCache _objectCache;

        public InMemoryCache()
            : this(func => Task.Run(func))
        {
        }

        public InMemoryCache(IMemoryCache objectCache)
            : this(objectCache, func => Task.Run(func))
        {
        }

        public InMemoryCache(Action<Func<Task>> doInBackground)
            : this(new MemoryCache(new MemoryCacheOptions()), doInBackground)
        {
        }

        public InMemoryCache(IMemoryCache objectCache, Action<Func<Task>> doInBackground)
        {
            _doInBackground = doInBackground;
            _objectCache = objectCache;
        }

        public T TryGetOrAdd<T>(
            string key,
            TimeSpan validFor,
            Func<T> valueFactory)
            where T : class
        {
            return TryGetOrAdd(key, validFor, valueFactory, 1);
        }

        public T TryGetOrAdd<T>(
            string key,
            TimeSpan validFor,
            Func<T> valueFactory,
            double secondsBeforeExpirationTimeToRevalidate)
            where T : class
        {
            var expirationTime = validFor.GetDateTimeOffset();
            var newValue = new CacheItem<T>(valueFactory, expirationTime);
            var oldValue = AddOrGetExisting(key, newValue, expirationTime);

            if (oldValue.IsMissing())
            {
                return WaitForFactory(key, newValue);
            }

            if (oldValue.WillExpireWithin(secondsBeforeExpirationTimeToRevalidate))
            {
                _doInBackground(() =>
                {
                    try
                    {
                        WaitForFactoryThenOverwriteCache(key, newValue, expirationTime);
                        return Task.FromResult(true);
                    }
                    catch
                    {
                        _objectCache.Set(key, oldValue.Bump(expirationTime), expirationTime);
                        throw;
                    }
                });
            }

            return WaitForFactory(key, oldValue);
        }

        public Task<T> TryGetOrAdd<T>(
            string key,
            TimeSpan validFor,
            Func<Task<T>> valueFactory)
            where T : class
        {
            return TryGetOrAdd(key, validFor, valueFactory, 1);
        }

        public async Task<T> TryGetOrAdd<T>(
            string key,
            TimeSpan validFor,
            Func<Task<T>> valueFactory,
            double secondsBeforeExpirationTimeToRevalidate)
            where T : class
        {
            var expirationTime = validFor.GetDateTimeOffset();
            var newValue = new CacheItem<T>(valueFactory, expirationTime);
            var oldValue = AddOrGetExisting(key, newValue, expirationTime);

            if (oldValue.IsMissing())
            {
                return await WaitForFactoryAsync(key, newValue).ConfigureAwait(false);
            }

            if (oldValue.WillExpireWithin(secondsBeforeExpirationTimeToRevalidate))
            {
                _doInBackground(async () =>
                {
                    try
                    {
                        await WaitForFactoryThenOverwriteCacheAsync(key, newValue, expirationTime).ConfigureAwait(false);
                    }
                    catch
                    {
                        _objectCache.Set(key, oldValue.Bump(expirationTime), expirationTime);
                        throw;
                    }
                });
            }

            return await WaitForFactoryAsync(key, oldValue).ConfigureAwait(false);
        }

        public void Store<T>(string key, T value, TimeSpan validFor)
            where T : class
        {
            if (value != default(T))
            {
                var expirationTime = validFor.GetDateTimeOffset();
                var newValue = new CacheItem<T>(() => value, expirationTime);

                _objectCache.Set(key, newValue, expirationTime);
            }
        }

        public void Remove(string key)
        {
            _objectCache.Remove(key);
        }

        public T Get<T>(string key)
            where T : class
        {
            var fullKey = key;

            var value = _objectCache.Get(fullKey) as CacheItem<T>;

            if (value == null)
            {
                return default(T);
            }

            try
            {
                return value.Value;
            }
            catch
            {
                _objectCache.Remove(fullKey);
                throw;
            }
        }

        private CacheItem<T> AddOrGetExisting<T>(string key, CacheItem<T> item, DateTimeOffset expirationTime)
            where T : class
        {
            if(_objectCache.TryGetValue(key, out var existingItem)
               && existingItem is CacheItem<T> result
               && !result.HasExpired())
            {
                return result;
            }

            _objectCache.Set(key, item);

            return item;
        }

        private T WaitForFactory<T>(string key, CacheItem<T> lazyValue)
            where T : class
        {
            try
            {
                var value = lazyValue.Value;
                if (value == null)
                {
                    _objectCache.Remove(key);
                }

                return value;
            }
            catch
            {
                _objectCache.Remove(key);
                throw;
            }
        }

        private async Task<T> WaitForFactoryAsync<T>(string key, CacheItem<T> lazyValue)
            where T : class
        {
            try
            {
                var value = await lazyValue.ValueAsync.ConfigureAwait(false);
                if (value == null)
                {
                    _objectCache.Remove(key);
                }

                return value;
            }
            catch
            {
                _objectCache.Remove(key);
                throw;
            }
        }

        private void WaitForFactoryThenOverwriteCache<T>(string key, CacheItem<T> lazyValue, DateTimeOffset expirationTime)
            where T : class
        {
            var value = lazyValue.Value;
            if (value != null)
            {
                _objectCache.Set(key, lazyValue, expirationTime);
            }
        }

        private async Task WaitForFactoryThenOverwriteCacheAsync<T>(string key, CacheItem<T> lazyValue, DateTimeOffset expirationTime)
            where T : class
        {
            var value = await lazyValue.ValueAsync.ConfigureAwait(false);
            if (value != null)
            {
                _objectCache.Set(key, lazyValue, expirationTime);
            }
        }
    }
}