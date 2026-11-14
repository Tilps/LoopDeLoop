using System;
using System.Buffers.Text;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace LoopDeLoop
{
    public static class PuzzleCompression
    {
        public static string Compress(string text) => CompressToUrlSafe(text);
        public static string Decompress(string urlSafeBase64) => DecompressFromUrlSafe(urlSafeBase64);

        public static string CompressToUrlSafe(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            using var input = new StringStream(text, Encoding.UTF8);
            using var ms = new MemoryStream();
            using (var def = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true))
            {
                input.CopyTo(def);
            }
            ReadOnlySpan<byte> compressed = ms.TryGetBuffer(out var buffer) ? buffer : ms.ToArray();
            return Base64Url.EncodeToString(compressed);
        }

        public static string DecompressFromUrlSafe(string urlSafeBase64)
        {
            if (string.IsNullOrEmpty(urlSafeBase64)) return string.Empty;
            byte[] compressed = Base64Url.DecodeFromChars(urlSafeBase64);
            using var inMs = new ReadOnlyMemoryStream(compressed);
            using var def = new DeflateStream(inMs, CompressionMode.Decompress);
            using var outMs = new MemoryStream();
            def.CopyTo(outMs);
            ReadOnlySpan<byte> utf8 = outMs.TryGetBuffer(out var buffer) ? buffer : outMs.ToArray();
            return Encoding.UTF8.GetString(utf8);
        }
    }
}
