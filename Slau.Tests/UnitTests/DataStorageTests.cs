using NUnit.Framework;
using Slau.Master.Storage;
using Slau.Common.Helpers;
using Slau.Common.Models;
using System.IO;

namespace Slau.Tests.UnitTests
{
    [TestFixture]
    public class DataStorageTests
    {
        [Test] // 7. Запись и чтение строки из MMF
        public void MMF_SetAndGetRow_Integrity()
        {
            using (var storage = new LargeMatrixStorage(10))
            {
                double[] row = { 1.1, 2.2, 3.3, 4.4, 5.5, 6.6, 7.7, 8.8, 9.9, 0.0, 100.0 };
                storage.SetRow(5, row);
                var read = storage.GetRow(5);
                Assert.That(read, Is.EqualTo(row));
            }
        }

        [Test] // 8. Перезапись строки в MMF
        public void MMF_OverwriteRow_CorrectData()
        {
            using (var storage = new LargeMatrixStorage(5))
            {
                double[] r1 = { 1, 1, 1, 1, 1, 1 };
                double[] r2 = { 2, 2, 2, 2, 2, 2 };
                storage.SetRow(0, r1);
                storage.SetRow(0, r2);
                Assert.That(storage.GetRow(0), Is.EqualTo(r2));
            }
        }

        [Test] // 9. Сериализация MatrixChunk
        public void Serialization_MatrixChunk_Consistency()
        {
            var chunk = new MatrixChunk { StartRowIndex = 0, Data = new[] { 1.0, 2.0 } };
            var bytes = SerializationHelper.Serialize(chunk);
            var back = (MatrixChunk)SerializationHelper.Deserialize(bytes);
            Assert.That(back.Data, Is.EqualTo(chunk.Data));
        }

        [Test] // 10. Сериализация NetworkMessage
        public void Serialization_Message_Consistency()
        {
            var msg = new Slau.Common.Networking.NetworkMessage { Command = Slau.Common.Networking.ProtocolCommand.IDLE };
            var bytes = SerializationHelper.Serialize(msg);
            var back = (Slau.Common.Networking.NetworkMessage)SerializationHelper.Deserialize(bytes);
            Assert.That(back.Command, Is.EqualTo(msg.Command));
        }

        [Test] // 11. Создание файла при инициализации Storage
        public void MMF_FileCreation_Exists()
        {
            using (var s = new LargeMatrixStorage(5))
            {
                string path = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "matrix_data.bin");
                Assert.That(File.Exists(path), Is.True);
            }
        }

        [Test] // 12. Инициализация пустого воркера
        public void WorkerManager_InitialCount_Zero()
        {
            var wm = new Slau.Master.Distribution.WorkerManager();
            Assert.That(wm.Nodes, Is.Empty);
        }

        [Test] // 13. Добавление воркера
        public void WorkerManager_AddWorker_IncrementsCount()
        {
            var wm = new Slau.Master.Distribution.WorkerManager();
            wm.AddWorker("127.0.0.1", 5000);
            Assert.That(wm.Nodes.Count, Is.EqualTo(1));
        }

        [Test] // 14. Проверка свойств воркера
        public void WorkerNode_Properties_Correct()
        {
            var node = new Slau.Master.Distribution.WorkerNode { Ip = "1.1.1.1", Port = 80 };
            Assert.That(node.Ip, Is.EqualTo("1.1.1.1"));
            Assert.That(node.Port, Is.EqualTo(80));
        }
    }
}