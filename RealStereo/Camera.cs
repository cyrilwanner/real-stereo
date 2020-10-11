using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace RealStereo
{
    class Camera
    {
        private VideoCapture capture;
        private PeopleDetector peopleDetector;
        private Image<Bgr, byte> frame;

        public Camera(int cameraIndex, PeopleDetector peopleDetector)
        {
            this.capture = new VideoCapture(cameraIndex);
            this.peopleDetector = peopleDetector;
        }

        public void Process()
        {
            Mat rawFrame = capture.QueryFrame();

            // resize image for more performant detection
            frame = rawFrame.ToImage<Bgr, byte>();
            double ratio = 400.0 / frame.Width;
            frame = frame.Resize((int) (frame.Width * ratio), (int) (frame.Height * ratio), Inter.Cubic);

            // detect people
            MCvObjectDetection[] regions = peopleDetector.Detect(frame);
            MCvObjectDetection[] normalized = peopleDetector.Normalize(regions);
            
            // draw both region arrays on the image
            foreach (MCvObjectDetection region in regions)
            {
                frame.Draw(region.Rect, new Bgr(Color.Blue), 1);
            }
            
            foreach (MCvObjectDetection region in normalized)
            {
                frame.Draw(region.Rect, new Bgr(Color.Green), 2);
            }
        }

        public BitmapImage GetFrame()
        {
            if (frame == null)
            {
                return null;
            }

            return ToBitmapImage(frame.ToBitmap());
        }
        public BitmapImage ToBitmapImage(Bitmap bitmap)
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
    }
}
