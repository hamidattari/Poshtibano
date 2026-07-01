using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Poshtibano.Desk.Shared.Tools;

public static class ScreenTools
{
    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi)]
    struct DEVMODE
    {
        public const int CCHDEVICENAME = 32;
        public const int CCHFORMNAME = 32;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
        [FieldOffset(0)]
        public string dmDeviceName;

        [FieldOffset(32)] public short dmSpecVersion;
        [FieldOffset(34)] public short dmDriverVersion;
        [FieldOffset(36)] public short dmSize;
        [FieldOffset(38)] public short dmDriverExtra;
        [FieldOffset(40)] public int dmFields;

        // Union for position/orientation
        [FieldOffset(44)] public int dmPositionX;
        [FieldOffset(48)] public int dmPositionY;
        [FieldOffset(52)] public int dmDisplayOrientation;
        [FieldOffset(56)] public int dmDisplayFixedOutput;

        [FieldOffset(60)] public short dmColor;
        [FieldOffset(62)] public short dmDuplex;
        [FieldOffset(64)] public short dmYResolution;
        [FieldOffset(66)] public short dmTTOption;
        [FieldOffset(68)] public short dmCollate;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
        [FieldOffset(72)]
        public string dmFormName;

        [FieldOffset(102)] public short dmLogPixels;
        [FieldOffset(104)] public int dmBitsPerPel;
        [FieldOffset(108)] public int dmPelsWidth;  
        [FieldOffset(112)] public int dmPelsHeight; 

        [FieldOffset(116)] public int dmDisplayFlags;
    }

    [DllImport("user32.dll", CharSet = CharSet.Ansi)]
    static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

    const int ENUM_CURRENT_SETTINGS = -1;

    public static Rectangle GetPhysicalBound(this Screen screen)
    {
        DEVMODE dm = new DEVMODE();
        dm.dmSize = (short)Marshal.SizeOf(dm);

        if (EnumDisplaySettings(screen.DeviceName, ENUM_CURRENT_SETTINGS, ref dm))
        {
            // 1. Compute the scaling factor
            // Physical width (from the driver) divided by logical width (from Windows)
            float scalingFactor = (float)dm.dmPelsWidth / screen.Bounds.Width;

            // 2. Calculate the physical position
            // Because dmPosition is often incorrect, we multiply the logical Windows position by a scale factor
            int physicalX = (int)(screen.Bounds.X * scalingFactor);
            int physicalY = (int)(screen.Bounds.Y * scalingFactor);

            return new Rectangle(physicalX, physicalY, dm.dmPelsWidth, dm.dmPelsHeight);
        }

        return screen.Bounds;
    }

    /// <summary>
    /// Calculates the monitor scale factor (e.g., 1.5 for 150%).
    /// </summary>
    public static float GetScalingFactor(this Screen screen)
    {
        DEVMODE dm = new DEVMODE();
        dm.dmSize = (short)Marshal.SizeOf(dm);

        if (EnumDisplaySettings(screen.DeviceName, ENUM_CURRENT_SETTINGS, ref dm))
        {
            return (float)dm.dmPelsWidth / screen.Bounds.Width;
        }

        return 1.0f; // Default 100%
    }
}