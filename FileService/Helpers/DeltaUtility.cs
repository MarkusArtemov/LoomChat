using System.IO;
using BsDiff;

namespace De.Hsfl.LoomChat.File.Helpers
{
    /// <summary>
    /// Provides methods to create and apply binary deltas with bsdiff
    /// </summary>
    public static class DeltaUtility
    {
        /// <summary>
        /// Creates a delta file that transforms oldFilePath into newFilePath
        /// </summary>
        public static void CreateDelta(string oldFilePath, string newFilePath, string deltaPath)
        {
            using var fsOld = global::System.IO.File.OpenRead(oldFilePath);
            using var fsNew = global::System.IO.File.OpenRead(newFilePath);

            var oldBytes = new byte[fsOld.Length];
            fsOld.Read(oldBytes, 0, oldBytes.Length);

            var newBytes = new byte[fsNew.Length];
            fsNew.Read(newBytes, 0, newBytes.Length);

            using var fsDelta = global::System.IO.File.Create(deltaPath);
            BinaryPatch.Create(oldBytes, newBytes, fsDelta);
        }

        /// <summary>
        /// Applies a delta file to baseFilePath and writes the result to outputFilePath
        /// </summary>
        public static void ApplyDelta(string baseFilePath, string deltaPath, string outputFilePath)
        {
            using var fsBase = global::System.IO.File.OpenRead(baseFilePath);
            var baseBytes = new byte[fsBase.Length];
            fsBase.Read(baseBytes, 0, baseBytes.Length);

            using var fsDelta = global::System.IO.File.OpenRead(deltaPath);
            var deltaBytes = new byte[fsDelta.Length];
            fsDelta.Read(deltaBytes, 0, deltaBytes.Length);

            using var fsOut = global::System.IO.File.Create(outputFilePath);

            var baseMem = new MemoryStream(baseBytes);

            BinaryPatch.Apply(
                baseMem,
                () => new MemoryStream(deltaBytes),
                fsOut
            );
        }
    }
}
