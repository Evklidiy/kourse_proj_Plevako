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
            Log($"Сгенерирована матрица {n}x{n} в MMF-хранилище.");
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
            SaveFileDialog saveFileDialog = new SaveFileDialog { Filter = "Text files (*.txt)|*.txt", FileName = "matrix_50k.txt" };
            if (saveFileDialog.ShowDialog() == true)
            {
                Log("Создание файла...");
                using (StreamWriter sw = new StreamWriter(saveFileDialog.FileName))
                {
                    sw.WriteLine(n);
                    Random r = new Random();
                    for (int i = 0; i < n; i++)
                    {
                        var row = new System.Text.StringBuilder();
                        for (int j = 0; j < n; j++) row.Append((r.NextDouble() * 10).ToString("F2") + " ");
                        row.Append((r.NextDouble() * 100).ToString("F2"));
                        sw.WriteLine(row.ToString());
                    }
                }
                Log("Тестовый файл создан.");
            }
        }

        private void BtnDistributed_Click(object sender, RoutedEventArgs e)
        {
            if (_storage == null) { MessageBox.Show("Нет данных!"); return; }

            var wm = new WorkerManager();
            foreach (var w in TxtWorkers.Text.Split(','))
            {
                var parts = w.Trim().Split(':');
                wm.AddWorker(parts[0], int.Parse(parts[1]));
            }

            Log("Запуск распределенного расчета...");
            var controller = new DistributedGaussController(_storage, wm);
            Stopwatch sw = Stopwatch.StartNew();
            controller.Solve();
            sw.Stop();
            Log($"Завершено за {sw.ElapsedMilliseconds} мс.");
        }

        private void BtnLocalGauss_Click(object sender, RoutedEventArgs e)
        {
            if (_storage == null) { MessageBox.Show("Сначала загрузите или сгенерируйте данные!"); return; }

            Log("Запуск ЛОКАЛЬНОГО метода Гаусса-Джордана...");
            int n = _storage.N;
            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                // Основной цикл алгоритма (выполняется только на Мастере)
                for (int k = 0; k < n; k++)
                {
                    // 1. Получаем и нормализуем опорную строку
                    double[] pivotRow = _storage.GetRow(k);
                    double pivotElement = pivotRow[k];

                    for (int j = k; j <= n; j++)
                        pivotRow[j] /= pivotElement;

                    _storage.SetRow(k, pivotRow);

                    // 2. Исключаем элементы во всех остальных строках
                    for (int i = 0; i < n; i++)
                    {
                        if (i == k) continue;

                        double[] currentRow = _storage.GetRow(i);
                        double factor = currentRow[k];

                        if (Math.Abs(factor) > 1e-25)
                        {
                            for (int j = k; j <= n; j++)
                            {
                                currentRow[j] -= factor * pivotRow[j];
                            }
                            _storage.SetRow(i, currentRow);
                        }
                    }

                    // Обновляем лог каждые 500 шагов, чтобы видеть прогресс
                    if (k % 500 == 0)
                    {
                        // Используем Dispatcher, чтобы UI не зависал наглухо
                        Dispatcher.Invoke(() => Log($"Локальный Гаусс: шаг {k}..."), System.Windows.Threading.DispatcherPriority.Background);
                    }
                }

                sw.Stop();
                Log($"ЛОКАЛЬНЫЙ ГАУСС ЗАВЕРШЕН. Время: {sw.ElapsedMilliseconds} мс.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при локальном расчете: " + ex.Message);
            }
        }

        private void BtnLocal_Click(object sender, RoutedEventArgs e)
        {
            if (_storage == null) return;
            Log("Запуск локальной релаксации (подготовка данных)...");

            int n = _storage.N;
            double[][] matrixA = new double[n][];
            double[] vectorB = new double[n];

            for (int i = 0; i < n; i++)
            {
                double[] row = _storage.GetRow(i);
                matrixA[i] = new double[n];
                Array.Copy(row, 0, matrixA[i], 0, n);
                vectorB[i] = row[n];
            }

            RelaxationSolver solver = new RelaxationSolver(1e-6, 500);
            Stopwatch sw = Stopwatch.StartNew();
            solver.Solve(matrixA, vectorB);
            sw.Stop();
            Log($"Релаксация: {sw.ElapsedMilliseconds} мс.");
        }
    }
}