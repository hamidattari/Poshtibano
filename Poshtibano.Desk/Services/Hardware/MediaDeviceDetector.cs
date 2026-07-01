using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using NAudio.Wave;

namespace Poshtibano.Desk.Services.Hardware
{
    /// <summary>
    /// Detects available audio and video capture devices on the system.
    /// </summary>
    public static class MediaDeviceDetector
    {
        /// <summary>
        /// Checks if at least one microphone is available on the system.
        /// </summary>
        /// <returns>True if a microphone is available, false otherwise.</returns>
        public static bool HasMicrophone()
        {
            try
            {
                int deviceCount = WaveInEvent.DeviceCount;
                return deviceCount > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Error detecting microphone: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets a list of all available microphone devices.
        /// </summary>
        /// <returns>List of microphone device names with their indices.</returns>
        public static List<(int Index, string Name)> GetMicrophones()
        {
            var devices = new List<(int, string)>();
            try
            {
                for (int i = 0; i < WaveInEvent.DeviceCount; i++)
                {
                    var capabilities = WaveInEvent.GetCapabilities(i);
                    devices.Add((i, capabilities.ProductName));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Error getting microphones: {ex.Message}");
            }
            return devices;
        }

        /// <summary>
        /// Checks if at least one webcam is available on the system.
        /// </summary>
        /// <returns>True if a webcam is available, false otherwise. </returns>
        public static bool HasWebcam()
        {
            try
            {
                // Use WMI to detect video capture devices
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_PnPEntity WHERE (PNPClass = 'Image' OR PNPClass = 'Camera')"))
                {
                    var devices = searcher.Get();
                    return devices.Count > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Error detecting webcam via WMI: {ex.Message}");

                // Fallback:  Try DirectShow enumeration
                return HasWebcamViaDirectShow();
            }
        }

        /// <summary>
        /// Fallback method to detect webcam using DirectShow-style enumeration.
        /// </summary>
        private static bool HasWebcamViaDirectShow()
        {
            try
            {
                // Try to find USB Video Class devices
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_PnPEntity WHERE Caption LIKE '%Camera%' OR Caption LIKE '%Webcam%' OR Caption LIKE '%Video%'"))
                {
                    var devices = searcher.Get();
                    foreach (ManagementObject device in devices)
                    {
                        string caption = device["Caption"]?.ToString() ?? "";
                        string pnpClass = device["PNPClass"]?.ToString() ?? "";

                        if (pnpClass.Contains("Image") || pnpClass.Contains("Camera") ||
                            caption.Contains("Camera") || caption.Contains("Webcam"))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a list of all available webcam devices. 
        /// </summary>
        /// <returns>List of webcam device names. </returns>
        public static List<string> GetWebcams()
        {
            var devices = new List<string>();
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_PnPEntity WHERE (PNPClass = 'Image' OR PNPClass = 'Camera')"))
                {
                    foreach (ManagementObject device in searcher.Get())
                    {
                        string name = device["Caption"]?.ToString();
                        if (!string.IsNullOrEmpty(name))
                        {
                            devices.Add(name);
                        }
                    }
                }

                // If no devices found via PNPClass, try broader search
                if (devices.Count == 0)
                {
                    using (var searcher = new ManagementObjectSearcher(
                        "SELECT * FROM Win32_PnPEntity WHERE Caption LIKE '%Camera%' OR Caption LIKE '%Webcam%'"))
                    {
                        foreach (ManagementObject device in searcher.Get())
                        {
                            string name = device["Caption"]?.ToString();
                            if (!string.IsNullOrEmpty(name) && !devices.Contains(name))
                            {
                                devices.Add(name);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Error getting webcams: {ex.Message}");
            }
            return devices;
        }

        /// <summary>
        /// Gets summary of available media devices.
        /// </summary>
        public static (bool HasMic, bool HasCam, int MicCount, int CamCount) GetDeviceSummary()
        {
            bool hasMic = HasMicrophone();
            bool hasCam = HasWebcam();
            int micCount = hasMic ? GetMicrophones().Count : 0;
            int camCount = hasCam ? GetWebcams().Count : 0;

            Console.WriteLine($"[{DateTime.Now}] 🎤 Microphones: {micCount}, 📷 Webcams:  {camCount}");

            return (hasMic, hasCam, micCount, camCount);
        }
    }
}