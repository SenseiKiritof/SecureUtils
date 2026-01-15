using Microsoft.Extensions.Configuration.Json;

namespace SSecureUtils.ConfigProviders;

internal class SecureConfigProvider : JsonConfigurationProvider
{
    private readonly SecureConfigSource _source;
    
    public SecureConfigProvider(SecureConfigSource source) : base(source)
    {
        _source = source;
    }
    
    public override void Load()
    {
        base.Load();
        foreach (var (key, value) in Data)
        {

            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            Data[key] = Utils.SecureUtils.Decrypt(value, _source.Key);
        }
    }
}