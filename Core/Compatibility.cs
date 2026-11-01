using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace SmartInventoryArrange.Core;

internal static class CoreCompatibility
{
    public static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    public static double Clamp(double value, double min, double max) => value < min ? min : value > max ? max : value;

    public static string Serialize<T>(T value) => JsonConvert.SerializeObject(value);

    public static byte[] SerializeToUtf8Bytes<T>(T value) => Encoding.UTF8.GetBytes(Serialize(value));

    public static string Sha256Hex(byte[] bytes)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
        var text = new StringBuilder(hash.Length * 2);
        foreach (var b in hash) text.Append(b.ToString("X2"));
        return text.ToString();
    }

    public static long GetAllocatedBytesForCurrentThread() => GC.GetTotalMemory(false);

    // netstandard2.1 has no per-thread allocation counter. This is non-negative
    // process-memory telemetry only; collection can make the raw delta negative.
    public static long GetAllocatedBytesSince(long baseline) => Math.Max(0, GC.GetTotalMemory(false) - baseline);

    public static byte[] ToUnsignedBytes(BigInteger value)
    {
        var bytes = value.ToByteArray();
        if (bytes.Length > 1 && bytes[^1] == 0) Array.Resize(ref bytes, bytes.Length - 1);
        return bytes;
    }

    public static int TrailingZeroCount(byte value)
    {
        if (value == 0) return 8;
        int count = 0;
        while ((value & 1) == 0) { value >>= 1; count++; }
        return count;
    }

    public static int PopCount(byte value)
    {
        int count = 0;
        while (value != 0) { value &= (byte)(value - 1); count++; }
        return count;
    }

    internal sealed class ReferenceComparer<T> : IEqualityComparer<T> where T : class
    {
        public static readonly ReferenceComparer<T> Instance = new();
        private ReferenceComparer() { }
        public bool Equals(T? x, T? y) => ReferenceEquals(x, y);
        public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
