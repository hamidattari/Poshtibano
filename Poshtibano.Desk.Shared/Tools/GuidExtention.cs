using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poshtibano.Desk.Shared.Tools
{
    public static class GuidExtention
    {
        public static char GlobalSpacer { get; set; } = '-';
        public static string GuidToFormattedText(this Guid guid)
        {
            var number = ConvertGuidToNumber(guid);
            string formattedText = FormatNumber(number.ToString());

            return formattedText;
        }

        /// <summary>
        /// Convert GUID to 10-digit number
        /// </summary>
        public static long ConvertGuidToNumber(this Guid guid)
        {
            if (guid == Guid.Empty)
                return 0;

            // Get byte array from GUID
            byte[] guidBytes = guid.ToByteArray();

            // Use first 8 bytes to create a long value
            long value = BitConverter.ToInt64(guidBytes, 0);

            // Make it positive and limit to 10 digits
            value = Math.Abs(value);

            // Ensure it's exactly 10 digits (1000000000 to 9999999999)
            value = (value % 9000000000L) + 1000000000L;

            return value;
        }

        /// <summary>
        /// Convert 10-digit number back to GUID (if needed for reverse operation)
        /// </summary>
        public static Guid ConvertNumberToGuid(this long number)
        {
            number -= 1000000000L;

            byte[] bytes = new byte[16];
            byte[] numberBytes = BitConverter.GetBytes(number);

            // Fill first 8 bytes with the number
            Array.Copy(numberBytes, bytes, Math.Min(numberBytes.Length, 8));

            // Fill remaining bytes with a pattern (you can customize this)
            for (int i = 8; i < 16; i++)
            {
                bytes[i] = (byte)(i * 17); // Simple pattern
            }

            return new Guid(bytes);
        }

        /// <summary>
        /// Reverse number: 1-234-567-891 to 891-576-234-1
        /// </summary>
        public static string ReverseSessioId(this string digits)
        {
            digits = string.Join("-", digits.Split('-').Reverse());
            return digits;
        }


        /// <summary>
        /// Format number with spaces: 1 234 567 891
        /// </summary>
        public static string FormatNumber(string digits)
        {
            if (string.IsNullOrEmpty(digits))
                return string.Empty;

            // Remove any existing spaces
            digits = GetDigitsOnly(digits);

            // Apply formatting based on length
            StringBuilder formatted = new StringBuilder();

            for (int i = 0; i < digits.Length; i++)
            {
                formatted.Append(digits[i]);

                // Add space after positions: 1, 4, 7, 9
                if (i == 0 && digits.Length > 1) // After first digit
                {
                    formatted.Append(GlobalSpacer);
                }
                else if (i == 3 && digits.Length > 4) // After 4th digit
                {
                    formatted.Append(GlobalSpacer);
                }
                else if (i == 6 && digits.Length > 7) // After 7th digit
                {
                    formatted.Append(GlobalSpacer);
                }
                else if (i == 9 && digits.Length > 10) // After 9th digit
                {
                    formatted.Append(GlobalSpacer);
                }
            }

            return formatted.ToString();
        }

        /// <summary>
        /// Extract only digits from text
        /// </summary>
        public static string GetDigitsOnly(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return new string(text.Where(char.IsDigit).ToArray());
        }
    }
}
