using Poshtibano.Common;
using System;

namespace Poshtibano.Desk.Models
{
    public class RecentConnection
    {
        public int _connectionCount;

        public Guid Id { get; set; }
        public string DisplayName { get; set; }
        public ClientRole Role { get; set; }
        public DateTime LastConnectedAt { get; set; }
        public string SessionId { get; set; }
        public int ConnectionCount 
        { 
            get => _connectionCount; 
            set
            {
                _connectionCount = value;
                OnUpdateUi?.Invoke();
            }
        }

        public event Action OnUpdateUi;

        public RecentConnection()
        {
            Id = Guid.NewGuid();
            ConnectionCount = 1;
            LastConnectedAt = DateTime.Now;
        }

        public string GetPersianDate()
        {
            var persianCalendar = new System.Globalization.PersianCalendar();
            int year = persianCalendar.GetYear(LastConnectedAt);
            int month = persianCalendar.GetMonth(LastConnectedAt);
            int day = persianCalendar.GetDayOfMonth(LastConnectedAt);
            string time = LastConnectedAt.ToString("HH:mm");

            return $"{year}/{month:00}/{day:00} {time}";
        }

        public string GetRoleName()
        {
            return Role == ClientRole.Agent ? "عامل" : "کنترلر";
        }
    }
}