using System;
using System.IO;

namespace Mews.SignatureChecker
{
    public static class Extensions
    {
        public static byte[] ReadFully(this Stream stream, bool seekToBeginning = false)
        {
            if (seekToBeginning)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            var buffer = new byte[32768];
            using (var ms = new MemoryStream())
            {
                while (true)
                {
                    var read = stream.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                    {
                        return ms.ToArray();
                    }
                    ms.Write(buffer, 0, read);
                }
            }
        }

        public static string ToSignatureString(this DateTime dateTime)
        {
            return dateTime.ToString("yyyyMMddHHmmss");
        }
    }
}