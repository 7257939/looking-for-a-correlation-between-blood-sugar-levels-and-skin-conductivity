using System;

namespace Bio
{
    internal interface ICalculator
    {
        double Calculate(double r, double i);
    }

    internal class Resistance : ICalculator
    {
        public double Calculate(double r, double i)
        {
            double testValue = Math.Sqrt(r * r + i * i);
            testValue *= 1.015290271488619e-9f;
            testValue = 1 / testValue;
            return testValue;
        }

    }

    internal class Phase : ICalculator
    {
        public double Calculate(double r, double i)
        {
            double testValue;
            if (r >= 0 & i >= 0)
            {
                testValue = Math.Atan(r / i) * (180 / Math.PI);
            }
            else if (r < 0 & i >= 0)
            {
                testValue = 180 + Math.Atan(r / i) * (180 / Math.PI);
            }
            else if (r < 0 & i < 0)
            {
                testValue = 180 + Math.Atan(r / i) * (180 / Math.PI);
            }
            else
            {
                testValue = 360 + Math.Atan(r / i) * (180 / Math.PI);
            }
            return testValue;
        }

    }
}
