using Microsoft.Extensions.Configuration;
using SSecureUtils.ConfigProviders;

namespace SSecureUtils.Extensions;

public static class ConfigBuilderExtension
{
    public static IConfigurationBuilder AddSecureConfig(this IConfigurationBuilder builder, string fileName, string key, bool optional = false)
    {

        /*builder.Add((Action<SecureConfigSource>) (s =>
        {
            s.FileProvider = null;
            s.Path = fileName;
            s.Optional = false;
            s.ReloadOnChange = false;
            s.ResolveFileProvider();
            s.Key = key;
        }));*/

        /*builder.Add(new SecureConfigSource
        {
            Path = fileName,
            Key = key,
            Optional = optional
        });*/

        builder.Add(new SecureConfigSource(fileName, key, optional));
        
        return builder;
    }
}