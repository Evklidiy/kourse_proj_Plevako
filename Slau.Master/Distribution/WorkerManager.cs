using System.Collections.Generic;
using System.Net.Sockets;

namespace Slau.Master.Distribution
{
    public class WorkerNode
    {
        public string Ip { get; set; }
        public int Port { get; set; }
        public bool IsAlive { get; set; }
    }

    public class WorkerManager
    {
        public List<WorkerNode> Nodes { get; } = new List<WorkerNode>();

        public void AddWorker(string ip, int port)
        {
            Nodes.Add(new WorkerNode { Ip = ip, Port = port, IsAlive = true });
        }
    }
}