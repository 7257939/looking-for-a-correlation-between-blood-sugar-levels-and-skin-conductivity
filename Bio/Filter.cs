using System;

namespace Bio
{
    internal interface IFilter
    {
        (bool, double) Calculate(double data);
    }

    internal class Median : IFilter
    {
        private double a;
        private double b;
        private double c;
        private int FilterCounter;
        public (bool, double) Calculate(double data)
        {
            int FilterLength = 3;
            a = b;
            b = c;
            c = data;
            if (FilterCounter++ == FilterLength)
            {
                FilterCounter = 0;
                return (true, (a < b) ? ((b < c) ? b : ((c < a) ? a : c)) : ((a < c) ? a : ((c < b) ? b : c)));
            }
            else
            {
                return (false, 0);
            }

        }

    }

    internal class Kalman : IFilter
    {
        readonly double _q = 0.02;
        private double _err_measure = 0.8;
        private double _err_estimate = 0.8;
        private double _last_estimate;
        private int FilterCounter;
        public (bool, double) Calculate(double newVal)
        {
            int FilterLength = 5;
            double _kalman_gain, _current_estimate;
            _kalman_gain = _err_estimate / (_err_estimate + _err_measure);
            _current_estimate = _last_estimate + (double)_kalman_gain * (newVal - _last_estimate);
            _err_estimate = (1.0 - _kalman_gain) * _err_estimate + Math.Abs(_last_estimate - _current_estimate) * _q;
            _last_estimate = _current_estimate;
            if (FilterCounter++ == FilterLength)
            {
                FilterCounter = 0;
                return (true, _current_estimate);
            }
            else
            {
                return (false, 0);
            }
        }

    }

    internal class Average : IFilter
    {
        private static readonly int AverageLength = 10;
        private readonly double[] yi = new double[AverageLength];
        private readonly double[] xiyi = new double[AverageLength];
        private int FilterCounter;
        public (bool, double) Calculate(double data)
        {
            int FilterLength = AverageLength;
            double sum1 = 0;
            double sum2 = 0;
            double sum3 = 0;
            double sum4 = 0;
            double a, b;

            for (int i = 0; i < AverageLength - 1; i++)
            {
                yi[i] = yi[i + 1];
                xiyi[i] = i * yi[i];
                sum1 += yi[i];
                sum2 += xiyi[i];
                sum3 += i;
                sum4 += i * i;
            }
            yi[AverageLength - 1] = data;
            xiyi[AverageLength - 1] = (AverageLength - 1) * data;
            sum1 += yi[AverageLength - 1];
            sum2 += xiyi[AverageLength - 1];
            sum3 += AverageLength - 1;
            sum4 += (AverageLength - 1) * (AverageLength - 1);
            a = (AverageLength * sum2 - sum3 * sum1) / (AverageLength * sum4 - sum3 * sum3);
            b = (sum1 - a * sum3) / AverageLength;
            if (FilterCounter++ == FilterLength)
            {
                FilterCounter = 0;
                return (true, a * AverageLength + b);
            }
            else
            {
                return (false, 0);
            }

        }

    }
    /*
        internal class Filter
        {
            private double a;
            private double b;
            private double c;
            private int FilterCounter;

            readonly double _q = 0.02;
            private double _err_measure = 0.8;
            private double _err_estimate = 0.8;
            private double _last_estimate;

            private static readonly int AverageLength = 10;
            private readonly double[] yi  = new double[AverageLength];
            private readonly double[] xiyi = new double[AverageLength];

            public (bool,double) Median(double data)
            {
                int FilterLength = 3;
                a = b;
                b = c;
                c = data;
                if (FilterCounter++ == FilterLength)
                {
                    FilterCounter = 0;
                    return (true, (a < b) ? ((b < c) ? b : ((c < a) ? a : c)) : ((a < c) ? a : ((c < b) ? b : c)));
                }
                else
                {
                    return (false, 0);
                }

            }

            public (bool,double) Kalman(double newVal)
            {
                int FilterLength = 5;
                double _kalman_gain, _current_estimate;
                _kalman_gain = _err_estimate / (_err_estimate + _err_measure);
                _current_estimate = _last_estimate + (double)_kalman_gain * (newVal - _last_estimate);
                _err_estimate = (1.0 - _kalman_gain) * _err_estimate + Math.Abs(_last_estimate - _current_estimate) * _q;
                _last_estimate = _current_estimate;
                if (FilterCounter++ == FilterLength)
                {
                    FilterCounter = 0;
                    return (true, _current_estimate);
                }
                else
                {
                    return (false, 0);
                }
            }

            public (bool,double) Average(double data)
            {
                int FilterLength = AverageLength;
                double sum1 = 0;
                double sum2 = 0;
                double sum3 = 0;
                double sum4 = 0;
                double a, b;

                for (int i = 0; i < AverageLength - 1 ; i++)
                {
                    yi[i] = yi[i + 1];
                    xiyi[i] = i * yi[i];
                    sum1 +=yi[i];
                    sum2 += xiyi[i];
                    sum3 += i;
                    sum4 += i * i;
                }
                yi[AverageLength - 1] = data;
                xiyi[AverageLength - 1] = (AverageLength-1) * data;
                sum1 += yi[AverageLength - 1];
                sum2 += xiyi[AverageLength - 1];
                sum3 += AverageLength - 1;
                sum4 += (AverageLength - 1) * (AverageLength - 1);
                a = (AverageLength * sum2 - sum3 * sum1) / (AverageLength * sum4 - sum3 * sum3);
                b = (sum1 - a * sum3) / AverageLength;
                if (FilterCounter++ == FilterLength)
                {
                    FilterCounter = 0;
                    return (true, a * AverageLength + b);
                }
                else
                {
                    return (false, 0);
                }

            }
        }
    */
}
