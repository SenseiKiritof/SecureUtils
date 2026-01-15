using System.Security.Cryptography;
using System.Text;

namespace SSecureUtils.Utils;

public static class SecureUtils
{
    public static string Encrypt(string text, string key)
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Key = Encoding.UTF8.GetBytes(key);

        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var msEncrypt = new MemoryStream();
        msEncrypt.Write(aes.IV, 0, 16);
        using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            //Write all data to the stream.
            swEncrypt.Write(text);
        }
        var encrypted = msEncrypt.ToArray();

        return Convert.ToBase64String(encrypted);
    }

    public static string Decrypt(string base64Text, string key)
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Key = Encoding.UTF8.GetBytes(key);
        
        using var msDecrypt = new MemoryStream(Convert.FromBase64String(base64Text));
            
        var bytes = new byte[16];
        _ = msDecrypt.Read(bytes, 0, bytes.Length);
        aes.IV = bytes;

        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);

        return srDecrypt.ReadToEnd();
    }
}