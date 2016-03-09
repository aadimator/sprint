using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Paper_Portal.Helpers
{
    class Encrypt
    {

        public string Password { get; set; }

        private byte[] AES_Encrypt(byte[] bytesToBeEncrypted, byte[] passwordBytes)
        {
            byte[] encryptedBytes = null;

            // Set your salt here, change it to meet your flavor:
            // The salt bytes must be at least 8 bytes.
            byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using (MemoryStream ms = new MemoryStream())
            {
                using (AesManaged AES = new AesManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC;

                    using (var cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                        cs.Close();
                    }
                    encryptedBytes = ms.ToArray();
                }
            }

            return encryptedBytes;
        }

        private byte[] AES_Decrypt(byte[] bytesToBeDecrypted, byte[] passwordBytes)
        {
            byte[] decryptedBytes = null;

            // Set your salt here, change it to meet your flavor:
            // The salt bytes must be at least 8 bytes.
            byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using (MemoryStream ms = new MemoryStream())
            {
                using (AesManaged AES = new AesManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC;

                    using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
                        cs.Close();
                    }
                    decryptedBytes = ms.ToArray();
                }
            }

            return decryptedBytes;
        }

        public void EncryptFile(string file, string output)
        {
            //byte[] bytesToBeEncrypted = File.ReadAllBytes(file);
            Stream stream = File.OpenRead(file);
            EncryptFile(stream, output);
        }
        public void EncryptFile(Stream inputStream, string output)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                inputStream.CopyTo(ms);
                byte[] bytesToBeEncrypted = ms.ToArray();
                EncryptFile(bytesToBeEncrypted, output);
            }
        }
        private void EncryptFile(byte[] bytesToBeEncrypted, string output)
        {
            Password = GenerateToken(30);

            System.Console.WriteLine(Password);
            System.Console.WriteLine();
            byte[] passwordBytes = Encoding.UTF8.GetBytes(Password);

            // Hash the password with SHA256
            passwordBytes = SHA256.Create().ComputeHash(passwordBytes);

            byte[] bytesEncrypted = AES_Encrypt(bytesToBeEncrypted, passwordBytes);

            File.WriteAllBytes(output, bytesEncrypted);
        }

        public void DecryptFile(string inputPath, string outputPath, string EncKey = "")
        {
            using (var file = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write))
            {
                DecryptFile(inputPath, EncKey).WriteTo(file);
            }
        }
        public MemoryStream DecryptFile (string inputPath, string EncKey = "")
        {
            // set Password before calling this function
            if (Password == null && EncKey != "")
            {
                Password = EncKey;
            }
            if (Password == null && EncKey == "")
            {
                throw new NullReferenceException();
            }

            byte[] bytesToBeDecrypted = File.ReadAllBytes(inputPath);
            byte[] passwordBytes = Encoding.UTF8.GetBytes(Password);
            passwordBytes = SHA256.Create().ComputeHash(passwordBytes);

            byte[] bytesDecrypted = AES_Decrypt(bytesToBeDecrypted, passwordBytes);

            return new MemoryStream(bytesDecrypted);
        }

        public string GenerateToken(int length)
        {
            RNGCryptoServiceProvider cryptRNG = new RNGCryptoServiceProvider();
            byte[] tokenBuffer = new byte[length];
            cryptRNG.GetBytes(tokenBuffer);
            return Convert.ToBase64String(tokenBuffer);
        }

        public string ComputeHash (string FilePath)
        {
            return ComputeHash(File.OpenRead(FilePath));
        }
        public string ComputeHash (Stream input)
        {
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(input)).Replace("-", string.Empty);
            }
        }

        public bool VerifyHash (string original, string test)
        {
            return String.Compare(original, test) == 0;
        }
    }
}
