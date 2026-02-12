using System.Security.Cryptography;
using System.Text;

namespace WebApp_Core_Identity.Services
{
    public class EncryptionService
    {
        private readonly string _key;

        public EncryptionService(IConfiguration configuration)
 {
            _key = configuration["Encryption:Key"] ?? "FreshFarmMarket2025SecureKey123!"; // 32 chars for AES-256
        }

        public string Encrypt(string plainText)
   {
            if (string.IsNullOrEmpty(plainText))
 return plainText;

     using (Aes aes = Aes.Create())
 {
      aes.Key = Encoding.UTF8.GetBytes(_key.PadRight(32).Substring(0, 32));
          aes.GenerateIV();

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

  using (MemoryStream ms = new MemoryStream())
         {
              // Prepend IV to encrypted data
         ms.Write(aes.IV, 0, aes.IV.Length);

    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
       {
    using (StreamWriter sw = new StreamWriter(cs))
  {
               sw.Write(plainText);
       }
           }

          return Convert.ToBase64String(ms.ToArray());
           }
            }
  }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
            return cipherText;

  try
            {
          byte[] buffer = Convert.FromBase64String(cipherText);

       using (Aes aes = Aes.Create())
    {
  aes.Key = Encoding.UTF8.GetBytes(_key.PadRight(32).Substring(0, 32));

     // Extract IV from the beginning
  byte[] iv = new byte[aes.IV.Length];
        Array.Copy(buffer, 0, iv, 0, iv.Length);
             aes.IV = iv;

 ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

        using (MemoryStream ms = new MemoryStream(buffer, iv.Length, buffer.Length - iv.Length))
             {
            using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
       {
             using (StreamReader sr = new StreamReader(cs))
 {
        return sr.ReadToEnd();
   }
          }
    }
        }
       }
            catch
       {
  return "[Decryption Failed]";
            }
        }
    }
}
