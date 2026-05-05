using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SharpFort.Core.Helper
{
    public class MD5Helper
    {
        /// <summary>
        /// 生成PasswordSalt
        /// </summary>
        /// <returns>返回string</returns>
        public static string GenerateSalt()
        {
            byte[] buf = new byte[16];
            RandomNumberGenerator.Fill(buf);
            return Convert.ToBase64String(buf);
        }

        /// <summary>
        /// 加密密码
        /// </summary>
        /// <param name="pass">密码</param>
        /// <param name="passwordFormat">加密类型</param>
        /// <param name="salt">PasswordSalt</param>
        /// <returns>加密后的密码</returns>
        public static string SHA2Encode(string pass, string salt, int passwordFormat = 1)
        {
            if (passwordFormat == 0) // MembershipPasswordFormat.Clear
                return pass;

            byte[] bIn = Encoding.Unicode.GetBytes(pass);
            byte[] bSalt = Convert.FromBase64String(salt);
            byte[] bAll = new byte[bSalt.Length + bIn.Length];
            byte[]? bRet = null;

            Buffer.BlockCopy(bSalt, 0, bAll, 0, bSalt.Length);
            Buffer.BlockCopy(bIn, 0, bAll, bSalt.Length, bIn.Length);

            bRet = SHA512.HashData(bAll);

            return ConvertEx.ToUrlBase64String(bRet);
        }

        /// <summary>
        /// 16位MD5加密
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public static string MD5Encrypt16(string password)
        {
#pragma warning disable CA5351 // MD5 用于非安全性哈希（兼容性需求）
            string t2 = BitConverter.ToString(MD5.HashData(Encoding.Default.GetBytes(password)), 4, 8);
#pragma warning restore CA5351
            t2 = t2.Replace("-", string.Empty);
            return t2;
        }

        /// <summary>
        /// 32位MD5加密
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public static string MD5Encrypt32(string password = "")
        {
            string pwd = string.Empty;
            try
            {
                if (!string.IsNullOrEmpty(password) && !string.IsNullOrWhiteSpace(password))
                {
#pragma warning disable CA5351 // MD5 用于非安全性哈希（兼容性需求）
                    byte[] s = MD5.HashData(Encoding.UTF8.GetBytes(password));
#pragma warning restore CA5351
                    foreach (var item in s)
                    {
                        pwd = string.Concat(pwd, item.ToString("X2", CultureInfo.InvariantCulture));
                    }
                }
            }
            catch
            {
                throw new InvalidOperationException($"错误的 password 字符串:【{password}】");
            }
            return pwd;
        }

        /// <summary>
        /// 64位MD5加密
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public static string MD5Encrypt64(string password)
        {
#pragma warning disable CA5351 // MD5 用于非安全性哈希（兼容性需求）
            byte[] s = MD5.HashData(Encoding.UTF8.GetBytes(password));
#pragma warning restore CA5351
            return Convert.ToBase64String(s);
        }
    }
#pragma warning disable CA1711 // "Ex" 后缀是工具类命名约定
    public class ConvertEx
#pragma warning restore CA1711
    {
        static readonly char[] padding = { '=' };
        public static string ToUrlBase64String(byte[] inArray)
        {
            var str = Convert.ToBase64String(inArray);
            str = str.TrimEnd(padding).Replace('+', '-').Replace('/', '_');

            return str;
        }

        public static byte[] FromUrlBase64String(string s)
        {
            string incoming = s.Replace('_', '/').Replace('-', '+');
            switch (s.Length % 4)
            {
                case 2: incoming += "=="; break;
                case 3: incoming += "="; break;
            }
            byte[] bytes = Convert.FromBase64String(incoming);

            return bytes;
        }
    }
}
