using Poshtibano.Common;
using Poshtibano.Desk.Shared;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Poshtibano.Desk.Shared.Services
{
    public enum ChatMessageMode
    {
        Local,
        Remote,
        Warning
    }

    public class ChatMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Text { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public ChatMessageMode Mode { get; set; }
        public ClientRole SenderRole { get; set; }

        public bool IsLiked { get; set; } = false;
        public bool IsEdited { get; set; } = false;
    }

    public class ChatManager : IDisposable
    {
        private readonly PeerConnectionManager peer;
        private readonly ClientRole localRole;
        private readonly SynchronizationContext uiContext;

        private Dictionary<Guid, ChatMessage> messageCache = new();

        public event Action<ChatMessage> OnNewMessage;
        public event Action<ChatEvent> OnChatEventReceived;

        public ChatManager(PeerConnectionManager peerConnectionManager, ClientRole localRole)
        {
            peer = peerConnectionManager ?? throw new ArgumentNullException(nameof(peerConnectionManager));
            this.localRole = localRole;
            uiContext = SynchronizationContext.Current;
        }

        /// <summary>
        /// </summary>
        public void HandlePacket(Packet packet)
        {
            if (packet == null) return;

            try
            {
                // ChatEvent handling
                if (packet.Type == PacketType.ChatEvent)
                {
                    HandleChatEvent(packet);
                    return;
                }

                if (packet.Type != PacketType.Chat) return;

                var text = packet.Data != null ? Encoding.UTF8.GetString(packet.Data) : string.Empty;
                var msg = new ChatMessage
                {
                    Id = packet.MessageId != Guid.Empty ? packet.MessageId : Guid.NewGuid(),
                    Text = text,
                    Timestamp = new DateTimeOffset(packet.TimestampTicks, TimeSpan.Zero),
                    Mode = ((ClientRole)packet.SenderRole) == ClientRole.Agent ? ChatMessageMode.Local : ChatMessageMode.Remote,
                    SenderRole = (ClientRole)packet.SenderRole
                };

                messageCache[msg.Id] = msg;

                Console.WriteLine($"📨 Chat message received with ID: {msg.Id}");

                try
                {
                    if (uiContext != null)
                        uiContext.Post(_ => OnNewMessage?.Invoke(msg), null);
                    else
                    {
                        //Console.WriteLine($"[{DateTime.Now}] ⚠️ No UI context in ChatManager, posting to thread pool");
                        //Task.Run(() =>
                        OnNewMessage?.Invoke(msg);
                        //);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] ❌ Error invoking OnNewMessage: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error in HandlePacket: {ex.Message}");
            }
        }

        /// <summary>
        /// </summary>
        private void HandleChatEvent(Packet packet)
        {
            try
            {
                var json = Encoding.UTF8.GetString(packet.Data);
                var chatEvent = JsonSerializer.Deserialize<ChatEvent>(json);

                if (chatEvent != null)
                {
                    Console.WriteLine($"📤 Chat event received: {chatEvent.EventType} for message {chatEvent.MessageId}");

                    if (uiContext != null)
                        uiContext.Post(_ => OnChatEventReceived?.Invoke(chatEvent), null);
                    else
                        OnChatEventReceived?.Invoke(chatEvent);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error handling chat event: {ex.Message}");
            }
        }

        public Task SendMessageAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Task.CompletedTask;

            var messageId = Guid.NewGuid();

            var packet = new Packet
            {
                IsDropable = false,
                SequenceNumber = (ulong)DateTime.UtcNow.Ticks,
                Type = PacketType.Chat,
                Data = Encoding.UTF8.GetBytes(text),
                MessageId = messageId,
                SenderRole = (byte)localRole,
                TimestampTicks = DateTime.UtcNow.Ticks
            };

            var serialized = Packet.Serialize(packet);

            Task.Run(() =>
            {
                try
                {
                    peer.SendChat(serialized);
                    Console.WriteLine($"💬 Chat message sent: {text.Substring(0, Math.Min(30, text.Length))}...  [ID: {messageId}]");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Chat send error: {ex.Message}");
                }
            });

            var msg = new ChatMessage
            {
                Id = messageId,
                Text = text,
                Timestamp = DateTimeOffset.UtcNow,
                Mode = ChatMessageMode.Local,
                SenderRole = localRole
            };

            messageCache[msg.Id] = msg;

            if (uiContext != null)
                uiContext.Post(_ => OnNewMessage?.Invoke(msg), null);
            else
                OnNewMessage?.Invoke(msg);

            return Task.CompletedTask;
        }

        /// <summary>
        /// </summary>
        public void SendChatEvent(Guid messageId, ChatEventType eventType, object data)
        {
            try
            {
                var chatEvent = new ChatEvent
                {
                    MessageId = messageId,
                    EventType = eventType
                };

                if (eventType == ChatEventType.MessageLiked)
                {
                    chatEvent.IsLiked = (bool)data;
                }
                else if (eventType == ChatEventType.MessageEdited)
                {
                    chatEvent.NewText = (string)data;
                }

                var json = JsonSerializer.Serialize(chatEvent);
                var packet = new Packet
                {
                    IsDropable = false,
                    Type = PacketType.ChatEvent,
                    Data = Encoding.UTF8.GetBytes(json),
                    MessageId = messageId,
                    SenderRole = (byte)localRole,
                    TimestampTicks = DateTime.UtcNow.Ticks
                };

                var serialized = Packet.Serialize(packet);
                peer.SendChat(serialized);

                Console.WriteLine($"📤 Chat event sent: {eventType} for message {messageId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error sending chat event: {ex.Message}");
            }
        }

        public void Dispose()
        {
            messageCache?.Clear();
        }
    }
}