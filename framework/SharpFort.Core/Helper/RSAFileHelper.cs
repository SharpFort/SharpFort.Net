using System.Security.Cryptography;

namespace SharpFort.Core.Helper
{
    public class RSAFileHelper
    {
        public static RSA GetKey()
        {
            return GetRSA("key.pem");
        }
        public static RSA GetPublicKey()
        {
            return GetRSA("public.pem");
        }

        private static RSA GetRSA(string fileName)
        {
            string rootPath = Directory.GetCurrentDirectory();
            string filePath = Path.Combine(rootPath, fileName);
            if (!File.Exists(filePath))
                throw new FileNotFoundException("文件不存在", filePath);
            string key = File.ReadAllText(filePath);
            var rsa = RSA.Create();
            rsa.ImportFromPem(key.AsSpan());
            return rsa;
        }
    }
}
