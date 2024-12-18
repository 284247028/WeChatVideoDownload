﻿using System.Text.Json.Serialization;

namespace finder
{


    //public record VideoData(string Description, string Url, string DecodeKey, string UrlToken, string ShortTitle, byte[] Key);
    public class VideoData
    {
        [JsonPropertyName("decode_key")]
        public string? DecodeKey { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("uploader")]
        public string? Uploader { get; set; }

        public byte[]? KeyData { get;set; }
    }

    public class VideoData2
    {
        [JsonPropertyName("key")]
        public string? DecodeKey { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("title")]
        public string? Description { get; set; }

        [JsonPropertyName("uploader")]
        public string? Uploader { get; set; }

        public byte[]? KeyData { get; set; }
    }

}
