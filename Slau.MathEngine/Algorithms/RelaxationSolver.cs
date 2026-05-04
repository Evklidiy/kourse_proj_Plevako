using System;
using Slau.MathEngine.Interfaces;

namespace Slau.MathEngine.Algorithms
{
    public class RelaxationSolver : ISlauSolver
    {
        private readonly double _eps;
        private readonly int _maxIterations;

        public RelaxationSolver(double eps = 1e-6, int maxIterations = 5000)
        {
            _eps = eps;
            _maxIterations = maxIterations;
        }

        public double[] Solve(double[][] matrix, double[] b)
        {
            int n = b.Length;
            double[] x = new double[n]; // Начальное приближение (нули)
            double[] nextX = new double[n];

            for (int iter = 0; iter < _maxIterations; iter++)
            {
                for (int i = 0; i < n; i++)
                {
                    double sum = 0;
                    for (int j = 0; j < n; j++)
                    {
                        if (i != j)
                        {
                            sum += matrix[i][j] * x[j];
                        }
                    }
                    nextX[i] = (b[i] - sum) / matrix[i][i];
                }

                // Проверка на сходимость
                double maxDiff = 0;
                for (int i = 0; i < n; i++)
                {
                    maxDiff = Math.Max(maxDiff, Math.Abs(nextX[i] - x[i]));
                }

                Array.Copy(nextX, x, n);

                if (maxDiff < _eps) break;
            }

            return x;
        }
    }
}