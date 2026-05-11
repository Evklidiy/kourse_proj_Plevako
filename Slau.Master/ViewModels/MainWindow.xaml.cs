using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using Slau.Master.Storage;
using Slau.Master.Distribution;
using Slau.Master.Logic;
using Slau.MathEngine.Algorithms;
using Microsoft.Win32;

namespace Slau.Master
{
    public partial class MainWindow : Window
    {
        private LargeMatrixStorage _storage;

        public MainWindow()
        {
            InitializeComponent();
            Log("Система готова.");
        }

        private void Log(string msg) => TxtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\n");

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            int n = int.Parse(TxtN.Text);
            if (_storage != null) _storage.Dispose();
            _storage = new LargeMatrixStorage(n);
            _storage.GenerateRandom();
            Log($"Сгенерирована матрица {n}x{n} в MMF (результат должен быть x=1).");
        }

        private void BtnLoadFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Text files (*.txt)|*.txt" };
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string firstLine = File.ReadLines(openFileDialog.FileName).First();
                    int n = int.Parse(firstLine.Trim());
                    if (_storage != null) _storage.Dispose();
                    _storage = new LargeMatrixStorage(n);
                    _storage.LoadFromFile(openFileDialog.FileName);
                    TxtN.Text = n.ToString();
                    Log($"Файл {openFileDialog.SafeFileName} загружен.");
                }
                catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
            }
        }

        private void BtnCreateTestFile_Click(object sender, RoutedEventArgs e)
        {
            int n = int.Parse(TxtN.Text);
            SaveFileDialog sfd = new SaveFileDialog { Filter = "Text files|*.txt", FileName = "matrix_50k.txt" };
            if (sfd.ShowDialog() == true)
            {
                Log("Создание файла...");
                using (StreamWriter sw = new StreamWriter(sfd.FileName))
                {
                    sw.WriteLine(n);
                    Random r = new Random();
                    for (int i = 0; i < n; i++)
                    {
                        var row = new System.Text.StringBuilder();
                        for (int j = 0; j < n; j++) row.Append((r.NextDouble() * 10).ToString("F2") + " ");
                        row.Append((n + i).ToString("F2")); // Формула из примера друга для x=1
                        sw.WriteLine(row.ToString());
                    }
                }
                Log("Тестовый файл создан.");
            }
        }

        private void ShowResultPreview(double[] x)
        {
            Log("--- РЕЗУЛЬТАТ (Вектор X) ---");
            for (int i = 0; i < Math.Min(5, x.Length); i++)
                Log($"X[{i}] = {x[i]:F6}");

            if (x.Length > 5) Log("... и так далее ...");

            if (MessageBox.Show("Расчет завершен. Сохранить полный результат в файл?", "Готово", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                SaveFileDialog sfd = new SaveFileDialog { Filter = "Text files|*.txt", FileName = "solution.txt" };
                if (sfd.ShowDialog() == true)
                {
                    File.WriteAllLines(sfd.FileName, x.Select(v => v.ToString("F10")));
                    Log("Файл сохранен.");
                }
            }
        }

        private void BtnDistributed_Click(object sender, RoutedEventArgs e)
        {
            if (_storage == null) return;
            var wm = new WorkerManager();
            foreach (var w in TxtWorkers.Text.Split(','))
            {
                var parts = w.Trim().Split(':');
                wm.AddWorker(parts[0], int.Parse(parts[1]));
            }

            Log("Запуск распределенного расчета...");
            var controller = new DistributedGaussController(_storage, wm);
            Stopwatch sw = Stopwatch.StartNew();
            double[] result = controller.Solve();
            sw.Stop();

            Log($"Завершено за {sw.ElapsedMilliseconds} мс.");
            ShowResultPreview(result);
        }

        private void BtnLocalGauss_Click(object sender, RoutedEventArgs e)
        {
            if (_storage == null) return;
            Log("Запуск локального Гаусса...");
            int n = _storage.N;
            Stopwatch sw = Stopwatch.StartNew();

            for (int k = 0; k < n; k++)
            {
                double[] pivotRow = _storage.GetRow(k);
                double pivotElement = pivotRow[k];
                for (int j = k; j <= n; j++) pivotRow[j] /= pivotElement;
                _storage.SetRow(k, pivotRow);

                for (int i = 0; i < n; i++)
                {
                    if (i == k) continue;
                    double[] curr = _storage.GetRow(i);
                    double factor = curr[k];
                    if (Math.Abs(factor) < 1e-25) continue;
                    for (int j = k; j <= n; j++) curr[j] -= factor * pivotRow[j];
                    _storage.SetRow(i, curr);
                }
                if (k % 500 == 0) Dispatcher.Invoke(() => Log($"Шаг {k}..."), System.Windows.Threading.DispatcherPriority.Background);
            }

            double[] resultX = new double[n];
            for (int i = 0; i < n; i++) resultX[i] = _storage.GetRow(i)[n];

            sw.Stop();
            Log($"Локальный Гаусс завершен за {sw.ElapsedMilliseconds} мс.");
            ShowResultPreview(resultX);
        }

        private void BtnLocal_Click(object sender, RoutedEventArgs e)
        {
            if (_storage == null) return;
            Log("Локальная релаксация...");
            int n = _storage.N;
            double[][] A = new double[n][];
            double[] B = new double[n];
            for (int i = 0; i < n; i++)
            {
                double[] r = _storage.GetRow(i);
                A[i] = new double[n];
                Array.Copy(r, 0, A[i], 0, n);
                B[i] = r[n];
            }
            var solver = new RelaxationSolver(1e-6, 500);
            Stopwatch sw = Stopwatch.StartNew();
            double[] result = solver.Solve(A, B);
            sw.Stop();
            Log($"Релаксация завершена за {sw.ElapsedMilliseconds} мс.");
            ShowResultPreview(result);
        }
    }
}