using Poshtibano.Common;

namespace Poshtibano.Desk.Services.Networking
{
    public static class PacketHandlerClipboardExtension
    {
        public static bool IsClipboardPacket(PacketType type)
        {
            return type == PacketType.ClipboardText
                || type == PacketType.ClipboardFiles
                || type == PacketType.ClipboardFileRequest;
        }
    }
}