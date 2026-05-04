using System;

namespace Slau.Common.Models
{
    [Serializable]
    public class PivotRowData
    {
        public int PivotRowIndex { get; set; } // Индекс k
        public double[] Row { get; set; }      // Сама нормализованная строка
    }
}