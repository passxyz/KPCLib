using System;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;

namespace PassXYZLib
{
    /// <summary>
    /// Base58Check Encoding / Decoding (Bitcoin-style)
    /// </summary>
    /// <remarks>
    /// See here for more details: https://en.bitcoin.it/wiki/Base58Check_encoding
    /// </remarks>
    public static class Base58CheckEncoding
    {
        private const int CHECK_SUM_SIZE = 4;
        private const string DIGITS = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

        /// <summary>
        /// Encodes data with a 4-byte checksum
        /// </summary>
        /// <param name="data">Data to be encoded</param>
        /// <returns></returns>
        public static string ToBase58CheckString(byte[] data)
        {
            return ToBase58String(_AddCheckSum(data));
        }

        /// <summary>
        /// Convert a Base58 string back to a UTF8 string
        /// </summary>
        /// <param name="data">Data to be decoded</param>
        /// <returns>UTF8 string</returns>
        public static string GetString(string data)
        {
            var byt = DecodePlain(data);
            return System.Text.Encoding.UTF8.GetString(byt, 0, byt.Length);
        }

        /// <summary>
        /// Encodes data in plain Base58, without any checksum.
        /// </summary>
        /// <param name="data">The data to be encoded</param>
        /// <returns>Base58 string</returns>
        public static string ToBase58String(string data)
        {
            byte[] byt = System.Text.Encoding.UTF8.GetBytes(data);
            return ToBase58String(byt);
        }

        /// <summary>
        /// Encodes data in plain Base58, without any checksum.
        /// </summary>
        /// <param name="data">The data to be encoded</param>
        /// <returns>Base58 string</returns>
        public static string ToBase58String(byte[] data)
        {
            // Decode byte[] to BigInteger
            var intData = data.Aggregate<byte, BigInteger>(0, (current, t) => current * 256 + t);

            // Encode BigInteger to Base58 string
            var result = string.Empty;
            while (intData > 0)
            {
                var remainder = (int)(intData % 58);
                intData /= 58;
                result = DIGITS[remainder] + result;
            }

            // Append `1` for each leading 0 byte
            for (var i = 0; i < data.Length && data[i] == 0; i++)
            {
                result = '1' + result;
            }

            return result;
        }

        /// <summary>
        /// Decodes data in Base58Check format (with 4 byte checksum)
        /// </summary>
        /// <param name="data">Data to be decoded</param>
        /// <returns>Returns decoded data if valid; throws FormatException if invalid</returns>
        public static byte[] Decode(string data)
        {
            var dataWithCheckSum = DecodePlain(data);
            var dataWithoutCheckSum = _VerifyAndRemoveCheckSum(dataWithCheckSum);

            if (dataWithoutCheckSum == null)
            {
                throw new FormatException("Base58 checksum is invalid");
            }

            return dataWithoutCheckSum;
        }

        /// <summary>
        /// Decodes data in plain Base58, without any checksum.
        /// </summary>
        /// <param name="data">Data to be decoded</param>
        /// <returns>Returns decoded data if valid; throws FormatException if invalid</returns>
        public static byte[] DecodePlain(string data)
        {
            // Decode Base58 string to BigInteger 
            BigInteger intData = 0;
            for (var i = 0; i < data.Length; i++)
            {
                var digit = DIGITS.IndexOf(data[i]); //Slow

                if (digit < 0)
                {
                    throw new FormatException(string.Format("Invalid Base58 character `{0}` at position {1}", data[i], i));
                }

                intData = intData * 58 + digit;
            }

            // Encode BigInteger to byte[]
            // Leading zero bytes get encoded as leading `1` characters
            var leadingZeroCount = data.TakeWhile(c => c == '1').Count();
            var leadingZeros = Enumerable.Repeat((byte)0, leadingZeroCount);
            var bytesWithoutLeadingZeros =
              intData.ToByteArray()
              .Reverse()// to big endian
              .SkipWhile(b => b == 0);//strip sign byte
            var result = leadingZeros.Concat(bytesWithoutLeadingZeros).ToArray();

            return result;
        }

        private static byte[] _AddCheckSum(byte[] data)
        {
            var checkSum = _GetCheckSum(data);
            var dataWithCheckSum = ArrayHelpers.ConcatArrays(data, checkSum);

            return dataWithCheckSum;
        }

        //Returns null if the checksum is invalid
        private static byte[] _VerifyAndRemoveCheckSum(byte[] data)
        {
            var result = ArrayHelpers.SubArray(data, 0, data.Length - CHECK_SUM_SIZE);
            var givenCheckSum = ArrayHelpers.SubArray(data, data.Length - CHECK_SUM_SIZE);
            var correctCheckSum = _GetCheckSum(result);

            return givenCheckSum.SequenceEqual(correctCheckSum) ? result : null;
        }

        private static byte[] _GetCheckSum(byte[] data)
        {
            SHA256 sha256 = new SHA256Managed();
            var hash1 = sha256.ComputeHash(data);
            var hash2 = sha256.ComputeHash(hash1);

            var result = new byte[CHECK_SUM_SIZE];
            Buffer.BlockCopy(hash2, 0, result, 0, result.Length);

            return result;
        }
    }
}
