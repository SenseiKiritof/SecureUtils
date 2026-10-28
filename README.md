# Secure Utils Library

This library exposes two utils:
- **AddSecureConfig** method
- **SecureUtils** class

## Installation
```
dotnet add package SSecureUtils
```
The current version on nuget.org is 1.1.0.

### Build from source

To use a local build, create a local feed and push the generated package:
```
dotnet pack -c Release -o ./artifacts
dotnet nuget add source ~/nuget-local --name LocalPackages
dotnet nuget push ./artifacts/SSecureUtils.*.nupkg --source LocalPackages
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

Both methods use AES-GCM, which hides the content *and* detects tampering: a value
altered after encryption is rejected instead of being decrypted into something else.
The key is hashed with SHA-256, so any key length works — AES itself only accepts
16, 24 or 32 bytes.

### Encrypt

Takes the string to encrypt and the key. The output is `v2:` followed by base64 of
`nonce (12 bytes) | tag (16 bytes) | ciphertext`.

### Decrypt

Takes the encrypted string and the key. Values carrying the `v2:` prefix go through
AES-GCM; values without it are read with the previous format, AES-CBC with the IV
in the first 16 bytes. **Nothing already encrypted needs to be re-encrypted**: the
old format stays readable, it is simply no longer produced.

A value that has been tampered with, or a wrong key, raises `CryptographicException`
rather than returning garbage — that is the point of the change.

### Why it changed

The previous format was AES-CBC with no authentication. Ciphertext could be altered
without anyone noticing, and in CBC flipping a bit in one block flips the same bit
in the next block of plaintext: targeted manipulation, not noise. Decryption errors
were also distinguishable from one another, which is what makes a padding oracle
possible.

## CyberChef

> These recipes produce and read the **legacy CBC format**, which is still accepted on
> decryption but is no longer what the library writes. They remain useful to read
> values encrypted before the change.

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
