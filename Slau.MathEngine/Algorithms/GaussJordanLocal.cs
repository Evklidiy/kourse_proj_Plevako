using Slau.MathEngine.Interfaces;

namespace Slau.MathEngine.Algorithms
{
    public class GaussJordanLocal : ISlauSolver
    {
        public double[] Solve(double[][] matrix, double[] b)
        {
            int n = b.Length;

            for (int k = 0; k < n; k++)
            {
                // 1. Нормализация ведущей строки
                double pivot = matrix[k][k];
                for (int j = k; j < n; j++)
                {
                    matrix[k][j] /= pivot;
                }
                b[k] /= pivot;

                // 2. Исключение элементов во всех остальных строках
                for (int i = 0; i < n; i++)
                {
                    if (i != k)
                    {
                        double factor = matrix[i][k];
                        for (int j = k; j < n; j++)
                        {
                            matrix[i][j] -= factor * matrix[k][j];
                        }
                        b[i] -= factor * b[k];
                    }
                }
            }
            return b; // Вектор B после преобразований становится вектором X
        }
    }
}