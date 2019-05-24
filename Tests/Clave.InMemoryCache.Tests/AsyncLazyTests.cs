namespace Clave.InMemoryCache.Tests
{
    using System;
    using System.Threading.Tasks;

    using NUnit.Framework;

    using Shouldly;

    [TestFixture]
    public class AsyncLazyTests
    {
        [Test]
        public async Task LazyToAsyncLazy()
        {
            var lazy = new AtomicLazy<string>(() => "hello");
            var result = await lazy.ToAsync().Value;
            result.ShouldBe("hello");
        }

        [Test]
        public void AsyncLazyToLazy()
        {
            var lazy = new AtomicLazy<Task<string>>(() => Task.FromResult("hello"));
            var result = lazy.FromAsync().Value;
            result.ShouldBe("hello");
        }
    }
}