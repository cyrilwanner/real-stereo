using System;
using System.Collections.Generic;

namespace RealStereo
{
    public class VolumeInterpolation
    {
        private static int ORIGIN_SIZE = 500;
        private static int TARGET_SIZE = 100;
        private static int POWER = 2;

        public double[,,] Values;

        public VolumeInterpolation(List<PointConfiguration> points)
        {
            Values = new double[TARGET_SIZE + 1, TARGET_SIZE + 1, points[0].Volumes.Count];
            CalculateValues(points);
        }

        public int GetScale()
        {
            return ORIGIN_SIZE / TARGET_SIZE;
        }

        public int MapCoordinate(int x)
        {
            return x / (ORIGIN_SIZE / TARGET_SIZE);
        }

        private void CalculateValues(List<PointConfiguration> points)
        {
            // interpolate values using Shepard's method for scattered data
            for (int x = 0; x <= TARGET_SIZE; x++)
            {
                for (int y = 0; y <= TARGET_SIZE; y++)
                {
                    for (int speaker = 0; speaker < points[0].Volumes.Count; speaker++)
                    {
                        double totalWeight = 0;
                        double totalValue = 0;
                        bool valueSet = false;

                        foreach (PointConfiguration point in points)
                        {
                            int pointX = MapCoordinate(point.Coordinates.X);
                            int pointY = MapCoordinate(point.Coordinates.Y);

                            if (pointX == x && pointY == y)
                            {
                                Values[x, y, speaker] = point.Volumes[speaker][0];
                                valueSet = true;
                                break;
                            }

                            int distanceX = pointX - x;
                            int distanceY = pointY - y;
                            double weight = Math.Pow(Math.Sqrt(distanceX * distanceX + distanceY * distanceY), POWER);
                            weight = 1 / weight;
                            totalValue += weight * point.Volumes[speaker][0];
                            totalWeight += weight;
                        }

                        if (!valueSet)
                        {
                            Values[x, y, speaker] = totalValue / totalWeight;
                        }
                    }
                }
            }
        }
    }
}
