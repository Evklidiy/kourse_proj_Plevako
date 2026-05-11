using System;
using System.Net;
using System.Net.Sockets;
using Slau.Common.Helpers;
using Slau.Common.Networking;
using Slau.Common.Models;

namespace Slau.Worker.Services
{
    public class WorkerServer
    {
        private readonly int _port;
        private readonly ComputationService _calcService;

        public WorkerServer(int port)
        {
            _port = port;
            _calcService = new ComputationService();
        }

        public void SetupLimits(int cores, long ramMb)
        {
            _calcService.MaxDegreeOfParallelism = cores;
            _calcService.MaxMemoryMb = ramMb;
        }

        public void Start()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, _port);
            listener.Start();
            Console.WriteLine($"[СЕРВЕР] Ожидание Мастера на порту {_port}...");

            while (true)
            {
                try
                {
                    using (TcpClient client = listener.AcceptTcpClient())
                    using (NetworkStream stream = client.GetStream())
                    {
                        byte[] lenBuf = new byte[4];
                        if (stream.Read(lenBuf, 0, 4) < 4) continue;
                        int len = BitConverter.ToInt32(lenBuf, 0);

                        byte[] buffer = new byte[len];
                        int totalRead = 0;
                        while (totalRead < len)
                        {
                            int read = stream.Read(buffer, totalRead, len - totalRead);
                            if (read == 0) break;
                            totalRead += read;
                        }

                        NetworkMessage msg = SerializationHelper.Deserialize<NetworkMessage>(buffer);
                        HandleCommand(msg, stream);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ОШИБКА] {ex.Message}");
                }
            }
        }

        private void HandleCommand(NetworkMessage msg, NetworkStream stream)
        {
            switch (msg.Command)
            {
                case ProtocolCommand.INIT_CHUNKS:
                    _calcService.Initialize((MatrixChunk)msg.Data);
                    Console.WriteLine($"[СЕТЬ] Данные чанка приняты.");
                    SendOkResponse(stream);
                    break;

                case ProtocolCommand.PROCESS_GAUSS:
                    PivotRowData pd = (PivotRowData)msg.Data;

                    // ВЕРНУЛ ЛОГИРОВАНИЕ: каждые 100 шагов
                    if (pd.PivotRowIndex % 100 == 0)
                        Console.WriteLine($"[РАСЧЕТ] Шаг метода Гаусса: {pd.PivotRowIndex}");

                    _calcService.ProcessGaussStep(pd);
                    SendOkResponse(stream);
                    break;

                case ProtocolCommand.GET_SPECIFIC_ROW:
                    int rIdx = Convert.ToInt32(msg.Data);
                    byte[] rowData = SerializationHelper.Serialize(_calcService.GetRowForMaster(rIdx));
                    stream.Write(BitConverter.GetBytes(rowData.Length), 0, 4);
                    stream.Write(rowData, 0, rowData.Length);
                    break;

                case ProtocolCommand.GET_RESULT:
                    Console.WriteLine("[СЕТЬ] Отправка итогового вектора X...");
                    byte[] res = SerializationHelper.Serialize(_calcService.GetResults());
                    stream.Write(BitConverter.GetBytes(res.Length), 0, 4);
                    stream.Write(res, 0, res.Length);
                    break;
            }
        }

        private void SendOkResponse(NetworkStream stream)
        {
            byte[] ok = BitConverter.GetBytes(true);
            stream.Write(BitConverter.GetBytes(ok.Length), 0, 4);
            stream.Write(ok, 0, ok.Length);
        }
    }
}