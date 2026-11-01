using Newtonsoft.Json.Linq;
namespace SmartInventoryArrange.Core;

/// <summary>Frozen explanatory model. Only breaks equal explicit geometry ranks;
/// no model coefficient can bypass item safety or veto the approved visual policy.</summary>
internal sealed class FrozenPreferenceModel
{
    private readonly string[] _names;
    private readonly double[] _scale;
    private readonly double[] _weights;
    private FrozenPreferenceModel(string[] names, double[] scale, double[] weights)
    { _names = names; _scale = scale; _weights = weights; }
    private static readonly Lazy<FrozenPreferenceModel> Embedded = new(LoadCore);
    public static FrozenPreferenceModel LoadEmbedded() => Embedded.Value;
    private static FrozenPreferenceModel LoadCore()
    {
        using var stream = typeof(FrozenPreferenceModel).Assembly.GetManifestResourceStream("SmartInventoryArrange.directional21.json") ?? throw new InvalidOperationException("Frozen preference resource missing.");
        using var reader = new StreamReader(stream); var r = JObject.Parse(reader.ReadToEnd());
        if (r["kind"]?.Value<string>() != "nonnegative-regularized-pair-logistic" || r["profile"]?.Value<string>() != "semantic") throw new InvalidOperationException("Frozen model schema mismatch.");
        var names = r["featureNames"]!.Select(q => q.Value<string>()!).ToArray(); var scale = r["scale"]!.Select(q => q.Value<double>()).ToArray(); var weights = r["weights"]!.Select(q => q.Value<double>()).ToArray();
        if (names.Length != scale.Length || names.Length != weights.Length || names.Distinct().Count() != names.Length || scale.Any(v => !CoreCompatibility.IsFinite(v) || v <= 0) || weights.Any(v => !CoreCompatibility.IsFinite(v) || v < 0)) throw new InvalidOperationException("Frozen model numeric domain invalid.");
        return new(names, scale, weights);
    }
    public double Value(IReadOnlyDictionary<string, double> features)
    {
        double value = 0; for (int i = 0; i < _names.Length; i++)
        { if (!features.TryGetValue(_names[i], out var f) || !CoreCompatibility.IsFinite(f)) throw new InvalidOperationException("Preference feature missing or not finite: " + _names[i]); value += f / _scale[i] * _weights[i]; }
        return value;
    }
}
