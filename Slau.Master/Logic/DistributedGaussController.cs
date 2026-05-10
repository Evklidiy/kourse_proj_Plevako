using System;
using System.Net.Sockets;
using System.Diagnostics;
using Slau.Common.Helpers;
using Slau.Common.Models;
using Slau.Common.Networking;
using Slau.Master.Storage;
using Slau.Master.Distribution;

namespace Slau.Master.Logic
{
    public class DistributedGaussController
    {
        private readonly LargeMatrixStorage _storage;
        private readonly WorkerManager _workerManager;

        public DistributedGaussController(LargeMatrixStorage storage, WorkerManager workers)
        {
            _storage = storage;
            _workerManager = workers;
        }

        public double[] Solve()
        {
            int n = _storage.N;
            int workersCount = _workerManager.Nodes.Count;

            // 1. Распределение (Ждем подтверждения)
            int rowsPerWorker = n / workersCount;
            for (int i = 0; i < workersCount; i++)
            {
                int startRow = i * rowsPerWorker;
                int count = (i == workersCount - 1) ? (n - startRow) : rowsPerWorker;
                var chunk = CreateChunk(startRow, count);

                // Ждем пока воркер инициализирует данные
                SendMessageWithResponse(_workerManager.Nodes[i], ProtocolCommand.INIT_CHUNKS, chunk);
            }

            // 2. Цикл Гаусса (Ждем подтверждения каждого шага)
            for (int k = 0; k < n; k++)
            {
                double[] pivotRow = _storage.GetRow(k);
                double pivotElement = pivotRow[k];

                for (int j = k; j <= n; j++) pivotRow[j] /= pivotElement;
                _storage.SetRow(k, pivotRow);

                var pivotData = new PivotRowData { PivotRowIndex = k, Row = pivotRow };

                foreach (var worker in _workerManager.Nodes)
                {
                    // ВАЖНО: Ждем пока воркер закончит расчет шага k
                    SendMessageWithResponse(worker, ProtocolCommand.PROCESS_GAUSS, pivotData);
                }

                if (k % 500 == 0) Debug.WriteLine($"Шаг {k} завершен.");
            }

            // 3. Сбор результата
            double[] resultX = new double[n];
            foreach (var worker in _workerManager.Nodes)
            {
                var part = (double[])SendMessageWithResponse(worker, ProtocolCommand.GET_RESULT, null);
                // Тут можно добавить логику записи X обратно в storage
            }

            return resultX;
        }

        private MatrixChunk CreateChunk(int start, int count)
        {
            int n = _storage.N;
            double[] flatData = new double[(long)count * (n + 1)];
            for (int i = 0; i < count; i++)
            {
                double[] row = _storage.GetRow(start + i);
                Array.Copy(row, 0, flatData, (long)i * (n + 1), n + 1);
            }
            return new MatrixChunk { StartRowIndex = start, RowCount = count, TotalColumns = n + 1, Data = flatData };
        }

        private object SendMessageWithResponse(WorkerNode node, ProtocolCommand cmd, object data)
        {
            try
            {
                using (TcpClient client = new TcpClient(node.Ip, node.Port))
                using (NetworkStream stream = client.GetStream())
                {
                    NetworkMessage msg = new NetworkMessage { Command = cmd, Data = data };
                    byte[] buffer = SerializationHelper.Serialize(msg);
                    stream.Write(BitConverter.GetBytes(buffer.Length), 0, 4);
                    stream.Write(buffer, 0, buffer.Length);

                    // Ждем ответ (ОК или данные)
                    byte[] lengthBuffer = new byte[4];
                    int readLen = stream.Read(lengthBuffer, 0, 4);
                    if (readLen < 4) return null;

                    int length = BitConverter.ToInt32(lengthBuffer, 0);
                    byte[] responseBuffer = new byte[length];
                    int totalRead = 0;
                    while (totalRead < length)
                    {
                        totalRead += stream.Read(responseBuffer, totalRead, length - totalRead);
                    }
                    return SerializationHelper.Deserialize(responseBuffer);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Ошибка связи: " + ex.Message);
                return null;
            }
        }
    }
}