using System.Security.Cryptography;
using System.Text;

namespace clipsync;


public class EncryptHelper {

    public static async Task<byte[]> encryptAsync(String data, String key){
        return await encryptAsync(Encoding.UTF8.GetBytes(data), key);
    }

    private static byte[] generateKey(String key, int keySize = 128){
        using(MemoryStream output = new MemoryStream(new byte[keySize/8]) ){;
            output.Write(Encoding.ASCII.GetBytes(key, 0, keySize/8), 0, keySize/128);
            return output.ToArray();
        }
    }

    public static async Task<byte[]> encryptAsync(byte[] data, String password){
        var provider = Aes.Create();
        var hash = MD5.Create();
        provider.KeySize = 128;
        provider.Key = hash.ComputeHash(Encoding.UTF8.GetBytes(password));
        provider.GenerateIV();
        var outputStream = new MemoryStream();

        var encryptor = provider.CreateEncryptor();
        outputStream.Write(provider.IV, 0, provider.IV.Length);
        using(CryptoStream cryptoStream = new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write) ){
            await cryptoStream.WriteAsync(data);
        }
        return outputStream.ToArray();
    }

    public static async Task<byte[]> decryptAsync(byte[] data, String password){
        var provider = Aes.Create();
        var hash = MD5.Create();

        using(MemoryStream input = new MemoryStream(data)){
            provider.KeySize = 128;
            provider.Key = hash.ComputeHash(Encoding.UTF8.GetBytes(password));
            byte[] iv = new byte[16];
            input.Read(iv, 0, 16);
            provider.IV = iv;

            var outputStream = new MemoryStream();
            var decryptor = provider.CreateDecryptor();
            using(CryptoStream cryptoStream = new CryptoStream(input, decryptor, CryptoStreamMode.Read) )
            {
                await cryptoStream.CopyToAsync(outputStream);
            }
            return outputStream.ToArray();
        }
    }
}
