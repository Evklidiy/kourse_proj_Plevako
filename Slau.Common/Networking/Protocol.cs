namespace Slau.Common.Networking
{
    public enum ProtocolCommand : byte
    {
        IDLE = 0,
        INIT_CHUNKS = 1,    // Рассылка частей матрицы воркерам
        PROCESS_GAUSS = 2,  // Команда на выполнение шага Гаусса
        UPDATE_DATA = 3,    // Обновление данных (если нужно)
        GET_RESULT = 4,     // Сбор решения X
        STATUS_CHECK = 5,   // Проверка готовности воркера
        ERROR = 255
    }
}