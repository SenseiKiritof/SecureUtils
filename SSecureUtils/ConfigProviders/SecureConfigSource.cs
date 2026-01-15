using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace SSecureUtils.ConfigProviders;

internal class SecureConfigSource : JsonConfigurationSource
{

    internal readonly string Key;

    public SecureConfigSource(string path, string key, bool optional = false)
    {
        FileProvider = null;
        Path = path;
        Optional = optional;
        ReloadOnChange = false;
        ResolveFileProvider();
        Key = key;
    }

    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        EnsureDefaults(builder);
        return new SecureConfigProvider(this);
    }
}