using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;

namespace Clave.InMemoryCache.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class SimultaneouslyTests
    {
        [Test]
        public async Task TestTask()
        {
            var sw = Stopwatch.StartNew();
            await Simultaneously.Run(20, () => Delay("test"));
            sw.ElapsedMilliseconds.ShouldBeInRange(100, 150);
        }

        [Test]
        public async Task TestThread()
        {
            var sw = Stopwatch.StartNew();
            await Simultaneously.Run(2, () => Sleep("test"));
            sw.ElapsedMilliseconds.ShouldBeInRange(100, 150);
        }

        private static async Task<string> Delay(string value)
        {
            await Task.Delay(100);
            return value;
        }

        private static string Sleep(string value)
        {
            Thread.Sleep(100);
            return value;
        }
    }
}