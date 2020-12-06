using System.Drawing;
using System.Windows.Media.Imaging;

namespace RealStereo.Balancing
{
    public class WorkerResult
    {
        private Point? coordinates = null;
        private BitmapImage[] frames;

        public WorkerResult(int numCameras)
        {
            frames = new BitmapImage[numCameras];
        }

        public void SetCoordinates(Point coordinates)
        {
            this.coordinates = coordinates;
        }

        public void SetFrame(int camera, BitmapImage frame)
        {
            if (camera < frames.Length)
            {
                frames[camera] = frame;
            }
        }

        public Point? GetCoordinates()
        {
            return coordinates;
        }

        public BitmapImage[] GetFrames()
        {
            return frames;
        }
    }
}
