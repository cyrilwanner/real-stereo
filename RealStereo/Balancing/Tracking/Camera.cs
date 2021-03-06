﻿using Emgu.CV;
using Emgu.CV.Structure;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Inter = Emgu.CV.CvEnum.Inter;

namespace RealStereo.Balancing.Tracking
{
    public class Camera
    {
        private VideoCapture capture;
        private PeopleDetector peopleDetector;
        private Image<Bgr, byte> frame;
        private MCvObjectDetection[] people;
        private List<MCvObjectDetection[]> history = new List<MCvObjectDetection[]>();

        public Camera(int cameraIndex, PeopleDetector peopleDetector)
        {
            capture = new VideoCapture(cameraIndex, VideoCapture.API.Msmf);
            this.peopleDetector = peopleDetector;
        }

        /// <summary>
        /// Processes the next frame from the camera and optionally detects people in it.
        /// </summary>
        /// <param name="detectPeople">If true, people will be detected in the frame.</param>
        public void Process(bool detectPeople)
        {
            Mat rawFrame = capture.QueryFrame();
            if (rawFrame == null)
            {
                return;
            }

            // resize image for more performant detection
            frame = rawFrame.ToImage<Bgr, byte>();
            double ratio = 500.0 / frame.Width;
            frame = frame.Resize((int) (frame.Width * ratio), (int) (frame.Height * ratio), Inter.Cubic);

            if (!detectPeople)
            {
                return;
            }

            // detect people
            MCvObjectDetection[] regions = peopleDetector.Detect(frame);

            // if no people got detected, assume they haven't moved and use the previous regions
            if (regions.Length == 0)
            {
                DrawRegions(people, new Bgr(Color.Green), 2);
                return;
            }

            // normalize people by combining nearby regions and only use ones that were present in previous frames
            MCvObjectDetection[] updatedPeople = peopleDetector.Normalize(regions, people, history);
            peopleDetector.RotateHistory(regions, ref history);

            if (updatedPeople.Length > 0)
            {
                people = updatedPeople;
            }

            // draw both region arrays on the image
            DrawRegions(regions, new Bgr(Color.Blue), 1);
            DrawRegions(people, new Bgr(Color.LimeGreen), 2);
        }

        /// <summary>
        /// Get the current camera frame.
        /// If people have been detected for that frame, their regions are drawn in the frame.
        /// </summary>
        /// <returns>Camera frame as a Bitmap Image.</returns>
        public BitmapImage GetFrame()
        {
            if (frame == null)
            {
                return null;
            }

            return ToBitmapImage(frame.ToBitmap());
        }

        /// <summary>
        /// Calculates the coordinates of the detected person in the current camera frame.
        /// Based on the given orientation, either the X-coordinate (horizontal) or Y-coordinate (vertical) will be set.
        /// If no person is detected, null will be returned.
        /// </summary>
        /// <param name="orientation"></param>
        /// <returns></returns>
        public Point? GetCoordinates(Orientation orientation)
        {
            if (people == null || people.Length == 0)
            {
                return null;
            }

            Point point = new Point(0, 0);

            if (orientation == Orientation.Horizontal)
            {
                point.X = people[0].Rect.X + people[0].Rect.Width / 2;
            }
            else if (orientation == Orientation.Vertical)
            {
                point.Y = people[0].Rect.X + people[0].Rect.Width / 2;
            }

            return point;
        }

        /// <summary>
        /// Converts a Bitmap into a BitmapImage for easier use in WPF.
        /// </summary>
        /// <param name="bitmap">Bitmap source.</param>
        /// <returns>Converted BitmapImage.</returns>
        private BitmapImage ToBitmapImage(Bitmap bitmap)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png);
                stream.Position = 0;

                BitmapImage result = new BitmapImage();

                result.BeginInit();
                result.CacheOption = BitmapCacheOption.OnLoad;
                result.StreamSource = stream;
                result.EndInit();
                result.Freeze();

                return result;
            }
        }

        /// <summary>
        /// Draws the given region onto the current camera frame
        /// </summary>
        /// <param name="regions">Regions to draw.</param>
        /// <param name="color">Color of the regions.</param>
        /// <param name="thickness">Thickness in pixels of the regions.</param>
        private void DrawRegions(MCvObjectDetection[] regions, Bgr color, int thickness)
        {
            if (regions == null || regions.Length == 0)
            {
                return;
            }

            foreach (MCvObjectDetection region in regions)
            {
                frame.Draw(region.Rect, color, thickness);
            }
        }
    }
}
