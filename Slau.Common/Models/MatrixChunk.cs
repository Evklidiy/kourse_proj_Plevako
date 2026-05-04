using System;

namespace Slau.Common.Models
{
    [Serializable]
    public class MatrixChunk
    {
        public int StartRowIndex { get; set; } // С какой строки начинается блок
        public int RowCount { get; set; }      // Количество строк в блоке
        public int TotalColumns { get; set; }  // N + 1
        public double[] Data { get; set; }     // Плоский массив данных для передачи
    }
}