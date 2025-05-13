using WifiLocator.Core.Approximation.Interfaces;

namespace WifiLocator.Core.Approximation
{
    public class GeoCoordinateConverter : IGeoConverter
    {
        const double equatorialRadius = 6378137.0;
        const double eccentricity = 8.1819190842622e-2;


        // Based on standard WGS84 ECEF formula 
        // https://en.wikipedia.org/wiki/Geographic_coordinate_conversion#From_geodetic_to_ECEF_coordinates
        public (double x, double y, double z) GPSToECEF(double latitude, double longitude, double altitude)
        {
            // WGS84 ellipsoid constants
            double latitudeRadians = DegreesToRadians(latitude);
            double longitudeRadians = DegreesToRadians(longitude);

            double N = equatorialRadius / Math.Sqrt(1 - eccentricity * eccentricity * Math.Sin(latitudeRadians) * Math.Sin(latitudeRadians));

            // ECEF coordinate calculations
            double x = (N + altitude) * Math.Cos(latitudeRadians) * Math.Cos(longitudeRadians);
            double y = (N + altitude) * Math.Cos(latitudeRadians) * Math.Sin(longitudeRadians);
            double z = ((1 - eccentricity * eccentricity) * N + altitude) * Math.Sin(latitudeRadians);

            return (x, y, z);
        }


        /*
         * ECEF to Geodetic coordinate conversion using WGS84 reference ellipsoid
         * Bowring's irrational geodetic-latitude equation
         * https://en.wikipedia.org/wiki/Geographic_coordinate_conversion#From_ECEF_to_geodetic_coordinates
         * https://www.mathworks.com/help/aeroblks/ecefpositiontolla.html
         */
        public (double latitude, double longitude) ECEFToGPS(double x, double y, double z)
        {
            double polarRadius = Math.Sqrt(equatorialRadius * equatorialRadius * (1 - eccentricity * eccentricity));
            double secondEccentricity = Math.Sqrt((equatorialRadius * equatorialRadius - polarRadius * polarRadius) / (polarRadius * polarRadius));

            // ECEF to Geodetic conversion
            double distance = Math.Sqrt(x * x + y * y);
            double angle = Math.Atan2(equatorialRadius * z, polarRadius * distance);

            
            double latitudeRadians = Math.Atan2(
                z + secondEccentricity * secondEccentricity * polarRadius * Math.Pow(Math.Sin(angle), 3),
                distance - eccentricity * eccentricity * equatorialRadius * Math.Pow(Math.Cos(angle), 3)
            );

            // radius of curvature 
            double N = equatorialRadius / Math.Sqrt(1 - eccentricity * eccentricity * Math.Sin(latitudeRadians) * Math.Sin(latitudeRadians));

            // convert radians back to degrees
            double latitude = RadiansToDegrees(latitudeRadians);


            double longitudeRadians = Math.Atan2(y, x);
            double longitude = RadiansToDegrees(longitudeRadians);

            return (latitude, longitude);
        }


        /*
         * Conversion from ECEF to ENU system
         * based on https://en.wikipedia.org/wiki/Geographic_coordinate_conversion#From_ECEF_to_ENU
         */
        public (double east, double north) EcefToEnu(
            (double x, double y, double z) point,
            (double x, double y, double z) origin,
            double originLatitude, double originLongitude)
        {
            double latitudeRadians = DegreesToRadians(originLatitude);
            double longitudeRadians = DegreesToRadians(originLongitude);

            var dx = point.x - origin.x;
            var dy = point.y - origin.y;
            var dz = point.z - origin.z;

            double east = -Math.Sin(longitudeRadians) * dx + Math.Cos(longitudeRadians) * dy;
            double north = -Math.Sin(latitudeRadians) * Math.Cos(longitudeRadians) * dx
                         - Math.Sin(latitudeRadians) * Math.Sin(longitudeRadians) * dy
                         + Math.Cos(latitudeRadians) * dz;

            return (east, north);
        }


        /*
         * Vectors for conversion from ENU to ECEF
         * based on https://en.wikipedia.org/wiki/Geographic_coordinate_conversion#From_ENU_to_ECEF
         */
        public (double x, double y, double z) NorthUnitVector(double latitude, double longitude)
        {
            double latitudeRadians = DegreesToRadians(latitude);
            double longitudeRadians = DegreesToRadians(longitude);

            return (
                -Math.Sin(latitudeRadians) * Math.Cos(longitudeRadians),
                -Math.Sin(latitudeRadians) * Math.Sin(longitudeRadians),
                 Math.Cos(latitudeRadians)
            );
        }

        public (double x, double y, double z) EastUnitVector(double longitude)
        {
            double longitudeRadians = DegreesToRadians(longitude);

            return (-Math.Sin(longitudeRadians), Math.Cos(longitudeRadians), 0);
        }

        /*
         * This method was adapted from
         * https://community.esri.com/t5/coordinate-reference-systems-blog/distance-on-a-sphere-the-haversine-formula/ba-p/902128
         */
        public double Haversine(double lat1, double lon1, double lat2, double lon2)
        {
            double phi1 = DegreesToRadians(lat1);
            double phi2 = DegreesToRadians(lat2);
            double deltaPhi = DegreesToRadians(lat2 - lat1);
            double deltaLambda = DegreesToRadians(lon2 - lon1);

            // Haversine formula
            double a = Math.Sin(deltaPhi / 2) * Math.Sin(deltaPhi / 2) +
                       Math.Cos(phi1) * Math.Cos(phi2) *
                       Math.Sin(deltaLambda / 2) * Math.Sin(deltaLambda / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            // distance in meters
            return equatorialRadius * c;
        }

        private static double RadiansToDegrees(double radians)
        {
            return radians * 180.0 / Math.PI;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}
