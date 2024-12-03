

namespace finder
{
    public class VideoKeyDataEventArgs : EventArgs
    {
        public VideoKeyData VideoKeyData { get; private set; }

        public VideoKeyDataEventArgs(VideoKeyData videoKeyData)
        {
            VideoKeyData = videoKeyData;
        }
    }
}
