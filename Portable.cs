using System;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace SmartInventoryArrange;

internal static class Portable
{
    internal static string Json(object value) => JsonConvert.SerializeObject(value);
    internal static byte[] Utf8(object value) => Encoding.UTF8.GetBytes(Json(value));
    internal static string Sha256Hex(byte[] bytes)
    {
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
    }
    internal static string Sha256Hex(string value) => Sha256Hex(Encoding.UTF8.GetBytes(value));
    internal static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
