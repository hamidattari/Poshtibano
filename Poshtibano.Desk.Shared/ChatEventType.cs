namespace Poshtibano.Desk.Shared
{
    public enum ChatEventType : byte
    {
        MessageLiked = 1,
        MessageEdited = 2,
        MessageDeleted = 3
    }

    public class ChatEvent
    {
        public Guid MessageId { get; set; }
        public ChatEventType EventType { get; set; }
        public bool IsLiked { get; set; }

        public bool IsDeleted { get; set; }
        public string NewText { get; set; }
    }
}
