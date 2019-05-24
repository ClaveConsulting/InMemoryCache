using Microsoft.Extensions.Caching.Memory;

namespace Clave.InMemoryCache.Tests
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using NSubstitute;
    using NSubstitute.ExceptionExtensions;

    using NUnit.Framework;

    using Shouldly;

    public class InMemoryCacheTests
    {
        private const string Result = "result";

        private const string Key = " cacheKey ";

        private const string Result1 = "result1";

        private const string Result2 = "result2";

        private IInMemoryCache _inMemoryCache;
        private TimeSpan _cacheTimeSpan;
        private TimeSpan _staleTimeSpan;
        private TimeSpan _expiredTimeSpan;

        [SetUp]
        public void SetUp()
        {
            _inMemoryCache = new InMemoryCache();
            _cacheTimeSpan = TimeSpan.FromSeconds(2);
            _staleTimeSpan = TimeSpan.FromSeconds(1.5);
            _expiredTimeSpan = TimeSpan.FromSeconds(3);
        }

        [Test]
        public void Get_NoDataInCache_ShouldReturn_Null()
        {
            var result = _inMemoryCache.Get<string>(Key);
            result.ShouldBeNull();
        }

        [Test]
        public void StoreTwite_ShouldOverwriteExistingValue()
        {
            _inMemoryCache.Store(Key, Result1, _cacheTimeSpan);
            _inMemoryCache.Store(Key, Result2, _cacheTimeSpan);
            var result = _inMemoryCache.Get<string>(Key);
            result.ShouldBe(Result2);
        }

        [TestCase("result", 1)]
        [TestCase(null, 3)]
        public void GetStore_Value_ShouldQueryForData(string result, int callCount)
        {
            var func = CreateFunc(result);

            var r1 = GetStore(Key, _cacheTimeSpan, func);
            var r2 = GetStore(Key, _cacheTimeSpan, func);
            var r3 = GetStore(Key, _cacheTimeSpan, func);

            r1.ShouldBe(result);
            r2.ShouldBe(result);
            r3.ShouldBe(result);
            func.Received(callCount).Invoke();
        }

        [Test]
        public async Task TryGetOrAddAsync_NonNullValue_ShouldQueryForDataOnlyOnce()
        {
            var func = CreateAsyncFunc(Result);

            var r1 = await _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func).ConfigureAwait(false);
            var r2 = await _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func).ConfigureAwait(false);
            var r3 = await _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func).ConfigureAwait(false);

            r1.ShouldBe(Result);
            r2.ShouldBe(Result);
            r3.ShouldBe(Result);
            await func.Received(1).Invoke().ConfigureAwait(false);
        }

        [TestCase("result", 1, Description = "When the valueFactory returns a value, it should not be called again")]
        [TestCase(null, 3, Description = "When the value factory returns null, it should be called again next time")]
        public void AddOrGetExisting_Value_ShouldQueryForDataTheCorrectNumberOfTimes(string result, int callCount)
        {
            var func = CreateFunc(result);

            var r1 = _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func);
            var r2 = _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func);
            var r3 = _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func);

            r1.ShouldBe(result);
            r2.ShouldBe(result);
            r3.ShouldBe(result);
            func.Received(callCount).Invoke();
        }

        [TestCase("result", 0, Description = "When the valueFactory returns a value, it should not be called again")]
        [TestCase(null, 3, Description = "When the value factory returns null, it should be called again next time")]
        public void Store_TryGetOrAdd_Value_ShouldQueryForData(string result, int callCount)
        {
            var func = CreateFunc(result);

            _inMemoryCache.Store(Key, result, _cacheTimeSpan);
            var r1 = _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func);
            var r2 = _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func);
            var r3 = _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func);

            r1.ShouldBe(result);
            r2.ShouldBe(result);
            r3.ShouldBe(result);
            func.Received(callCount).Invoke();
        }

        [TestCase("result")]
        [TestCase(null)]
        public void TryGetOrAdd_Get_Value_ShouldReturnTheSameValue(string result)
        {
            var func = CreateFunc(result);

            var r1 = _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func);
            var r2 = _inMemoryCache.Get<string>(Key);

            r1.ShouldBe(result);
            r2.ShouldBe(result);
            func.Received(1).Invoke();
        }

        [Test]
        public async Task GetStore_ConcurrentCalls_ShouldQueryForDataBeCalledTwice()
        {
            var func = CreateFuncWithDelay(Result);

            var t1 = Task.Run(() => GetStore(Key, _cacheTimeSpan, () => func()));
            var t2 = Task.Run(() => GetStore(Key, _cacheTimeSpan, () => func()));
            var r1 = await t1.ConfigureAwait(false);
            var r2 = await t2.ConfigureAwait(false);

            r1.ShouldBe(Result);
            r2.ShouldBe(Result);
            func.Received(2).Invoke();
        }

        [Test]
        public async Task TryGetOrAdd_ConcurrentCalls_ShouldQueryForDataBeCalledOnlyOnce()
        {
            var func = CreateFuncWithDelay(Result);

            var t1 = Task.Run(() => _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, () => func()));
            var t2 = Task.Run(() => _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, () => func()));
            var r1 = await t1.ConfigureAwait(false);
            var r2 = await t2.ConfigureAwait(false);

            r1.ShouldBe(Result);
            r2.ShouldBe(Result);
            func.Received(1).Invoke();
        }

        [Test]
        public void GetStore_WithException_ShouldQueryForDataBeCalledTwice()
        {
            var func1 = CreateFuncThatThrows<string>();
            var func2 = CreateFunc(Result);

            Should.Throw<Exception>(() => GetStore(Key, _cacheTimeSpan, func1));
            var r2 = GetStore(Key, _cacheTimeSpan, func2);

            r2.ShouldBe(Result);
            func1.Received(1).Invoke();
            func2.Received(1).Invoke();
        }

        [Test]
        public void TryGetOrAdd_WithException_ShouldQueryForDataBeCalledTwice()
        {
            var func1 = CreateFuncThatThrows<string>();
            var func2 = CreateFunc(Result);

            Should.Throw<Exception>(() => _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func1));
            var r2 = _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func2);

            r2.ShouldBe(Result);
            func1.Received(1).Invoke();
            func2.Received(1).Invoke();
        }

        [Test]
        public void TryGetOrAdd_AfterExpiration_ShouldQueryForData()
        {
            var func1 = CreateFunc(Result1);
            var func2 = CreateFunc(Result2);

            var r1 = _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func1);
            Thread.Sleep(_expiredTimeSpan);
            var r2 = _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func2);

            r1.ShouldBe(Result1);
            r2.ShouldBe(Result2);
            func1.Received(1).Invoke();
            func2.Received(1).Invoke();
        }

        [Test]
        public void TryGetOrAdd_RightBeforeExpiration_ShouldQueryForData()
        {
            var func1 = CreateFunc(Result1);
            var func2 = CreateFunc(Result2);

            var r1 = _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func1);
            Thread.Sleep(_staleTimeSpan);
            var r2 = _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func2);
            Thread.Sleep(_staleTimeSpan);
            var r3 = _inMemoryCache.Get<string>(Key);

            r1.ShouldBe(Result1);
            r2.ShouldBe(Result1);
            r3.ShouldBe(Result2);
            func1.Received(1).Invoke();
            func2.Received(1).Invoke();
        }

        [Test]
        public async Task TryGetOrAdd_TwoThreadsRightBeforeExpiration_ShouldReturnTheStaleDataForAllThreadsUntilRevalidateIsDone()
        {
            var func1 = CreateFunc(Result1);
            var func2 = CreateFuncWithDelay(Result2);

            var r1 = _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func1);
            Thread.Sleep(_staleTimeSpan);
            var t2 = Task.Run(() => _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func2));
            var t3 = Task.Run(() => _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func2));
            var r = await Task.WhenAll(t2, t3).ConfigureAwait(false);
            Thread.Sleep(300);
            var r4 = _inMemoryCache.Get<string>(Key);

            r1.ShouldBe(Result1);
            r.ShouldAllBe(c => c == Result1);
            r4.ShouldBe(Result2);
            func1.Received(1).Invoke();
            func2.Received(1).Invoke();
        }

        [Test]
        public async Task TryGetOrAddAsync_TwoThreadsRightBeforeExpiration_ShouldReturnTheStaleDataForAllThreadsUntilRevalidateIsDone()
        {
            var func1 = CreateAsyncFunc(Result1);
            var func2 = CreateFuncWithDelay(Task.FromResult(Result2));

            var r1 = _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func1);
            Thread.Sleep(_staleTimeSpan);
            var t2 = Task.Run(() => _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func2));
            var t3 = Task.Run(() => _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func2));
            var r = await Task.WhenAll(t2, t3).ConfigureAwait(false);
            Thread.Sleep(300);
            var r4 = _inMemoryCache.Get<string>(Key);

            r1.Result.ShouldBe(Result1);
            r.ShouldAllBe(c => c == Result1);
            r4.ShouldBe(Result2);
            await func1.Received(1).Invoke();
            await func2.Received(1).Invoke();
        }

        [Test]
        public async Task TryGetOrAddAsync_ThenSync_WithTwoThreadsRightBeforeExpiration_ShouldReturnTheStaleDataForAllThreadsUntilRevalidateIsDone()
        {
            var func1 = CreateAsyncFunc(Result1);
            var func2 = CreateFuncWithDelay(Result2);

            var r1 = _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func1);
            Thread.Sleep(_staleTimeSpan);
            var t2 = Task.Run(() => _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func2));
            var t3 = Task.Run(() => _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func2));
            var r = await Task.WhenAll(t2, t3).ConfigureAwait(false);
            Thread.Sleep(300);
            var r4 = _inMemoryCache.Get<string>(Key);

            r1.Result.ShouldBe(Result1);
            r.ShouldAllBe(c => c == Result1);
            r4.ShouldBe(Result2);
            await func1.Received(1).Invoke();
            func2.Received(1).Invoke();
        }

        [Test]
        public async Task TryGetOrAdd_ThenAsync_WithTwoThreadsRightBeforeExpiration_ShouldReturnTheStaleDataForAllThreadsUntilRevalidateIsDone()
        {
            var func1 = CreateFunc(Result1);
            var func2 = CreateFuncWithDelay(Task.FromResult(Result2));

            var r1 = _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func1);
            Thread.Sleep(_staleTimeSpan);
            var t2 = Task.Run(() => _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func2));
            var t3 = Task.Run(() => _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func2));
            var r = await Task.WhenAll(t2, t3).ConfigureAwait(false);
            Thread.Sleep(300);
            var r4 = _inMemoryCache.Get<string>(Key);

            r1.ShouldBe(Result1);
            r.ShouldAllBe(c => c == Result1);
            r4.ShouldBe(Result2);
            func1.Received(1).Invoke();
            await func2.Received(1).Invoke();
        }

        [Test]
        public async Task GetInBackgroundAndStore_TwoThreadsRightBeforeExpiration_ShouldReturnTheStaleDataForBothThreads()
        {
            var func1 = CreateFunc(Result1);
            var func2 = CreateAsyncFuncWithDelay(Result2);

            var r1 = _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func1);
            Thread.Sleep(_staleTimeSpan);
            var r2 = await _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func2, 1);
            var r3 = await _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func2, 1);

            r1.ShouldBe(Result1);
            r2.ShouldBe(Result1);
            r3.ShouldBe(Result1);
            func1.Received(1).Invoke();
            Thread.Sleep(_staleTimeSpan);
            await func2.Received(1).Invoke();
        }

        [Test]
        public async Task GetInBackgroundAndStore_ThrowsExceptionWhileRevalidating_ShouldBumpTheOldValue()
        {
            var func1 = CreateFunc(Result1);
            var func2 = CreateAsyncFuncThatThrowsWithDelay<string>();

            var r1 = _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func1);
            Thread.Sleep(_staleTimeSpan);
            var r2 = await _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func2, 1);
            Thread.Sleep(_staleTimeSpan);
            var r3 = await _inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func2, 1);
            Thread.Sleep(_staleTimeSpan);

            r1.ShouldBe(Result1);
            r2.ShouldBe(Result1);
            r3.ShouldBe(Result1);
            func1.Received(1).Invoke();
            await func2.Received(2).Invoke();
        }

        [Test]
        public async Task GetInBackgroundAndStore_ThrowsExceptionWhileRevalidating_ShouldLogValue()
        {
            var func1 = CreateFunc(Result1);
            var func2 = CreateAsyncFuncThatThrowsWithDelay<string>();

            Exception coughtException = null;

            var inMemoryCache = new InMemoryCache(
                func => Task.Run(async () =>
                {
                    try
                    {
                        await func();
                    }
                    catch (Exception e)
                    {
                        coughtException = e;
                    }
                }));

            var r1 = inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func1);
            Thread.Sleep(_staleTimeSpan);
            var r2 = await inMemoryCache.TryGetOrAdd(Key, _cacheTimeSpan, func2, 1);
            Thread.Sleep(_staleTimeSpan);

            r1.ShouldBe(Result1);
            r2.ShouldBe(Result1);
            func1.Received(1).Invoke();
            await func2.Received(1).Invoke();
            coughtException.Message.ShouldBe("oh noes");
        }

        private string GetStore(string key, TimeSpan validFor, Func<string> dataFunc)
        {
            var result = _inMemoryCache.Get<string>(key);

            if (result != null)
            {
                return result;
            }

            var s = dataFunc();
            _inMemoryCache.Store(key, s, validFor);
            return s;
        }

        private static Func<T> CreateFunc<T>(T result)
        {
            var func = Substitute.For<Func<T>>();
            func.Invoke().Returns(info => result);
            return func;
        }

        private static Func<Task<T>> CreateAsyncFunc<T>(T result)
        {
            var func = Substitute.For<Func<Task<T>>>();
            func.Invoke().Returns(info => result);
            return func;
        }

        private static Func<T> CreateFuncWithDelay<T>(T result)
        {
            var func = Substitute.For<Func<T>>();
            func.Invoke().Returns(
                info =>
                {
                    Thread.Sleep(200);
                    return result;
                });
            return func;
        }

        private static Func<Task<T>> CreateAsyncFuncWithDelay<T>(T result)
        {
            var func = Substitute.For<Func<Task<T>>>();
            func.Invoke().Returns(
                info =>
                {
                    Thread.Sleep(200);
                    return result;
                });
            return func;
        }

        private static Func<T> CreateFuncThatThrows<T>()
        {
            var func = Substitute.For<Func<T>>();
            func.Invoke().Throws(new Exception("oh noes"));
            return func;
        }

        private static Func<Task<T>> CreateAsyncFuncThatThrowsWithDelay<T>()
        {
            var func = Substitute.For<Func<Task<T>>>();
            func.Invoke().Returns<Task<T>>(
                info =>
                {
                    Thread.Sleep(200);
                    throw new Exception("oh noes");
                });
            return func;
        }
    }
}
