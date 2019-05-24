namespace Clave.InMemoryCache.Tests
{
    using System;
    using System.Threading.Tasks;

    using NUnit.Framework;

    using Shouldly;

    [TestFixture]
    public class CacheItemTests
    {
        private static readonly DateTimeOffset Now = DateTimeOffset.Now;

        [Test]
        public void TestSyncInSyncOut()
        {
            var cacheItem = new CacheItem<string>(() => "hello", Now);
            cacheItem.Value.ShouldBe("hello");
        }

        [Test]
        public void TestAsyncInSyncOut()
        {
            var cacheItem = new CacheItem<string>(() => Task.FromResult("hello"), Now);
            cacheItem.Value.ShouldBe("hello");
        }

        [Test]
        public void TestAsyncInSyncOutMultiple()
        {
            var cacheItem = new CacheItem<string>(() => Task.FromResult("hello"), Now);
            cacheItem.Value.ShouldBe("hello");
            cacheItem.Value.ShouldBe("hello");
            cacheItem.Value.ShouldBe("hello");
        }

        [Test]
        public async Task TestSyncInAsyncOut()
        {
            var cacheItem = new CacheItem<string>(() => "hello", Now);
            var result = await cacheItem.ValueAsync;
            result.ShouldBe("hello");
        }

        [Test]
        public async Task TestSyncInAsyncOutMultiple()
        {
            var cacheItem = new CacheItem<string>(() => "hello", Now);
            var result = await cacheItem.ValueAsync;
            result.ShouldBe("hello");
            result.ShouldBe("hello");
            result.ShouldBe("hello");
        }

        [Test]
        public async Task TestAsyncInAsyncOut()
        {
            var cacheItem = new CacheItem<string>(() => Task.FromResult("hello"), Now);
            var result = await cacheItem.ValueAsync;
            result.ShouldBe("hello");
        }

        [Test]
        public void NotExpired()
        {
            var cacheItem = new CacheItem<string>(() => "hello", Now);
            cacheItem.HasExpired(Now).ShouldBeFalse();
        }

        [Test]
        public void HasExpired()
        {
            var cacheItem = new CacheItem<string>(() => "hello", Now);
            cacheItem.HasExpired(Now.AddSeconds(10)).ShouldBeTrue();
        }

        [Test]
        public void HasExpiredSecondTimeShouldBeFalse()
        {
            var cacheItem = new CacheItem<string>(() => "hello", Now);
            cacheItem.HasExpired(Now.AddSeconds(10)).ShouldBeTrue();
            cacheItem.HasExpired(Now.AddSeconds(10)).ShouldBeFalse();
        }
    }
}