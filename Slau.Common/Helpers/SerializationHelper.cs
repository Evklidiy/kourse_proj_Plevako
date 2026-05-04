using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Slau.Common.Helpers
{
    public static class SerializationHelper
    {
        // Используем BinaryFormatter для простоты реализации учебного проекта.
        // Игнорируем предупреждение об устаревании (SYSLIB0011).
#pragma warning disable SYSLIB0011

        public static byte[] Serialize(object obj)
        {
            if (obj == null) return null;
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public static object Deserialize(byte[] data)
        {
            if (data == null) return null;
            using (var ms = new MemoryStream(data))
            {
                var formatter = new BinaryFormatter();
                return formatter.Deserialize(ms);
            }
        }
#pragma warning restore SYSLIB0011
    }
}