using System;
using System.Linq;

namespace finder
{
    public class VideoDataEventArgs : EventArgs
    {
        public VideoData VideoData { get; private set; }

        public VideoDataEventArgs(VideoData videoData)
        {
            VideoData = videoData;
        }
    }  
}
