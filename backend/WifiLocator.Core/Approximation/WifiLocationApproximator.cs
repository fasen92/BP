
using MathNet.Numerics.LinearAlgebra;
using System;
using WifiLocator.Core.Approximation.Interfaces;
using WifiLocator.Core.Models;


namespace WifiLocator.Core.Approximation
{
    public class WifiLocationApproximator(IGeoConverter geoConverter, IClustering clustering) : IWifiLocationApproximator
    {
        private readonly IGeoConverter _geoConverter = geoConverter;
        private readonly IClustering _clustering = clustering;

        public WifiModel PerformApproximation(WifiModel wifiModel)
        {
            if (wifiModel.Locations == null || wifiModel.Locations.Count == 0)
            {
                wifiModel.ApproximatedLatitude = null;
                wifiModel.ApproximatedLongitude = null;
                wifiModel.UncertaintyRadius = null;
                return wifiModel;
            }

            // Filter the locations based on accuracy, sort them from the latest one
            List<LocationModel> locationList = FilterAndSortLocations(wifiModel);

            if (locationList.Count < 3)
            {
                wifiModel.ApproximatedLatitude = null;
                wifiModel.ApproximatedLongitude = null;
                wifiModel.UncertaintyRadius = null;
                wifiModel.Locations?.Clear();
                List<LocationModel> newLocations = locationList.Where(location => location.Id == Guid.Empty).ToList();
                foreach (LocationModel location in newLocations)
                {
                    wifiModel.Locations?.Add(location); // return new location to save through wifiModel
                }

                return wifiModel;
            }

            WifiDataObject wifiDataObject = new(wifiModel, locationList)
            {
                PreviousUsedLocations = new HashSet<LocationModel>(
                wifiModel.Locations.Where(l => l.UsedForApproximation)
            )
            };

            wifiDataObject = SelectLocationsForApproximation(wifiDataObject);


            if (wifiDataObject.LocationsForApproximation.Count == 0)
            {
                wifiModel.ApproximatedLatitude = null;
                wifiModel.ApproximatedLongitude = null;
                wifiModel.UncertaintyRadius = null;
                wifiModel.Locations.Clear();
                foreach (LocationModel location in wifiDataObject.LocationsToSave)
                {
                    wifiModel.Locations?.Add(location); // return new location to save through wifiModel
                }
                return wifiModel;
            }

            wifiDataObject = CalculatePosition(wifiDataObject);


            if (wifiDataObject.WifiModel.ApproximatedLatitude != null && wifiDataObject.WifiModel.ApproximatedLongitude != null)
            {
                wifiDataObject.LocationsToSave.AddRange(SetUsedForApproximation(wifiDataObject.LocationsForApproximation, wifiDataObject.PreviousUsedLocations));
            }
            else
            {
                wifiDataObject.LocationsToSave.AddRange(wifiDataObject.LocationsForApproximation.Where(location => location.Id == Guid.Empty).ToList());
            }

            wifiDataObject.WifiModel.Locations?.Clear();
            foreach (LocationModel location in wifiDataObject.LocationsToSave)
            {
                wifiDataObject.WifiModel.Locations?.Add(location);
            }

            if (HasLocationChanged(wifiModel, wifiDataObject.WifiModel, 30.0))
            {
                wifiDataObject.WifiModel.Address = AddressModel.Empty; // Assigning blank address triggers reevaluation
            }

            return wifiDataObject.WifiModel;
        }

        private WifiDataObject CalculatePosition(WifiDataObject wifiDataObject)
        {
            wifiDataObject = LeastSquares(wifiDataObject, out bool calculationSuccess);

            if (!calculationSuccess)
            {
                WeightedCentroid(wifiDataObject);
            }

            return wifiDataObject;
        }

        /**
         * This version of Non-Linear Least Squares approach is adapted and modified from:
         * Autonomous smartphone-based WiFi positioning system by using access points localization and crowdsourcing
         * Y. Zhuang , Z. Syed , J. Georgy b, N. El-Sheimy 
         * https://doi.org/10.1016/j.pmcj.2015.02.001
         */
        private WifiDataObject LeastSquares(WifiDataObject wifiDataObject, out bool calculationSuccess)
        {
            List<LocationModel> locations = wifiDataObject.LocationsForApproximation;
            if (locations.Count < 3)
            {
                calculationSuccess = false;
                return wifiDataObject;
            }
                
            double centroidLatitude = locations.Average(loc => loc.Latitude);
            double centroidLongitude = locations.Average(loc => loc.Longitude);
            var originECEF = _geoConverter.GPSToECEF(centroidLatitude, centroidLongitude, 0);

            List<PointValues> points = locations.Select(loc =>
            {
                (double x, double y, double z) ecef = _geoConverter.GPSToECEF(loc.Latitude, loc.Longitude, 0);
                (double east, double north) = _geoConverter.EcefToEnu(ecef, originECEF, centroidLatitude, centroidLongitude);
                return new PointValues(east, north, loc.SignaldBm);
            }).ToList();

            points = MergeNearbyPoints(points, 3.0);

            double meanX = points.Average(p => p.X);
            double meanY = points.Average(p => p.Y);

            double totalRssi = points.Sum(p => Math.Abs(p.RssidBm));

            Vector<double> parameters = Vector<double>.Build.DenseOfArray([meanX, meanY, 3.0, 35.0]);
            for (int iter = 0; iter < 30; iter++)
            {
                Vector<double> h = Model(parameters, points);
                Vector<double> z = Vector<double>.Build.Dense(points.Count, i => points[i].RssidBm);
                Vector<double> residuals = z - h;
                Matrix<double> J = Jacobian(parameters, points);

                double delta = 5.0; 
                Vector<double> weightedResiduals = Vector<double>.Build.Dense(points.Count);
                Matrix<double> weightedJacobian = Matrix<double>.Build.Dense(points.Count, 4);

                for (int i = 0; i < points.Count; i++)
                {
                    double r = residuals[i];
                    double signalWeight = Math.Abs(points[i].RssidBm) / totalRssi;
                    double totalWeight = 1.0;
              
                    double huber = HuberWeight(r, delta);
                    totalWeight = signalWeight * huber;
                    

                    weightedResiduals[i] = r * totalWeight;

                    for (int j = 0; j < 4; j++)
                    {
                        weightedJacobian[i, j] = J[i, j] * totalWeight;
                    }
                }

                Matrix<double> JT = weightedJacobian.Transpose();
                Matrix<double> JTJ = JT * weightedJacobian;
                Vector<double> JTr = JT * weightedResiduals;

                Vector<double> deltaParams;
                try
                {
                    deltaParams = JTJ.Solve(JTr);
                }
                catch
                {
                    break;
                }

                parameters += deltaParams;

                if (deltaParams.L2Norm() < 1e-6 && residuals.L2Norm() < 1.0) break;

                if (parameters.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
                {
                    break;
                }

                double n = parameters[2];
                double A = parameters[3];
                if (n < 0.1 || n > 10 || A < 0 || A > 150)
                {
                    break;
                }
            }

            if (parameters[2] < 2.0 || parameters[2] > 6.0 || double.IsNaN(parameters[2]))
            {
                calculationSuccess = false; // invalid path loss exponent
                return wifiDataObject;
            }
            if (parameters[3] < 0.0 || parameters[3] > 100.0 || double.IsNaN(parameters[3]))
            {
                // approximation is too far from centroid
                calculationSuccess = false; 
                return wifiDataObject;
            }

            List<Vector<double>> positions = [];
            foreach (PointValues point in points)
            {
                positions.Add(Vector<double>.Build.DenseOfArray(new double[] { point.X, point.Y}));
            }

            double HDOP = CalculateHDOP(positions, parameters[0], parameters[1]);

            if (HDOP > 6.0 || double.IsNaN(HDOP))
            {
                // the approximation error is too large
                calculationSuccess = false;
                return wifiDataObject;
            }

            var estimatedECEF = Add(originECEF,
                Add(Multiply(_geoConverter.EastUnitVector(centroidLongitude), parameters[0]),
                    Multiply(_geoConverter.NorthUnitVector(centroidLatitude, centroidLongitude), parameters[1])));

            var (finalLatitude, finalLongitude) = _geoConverter.ECEFToGPS(estimatedECEF.x, estimatedECEF.y, estimatedECEF.z);

            wifiDataObject.WifiModel.ApproximatedLatitude = finalLatitude;
            wifiDataObject.WifiModel.ApproximatedLongitude = finalLongitude;
            wifiDataObject.WifiModel.UncertaintyRadius = CalculatePossibleRange(locations, finalLatitude, finalLongitude, -parameters[3], parameters[2], HDOP);
            calculationSuccess = true;
            return wifiDataObject;
        }

        private static (double x, double y, double z) Add((double x, double y, double z) a, (double x, double y, double z) b) =>
            (a.x + b.x, a.y + b.y, a.z + b.z);

        private static (double x, double y, double z) Multiply((double x, double y, double z) v, double scalar) =>
            (v.x * scalar, v.y * scalar, v.z * scalar);

        static Vector<double> Model(Vector<double> parameters, List<PointValues> points)
        {
            // parameters = [x0, y0, n, A]
            var result = Vector<double>.Build.Dense(points.Count);
            double x0 = parameters[0], y0 = parameters[1], n = parameters[2], A = parameters[3];

            for (int i = 0; i < points.Count; i++)
            {
                double dx = x0 - points[i].X;
                double dy = y0 - points[i].Y;
                double d = Math.Sqrt(dx * dx + dy * dy);
                if (d < 1e-6) d = 1e-6;
                result[i] = - A - 10 * n * Math.Log10(d);
            }
            return result;
        }

        static Matrix<double> Jacobian(Vector<double> parameters, List<PointValues> points)
        {
            var J = Matrix<double>.Build.Dense(points.Count, 4);
            double x0 = parameters[0], y0 = parameters[1], n = parameters[2];

            for (int i = 0; i < points.Count; i++)
            {
                double dx = x0 - points[i].X;
                double dy = y0 - points[i].Y;
                double d = Math.Sqrt(dx * dx + dy * dy);
                if (d < 1e-6)
                    d = 1e-6;
                double logD = Math.Log10(d);

                J[i, 0] = -10 * n * (dx / (d * d * Math.Log(10))); // dRSS / dx0
                J[i, 1] = -10 * n * (dy / (d * d * Math.Log(10))); // dRSS / dy0
                J[i, 2] = -10 * logD;                              // dRSS / dn
                J[i, 3] = - 1;                                     // dRSS / dA
            }
            return J;
        }

        private static double HuberWeight(double r, double delta)
        {
            double absR = Math.Abs(r);
            return absR <= delta ? 1.0 : delta / absR;
        }

        static List<PointValues> MergeNearbyPoints(List<PointValues> points, double epsilon)
        {
            var merged = new List<PointValues>();
            var visited = new bool[points.Count];

            for (int i = 0; i < points.Count; i++)
            {
                if (visited[i]) continue;

                var cluster = new List<PointValues> { points[i] };
                visited[i] = true;

                for (int j = i + 1; j < points.Count; j++)
                {
                    if (visited[j]) continue;

                    double dx = points[i].X - points[j].X;
                    double dy = points[i].Y - points[j].Y;
                    double dist = Math.Sqrt(dx * dx + dy * dy);

                    if (dist < epsilon)
                    {
                        cluster.Add(points[j]);
                        visited[j] = true;
                    }
                }

                double sumWeights = cluster.Sum(p => Math.Abs(p.RssidBm));
                double x = cluster.Sum(p => p.X * Math.Abs(p.RssidBm)) / sumWeights;
                double y = cluster.Sum(p => p.Y * Math.Abs(p.RssidBm)) / sumWeights;
                double rssi = cluster.Sum(p => p.RssidBm * Math.Abs(p.RssidBm)) / sumWeights;

                merged.Add(new PointValues(x, y, rssi));
            }

            return merged;
        }


        private WifiDataObject WeightedCentroid(WifiDataObject wifiDataObject)
        {
            double totalWeight = 0;
            double sumLatitude = 0;
            double sumLongitude = 0;

            foreach (LocationModel location in wifiDataObject.LocationsForApproximation)
            {
                double weight = Math.Pow(10, (location.SignaldBm + 100) / 10.0);
                sumLatitude += location.Latitude * weight;
                sumLongitude += location.Longitude * weight;
                totalWeight += weight;
            }

            
            double centroidLatitude = sumLatitude / totalWeight;
            double centroidLongitude = sumLongitude / totalWeight;
            var originECEF = _geoConverter.GPSToECEF(centroidLatitude, centroidLongitude, 0);

            List<Vector<double>> positions = wifiDataObject.LocationsForApproximation.Select(loc =>
            {
                (double x, double y, double z) ecef = _geoConverter.GPSToECEF(loc.Latitude, loc.Longitude, 0);
                (double east, double north) = _geoConverter.EcefToEnu(ecef, originECEF, centroidLatitude, centroidLongitude);
                return Vector<double>.Build.DenseOfArray(new double[] { east, north });
            }).ToList();

            double HDOP = CalculateHDOP(positions,0,0);

            var Parameters = FindBestParameters(wifiDataObject.LocationsForApproximation, centroidLatitude, centroidLongitude);
            wifiDataObject.WifiModel.ApproximatedLatitude = centroidLatitude;
            wifiDataObject.WifiModel.ApproximatedLongitude = centroidLongitude;
            wifiDataObject.WifiModel.UncertaintyRadius = CalculatePossibleRange(wifiDataObject.LocationsForApproximation, centroidLatitude, centroidLongitude, Parameters.p0, Parameters.n, HDOP);

            return wifiDataObject;
        }

        private static double SignalToDistance(double rssidBm, double referenceSignaldBm, double pathLossExponent)
        {
            double distance = Math.Pow(10, (referenceSignaldBm - rssidBm) / (10 * pathLossExponent));
            return Math.Clamp(distance, 0.5, 100);
        }

        private static List<LocationModel> FilterAndSortLocations(WifiModel wifiModel)
        {
            const double MAX_ACCURACY = 60; // Maximum accuracy in meters
            const double MAX_RSSIDBM = -85;

            if (wifiModel == null || wifiModel.Locations == null)
            {
                return [];
            }

            List<LocationModel> filteredLocations = [];

            foreach (var location in wifiModel.Locations)
            {
                if (location.Accuracy <= MAX_ACCURACY && location.SignaldBm >= MAX_RSSIDBM)
                {
                    filteredLocations.Add(location);
                }
            }

            return filteredLocations.OrderByDescending(location => location.Seen).ToList(); ;
        }

        private bool HasLocationChanged(WifiModel oldWifi, WifiModel newWifi, double thresholdMeters)
        {
            if (!oldWifi.ApproximatedLatitude.HasValue || !oldWifi.ApproximatedLongitude.HasValue ||
                !newWifi.ApproximatedLatitude.HasValue || !newWifi.ApproximatedLongitude.HasValue)
            {

                return false;
            }
            else
            {
                double distance = _geoConverter.Haversine((double)oldWifi.ApproximatedLatitude, (double)oldWifi.ApproximatedLongitude, 
                                                          (double)newWifi.ApproximatedLatitude, (double)newWifi.ApproximatedLongitude);
                return distance >= thresholdMeters;
            }

        }

        public WifiDataObject SelectLocationsForApproximation(WifiDataObject wifiDataObject,
        double epsilon = 70,
        int minPoints = 2,
        double timeConstantDays = 100)
        {
            List<LocationModel> locations = wifiDataObject.LocationsAll;

            if (locations == null || locations.Count == 0)
            {
                return wifiDataObject;
            }

            if (!IsClusteringNeeded(wifiDataObject)){
                wifiDataObject.LocationsToSave = wifiDataObject.LocationsAll.Where(location => location.Id == Guid.Empty).ToList();
                wifiDataObject.LocationsForApproximation = wifiDataObject.LocationsAll.Where(location => location.UsedForApproximation).ToList();
                wifiDataObject.LocationsForApproximation.AddRange(wifiDataObject.LocationsToSave); 
                return wifiDataObject;
            }

            var clusters = _clustering.DBSCAN(locations, epsilon, minPoints);

            if (clusters.Count == 0)
            {
                wifiDataObject.LocationsToSave = locations.Where(location => location.Id == Guid.Empty).ToList();
                return wifiDataObject;
            }

            List<LocationModel> bestCluster = [];
            DateTime now = DateTime.Now;
            double bestScore = double.NegativeInfinity;
            int bestClusterId = 0;

            foreach (var cluster in clusters)
            {
                if (cluster.Key == -1)
                {
                    continue;
                }
                double totalWeight = 0;

                foreach (var point in cluster.Value)
                {
                    double deltaDays = (now - point.Seen).TotalDays;
                    // exponential decay
                    double weight = Math.Exp(-deltaDays / timeConstantDays);

                    totalWeight += weight;
                }

                if (totalWeight > 0)
                {
                    if (totalWeight > bestScore)
                    {
                        bestScore = totalWeight;
                        bestCluster = cluster.Value;
                        bestClusterId = cluster.Key;
                    }
                }
            }

            if (bestCluster.Count >= 3)
            {
                wifiDataObject.LocationsForApproximation = bestCluster;
            }

            List<LocationModel> newLocationsList = [];
            foreach (var cluster in clusters.Values)
            {
                foreach (var location in cluster)
                {
                    if (location.Id == Guid.Empty)
                    {
                        wifiDataObject.LocationsToSave.Add(location);
                    }
                }
            }

            return wifiDataObject;
        }

        private bool IsClusteringNeeded(WifiDataObject wifiDataObject)
        {
            if (wifiDataObject.WifiModel.ApproximatedLatitude != null && wifiDataObject.WifiModel.ApproximatedLatitude != null) {
                List<LocationModel> newLocations = wifiDataObject.LocationsAll.Where(location => location.Id == Guid.Empty).ToList();
                foreach (LocationModel newLocation in newLocations) {
                    if (_geoConverter.Haversine(newLocation.Latitude, newLocation.Longitude,
                        (double)wifiDataObject.WifiModel.ApproximatedLatitude, (double)wifiDataObject.WifiModel.ApproximatedLatitude)
                        > 30)
                    {
                        return true;
                    }
                }

                return false;
            }
            return true;
        }

        private double CalculatePossibleRange(List<LocationModel> usedLocations, double approxLatitude, double approxLongitude, double referenceSignaldBm, double pathLossExponent, double HDOP)
        {
            double weightedSum = 0;
            double totalWeight = 0;

            foreach (var location in usedLocations)
            {
                double spatialDistance = _geoConverter.Haversine(approxLatitude, approxLongitude, location.Latitude, location.Longitude);
                double signalDistance = SignalToDistance(location.SignaldBm, referenceSignaldBm, pathLossExponent);
                double localUncertainty = Math.Abs(spatialDistance - signalDistance) + location.Accuracy;

                double weight = 1.0 / (location.Accuracy * location.Accuracy);
                weightedSum += localUncertainty * weight;
                totalWeight += weight;
            }

            double maxSignal = usedLocations.OrderBy(location => location.SignaldBm).First().SignaldBm;
            double maxSignalDistance = SignalToDistance(maxSignal,referenceSignaldBm, pathLossExponent);

            double minDistance = 1;
            double maxDistance = Math.Max(minDistance, maxSignalDistance);

            double sumResult = weightedSum / totalWeight;
            return Math.Clamp(((sumResult * HDOP) / 2),1,maxDistance);
        }

        private (double n, double p0) FindBestParameters(List<LocationModel> usedLocations, double approxLatitude, double approxLongitude)
        {
            List<double> distances = usedLocations.Select(location => _geoConverter.Haversine(approxLatitude, approxLongitude, location.Latitude, location.Longitude)).ToList();
            List<double> rssiValues = usedLocations.Select(location => location.SignaldBm).ToList();
            List<double> logDistances = distances.Select(d => Math.Log10(d)).ToList();
            double avgLogDistance = logDistances.Average();
            double avgRSSI = rssiValues.Average();

            double numerator = 0;
            double denominator = 0;
            for (int i = 0; i < logDistances.Count; i++)
            {
                numerator += (logDistances[i] - avgLogDistance) * (rssiValues[i] - avgRSSI);
                denominator += (logDistances[i] - avgLogDistance) * (logDistances[i] - avgLogDistance);
            }

            double slope = numerator / denominator;
            double intercept = avgRSSI - slope * avgLogDistance;

            double n = -slope / 10.0;
            double p0 = intercept;
            n = Math.Clamp(n, 1.0, 6.0);
            p0 = Math.Clamp(p0, -70.0, -30.0);

            return (n, p0);
        }

        /*
         * Calculates Horizontal Dilution of Precision, based on standard GNSS estimation methods and on
         * Integrity analysis for GPS-based navigation of UAVs in urban environment
         * O. K. Ibrayev, I. Petrov, J. Hajiyev
         * https://doi.org/10.3390/drones3030066
         */
        private double CalculateHDOP(List<Vector<double>> positions, double ApproximatedLatitude, double ApproximatedLongitude)
        {
            int count = positions.Count;
            Matrix<double> G = Matrix<double>.Build.Dense(count, 3);

            var approxPosition = Vector<double>.Build.DenseOfArray(new double[] { ApproximatedLatitude, ApproximatedLongitude }); 

            for (int i = 0; i < count; i++)
            {
                var pos = positions[i];
                double distance = (pos - approxPosition).L2Norm();
                G[i, 0] = (pos[0] - approxPosition[0]) / distance; // normalized x 
                G[i, 1] = (pos[1] - approxPosition[1]) / distance; // normalized y
                G[i, 2] = 1; // constant for height
            }

            // covariance matrix: (G^T * G)^-1
            Matrix<double> GTG = G.Transpose() * G;
            Matrix<double> GTG_inv = GTG.Inverse();

            // sum of diagonal elements
            double trace = GTG_inv.Trace();

            return trace;
        }

        private static List<LocationModel> SetUsedForApproximation(List<LocationModel> newUsed, HashSet<LocationModel> previousUsed)
        {
            List<LocationModel> updatedLocations = [];
            foreach (LocationModel newLocation in newUsed)
            {
                newLocation.UsedForApproximation = true;
                if (!previousUsed.Remove(newLocation))
                {
                    updatedLocations.Add(newLocation);
                }
            }

            foreach (LocationModel previousLocation in previousUsed)
            {
                previousLocation.UsedForApproximation = false;
                updatedLocations.Add(previousLocation);
            }

            return updatedLocations;
        }
    }
}
