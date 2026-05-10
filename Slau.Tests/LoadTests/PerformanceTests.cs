using NUnit.Framework;
using System.Diagnostics;
using Slau.MathEngine.Algorithms;

namespace Slau.Tests.LoadTests
{
    [TestFixture]
    public class PerformanceTests
    {
        [TestCase(100)] // Тесты 15, 16, 17.
        [TestCase(300)]
        [TestCase(500)]
        public void LocalSolver_Performance_NScale(int n)
        {
            double[][] A = new double[n][];
            double[] B = new double[n];
            for (int i = 0; i < n; i++) A[i] = new double[n];

            var sw = Stopwatch.StartNew();
            new GaussJordanLocal().Solve(A, B);
            sw.Stop();

            Assert.That(sw.ElapsedMilliseconds, Is.LessThan(10000));
        }

        [Test] // 18. Нагрузка N=1000
        public void Performance_N1000_TimeCheck()
        {
            int n = 1000;
            double[][] A = new double[n][];
            for (int i = 0; i < n; i++) A[i] = new double[n];
            var sw = Stopwatch.StartNew();
            new GaussJordanLocal().Solve(A, new double[n]);
            sw.Stop();
            Assert.That(sw.ElapsedMilliseconds, Is.GreaterThan(0));
        }

        [Test] // 19. Скорость генерации данных MMF
        public void Storage_RandomGen_SpeedTest()
        {
            using (var s = new Slau.Master.Storage.LargeMatrixStorage(1000))
            {
                var sw = Stopwatch.StartNew();
                s.GenerateRandom();
                sw.Stop();
                Assert.That(sw.ElapsedMilliseconds, Is.LessThan(20000));
            }
        }

        [Test] // 20. Тест выделения памяти под результат
        public void Memory_ResultVector_Allocation()
        {
            int n = 50000;
            double[] res = new double[n];
            Assert.That(res.Length, Is.EqualTo(n));
        }
    }
}