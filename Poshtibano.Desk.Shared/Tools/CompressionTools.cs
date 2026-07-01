using System.IO.Compression;

namespace Poshtibano.Desk.Shared.Tools
{
    public class CompressionTools
    {
        public static byte[] Compress(byte[] data)
        {

            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(ms, CompressionLevel.SmallestSize))
                {
                    gzip.Write(data, 0, data.Length);
                }
                return ms.ToArray();
            }
        }

        public static byte[] Decompress(byte[] data)
        {
            try
            {
                using (MemoryStream input = new MemoryStream(data))
                using (MemoryStream output = new MemoryStream())
                using (GZipStream gzip = new GZipStream(input, CompressionMode.Decompress))
                {
                    gzip.CopyTo(output);
                    return output.ToArray();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Decompress error: {ex.Message}");
                throw;
            }
        }

        public static byte[] Compress2(byte[] data)
        {
            using (var input = new MemoryStream(data))
            using (var output = new MemoryStream())
            using (var brotli = new BrotliStream(output, CompressionLevel.SmallestSize))
            {
                input.CopyTo(brotli);
                return output.ToArray();
            }
        }

        public static byte[] Decompress2(byte[] data)
        {
            using (var input = new MemoryStream(data))
            using (var output = new MemoryStream())
            using (var brotli = new BrotliStream(input, CompressionMode.Decompress))
            {
                brotli.CopyTo(output);
                return output.ToArray();
            }
        }
    }
}
