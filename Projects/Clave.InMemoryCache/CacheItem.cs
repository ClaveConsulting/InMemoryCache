namespace Clave.InMemoryCache
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class CacheItem<T>
        where T : class
    {
        private const int Valid = 0;

        private const int Stale = 1;

        private readonly DateTimeOffset _expirationTime;

        private AtomicLazy<T> _lazy;

        private AtomicLazy<Task<T>> _asyncLazy;

        private int _state = Valid;

        public CacheItem(Func<T> factory, DateTimeOffset expirationTime)
        {
            _lazy = new AtomicLazy<T>(factory);
            _expirationTime = expirationTime;
        }

        public CacheItem(Func<Task<T>> factory, DateTimeOffset expirationTime)
        {
            _asyncLazy = new AtomicLazy<Task<T>>(factory);
            _expirationTime = expirationTime;
        }

        public T Value => (_lazy ?? (_lazy = _asyncLazy.FromAsync())).Value;

        public Task<T> ValueAsync => (_asyncLazy ?? (_asyncLazy = _lazy.ToAsync())).Value;

        public bool HasExpired(DateTimeOffset atTime)
        {
            return atTime > _expirationTime && Interlocked.CompareExchange(ref _state, Stale, Valid) == Valid;
        }
    }
}