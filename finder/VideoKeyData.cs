﻿namespace finder
{


        public class VideoKeyData
        {
        public VideoKeyData(string url,byte[] key)
        {
            Url=url;
            Key=(byte[])key.Clone();
        }
            public string? Url { get; set; }
            public byte[]? Key { get; set; }

        }
  

}
