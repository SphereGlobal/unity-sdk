using System;
using System.Numerics;
using Newtonsoft.Json;

namespace SphereOne
{
    [JsonConverter(typeof(TolerantEnumConverter))]
    public enum SupportedChains
    {
        ETHEREUM,
        SOLANA,
        POLYGON,
        GNOSIS,
        OPTIMISM,
        IMMUTABLE,
        AVALANCHE,
        BINANCE,
        ARBITRUM,
        FANTOM,
        EOSEVM,
        Unknown
    }

    [JsonConverter(typeof(TolerantEnumConverter))]
    public enum WalletType
    {
        EOA,
        SMARTWALLET,
        Unknown
    }

    [Serializable]
    public class User
    {
        public string uid;
        public bool signedUp;
        public string name;
        public string email;
        public string username;
        public string currencyISO;
        public string countryCode;
        public string countryFlag;
        public bool isMerchant;
    }

    [Serializable]
    public class Wallet
    {
        public string address;
        public SupportedChains[] chains;
        public string publicKey;
        public WalletType type;
        public bool isImported;
        public string uid;
    }

    [Serializable]
    public class Balance
    {
        public BigInteger price;
        public BigInteger amount;
        public string address;
        public string chain;
        public TokenMetadata tokenMetadata;
    }

    [Serializable]
    public class TokenMetadata
    {
        public string symbol;
        public string name;
        public int decimals;
        public string address;
        public string logoURI;
        public string chain;
    }

    [Serializable]
    public class Nft
    {
        public string img;
        public string name;
        public string address;
        public string tokenType;
    }
}