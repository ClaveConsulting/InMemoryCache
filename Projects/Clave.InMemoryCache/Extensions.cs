namespace Clave.InMemoryCache
{
    using System;

    internal static class Extensions
    {
        internal static CacheItem<T> Bump<T>(this CacheItem<T> oldValue, DateTimeOffset expirationTime)
            where T : class
        {
            return new CacheItem<T>(() => oldValue.Value, expirationTime);
        }

        internal static bool IsMissing<T>(this CacheItem<T> oldValue)
            where T : class
        {
            return oldValue == null;
        }

        internal static DateTimeOffset GetDateTimeOffset(this TimeSpan validFor)
        {
            return GetNow().AddMilliseconds(validFor.TotalMilliseconds);
        }

        internal static bool HasExpired<T>(this CacheItem<T> oldValue)
            where T : class
        {
            return oldValue.HasExpired(GetNow());
        }

        internal static bool WillExpireWithin<T>(this CacheItem<T> oldValue, double seconds)
            where T : class
        {
            return oldValue.HasExpired(MoveIntoTheFuture(seconds));
        }

        internal static DateTimeOffset MoveIntoTheFuture(double seconds)
        {
            return GetNow().AddSeconds(seconds);
        }

        internal static DateTimeOffset GetNow()
        {
            return DateTimeOffset.UtcNow;
        }
    }
}