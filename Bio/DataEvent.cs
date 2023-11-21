using OxyPlot;
using System;
using System.Collections.Generic;

namespace Bio
{
    internal class DataEvent : EventArgs
    {
        public List<DataPoint> data;
        public int Progress;
    }
}
