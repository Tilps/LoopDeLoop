using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LoopDeLoop;

namespace LoopDeLoop.Tests
{
    [TestClass]
    public class CodecAndCompressionTests
    {
        [TestMethod]
        public void PuzzleCodec_TryDecode_InvalidInputs_ReturnsFalseGracefully()
        {
            Assert.IsFalse(PuzzleCodec.TryDecode(null!, out _, out _, out _, out _));
            Assert.IsFalse(PuzzleCodec.TryDecode("", out _, out _, out _, out _));
            Assert.IsFalse(PuzzleCodec.TryDecode("   ", out _, out _, out _, out _));
            Assert.IsFalse(PuzzleCodec.TryDecode("sq", out _, out _, out _, out _));
            Assert.IsFalse(PuzzleCodec.TryDecode("sq/abc", out _, out _, out _, out _));
            Assert.IsFalse(PuzzleCodec.TryDecode("sq/-5x5", out _, out _, out _, out _));
            Assert.IsFalse(PuzzleCodec.TryDecode("sq/0x0", out _, out _, out _, out _));
        }

        [TestMethod]
        public void PuzzleCodec_TryDecode_HandlesVariousUrlPrefixes()
        {
            // Standard code
            string code = "sq/3x3/a1b2";
            Assert.IsTrue(PuzzleCodec.TryDecode(code, out var m1, out int w1, out int h1, out var t1));
            Assert.IsNotNull(m1);
            Assert.AreEqual(3, w1);
            Assert.AreEqual(3, h1);
            Assert.AreEqual(MeshType.Square, t1);

            // Hash prefix '#...'
            Assert.IsTrue(PuzzleCodec.TryDecode("#" + code, out var m2, out int w2, out int h2, out var t2));
            Assert.IsNotNull(m2);
            Assert.AreEqual(3, w2);

            // Parameter prefix 'p=...'
            Assert.IsTrue(PuzzleCodec.TryDecode("p=" + code, out var m3, out int w3, out int h3, out var t3));
            Assert.IsNotNull(m3);
            Assert.AreEqual(3, w3);

            // Full parameter prefix 'puzzle=...'
            Assert.IsTrue(PuzzleCodec.TryDecode("puzzle=" + code, out var m4, out int w4, out int h4, out var t4));
            Assert.IsNotNull(m4);
            Assert.AreEqual(3, w4);
        }

        [TestMethod]
        public void PuzzleCodec_RunLengthEncoding_LongEmptyRunsHandled()
        {
            // 10x10 mesh has 100 cells
            var mesh = new Mesh(10, 10, MeshType.Square);
            // Put a clue only at cell 0 and cell 90
            mesh.SetClue(0, 3);
            mesh.SetClue(90, 2);

            string encoded = PuzzleCodec.Encode(mesh, 10, 10);
            Assert.IsTrue(encoded.Contains("z"), "Empty runs > 26 must produce 'z' runs");

            Assert.IsTrue(PuzzleCodec.TryDecode(encoded, out var decoded, out int w, out int h, out var type));
            Assert.IsNotNull(decoded);
            Assert.AreEqual(10, w);
            Assert.AreEqual(10, h);
            Assert.AreEqual(100, decoded.Cells.Count);
            Assert.AreEqual(3, decoded.Cells[0].TargetCount);
            Assert.AreEqual(2, decoded.Cells[90].TargetCount);
            Assert.AreEqual(-1, decoded.Cells[50].TargetCount);
            Assert.AreEqual(-1, decoded.Cells[99].TargetCount);
        }

        [TestMethod]
        public void PuzzleCompression_EmptyAndNull_ReturnsEmptyString()
        {
            Assert.AreEqual(string.Empty, PuzzleCompression.Compress(null!));
            Assert.AreEqual(string.Empty, PuzzleCompression.Compress(""));
            Assert.AreEqual(string.Empty, PuzzleCompression.Decompress(null!));
            Assert.AreEqual(string.Empty, PuzzleCompression.Decompress(""));
        }

        [TestMethod]
        public void PuzzleCompression_LargePayload_RoundTripsAccurately()
        {
            string payload = "LoopDeLoop-Slitherlink-Test-Payload-" + new string('x', 500) + "-12345";
            string compressed = PuzzleCompression.Compress(payload);
            Assert.IsFalse(string.IsNullOrEmpty(compressed));
            Assert.IsTrue(compressed.Length < payload.Length);

            string restored = PuzzleCompression.Decompress(compressed);
            Assert.AreEqual(payload, restored);
        }
    }
}
