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

        public double[] GetRowForMaster(int globalIndex)
        {
            int localIndex = globalIndex - _startRowIndex;
            int n = _localRows[localIndex].Length;
            double[] rowWithB = new double[n + 1];
            Array.Copy(_localRows[localIndex], 0, rowWithB, 0, n);
            rowWithB[n] = _localB[localIndex];
            return rowWithB;
        }

        public void ProcessGaussStep(PivotRowData pivotData)
        {
            int k = pivotData.PivotRowIndex;
            double[] pivotRow = pivotData.Row; // Это нормализованная строка (A[k,k] = 1)
            int nCoeffs = pivotRow.Length - 1;

            // ВАЖНО: Если мы владеем строкой K, мы должны обновить её нормализованным значением!
            int localPivotIndex = k - _startRowIndex;
            if (localPivotIndex >= 0 && localPivotIndex < _localRows.Length)
            {
                Array.Copy(pivotRow, 0, _localRows[localPivotIndex], 0, nCoeffs);
                _localB[localPivotIndex] = pivotRow[nCoeffs];
            }

            ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = this.MaxDegreeOfParallelism };

            Parallel.For(0, _localRows.Length, options, i =>
            {
                int globalIndex = _startRowIndex + i;
                if (globalIndex == k) return; // Эту строку мы уже обновили выше

                double factor = _localRows[i][k];
                if (Math.Abs(factor) < 1e-25) return;

                for (int j = k; j < nCoeffs; j++)
                {
                    _localRows[i][j] -= factor * pivotRow[j];
                }

                _localB[i] -= factor * pivotRow[nCoeffs];
                _localRows[i][k] = 0; // Обнуляем столбец
            });
        }

        public double[] GetResults() => _localB;

        private void CheckMemoryLimit()
        {
            long currentMem = Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024);
            if (currentMem > MaxMemoryMb)
                throw new Exception("Memory limit exceeded");
        }
    }
}