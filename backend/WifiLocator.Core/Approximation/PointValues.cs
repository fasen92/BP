using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WifiLocator.Core.Approximation
{
    public class PointValues(double x, double y, double rssidBm)
    {
        public double X = x;
        public double Y = y;
        public double RssidBm = rssidBm;
    }
}
