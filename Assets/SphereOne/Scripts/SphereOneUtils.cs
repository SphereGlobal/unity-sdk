using System;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SphereOne
{
    public static class SphereOneUtils
    {
        public static string BigIntToRoundedDollarString(BigInteger amount)
        {
            BigDecimal dec = new(amount, 6 * -1);

            return ((double)dec).ToString("F");
        }

        public static string BigIntToString(BigInteger amount, int decimals)
        {
            BigDecimal dec = new(amount, decimals * -1);

            return dec.ToString();
        }

        public static BigDecimal BigIntToBigDecimal(BigInteger amount, int decimals = 0)
        {
            return new BigDecimal(amount, decimals * -1);
        }

        public static string SecureRandomString(uint length, bool replaceSpecialCharacters = false)
        {
            uint byteSize = (uint)Math.Ceiling((double)(length * 3.0 / 4.0));

            var randomNumber = new byte[byteSize];
            using var rng = RandomNumberGenerator.Create();

            rng.GetBytes(randomNumber);

            string token = Convert.ToBase64String(randomNumber);

            if (replaceSpecialCharacters)
                token = RemoveSpecialCharacters(token, true);

            return token[..(int)length];
        }

        public static string RemoveSpecialCharacters(string str, bool replace = false)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

            StringBuilder sb = new();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_')
                {
                    sb.Append(c);
                }
                else if (replace)
                {
                    sb.Append(chars[UnityEngine.Random.Range(0, chars.Length)]);
                }
            }

            return sb.ToString();
        }

        private static readonly Regex sWhitespace = new(@"\s+");
        public static string ReplaceWhitespace(string input, string replacement)
        {
            return sWhitespace.Replace(input, replacement);
        }

        public static bool IsUrlValid(string url)
        {
            bool result = Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            return result;
        }
    }
}