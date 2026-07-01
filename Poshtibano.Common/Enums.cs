namespace Poshtibano.Common
{
    /// <summary>
    /// Defines all packet types used in the remote desktop communication protocol. 
    /// </summary>
    public enum PacketType : byte
    {
        Frame = 1,
        Event = 2,
        Chat = 3,
        ChatEvent = 4,
        Settings = 5,

        // File Transfer
        FileStart = 10,
        FileChunk = 11,
        FileEnd = 12,
        FileCancel = 13,

        // ✅ Audio/Video
        AudioData = 20,
        WebcamData = 21,

        // ✅ Clipboard Sharing
        ClipboardText = 30,
        ClipboardFiles = 31,

        // ✅ NEW: Clipboard file request (receiver asks sender to start transfer)
        ClipboardFileRequest = 32,
    }

    public enum ClientRole
    {
        None = 0,
        Agent = 1,
        Controller = 2,
    }

    public enum HubSessionState
    {
        NA,
        Ready,
        Ended
    }

    public enum PeerConnectionState
    {
        NA,
        Closed,
        Failed,
        Disconnected,
        Connecting,
        Connected,
        New
    }
}
