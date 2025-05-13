using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WifiLocator.Core.Approximation.Interfaces;
using WifiLocator.Core.Models;

namespace WifiLocator.Core.Approximation
{
    public class Clustering(IGeoConverter geoConverter) : IClustering
    {
        private readonly IGeoConverter _geoConverter = geoConverter;

        /**
         * DBSCAN algorithm implemented based on principles by 
         * Ester, M., Kriegel, H.-P., Sander, J., & Xu, X. (1996)
         * A Density-Based Algorithm for Discovering Clusters in Large Spatial Databases with Noise
         */
        public Dictionary<int, List<LocationModel>> DBSCAN(List<LocationModel> points, double epsilon, int minPoints)
        {
            int n = points.Count;
            int[] clusterIds = new int[n];
            bool[] visited = new bool[n];
            int clusterId = 0;

            for (int i = 0; i < n; i++)
            {
                if (visited[i])
                    continue;
                visited[i] = true;
                var neighbours = GetNeighbours(points, i, epsilon);
                if (neighbours.Count < minPoints)
                {
                    // mark as noise
                    clusterIds[i] = -1;
                }
                else
                {
                    clusterId++;
                    ExpandCluster(points, clusterIds, visited, i, neighbours, clusterId, epsilon, minPoints);
                }
            }


            // build dictionary by clusters
            var clusters = new Dictionary<int, List<LocationModel>>();
            for (int i = 0; i < n; i++)
            {
                if (!clusters.ContainsKey(clusterIds[i]))
                    clusters[clusterIds[i]] = new List<LocationModel>();
                clusters[clusterIds[i]].Add(points[i]);
            }
            return clusters;
        }

        // find immediate neighbours of the point
        private List<int> GetNeighbours(List<LocationModel> points, int index, double epsilon)
        {
            List<int> neighbors = [];
            var point = points[index];
            for (int j = 0; j < points.Count; j++)
            {
                if (j == index)
                    continue;
                var candidate = points[j];
                double distance = _geoConverter.Haversine(point.Latitude, point.Longitude, candidate.Latitude, candidate.Longitude);
                if (distance <= epsilon)
                    neighbors.Add(j);
            }
            return neighbors;
        }


        // find all other neighbours in cluster
        private void ExpandCluster(
            List<LocationModel> points,
            int[] clusterIDs,
            bool[] visited,
            int index,
            List<int> neighbours,
            int clusterId,
            double epsilon,
            int minPoints)
        {
            clusterIDs[index] = clusterId;
            Queue<int> seeds = new Queue<int>(neighbours);
            while (seeds.Count > 0)
            {
                int current = seeds.Dequeue();
                if (!visited[current])
                {
                    visited[current] = true;
                    var currentNeighbours = GetNeighbours(points, current, epsilon);
                    if (currentNeighbours.Count >= minPoints)
                    {
                        foreach (var n in currentNeighbours)
                        {
                            if (!seeds.Contains(n))
                                seeds.Enqueue(n);
                        }
                    }
                }
                if (clusterIDs[current] == 0)
                    clusterIDs[current] = clusterId;
            }
        }
    }
}
