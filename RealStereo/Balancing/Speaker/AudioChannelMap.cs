using System.Collections.Generic;

namespace RealStereo.Balancing.Speaker
{
    class AudioChannelMap
    {
        public static Dictionary<int, string> Map = new Dictionary<int, string>
        {
            { 0, "Left" },
            { 1, "Right" },
            { 2, "Center" },
            { 3, "Sub" },
            { 4, "Rear Left" },
            { 5, "Rear Right" },
            { 6, "Surround Left" },
            { 7, "Surround Right" },
        };
    }
}
