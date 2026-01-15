# Secure Utils Library

This library exposes two utils:
- **AddSecureConfig** method
- **SecureUtils** class

## Installation
```
dotnet pack -c Release
``` 
if needed, add local nuget feed:
``` 
mkdir ~/nuget-local
nuget add source ~/nuget-local --name LocalPackages
dotnet nuget push SSecureUtils/bin/Release/SSecureUtils.1.0.0.nupkg --source LocalPackages
```


## AddSecureConfig

This static method is useful to include in the project AES encrypted properties.
To use it:
- Include in the project an *appsettings.\<Environment\>.Secure.json* file with encrypted properties:
```
{
    "ConnectionStrings":
        "Default": "<encrypted string>"
}
```
- In the Program/Startup.cs add the following line:
```
builder.Configuration.AddSecureConfig($"appsettings.{builder.Environment.EnvironmentName}.Secure.json", builder.Configuration["ENC_KEY"]!);
```
The *ENC_KEY* is the environment variable where the encryption key is stored.

The decrypted property can now be used like any other property; for example:
```
_configuration["ConnectionStrings:Default"]
```

## Secure Utils

This static class exposes two utils methods:
- string Encrypt(string text, string key)
- string Decrypt(string base64Text, string key)

Both methods use AES encryption.

### Encrypt

The encrypt method takes as input parameters the string to encrypt and the key to use for AES encryption.
The IV is randomly generated and added as the first 16 bytes of the encrypted string; the output string will be in base64 format.

### Decrypt

The decrypt method takes as input parameters the encrypted string (in base 64 format) and the key to use for AES decryption.
The IV is retrieved from the first 16 bytes of the encrypted string and then used for decryption.

## CyberChef

To encrypt the properties without writing a dotnet core tool, [CyberChef](https://gchq.github.io/CyberChef) can be used.

To encrypt a property, generating a random IV, the following recipe can be used:
```
Register('(.*)',true,false,false)
HTTP_request('GET','https://www.random.org/cgi-bin/randbyte?nbytes=16&format=h','','Cross-Origin Resource Sharing',false)
From_Hex('Space')
Register('(.*)',true,false,true)
Find_/_Replace({'option':'Regex','string':'(.*)'},'$R1$R0',false,true,false,true)
AES_Encrypt({'option':'UTF8','string':'<encryption key>'},{'option':'UTF8','string':'$R1'},'CBC','Raw','Raw',{'option':'Hex','string':''})
To_Base64('A-Za-z0-9+/=')
```
where *<encryption key>* is the encryption key.

In CyberChef the property can also be decrypted using this recipe:
```
From_Base64('A-Za-z0-9+/=',false,false)
Register('(.*)',true,false,true)
Take_bytes(0,16,false)
To_Base64('A-Za-z0-9+/=')
Register('(.*)',true,false,true)
Find_/_Replace({'option':'Regex','string':'(.*)'},'$R0',false,true,false,true)
AES_Decrypt({'option':'UTF8','string':'<encryption key>'},{'option':'Base64','string':'$R1'},'CBC','Raw','Raw',{'option':'Hex','string':''},{'option':'Hex','string':''})
Drop_bytes(0,16,false)
```
where again *<encryption key>* is the encryption key.
