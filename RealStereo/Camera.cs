using Emgu.CV;
using Emgu.CV.Structure;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Inter = Emgu.CV.CvEnum.Inter;

namespace RealStereo
{
    class Camera
    {
        private VideoCapture capture;
        private PeopleDetector peopleDetector;
        private Image<Bgr, byte> frame;
        private MCvObjectDetection[] people;
        private List<MCvObjectDetection[]> history = new List<MCvObjectDetection[]>();

        public Camera(int cameraIndex, PeopleDetector peopleDetector)
        {
            this.capture = new VideoCapture(cameraIndex, VideoCapture.API.Msmf);
            this.peopleDetector = peopleDetector;
        }

        public void Process(bool detectPeople)
        {
            Mat rawFrame = capture.QueryFrame();

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

        public BitmapImage GetFrame()
        {
            if (frame == null)
            {
                return null;
            }

            return ToBitmapImage(frame.ToBitmap());
        }

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
