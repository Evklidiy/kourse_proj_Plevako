using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Slau.Common.Models;

namespace Slau.Worker.Services
{
    public class ComputationService
    {
        private double[][] _localRows;
        private double[] _localB;
        private int _startRowIndex;

        public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
        public long MaxMemoryMb { get; set; } = 4096;

        public void Initialize(MatrixChunk chunk)
        {
            CheckMemoryLimit();
            _startRowIndex = chunk.StartRowIndex;
            int nCoeffs = chunk.TotalColumns - 1;

            _localRows = new double[chunk.RowCount][];
            _localB = new double[chunk.RowCount];

            for (int i = 0; i < chunk.RowCount; i++)
            {
                _localRows[i] = new double[nCoeffs];
                for (int j = 0; j < nCoeffs; j++)
                    _localRows[i][j] = chunk.Data[i * chunk.TotalColumns + j];

                _localB[i] = chunk.Data[i * chunk.TotalColumns + nCoeffs];
            }
        }

        public void ProcessGaussStep(PivotRowData pivotData)
        {
            CheckMemoryLimit();
            int k = pivotData.PivotRowIndex;
            double[] pivotRow = pivotData.Row;
            int nTotal = pivotRow.Length;
            int nCoeffs = nTotal - 1;

            ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = this.MaxDegreeOfParallelism };

            Parallel.For(0, _localRows.Length, options, i =>
            {
                int globalIndex = _startRowIndex + i;
                if (globalIndex == k) return;

                // Проверка, чтобы индекс k не выходил за границы нашего массива строк
                if (k >= _localRows[i].Length) return;

                double factor = _localRows[i][k];
                if (Math.Abs(factor) < 1e-25) return;

                for (int j = k; j < _localRows[i].Length; j++)
                {
                    _localRows[i][j] -= factor * pivotRow[j];
                }
                _localB[i] -= factor * pivotRow[nCoeffs];
            });
        }

        public double[] GetResults() => _localB;

        private void CheckMemoryLimit()
        {
            long currentMem = Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024);
            if (currentMem > MaxMemoryMb)
                throw new Exception($"Лимит памяти превышен: {currentMem}MB");
        }
    }
}