using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WifiLocator.Core.Approximation.Interfaces
{
    public interface IGeoConverter
    {
        (double x, double y, double z) GPSToECEF(double latitude, double longitude, double altitude);
        (double latitude, double longitude) ECEFToGPS(double x, double y, double z);
        (double east, double north) EcefToEnu((double x, double y, double z) point, (double x, double y, double z) origin, double originLat, double originLon);
        (double x, double y, double z) NorthUnitVector(double latitude, double longitude);
        (double x, double y, double z) EastUnitVector( double longitude);
        double Haversine(double latitude1, double longitude1, double latitude2, double longitude2);
    }
}
