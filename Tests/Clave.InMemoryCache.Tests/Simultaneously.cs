using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Clave.InMemoryCache.Tests
{
    public static class Simultaneously
    {
        public static Task<T[]> Run<T>(int count, Func<T> func) => Task.WhenAll(Enumerable.Repeat(true, count).AsParallel().Select(x => Task.Run(func)));

        public static Task<T[]> Run<T>(int count, Func<Task<T>> func) => Task.WhenAll(Enumerable.Repeat(true, count).AsParallel().Select(x => func()));
    }
}