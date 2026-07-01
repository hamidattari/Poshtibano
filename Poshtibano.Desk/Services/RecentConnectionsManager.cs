using Poshtibano.Common;
using Poshtibano.Desk.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Poshtibano.Desk.Services
{
    public class RecentConnectionsManager
    {
        private const int MaxRecentConnections = 20;
        private readonly string _filePath;
        private List<RecentConnection> _recentConnections;

        public event Action OnConnectionsChanged;

        public RecentConnectionsManager()
        {
            string appDataPath = Path.Combine(
                Environment.CurrentDirectory,
                "PoshtibanoDesk"
            );

            Directory.CreateDirectory(appDataPath);
            _filePath = Path.Combine(appDataPath, "recent_connections.json");

            LoadConnections();
        }

        public List<RecentConnection> GetRecentConnections()
        {
            return _recentConnections
                .OrderByDescending(c => c.LastConnectedAt)
                .Take(MaxRecentConnections)
                .ToList();
        }

        public void AddOrUpdateConnection(Guid deviceId, string displayName, ClientRole role, string sessionId)
        {
            var existing = _recentConnections.FirstOrDefault(c => c.Id == deviceId && c.SessionId == sessionId);

            if (existing != null)
            {
                existing.LastConnectedAt = DateTime.Now;
                existing.ConnectionCount++;
                existing.DisplayName = displayName;
                existing.Role = role;
                existing.SessionId = sessionId;
            }
            else
            {
                var newConnection = new RecentConnection
                {
                    Id = deviceId,
                    DisplayName = displayName,
                    Role = role,
                    SessionId = sessionId,
                    LastConnectedAt = DateTime.Now,
                    ConnectionCount = 1
                };

                _recentConnections.Add(newConnection);
            }

            if (_recentConnections.Count > MaxRecentConnections)
            {
                _recentConnections = _recentConnections
                    .OrderByDescending(c => c.LastConnectedAt)
                    .Take(MaxRecentConnections)
                    .ToList();
            }

            SaveConnections();
            OnConnectionsChanged?.Invoke();
        }

        public void RemoveConnection(Guid id)
        {
            _recentConnections.RemoveAll(c => c.Id == id);
            SaveConnections();
            OnConnectionsChanged?.Invoke();
        }

        public void RenameConnection(RecentConnection connection, string newName)
        {
            //var connection = _recentConnections.FirstOrDefault(c => c.Id == id);

            if (connection != null)
            {
                connection.DisplayName = newName;
                SaveConnections();
                OnConnectionsChanged?.Invoke();
            }
        }

        public void ClearAll()
        {
            _recentConnections.Clear();
            SaveConnections();
            OnConnectionsChanged?.Invoke();
        }

        private void LoadConnections()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    string json = File.ReadAllText(_filePath);
                    _recentConnections = JsonSerializer.Deserialize<List<RecentConnection>>(json) ?? new List<RecentConnection>();
                }
                else
                {
                    _recentConnections = new List<RecentConnection>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading recent connections: {ex.Message}");
                _recentConnections = new List<RecentConnection>();
            }
        }

        private void SaveConnections()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_recentConnections, options);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving recent connections: {ex.Message}");
            }
        }
    }
}