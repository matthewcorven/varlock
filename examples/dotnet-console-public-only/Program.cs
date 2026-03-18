using System.Linq;
using Varlock.Generated; // 👈 Varlock: generated at build time through publicOnly=true

var publicProperties = typeof(VarlockConfig)
    .GetProperties()
    .Select((property) => property.Name)
    .OrderBy((name) => name)
    .ToArray();

Console.WriteLine($"PUBLIC_PROPERTIES = {string.Join(",", publicProperties)}");
Console.WriteLine($"HAS_SECRET_TOKEN = {publicProperties.Contains("SecretToken")}");
Console.WriteLine($"HAS_SENSITIVE_KEYS_METADATA = {typeof(VarlockConfigMetadata).GetProperty("SensitiveKeys") is not null}");
Console.WriteLine($"HAS_PROPERTY_BINDINGS_METADATA = {typeof(VarlockConfigMetadata).GetProperty("PropertyBindings") is not null}");
Console.WriteLine($"PROPERTY_KEYS_COUNT = {VarlockConfigMetadata.PropertyKeys.Count}");