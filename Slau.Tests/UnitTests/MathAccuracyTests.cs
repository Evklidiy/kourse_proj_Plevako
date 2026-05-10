using NUnit.Framework;
using Slau.MathEngine.Algorithms;
using System;

namespace Slau.Tests.UnitTests
{
    [TestFixture]
    public class MathAccuracyTests
    {
        [Test] // 1. Единичная матрица
        public void Gauss_IdentityMatrix_ShouldReturnB()
        {
            double[][] A = { new[] { 1.0, 0.0 }, new[] { 0.0, 1.0 } };
            double[] B = { 5.0, 10.0 };
            var res = new GaussJordanLocal().Solve(A, B);
            Assert.That(res, Is.EqualTo(B));
        }

        [Test] // 2. Система 3х3
        public void Gauss_3x3_CorrectResult()
        {
            double[][] A = { new[] { 3.0, 2.0, -1.0 }, new[] { 2.0, -2.0, 4.0 }, new[] { -1.0, 0.5, -1.0 } };
            double[] B = { 1.0, -2.0, 0.0 };
            var res = new GaussJordanLocal().Solve(A, B);
            Assert.That(res[0], Is.EqualTo(1).Within(0.1));
            Assert.That(res[1], Is.EqualTo(-2).Within(0.1));
            Assert.That(res[2], Is.EqualTo(-2).Within(0.1));
        }

        [Test] // 3. Система 1x1
        public void Gauss_1x1_CorrectResult()
        {
            double[][] A = { new[] { 5.0 } }; double[] B = { 10.0 };
            var res = new GaussJordanLocal().Solve(A, B);
            Assert.That(res[0], Is.EqualTo(2.0));
        }

        [Test] // 4. Отрицательные коэффициенты
        public void Gauss_NegativeValues_CorrectResult()
        {
            double[][] A = { new[] { -1.0, 1.0 }, new[] { 1.0, 1.0 } };
            double[] B = { 0.0, 2.0 };
            var res = new GaussJordanLocal().Solve(A, B);
            Assert.That(res[0], Is.EqualTo(1.0).Within(1e-5));
            Assert.That(res[1], Is.EqualTo(1.0).Within(1e-5));
        }

        [Test] // 5. Сходимость релаксации (Якоби)
        public void Relaxation_DiagonalDominance_Converges()
        {
            double[][] A = { new[] { 10.0, 1.0 }, new[] { 2.0, 10.0 } };
            double[] B = { 11.0, 12.0 };
            var res = new RelaxationSolver(1e-6, 100).Solve(A, B);
            Assert.That(res[0], Is.EqualTo(1.0).Within(0.1));
            Assert.That(res[1], Is.EqualTo(1.0).Within(0.1));
        }

        [Test] // 6. Сравнение результатов Гаусса и Релаксации
        public void Solvers_Comparison_ShouldBeClose()
        {
            double[][] A = { new[] { 4.0, 1.0 }, new[] { 1.0, 3.0 } };
            double[] B = { 1.0, 2.0 };
            var res1 = new GaussJordanLocal().Solve(A, B);
            var res2 = new RelaxationSolver().Solve(A, B);
            Assert.That(res1[0], Is.EqualTo(res2[0]).Within(0.1));
        }
    }
}