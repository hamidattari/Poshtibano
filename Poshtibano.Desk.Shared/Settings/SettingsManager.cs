using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Poshtibano.Common;
using Poshtibano.Desk.Shared.Tools;
using System.Management;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace Poshtibano.Desk.Shared.Settings
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class SettingsData
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? ApplicationId { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Password { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ClientRole? ClientRole { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? LastUpdated { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? DisplayName { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? HubAddress { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? RecordOutcomingFolder { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? RecordIncomingFolder { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? RecordOutcomingSessions { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? RecordIncomingSessions { get; set; }
    }

    public class SettingsManager
    {
        public static SettingsManager Instance;

        public static SettingsData Setting;

        private const string SETTINGS_FOLDER = "Settings";
        private const string SETTINGS_FILE = "app_settings.json";
        private const string ENCRYPTION_KEY = "MySecretKey12345";

        private bool _clipboardSharingEnabled = true;
        private Guid _applicationId = Guid.NewGuid();
        private string _settingsPath;
        private SettingsData _settings;

        static SettingsManager()
        {
            Instance = new SettingsManager();
        }

        public SettingsManager()
        {
            InitializeSettingsPath();
            _settings = Load() ?? CreateDefaultSettings();
            Setting = _settings;
            Save();
        }

        public Guid ApplicationGuid => _applicationId;
        public string Password => _settings.Password ?? string.Empty;
        public ClientRole ClientRole => _settings.ClientRole ?? ClientRole.Controller;
        public string SettingsPath => _settingsPath;

        public string DisplayName => _settings.DisplayName ?? "کاربر";
        public string HubAddress => _settings.HubAddress ?? "http://217.114.40.239:5000/hub";
        public string RecordOutcomingFolder => _settings.RecordOutcomingFolder;
        public string RecordIncomingFolder => _settings.RecordIncomingFolder;
        public bool RecordOutcomingSessions => _settings.RecordOutcomingSessions ?? false;
        public bool RecordIncomingSessions => _settings.RecordIncomingSessions ?? false;

        public bool ClipboardSharingEnabled
        {
            get => _clipboardSharingEnabled;
        }
        public static string SessionName { get; set; }
        public static string CallerName { get; set; }

        public void SetClipboardSharingEnabled(bool enabled)
        {
            _clipboardSharingEnabled = enabled;
            Save();
        }

        public static Guid GetHardwareBasedGuid()
        {
            string hardwareInfo = GetHardwareFingerprint();

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(hardwareInfo));
                byte[] guidBytes = new byte[16];
                Array.Copy(hash, guidBytes, 16);

                return new Guid(guidBytes);
            }
        }

        private static string GetHardwareFingerprint()
        {
            string mac = GetMacAddress();
            string cpu = GetCpuId();
            string disk = GetDiskSerial();

            return $"{mac}|{cpu}|{disk}";
        }

        private static string GetMacAddress()
        {
            try
            {
                var mac = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                                n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                                n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                    .Select(n => n.GetPhysicalAddress().ToString())
                    .FirstOrDefault(s => !string.IsNullOrEmpty(s));

                return mac ?? "NoMAC";
            }
            catch
            {
                return "NoMAC";
            }
        }

        private static string GetCpuId()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
                foreach (ManagementObject obj in searcher.Get())
                {
                    return obj["ProcessorId"]?.ToString() ?? "NoCPU";
                }
            }
            catch { }
            return "NoCPU";
        }

        private static string GetDiskSerial()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_PhysicalMedia");
                foreach (ManagementObject obj in searcher.Get())
                {
                    string serial = obj["SerialNumber"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(serial))
                        return serial;
                }
            }
            catch { }
            return "NoDisk";
        }

        private void InitializeSettingsPath()
        {
            string appPath = AppDomain.CurrentDomain.BaseDirectory;
            string settingsFolder = Path.Combine(appPath, SETTINGS_FOLDER);

            if (!Directory.Exists(settingsFolder))
            {
                Directory.CreateDirectory(settingsFolder);
            }

            _settingsPath = Path.Combine(settingsFolder, SETTINGS_FILE);
        }

        private SettingsData CreateDefaultSettings()
        {
            var recordingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recording");
            if (!Directory.Exists(recordingDirectory)) Directory.CreateDirectory(recordingDirectory);

            var incomingRecordingDirectory = Path.Combine(recordingDirectory, "Incoming");
            if (!Directory.Exists(incomingRecordingDirectory)) Directory.CreateDirectory(incomingRecordingDirectory);

            var outcomingRecordingDirectory = Path.Combine(recordingDirectory, "Outcoming");
            if (!Directory.Exists(outcomingRecordingDirectory)) Directory.CreateDirectory(outcomingRecordingDirectory);

            var id = GetHardwareBasedGuid();
            return new SettingsData
            {
                ApplicationId = id.ToString(),
                Password = string.Empty,
                ClientRole = ClientRole.Controller,
                DisplayName = id.GuidToFormattedText(),
                LastUpdated = DateTime.Now,
                HubAddress = "http://217.114.40.239:5000/hub",
                RecordOutcomingFolder = outcomingRecordingDirectory,
                RecordIncomingFolder = incomingRecordingDirectory,
                RecordOutcomingSessions = false,
                RecordIncomingSessions = false
            };
        }

        public SettingsData Load()
        {
            try
            {
                if (!File.Exists(_settingsPath))
                    return null;

                string json = File.ReadAllText(_settingsPath);
                var settings = JsonConvert.DeserializeObject<SettingsData>(json);

                if (settings != null)
                {
                    if (settings.ApplicationId == null)
                        settings.ApplicationId = EncryptString(Guid.NewGuid().ToString());
                    settings.ApplicationId = DecryptString(settings.ApplicationId);

                    if (settings.Password != null)
                        settings.Password = DecryptString(settings.Password);
                    else
                        settings.Password = string.Empty; // Or null, depending on your preference
                }
                _applicationId = new Guid(settings.ApplicationId);

                var recordingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recording");
                if (!Directory.Exists(recordingDirectory)) Directory.CreateDirectory(recordingDirectory);

                var incomingRecordingDirectory = Path.Combine(recordingDirectory, "Incoming");
                if (!Directory.Exists(incomingRecordingDirectory)) Directory.CreateDirectory(incomingRecordingDirectory);

                var outcomingRecordingDirectory = Path.Combine(recordingDirectory, "Outcoming");
                if (!Directory.Exists(outcomingRecordingDirectory)) Directory.CreateDirectory(outcomingRecordingDirectory);

                settings.RecordOutcomingFolder = outcomingRecordingDirectory;
                settings.RecordIncomingFolder = incomingRecordingDirectory;

                return settings;
            }
            catch
            {
                return null;
            }
        }

        public void Save()
        {
            try
            {
                _settings.LastUpdated = DateTime.Now;
                var settingsCopy = new SettingsData
                {
                    ApplicationId = EncryptString(_applicationId.ToString()),
                    Password = EncryptString(_settings.Password ?? string.Empty),
                    ClientRole = _settings.ClientRole,
                    LastUpdated = _settings.LastUpdated ?? DateTime.Now,
                    DisplayName = _settings.DisplayName ?? "کاربر",
                    HubAddress = _settings.HubAddress ?? "http://192.168.1.5:5000/hub",
                    RecordOutcomingFolder = _settings.RecordOutcomingFolder,
                    RecordIncomingFolder = _settings.RecordIncomingFolder,
                    RecordOutcomingSessions = _settings.RecordOutcomingSessions,
                    RecordIncomingSessions = _settings.RecordIncomingSessions
                };
                _applicationId = new Guid(DecryptString(settingsCopy.ApplicationId));

                var jsonsettings = new JsonSerializerSettings();
                jsonsettings.Converters.Add(new StringEnumConverter());

                string json = JsonConvert.SerializeObject(settingsCopy, Formatting.Indented, jsonsettings);
                File.WriteAllText(_settingsPath, json);

                Setting = settingsCopy;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطا در ذخیره تنظیمات: {ex.Message}", ex);
            }
        }

        public void SetHubAddress(string address)
        {
            _settings.HubAddress = address;
            Save();
        }

        public void SetRecordOutcomingFolder(string path)
        {
            _settings.RecordOutcomingFolder = path;
            Save();
        }

        public void SetRecordIncomingFolder(string path)
        {
            _settings.RecordIncomingFolder = path;
            Save();
        }

        public void SetRecordOutcomingSessions(bool record)
        {
            _settings.RecordOutcomingSessions = record;
            Save();
        }

        public void SetRecordIncomingSessions(bool record)
        {
            _settings.RecordIncomingSessions = record;
            Save();
        }

        public void SetClientRole(ClientRole role)
        {
            _settings.ClientRole = role;
            Save();
        }

        public void SetPassword(string password)
        {
            _settings.Password = password;
            Save();
        }

        public void SetDisplayName(string name)
        {
            _settings.DisplayName = name;
            Save();
        }

        private string EncryptString(string plainText)
        {
            byte[] key = Encoding.UTF8.GetBytes(ENCRYPTION_KEY.PadRight(32).Substring(0, 32));
            byte[] iv = new byte[16];

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        private string DecryptString(string cipherText)
        {
            byte[] key = Encoding.UTF8.GetBytes(ENCRYPTION_KEY.PadRight(32).Substring(0, 32));
            byte[] iv = new byte[16];
            byte[] buffer = Convert.FromBase64String(cipherText);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream ms = new MemoryStream(buffer))
                {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader sr = new StreamReader(cs))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
        }

        public void DeleteSettings()
        {
            if (File.Exists(_settingsPath))
            {
                File.Delete(_settingsPath);
                _settings = CreateDefaultSettings();
            }
        }
    }
}