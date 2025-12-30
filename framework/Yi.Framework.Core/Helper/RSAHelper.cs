using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Yi.Framework.Core.Helper
{
    /// <summary>
    /// RSA 加解密辅助类，使用 OpenSSL 格式的公钥加密/私钥解密
    /// </summary>
    /// <remarks>
    /// ⚠️ 安全审查说明：
    ///
    /// 1. 已知安全问题：
    ///    - 使用 PKCS1 填充模式（易受 Padding Oracle 攻击）
    ///    - RSAType.RSA 使用 SHA1 签名（已被证明不安全）
    ///    - 无密钥长度验证（应强制最少 2048 位）
    ///
    /// 2. 安全建议：
    ///    - 加密：使用 RSAEncryptionPadding.OaepSHA256 替代 Pkcs1
    ///    - 签名：始终使用 RSA2（SHA256），避免使用 RSA（SHA1）
    ///    - 密钥：生产环境使用至少 2048 位密钥（推荐 4096 位）
    ///    - 存储：私钥应使用 HSM 或密钥保管库保护
    ///    - 敏感数据：建议使用混合加密（RSA + AES）
    ///
    /// 3. 使用场景：
    ///    - 支付签名验证（如支付宝、微信支付）
    ///    - API 请求签名
    ///    - 小数据量加密传输
    ///
    /// 4. 公私钥生成（OpenSSL）：
    ///    <code>
    ///    # 生成 2048 位私钥
    ///    openssl genrsa -out private.pem 2048
    ///    # 导出公钥
    ///    openssl rsa -in private.pem -pubout -out public.pem
    ///    # 转换为 PKCS8 格式（Java 兼容）
    ///    openssl pkcs8 -topk8 -inform PEM -in private.pem -outform PEM -nocrypt -out private_pkcs8.pem
    ///    </code>
    /// </remarks>
    public class RSAHelper
    {
        public readonly RSA? _privateKeyRsaProvider;
        public readonly RSA? _publicKeyRsaProvider;
        private readonly HashAlgorithmName _hashAlgorithmName;
        private readonly Encoding _encoding;

        /// <summary>
        /// 实例化 RSAHelper
        /// </summary>
        /// <param name="rsaType">加密算法类型：RSA 使用 SHA1（不推荐）；RSA2 使用 SHA256（推荐）</param>
        /// <param name="encoding">字符编码类型</param>
        /// <param name="privateKey">Base64 编码的私钥（OpenSSL 格式）</param>
        /// <param name="publicKey">Base64 编码的公钥（可选，OpenSSL 格式）</param>
        /// <remarks>
        /// 安全建议：
        /// - 始终使用 RSAType.RSA2（SHA256 签名）
        /// - 密钥长度应至少 2048 位（支付宝要求）
        /// - 私钥应从安全存储加载，不要硬编码
        /// </remarks>
        public RSAHelper(RSAType rsaType, Encoding encoding, string privateKey, string? publicKey = null)
        {
            _encoding = encoding;
            if (!string.IsNullOrEmpty(privateKey))
            {
                _privateKeyRsaProvider = CreateRsaProviderFromPrivateKey(privateKey);
            }

            if (!string.IsNullOrEmpty(publicKey))
            {
                _publicKeyRsaProvider = CreateRsaProviderFromPublicKey(publicKey);
            }

            _hashAlgorithmName = rsaType == RSAType.RSA ? HashAlgorithmName.SHA1 : HashAlgorithmName.SHA256;
        }

        #region 使用私钥签名

        /// <summary>
        /// 使用私钥签名
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <returns></returns>
        public string Sign(string data)
        {
            byte[] dataBytes = _encoding.GetBytes(data);

            var signatureBytes = _privateKeyRsaProvider!.SignData(dataBytes, _hashAlgorithmName, RSASignaturePadding.Pkcs1);

            return Convert.ToBase64String(signatureBytes);
        }

        #endregion

        #region 使用公钥验签

        /// <summary>
        /// 使用公钥验签
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <param name="sign">签名</param>
        /// <returns></returns>
        public bool Verify(string data, string sign)
        {
            byte[] dataBytes = _encoding.GetBytes(data);
            byte[] signBytes = Convert.FromBase64String(sign);

            var verify = _publicKeyRsaProvider!.VerifyData(dataBytes, signBytes, _hashAlgorithmName, RSASignaturePadding.Pkcs1);

            return verify;
        }

        #endregion

        #region 解密

        //public string Decrypt(string cipherText)
        //{
        //	if (_privateKeyRsaProvider == null)
        //	{
        //		throw new Exception("_privateKeyRsaProvider is null");
        //	}
        //	return _encoding.GetString(_privateKeyRsaProvider.Decrypt(Convert.FromBase64String(cipherText), RSAEncryptionPadding.Pkcs1));
        //}

        /// <summary>
        /// 私钥解密（支持大量数据分块解密）
        /// </summary>
        /// <param name="cipherText">Base64 编码的密文字符串</param>
        /// <returns>解密后的明文字符串</returns>
        /// <exception cref="Exception">私钥提供者为 null 时抛出异常</exception>
        /// <remarks>
        /// 使用场景：
        /// 1. 解密超过密钥长度限制的大量数据（自动分块处理）
        /// 2. 私钥解密从前端或第三方系统传递的加密数据
        ///
        /// 实现机制：
        /// - 自动根据密钥长度（KeySize / 8）分块读取密文
        /// - 每块独立解密后拼接为完整明文
        /// - 使用 PKCS1 填充模式（兼容性最好，但建议生产环境使用 OAEP）
        /// </remarks>
        public string Decrypt(string cipherText)
        {
            if (_privateKeyRsaProvider == null)
            {
                throw new Exception("_privateKeyRsaProvider is null");
            }
            var bufferSize = _privateKeyRsaProvider.KeySize / 8;
            byte[] buffer = new byte[bufferSize];//待解密块  
            using (MemoryStream msInput = new MemoryStream(Convert.FromBase64String(cipherText)))
            {
                using (MemoryStream msOutput = new MemoryStream())
                {
                    int readLen; while ((readLen = msInput.Read(buffer, 0, bufferSize)) > 0)
                    {
                        byte[] dataToEnc = new byte[readLen];
                        Array.Copy(buffer, 0, dataToEnc, 0, readLen); byte[] encData = _privateKeyRsaProvider.Decrypt(dataToEnc, RSAEncryptionPadding.Pkcs1);
                        msOutput.Write(encData, 0, encData.Length);
                    }
                    byte[] result = msOutput.ToArray();
                    return _encoding.GetString(result);
                }
            }
        }

        #endregion

        #region 加密

        //public string Encrypt(string text)
        //{
        //	if (_publicKeyRsaProvider == null)
        //	{
        //		throw new Exception("_publicKeyRsaProvider is null");
        //	}
        //	return Convert.ToBase64String(_publicKeyRsaProvider.Encrypt(Encoding.UTF8.GetBytes(text), RSAEncryptionPadding.Pkcs1));
        //}

        /// <summary>
        /// 公钥加密（支持大量数据分块加密）
        /// </summary>
        /// <param name="text">待加密的明文字符串</param>
        /// <returns>Base64 编码的密文字符串</returns>
        /// <exception cref="Exception">公钥提供者为 null 时抛出异常</exception>
        /// <remarks>
        /// 使用场景：
        /// 1. 加密超过密钥长度限制的大量数据（自动分块处理）
        /// 2. 前端或第三方系统发送敏感数据前进行加密
        ///
        /// 实现机制：
        /// - 自动根据密钥长度计算每块最大数据量（KeySize / 8 - 11，PKCS1 填充需要 11 字节）
        /// - 每块独立加密后拼接为完整密文
        /// - 使用 PKCS1 填充模式（兼容性最好，但建议生产环境使用 OAEP）
        ///
        /// 安全建议：
        /// - 生产环境建议使用 RSAEncryptionPadding.OaepSHA256（更安全）
        /// - 密钥长度至少 2048 位（RSA2 标准）
        /// - 敏感数据建议使用混合加密（RSA + AES）
        /// </remarks>
        public string Encrypt(string text)
        {
            if (_publicKeyRsaProvider == null)
            {
                throw new Exception("_publicKeyRsaProvider is null");
            }
            var bufferSize = _publicKeyRsaProvider.KeySize / 8 - 11;
            byte[] buffer = new byte[bufferSize];//待加密块

            using (MemoryStream msInput = new MemoryStream(_encoding.GetBytes(text)))
            {
                using (MemoryStream msOutput = new MemoryStream())
                {
                    int readLen; while ((readLen = msInput.Read(buffer, 0, bufferSize)) > 0)
                    {
                        byte[] dataToEnc = new byte[readLen];
                        Array.Copy(buffer, 0, dataToEnc, 0, readLen); byte[] encData = _publicKeyRsaProvider.Encrypt(dataToEnc, RSAEncryptionPadding.Pkcs1);
                        msOutput.Write(encData, 0, encData.Length);
                    }
                    byte[] result = msOutput.ToArray();
                    return Convert.ToBase64String(result);
                }
            }
        }

        #endregion

        #region 使用私钥创建RSA实例
        /// <summary>
        /// 使用私钥创建RSA实例
        /// </summary>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        private RSA CreateRsaProviderFromPrivateKey(string privateKey)
        {
            var privateKeyBits = Convert.FromBase64String(privateKey);

            var rsa = RSA.Create();
            var rsaParameters = new RSAParameters();

            using (BinaryReader binr = new BinaryReader(new MemoryStream(privateKeyBits)))
            {
                byte bt = 0;
                ushort twobytes = 0;
                twobytes = binr.ReadUInt16();
                if (twobytes == 0x8130)
                    binr.ReadByte();
                else if (twobytes == 0x8230)
                    binr.ReadInt16();
                else
                    throw new Exception("Unexpected value read binr.ReadUInt16()");

                twobytes = binr.ReadUInt16();
                if (twobytes != 0x0102)
                    throw new Exception("Unexpected version");

                bt = binr.ReadByte();
                if (bt != 0x00)
                    throw new Exception("Unexpected value read binr.ReadByte()");

                rsaParameters.Modulus = binr.ReadBytes(GetIntegerSize(binr));
                rsaParameters.Exponent = binr.ReadBytes(GetIntegerSize(binr));
                rsaParameters.D = binr.ReadBytes(GetIntegerSize(binr));
                rsaParameters.P = binr.ReadBytes(GetIntegerSize(binr));
                rsaParameters.Q = binr.ReadBytes(GetIntegerSize(binr));
                rsaParameters.DP = binr.ReadBytes(GetIntegerSize(binr));
                rsaParameters.DQ = binr.ReadBytes(GetIntegerSize(binr));
                rsaParameters.InverseQ = binr.ReadBytes(GetIntegerSize(binr));
            }

            rsa.ImportParameters(rsaParameters);
            return rsa;
        }

        #endregion

        #region 使用公钥创建RSA实例
        /// <summary>
        /// 使用公钥创建RSA实例
        /// </summary>
        /// <param name="publicKeyString"></param>
        /// <returns></returns>
        public RSA? CreateRsaProviderFromPublicKey(string publicKeyString)
        {
            // encoded OID sequence for  PKCS #1 rsaEncryption szOID_RSA_RSA = "1.2.840.113549.1.1.1"
            byte[] seqOid = { 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00 };
            byte[] seq = new byte[15];

            var x509Key = Convert.FromBase64String(publicKeyString);

            // ---------  Set up stream to read the asn.1 encoded SubjectPublicKeyInfo blob  ------
            using (MemoryStream mem = new MemoryStream(x509Key))
            {
                using (BinaryReader binr = new BinaryReader(mem))  //wrap Memory Stream with BinaryReader for easy reading
                {
                    byte bt = 0;
                    ushort twobytes = 0;

                    twobytes = binr.ReadUInt16();
                    if (twobytes == 0x8130) //data read as little endian order (actual data order for Sequence is 30 81)
                        binr.ReadByte();    //advance 1 byte
                    else if (twobytes == 0x8230)
                        binr.ReadInt16();   //advance 2 bytes
                    else
                        return null;

                    seq = binr.ReadBytes(15);       //read the Sequence OID
                    if (!CompareBytearrays(seq, seqOid))    //make sure Sequence for OID is correct
                        return null;

                    twobytes = binr.ReadUInt16();
                    if (twobytes == 0x8103) //data read as little endian order (actual data order for Bit String is 03 81)
                        binr.ReadByte();    //advance 1 byte
                    else if (twobytes == 0x8203)
                        binr.ReadInt16();   //advance 2 bytes
                    else
                        return null;

                    bt = binr.ReadByte();
                    if (bt != 0x00)     //expect null byte next
                        return null;

                    twobytes = binr.ReadUInt16();
                    if (twobytes == 0x8130) //data read as little endian order (actual data order for Sequence is 30 81)
                        binr.ReadByte();    //advance 1 byte
                    else if (twobytes == 0x8230)
                        binr.ReadInt16();   //advance 2 bytes
                    else
                        return null;

                    twobytes = binr.ReadUInt16();
                    byte lowbyte = 0x00;
                    byte highbyte = 0x00;

                    if (twobytes == 0x8102) //data read as little endian order (actual data order for Integer is 02 81)
                        lowbyte = binr.ReadByte();  // read next bytes which is bytes in modulus
                    else if (twobytes == 0x8202)
                    {
                        highbyte = binr.ReadByte(); //advance 2 bytes
                        lowbyte = binr.ReadByte();
                    }
                    else
                        return null;
                    byte[] modint = { lowbyte, highbyte, 0x00, 0x00 };   //reverse byte order since asn.1 key uses big endian order
                    int modsize = BitConverter.ToInt32(modint, 0);

                    int firstbyte = binr.PeekChar();
                    if (firstbyte == 0x00)
                    {   //if first byte (highest order) of modulus is zero, don't include it
                        binr.ReadByte();    //skip this null byte
                        modsize -= 1;   //reduce modulus buffer size by 1
                    }

                    byte[] modulus = binr.ReadBytes(modsize);   //read the modulus bytes

                    if (binr.ReadByte() != 0x02)            //expect an Integer for the exponent data
                        return null;
                    int expbytes = binr.ReadByte();        // should only need one byte for actual exponent data (for all useful values)
                    byte[] exponent = binr.ReadBytes(expbytes);

                    // ------- create RSACryptoServiceProvider instance and initialize with public key -----
                    var rsa = RSA.Create();
                    RSAParameters rsaKeyInfo = new RSAParameters
                    {
                        Modulus = modulus,
                        Exponent = exponent
                    };
                    rsa.ImportParameters(rsaKeyInfo);

                    return rsa;
                }

            }
        }

        #endregion

        #region 导入密钥算法

        private int GetIntegerSize(BinaryReader binr)
        {
            byte bt = 0;
            int count = 0;
            bt = binr.ReadByte();
            if (bt != 0x02)
                return 0;
            bt = binr.ReadByte();

            if (bt == 0x81)
                count = binr.ReadByte();
            else
            if (bt == 0x82)
            {
                var highbyte = binr.ReadByte();
                var lowbyte = binr.ReadByte();
                byte[] modint = { lowbyte, highbyte, 0x00, 0x00 };
                count = BitConverter.ToInt32(modint, 0);
            }
            else
            {
                count = bt;
            }

            while (binr.ReadByte() == 0x00)
            {
                count -= 1;
            }
            binr.BaseStream.Seek(-1, SeekOrigin.Current);
            return count;
        }

        private bool CompareBytearrays(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;
            int i = 0;
            foreach (byte c in a)
            {
                if (c != b[i])
                    return false;
                i++;
            }
            return true;
        }

        #endregion

    }

    /// <summary>
    /// RSA 算法类型
    /// </summary>
    /// <remarks>
    /// 安全建议：生产环境请始终使用 RSA2（SHA256）
    /// </remarks>
    public enum RSAType
    {
        /// <summary>
        /// RSA with SHA1 签名（⚠️ 不推荐：SHA1 已被证明不安全）
        /// </summary>
        /// <remarks>
        /// 仅用于兼容旧系统，新项目请使用 RSA2
        /// </remarks>
        RSA = 0,

        /// <summary>
        /// RSA2 with SHA256 签名（✅ 推荐：安全性更高）
        /// </summary>
        /// <remarks>
        /// - 支付宝等支付平台要求使用 RSA2
        /// - 密钥长度至少 2048 位
        /// - SHA256 比 SHA1 更安全
        /// </remarks>
        RSA2
    }
}
