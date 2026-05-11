using System;
using System.Net.Sockets;
using System.Diagnostics;
using System.Collections.Generic;
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
            int rowsPerWorker = n / workersCount;

            // 1. Инициализация
            for (int i = 0; i < workersCount; i++)
            {
                int start = i * rowsPerWorker;
                int count = (i == workersCount - 1) ? (n - start) : rowsPerWorker;
                SendMessageWithResponse(_workerManager.Nodes[i], ProtocolCommand.INIT_CHUNKS, CreateChunk(start, count));
            }

            // 2. Цикл Гаусса
            for (int k = 0; k < n; k++)
            {
                int workerIndex = Math.Min(k / rowsPerWorker, workersCount - 1);
                var ownerNode = _workerManager.Nodes[workerIndex];

                // Получаем строку, нормализуем её и рассылаем
                double[] pivotRow = (double[])SendMessageWithResponse(ownerNode, ProtocolCommand.GET_SPECIFIC_ROW, k);
                double pivotElement = pivotRow[k];

                for (int j = k; j <= n; j++) pivotRow[j] /= pivotElement;

                var pivotData = new PivotRowData { PivotRowIndex = k, Row = pivotRow };

                foreach (var worker in _workerManager.Nodes)
                    SendMessageWithResponse(worker, ProtocolCommand.PROCESS_GAUSS, pivotData);
            }

            // 3. Сбор
            double[] fullX = new double[n];
            for (int i = 0; i < workersCount; i++)
            {
                double[] part = (double[])SendMessageWithResponse(_workerManager.Nodes[i], ProtocolCommand.GET_RESULT, null);
                int start = i * rowsPerWorker;
                Array.Copy(part, 0, fullX, start, part.Length);
            }
            return fullX;
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

                    byte[] lenBuf = new byte[4];
                    if (stream.Read(lenBuf, 0, 4) < 4) return null;
                    int len = BitConverter.ToInt32(lenBuf, 0);
                    byte[] responseBuffer = new byte[len];
                    int totalRead = 0;
                    while (totalRead < len)
                        totalRead += stream.Read(responseBuffer, totalRead, len - totalRead);

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