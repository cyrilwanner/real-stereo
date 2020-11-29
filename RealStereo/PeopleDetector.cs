using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace RealStereo
{
    public class PeopleDetector
    {
        private HOGDescriptor hogDescriptor;
        private static int GROUP_THRESHOLD = 50;
        private static int HISTORY_SIZE = 2;
        private static double SCORE_THRESHOLD = 0.3;

        public PeopleDetector()
        {
            hogDescriptor = new HOGDescriptor();
            hogDescriptor.SetSVMDetector(HOGDescriptor.GetDefaultPeopleDetector());
        }

        public MCvObjectDetection[] Detect(Image<Bgr, byte> frame)
        {
            MCvObjectDetection[] regions = hogDescriptor.DetectMultiScale(frame, 0, new Size(2, 2), new Size(8, 8));
            List<MCvObjectDetection> filteredRegions = new List<MCvObjectDetection>();

            foreach (MCvObjectDetection region in regions)
            {
                if (region.Score >= SCORE_THRESHOLD)
                {
                    filteredRegions.Add(region);
                }
            }

            return filteredRegions.ToArray();
        }

        public MCvObjectDetection[] Normalize(MCvObjectDetection[] regions, MCvObjectDetection[] previousPeople, List<MCvObjectDetection[]> history)
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
                    // enlarge rect to group close ones
                    MCvObjectDetection result = results[i];
                    Rectangle enlargedResult = EnlargeRectangle(result.Rect);
                    if (region.Rect.IntersectsWith(enlargedResult))
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

            if (previousPeople == null)
            {
                return results.ToArray();
            }

            // to reduce false-positives, only return regions where a person was already previously in or it is persistent in the history
            return FilterNewPeople(results, previousPeople, history);
        }

        public void RotateHistory(MCvObjectDetection[] people, ref List<MCvObjectDetection[]> history)
        {
            // initialize empty history if unset
            if (history.Count == 0)
            {
                for (int i = 0; i < HISTORY_SIZE; i++)
                {
                    history.Add(new MCvObjectDetection[] { });
                }
            }

            for (int i = HISTORY_SIZE - 1; i > 0; i--)
            {
                if (history.Count > i - 1)
                {
                    history[i] = history[i - 1];
                }
            }

            history[0] = people;
        }

        private Rectangle EnlargeRectangle(Rectangle rect)
        {
            return new Rectangle(rect.X - GROUP_THRESHOLD, rect.Y - GROUP_THRESHOLD, rect.Width + GROUP_THRESHOLD * 2, rect.Height + GROUP_THRESHOLD * 2);
        }

        private MCvObjectDetection[] FilterNewPeople(List<MCvObjectDetection> regions, MCvObjectDetection[] previousPeople, List<MCvObjectDetection[]> history)
        {
            for (int i = regions.Count - 1; i >= 0; i--)
            {
                Rectangle enlargedResult = EnlargeRectangle(regions[i].Rect);
                bool isNew = true;

                // track people -> check if the region is overlapping with a previously recognized person
                foreach (MCvObjectDetection previousPerson in previousPeople)
                {
                    if (previousPerson.Rect.IntersectsWith(enlargedResult))
                    {
                        isNew = false;
                        break;
                    }
                }

                // if it is not overlapping with a previously recognized person, check if it was in the history the whole time and mark it as a recognized person if so
                if (isNew)
                {
                    for (int j = 0; j < history.Count; j++)
                    {
                        bool intersects = false;
                        foreach (MCvObjectDetection historyEntry in history[j])
                        {
                            if (historyEntry.Rect.IntersectsWith(enlargedResult))
                            {
                                intersects = true;
                                break;
                            }
                        }

                        if (!intersects)
                        {
                            break;
                        }

                        if (j == history.Count - 1)
                        {
                            isNew = false;
                        }
                    }
                }

                if (isNew)
                {
                    regions.RemoveAt(i);
                }
            }

            return regions.ToArray();
        }
    }
}
