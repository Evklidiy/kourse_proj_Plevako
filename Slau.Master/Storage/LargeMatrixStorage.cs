using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Globalization;

namespace Slau.Master.Storage
{
    public class LargeMatrixStorage : IDisposable
    {
        public int N { get; private set; }
        private MemoryMappedFile _mmf;
        private readonly long _rowSizeBytes;

        public LargeMatrixStorage(int n)
        {
            N = n;
            // Размер строки: (N коэффициентов + 1 свободный член) * 8 байт
            _rowSizeBytes = (long)(n + 1) * sizeof(double);
            long totalSizeBytes = _rowSizeBytes * n;

            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "matrix_data.bin");
            if (File.Exists(filePath)) File.Delete(filePath);

            _mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Create, "SlauMap", totalSizeBytes);
        }

        public void SetRow(int rowIndex, double[] data)
        {
            using (var accessor = _mmf.CreateViewAccessor(rowIndex * _rowSizeBytes, _rowSizeBytes))
            {
                accessor.WriteArray(0, data, 0, data.Length);
            }
        }

        public double[] GetRow(int rowIndex)
        {
            double[] data = new double[N + 1];
            using (var accessor = _mmf.CreateViewAccessor(rowIndex * _rowSizeBytes, _rowSizeBytes))
            {
                accessor.ReadArray(0, data, 0, N + 1);
            }
            return data;
        }

        public void LoadFromFile(string filePath)
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                string firstLine = reader.ReadLine(); // Пропускаем размерность (уже задана)
                for (int i = 0; i < N; i++)
                {
                    string line = reader.ReadLine();
                    if (line == null) break;
                    string[] parts = line.Split(new[] { ' ', '\t', ';' }, StringSplitOptions.RemoveEmptyEntries);

                    double[] rowData = new double[N + 1];
                    for (int j = 0; j <= N; j++)
                    {
                        rowData[j] = double.Parse(parts[j], CultureInfo.InvariantCulture);
                    }
                    SetRow(i, rowData);
                }
            }
        }

        public void GenerateRandom()
        {
            Random r = new Random();
            for (int i = 0; i < N; i++)
            {
                double[] row = new double[N + 1];
                double sum = 0;
                for (int j = 0; j < N; j++)
                {
                    row[j] = r.NextDouble() * 5 + 1; // Числа от 1 до 6
                    sum += row[j];
                }
                row[i] += N * 2; // Гарантируем мощное диагональное преобладание
                sum += N * 2;
                row[N] = sum; // Значит x_i всегда будет равен 1.0
                SetRow(i, row);
            }
        }

        public void Dispose()
        {
            if (_mmf != null) _mmf.Dispose();
        }
    }
}