using System;

namespace Slau.Common.Networking
{
    [Serializable]
    public class NetworkMessage
    {
        public ProtocolCommand Command { get; set; }
        public object Data { get; set; } // Полезная нагрузка (Chunk, PivotRow и т.д.)
    }
}