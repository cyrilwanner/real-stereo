using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace RealStereo
{
    class PeopleDetector
    {
        private HOGDescriptor hogDescriptor;

        public PeopleDetector()
        {
            hogDescriptor = new HOGDescriptor();
            hogDescriptor.SetSVMDetector(HOGDescriptor.GetDefaultPeopleDetector());
        }

        public MCvObjectDetection[] Detect(Image<Bgr, byte> frame)
        {
            return hogDescriptor.DetectMultiScale(frame, 0, new Size(4, 4), new Size(8, 8));
        }

        public MCvObjectDetection[] Normalize(MCvObjectDetection[] regions)
        {
            List<MCvObjectDetection> origin = new List<MCvObjectDetection>(regions);
            List<MCvObjectDetection> results = new List<MCvObjectDetection>();

            // normalize overlapping regions
            while (origin.Count() > 0)
            {
                MCvObjectDetection region = origin.ElementAt(0);
                origin.Remove(region);

                // check if it intersects with another one
                bool intersects = false;
                for (int i = 0; i < results.Count; i++)
                {
                    MCvObjectDetection result = results[i];
                    if (region.Rect.IntersectsWith(result.Rect))
                    {
                        intersects = true;
                        int maxRight = Math.Max(result.Rect.Right, region.Rect.Right);
                        int maxBottom = Math.Max(result.Rect.Bottom, region.Rect.Bottom);

                        // update existing region with merged bounds
                        result.Rect.X = Math.Min(result.Rect.X, region.Rect.X);
                        result.Rect.Y = Math.Min(result.Rect.Y, region.Rect.Y);
                        result.Rect.Width = maxRight - result.Rect.X;
                        result.Rect.Height = maxBottom - result.Rect.Y;

                        results[i] = result;
                    }
                }

                if (!intersects)
                {
                    results.Add(region);
                }
            }

            return results.ToArray();
        }
    }
}
