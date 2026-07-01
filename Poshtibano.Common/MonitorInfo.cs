using System;
using System.Drawing;
using System.Text.Json.Serialization;

namespace Poshtibano.Common
{
    public class MonitorInfo
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("screenBounds")]
        public Rectangle ScreenBounds { get; set; }

        [JsonPropertyName("isPrimary")]
        public bool IsPrimary { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        [JsonPropertyName("Scale")]
        public float Scale { get; set; }

        public MonitorInfo() { }

        public MonitorInfo(int index, string name, Rectangle bounds, bool isPrimary, float scale = 1.0f, bool isActive = true)
        {
            Scale = scale;
            Index = index;
            Name = name;
            ScreenBounds = bounds;
            IsPrimary = isPrimary;
            IsActive = isActive;
        }

        public override string ToString() => $"Monitor #{Index}: {Name} @ {ScreenBounds} [Primary={IsPrimary}, Active={IsActive}]";
    }
}