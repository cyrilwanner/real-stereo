using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace RealStereo.Balancing.Tracking
{
    public class PeopleDetector
    {
        private static readonly int GroupThreshold = 50;
        private static readonly int HistorySize = 2;
        private static readonly double ScoreThreshold = 0.1;

        private HOGDescriptor descriptor;

        public PeopleDetector()
        {
            descriptor = new HOGDescriptor();
            descriptor.SetSVMDetector(HOGDescriptor.GetDefaultPeopleDetector());
        }

        /// <summary>
        /// Detect people in the given frame using the histogram of oriented gradients algorithm.
        /// </summary>
        /// <param name="frame">Input frame in which people should get detected.</param>
        /// <returns>Regions of detected people.</returns>
        public MCvObjectDetection[] Detect(Image<Bgr, byte> frame)
        {
            // detect people in the given frame using the HOG descriptor
            MCvObjectDetection[] regions = descriptor.DetectMultiScale(frame, 0, new Size(2, 2), new Size(8, 8));

            // filter the region based on the defined score
            List<MCvObjectDetection> filteredRegions = new List<MCvObjectDetection>();

            foreach (MCvObjectDetection region in regions)
            {
                if (region.Score >= ScoreThreshold)
                {
                    filteredRegions.Add(region);
                }
            }

            return filteredRegions.ToArray();
        }

        /// <summary>
        /// Normalize detected regions by combining nearby ones and ensuring they were already present in previous frames.
        /// This will reduce false-positives.
        /// </summary>
        /// <param name="regions">Newly detected regions.</param>
        /// <param name="previousPeople">Previously confirmed people.</param>
        /// <param name="history">History of detected regions of multiple previous frames.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Add new confirmed people to the history and ensure it does not exceed the defined length.
        /// </summary>
        /// <param name="people">Newly confirmed people.</param>
        /// <param name="history">Previous history.</param>
        public void RotateHistory(MCvObjectDetection[] people, ref List<MCvObjectDetection[]> history)
        {
            // initialize empty history if unset
            if (history.Count == 0)
            {
                for (int i = 0; i < HistorySize; i++)
                {
                    history.Add(new MCvObjectDetection[] { });
                }
            }

            for (int i = HistorySize - 1; i > 0; i--)
            {
                if (history.Count > i - 1)
                {
                    history[i] = history[i - 1];
                }
            }

            history[0] = people;
        }

        /// <summary>
        /// Will enlarge the given rectangle by the defined grouping threshold.
        /// </summary>
        /// <param name="rect">Input rectangle.</param>
        /// <returns>Enlarged rectangle.</returns>
        private Rectangle EnlargeRectangle(Rectangle rect)
        {
            return new Rectangle(rect.X - GroupThreshold, rect.Y - GroupThreshold, rect.Width + GroupThreshold * 2, rect.Height + GroupThreshold * 2);
        }

        /// <summary>
        /// Filters the given regions and removes them if they are not previously confirmed people or in the same place over multiple frames.
        /// </summary>
        /// <param name="regions">Newly detected regions.</param>
        /// <param name="previousPeople">Previously confirmed people.</param>
        /// <param name="history">Current history.</param>
        /// <returns></returns>
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
                    for (int historyIndex = 0; historyIndex < history.Count; historyIndex++)
                    {
                        bool intersects = false;
                        foreach (MCvObjectDetection historyEntry in history[historyIndex])
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

                        if (historyIndex == history.Count - 1)
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
