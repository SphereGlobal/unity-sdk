using System;
using System.Text;
using Newtonsoft.Json;

// Reference: https://github.com/monry/JWT-for-Unity/blob/master/JWT/JWT.cs

namespace SphereOne
{
    [Serializable]
    public class JwtPayload
    {
        public long exp;
    }

    public static class JwtUtils
    {
        public static long GetTokenExpirationTime(string token)
        {
            var parts = token.Split('.');
            if (parts.Length != 3)
            {
                throw new ArgumentException("Token must consist from 3 delimited by dot parts");
            }
            var header = parts[0];
            var payload = parts[1];
            var crypto = Base64UrlDecode(parts[2]);

            var headerJson = Encoding.UTF8.GetString(Base64UrlDecode(header));
            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(payload));

            var jwtPayload = JsonConvert.DeserializeObject<JwtPayload>(payloadJson);

            return jwtPayload.exp;
        }

        public static bool IsTokenValid(string token)
        {
            if (token == null)
                return false;

            if (token == "")
                return false;

            return !IsTokenExpired(token);
        }

        public static bool IsTokenExpired(string token)
        {
            var tokenTicks = GetTokenExpirationTime(token);
            var tokenDate = DateTimeOffset.FromUnixTimeSeconds(tokenTicks).UtcDateTime;

            var now = DateTime.Now.ToUniversalTime();

            var isExpired = tokenDate < now;

            return isExpired;
        }

        // from JWT spec
        public static string Base64UrlEncode(byte[] input)
        {
            var output = Convert.ToBase64String(input);
            output = output.Split('=')[0]; // Remove any trailing '='s
            output = output.Replace('+', '-'); // 62nd char of encoding
            output = output.Replace('/', '_'); // 63rd char of encoding
            return output;
        }

        // from JWT spec
        public static byte[] Base64UrlDecode(string input)
        {
            var output = input;
            output = output.Replace('-', '+'); // 62nd char of encoding
            output = output.Replace('_', '/'); // 63rd char of encoding
            switch (output.Length % 4) // Pad with trailing '='s
            {
                case 0: break; // No pad chars in this case
                case 2: output += "=="; break; // Two pad chars
                case 3: output += "="; break;  // One pad char
                default: throw new Exception("Illegal base64url string!");
            }
            var converted = Convert.FromBase64String(output); // Standard base64 decoder
            return converted;
        }
    }
}

