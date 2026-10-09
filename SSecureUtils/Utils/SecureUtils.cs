using System.Security.Cryptography;
using System.Text;

namespace SSecureUtils.Utils;

public static class SecureUtils
{
    /// <summary>
    /// Marcatore del formato autenticato. I valori cifrati prima di questo cambio non ce
    /// l'hanno e continuano a essere letti dal percorso legacy: la configurazione gia' in
    /// esercizio non va ricifrata per far ripartire i servizi.
    /// </summary>
    private const string AuthenticatedPrefix = "v2:";

    private const int NonceSize = 12;
    private const int TagSize = 16;

    /// <summary>
    /// Il parametro con la lunghezza del tag esiste da .NET 8; prima il costruttore aveva
    /// solo la chiave e il tag era comunque di 16 byte, quindi il formato non cambia.
    /// </summary>
    private static AesGcm CreateAesGcm(byte[] key) =>
#if NET8_0_OR_GREATER
        new(key, TagSize);
#else
        new(key);
#endif

    /// <summary>
    /// La chiave di configurazione e' una stringa di lunghezza qualunque, mentre AES ne
    /// vuole 16, 24 o 32 byte: l'impronta la porta a 32 senza vincolare chi la sceglie.
    /// Vale finche' la chiave ha entropia sua, com'e' per quelle generate a caso; una
    /// passphrase scelta a mano richiederebbe invece una derivazione con iterazioni.
    /// </summary>
    private static byte[] DeriveKey(string key) => SHA256.HashData(Encoding.UTF8.GetBytes(key));

    /// <summary>
    /// Cifra in AES-GCM, che oltre a nascondere il testo ne garantisce l'integrita':
    /// il formato precedente era CBC senza autenticazione, quindi il testo cifrato era
    /// modificabile e gli errori di decifratura distinguibili.
    /// </summary>
    public static string Encrypt(string text, string key)
    {
        var plaintext = Encoding.UTF8.GetBytes(text);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using (var aesGcm = CreateAesGcm(DeriveKey(key)))
        {
            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);
        }

        var payload = new byte[NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, payload, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, payload, NonceSize + TagSize, ciphertext.Length);

        return AuthenticatedPrefix + Convert.ToBase64String(payload);
    }

    public static string Decrypt(string base64Text, string key)
    {
        return base64Text.StartsWith(AuthenticatedPrefix, StringComparison.Ordinal)
            ? DecryptAuthenticated(base64Text[AuthenticatedPrefix.Length..], key)
            : DecryptLegacy(base64Text, key);
    }

    private static string DecryptAuthenticated(string base64Payload, string key)
    {
        var payload = Convert.FromBase64String(base64Payload);
        if (payload.Length < NonceSize + TagSize)
        {
            throw new CryptographicException("Il valore cifrato e' piu' corto del minimo consentito.");
        }

        var nonce = payload.AsSpan(0, NonceSize);
        var tag = payload.AsSpan(NonceSize, TagSize);
        var ciphertext = payload.AsSpan(NonceSize + TagSize);
        var plaintext = new byte[ciphertext.Length];

        // Se il valore e' stato manomesso, Decrypt solleva CryptographicException invece di
        // restituire testo arbitrario: e' il motivo per cui si e' passati a GCM.
        using (var aesGcm = CreateAesGcm(DeriveKey(key)))
        {
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
        }

        return Encoding.UTF8.GetString(plaintext);
    }

    /// <summary>
    /// Formato precedente: AES-CBC con IV in testa e chiave presa come byte grezzi della
    /// stringa. Resta per leggere quanto e' gia' cifrato in configurazione; non viene piu'
    /// prodotto. Quando tutti gli ambienti saranno stati ricifrati, questo metodo puo' sparire.
    /// </summary>
    private static string DecryptLegacy(string base64Text, string key)
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Key = Encoding.UTF8.GetBytes(key);

        var payload = Convert.FromBase64String(base64Text);
        if (payload.Length < 16)
        {
            throw new CryptographicException("Il valore cifrato non contiene un IV completo.");
        }

        var iv = new byte[16];
        Buffer.BlockCopy(payload, 0, iv, 0, iv.Length);
        aes.IV = iv;

        using var msDecrypt = new MemoryStream(payload, iv.Length, payload.Length - iv.Length);

        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);

        return srDecrypt.ReadToEnd();
    }
}
