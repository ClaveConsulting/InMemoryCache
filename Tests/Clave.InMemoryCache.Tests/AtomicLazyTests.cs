using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;

namespace Clave.InMemoryCache.Tests
{
    [TestFixture]
    public class AtomicLazyTests
    {
        [Test]
        public void TestWithInitialValue()
        {
            var lazy = new AtomicLazy<string>("result");

            lazy.Value.ShouldBe("result");
        }

        [Test]
        public void TestWithFactory()
        {
            var lazy = new AtomicLazy<string>(() => "result");

            lazy.Value.ShouldBe("result");
        }

        [Test]
        public async Task TestWithSlowFactory()
        {
            var counter = 0;

            var lazy = new AtomicLazy<string>(() =>
            {
                Interlocked.Increment(ref counter);
                return Delay("result");
            });

            await Simultaneously.Run(100, () => lazy.Value);

            lazy.Value.ShouldBe("result");

            counter.ShouldBe(1);
        }

        [Test]
        public void TestWithThrow()
        {
            var failedOnce = false;


            var lazy = new AtomicLazy<string>(() =>
            {
                if (failedOnce)
                {
                    return "success";
                }
                else
                {
                    failedOnce = true;
                    throw new Exception("Failed once");
                }
            });

            Should.Throw<Exception>(() => lazy.Value);

            lazy.Value.ShouldBe("success");
        }

        private static string Delay(string value)
        {
            Thread.Sleep(100);
            return value;
        }
    }
}