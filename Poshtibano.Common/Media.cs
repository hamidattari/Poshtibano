using System;
using System.Text.Json.Serialization;

namespace Poshtibano.Common
{
    public enum MediaStreamState : byte
    {
        Idle = 0,
        Requesting = 1,
        WaitingPermission = 2,
        Streaming = 3,
        Denied = 4,
        Stopped = 5,
        Disabled = 6,
    }

    public enum MediaType : byte
    {
        Audio = 1,
        Webcam = 2
    }

    public enum MediaRequestType : byte
    {
        IWantToSend = 1,
        IWantToReceive = 2
    }

    public class MediaPermissionRequest
    {
        [JsonPropertyName("mediaType")]
        public MediaType MediaType { get; set; }

        [JsonPropertyName("requestType")]
        public MediaRequestType RequestType { get; set; }

        [JsonPropertyName("requesterId")]
        public string RequesterId { get; set; }

        [JsonPropertyName("requesterName")]
        public string RequesterName { get; set; }

        [JsonPropertyName("requesterRole")]
        public ClientRole RequesterRole { get; set; }

        [JsonPropertyName("timestampTicks")]
        public long TimestampTicks { get; set; }
    }

    public class MediaPermissionResponse
    {
        [JsonPropertyName("mediaType")]
        public MediaType MediaType { get; set; }

        [JsonPropertyName("requestType")]
        public MediaRequestType RequestType { get; set; }

        [JsonPropertyName("allowed")]
        public bool Allowed { get; set; }

        [JsonPropertyName("responderId")]
        public string ResponderId { get; set; }

        [JsonPropertyName("responderRole")]
        public ClientRole ResponderRole { get; set; }

        [JsonPropertyName("timestampTicks")]
        public long TimestampTicks { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class MediaStateChange
    {
        [JsonPropertyName("mediaType")]
        public MediaType MediaType { get; set; }

        [JsonPropertyName("newState")]
        public MediaStreamState NewState { get; set; }

        [JsonPropertyName("senderRole")]
        public ClientRole SenderRole { get; set; }

        [JsonPropertyName("timestampTicks")]
        public long TimestampTicks { get; set; }
    }

    public class AudioMuteStateChange
    {
        [JsonPropertyName("isMuted")]
        public bool IsMuted { get; set; }

        [JsonPropertyName("senderRole")]
        public ClientRole SenderRole { get; set; }

        [JsonPropertyName("timestampTicks")]
        public long TimestampTicks { get; set; }
    }
}