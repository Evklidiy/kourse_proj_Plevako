namespace Slau.Common.Networking
{
    public enum ProtocolCommand : byte
    {
        IDLE = 0,
        INIT_CHUNKS = 1,
        PROCESS_GAUSS = 2,
        GET_RESULT = 3,
        GET_SPECIFIC_ROW = 4, // НОВАЯ КОМАНДА: получить одну строку от воркера
        STATUS_CHECK = 5,
        ERROR = 255
    }
}