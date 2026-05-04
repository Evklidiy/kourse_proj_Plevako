namespace Slau.MathEngine.Interfaces
{
    public interface ISlauSolver
    {
        // Метод принимает матрицу коэффициентов A и вектор свободных членов B
        double[] Solve(double[][] matrix, double[] b);
    }
}