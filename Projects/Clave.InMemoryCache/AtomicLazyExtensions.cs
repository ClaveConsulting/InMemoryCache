using System.Threading.Tasks;

namespace Clave.InMemoryCache
{
    public static class AtomicLazyExtensions
    {
        public static AtomicLazy<Task<T>> ToAsync<T>(this AtomicLazy<T> lazy)
        {
            return lazy == null ? null : new AtomicLazy<Task<T>>(() => Task.FromResult(lazy.Value));
        }

        public static AtomicLazy<T> FromAsync<T>(this AtomicLazy<Task<T>> asyncLazy)
        {
            return asyncLazy == null ? null : new AtomicLazy<T>(() => Task.Run(() => asyncLazy.Value).GetAwaiter().GetResult());
        }
    }
}