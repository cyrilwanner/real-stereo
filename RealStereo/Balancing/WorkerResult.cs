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

        /// <summary>
        /// Set the coordinates.
        /// </summary>
        /// <param name="coordinates">New coordinates.</param>
        public void SetCoordinates(Point coordinates)
        {
            this.coordinates = coordinates;
        }

        /// <summary>
        /// Set the camera frame.
        /// </summary>
        /// <param name="camera">Camera index.</param>
        /// <param name="frame">Frame.</param>
        public void SetFrame(int camera, BitmapImage frame)
        {
            if (camera < frames.Length)
            {
                frames[camera] = frame;
            }
        }

        /// <summary>
        /// Get the coordinates.
        /// </summary>
        /// <returns>Coordinates.</returns>
        public Point? GetCoordinates()
        {
            return coordinates;
        }

        /// <summary>
        /// Get the frames of all cameras.
        /// </summary>
        /// <returns>Frames indexed by their camera index.</returns>
        public BitmapImage[] GetFrames()
        {
            return frames;
        }
    }
}
