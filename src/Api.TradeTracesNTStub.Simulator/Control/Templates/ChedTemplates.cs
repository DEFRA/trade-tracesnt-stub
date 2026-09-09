using System.Reflection;
using System.Xml.Serialization;
using TracesNT.WebServices;

namespace Api.TradeTracesNTStub.Simulator.Control.Templates;

/// <summary>
/// The baseline CHED documents the control API layers overrides onto.
/// </summary>
/// <remarks>
/// Templates are real captured TRACES responses, so every code in them already carries the display
/// name TRACES looked up. That is what lets a test supply three fields and still get a document the
/// gateway maps exactly as it maps the real thing.
/// </remarks>
public static class ChedTemplates
{
    private static readonly Assembly s_assembly = typeof(ChedTemplates).Assembly;

    /// <summary>
    /// The generated type carries no XmlRoot, so the root element has to be named explicitly.
    /// </summary>
    private static readonly XmlSerializer s_serializer = new(
        typeof(SPSCertificateType),
        new XmlRootAttribute("SPSCertificate") { Namespace = SpsNamespaces.SpsCertificate }
    );

    private const string ResourcePrefix = "Api.TradeTracesNTStub.Simulator.Control.Templates.";

    /// <summary>Template name to embedded resource. Named templates let a fixture pick a variant.</summary>
    private static readonly IReadOnlyDictionary<string, string> s_resourceByName = new Dictionary<
        string,
        string
    >(StringComparer.OrdinalIgnoreCase)
    {
        ["CHEDA"] = $"{ResourcePrefix}CHEDA.xml",
    };

    public static IEnumerable<string> Names => s_resourceByName.Keys;

    public static bool Exists(string name) => s_resourceByName.ContainsKey(name);

    /// <summary>
    /// Reads a fresh copy of a template. Fresh because callers mutate what they get back — a shared
    /// instance would leak one test's overrides into the next.
    /// </summary>
    public static SPSCertificateType Load(string name)
    {
        if (!s_resourceByName.TryGetValue(name, out var resource))
        {
            throw new ArgumentException(
                $"No CHED template named '{name}'. Available: {string.Join(", ", Names)}.",
                nameof(name)
            );
        }

        using var stream =
            s_assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Template resource '{resource}' is missing from the assembly.");

        return (SPSCertificateType)s_serializer.Deserialize(stream)!;
    }

    /// <summary>Serialises a certificate back to TRACES XML. Used by tests and diagnostics, not the SOAP face.</summary>
    public static string ToXml(SPSCertificateType certificate)
    {
        using var writer = new StringWriter();
        s_serializer.Serialize(writer, certificate);
        return writer.ToString();
    }
}
