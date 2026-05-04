using System;

namespace Slau.Common.Models
{
    [Serializable]
    public class CalculationSettings
    {
        public int Dimension { get; set; }     // Число неизвестных N
        public double Epsilon { get; set; }    // Точность для метода релаксации
        public int MaxIterations { get; set; } // Лимит итераций
    }
}