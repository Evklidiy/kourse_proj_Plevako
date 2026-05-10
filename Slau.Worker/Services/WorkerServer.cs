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
            Console.WriteLine($"[СЕРВЕР] Ожидание подключения Мастера на порту {_port}...");

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

                        NetworkMessage msg = (NetworkMessage)SerializationHelper.Deserialize(buffer);
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
                    Console.WriteLine($"[СЕТЬ] Данные приняты.");
                    SendOkResponse(stream); // Подтверждаем мастеру готовность
                    break;

                case ProtocolCommand.PROCESS_GAUSS:
                    PivotRowData pd = (PivotRowData)msg.Data;
                    if (pd.PivotRowIndex % 500 == 0)
                        Console.WriteLine($"[РАСЧЕТ] Шаг: {pd.PivotRowIndex}");

                    _calcService.ProcessGaussStep(pd);
                    SendOkResponse(stream); // Подтверждаем выполнение шага
                    break;

                case ProtocolCommand.GET_RESULT:
                    byte[] responseData = SerializationHelper.Serialize(_calcService.GetResults());
                    stream.Write(BitConverter.GetBytes(responseData.Length), 0, 4);
                    stream.Write(responseData, 0, responseData.Length);
                    break;

                case ProtocolCommand.STATUS_CHECK:
                    SendOkResponse(stream);
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