namespace Clave.InMemoryCache
{
    using System;
    using System.Threading.Tasks;

    public interface IInMemoryCache
    {
        T TryGetOrAdd<T>(
            string key,
            TimeSpan validFor,
            Func<T> valueFactory)
            where T : class;

        T TryGetOrAdd<T>(
            string key,
            TimeSpan validFor,
            Func<T> valueFactory,
            double secondsBeforeExpirationTimeToRevalidate)
            where T : class;

        Task<T> TryGetOrAdd<T>(
            string key,
            TimeSpan validFor,
            Func<Task<T>> valueFactory)
            where T : class;

        Task<T> TryGetOrAdd<T>(
            string key,
            TimeSpan validFor,
            Func<Task<T>> valueFactory,
            double secondsBeforeExpirationTimeToRevalidate)
            where T : class;

        void Store<T>(string key, T value, TimeSpan validFor)
            where T : class;

        void Remove(string key);

        T Get<T>(string key)
            where T : class;
    }
}