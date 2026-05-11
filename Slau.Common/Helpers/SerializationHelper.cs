using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Slau.Common.Helpers
{
    public static class SerializationHelper
    {
        // Игнорируем предупреждение об устаревании BinaryFormatter для учебного проекта
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

        // Обычный метод десериализации (возвращает object)
        public static object Deserialize(byte[] data)
        {
            if (data == null) return null;
            using (var ms = new MemoryStream(data))
            {
                var formatter = new BinaryFormatter();
                return formatter.Deserialize(ms);
            }
        }

        // НОВЫЙ УНИВЕРСАЛЬНЫЙ МЕТОД (исправляет ошибку CS0308)
        public static T Deserialize<T>(byte[] data)
        {
            object result = Deserialize(data);
            return (T)result;
        }

#pragma warning restore SYSLIB0011
    }
}