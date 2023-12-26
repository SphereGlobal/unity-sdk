using System;
using System.Collections.Generic;
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
        FLOW,
        Unknown
    }

    [JsonConverter(typeof(TolerantEnumConverter))]
    public enum WalletType
    {
        EOA,
        SMARTWALLET,
        Unknown
    }

    [JsonConverter(typeof(TolerantEnumConverter))]
    public enum TxStatus
    {
        PENDING, // Not executed already
        PROCESSING, // Waiting to get mined
        SUCCESS,
        FAILURE,
        CANCELED,
        WAITING, // Waiting for user to finish the provider flow
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

#nullable enable
    [Serializable]
    public class ChargeItem
    {
        public ChargeItem() : this(default!, default!, default!, default!, null, null, null) { }

        public ChargeItem(string name, string image, double amount, double quantity,
                          string? nftUri = null, string? nftContractAddress = null,
                          SupportedChains? nftChain = null)
        {
            this.name = name;
            this.image = image;
            this.amount = amount;
            this.quantity = quantity;
            this.nftUri = nftUri;
            this.nftContractAddress = nftContractAddress;
            this.nftChain = nftChain;
        }

        public string name;
        public string image;
        public double amount;
        public double quantity;

        // Optional
        public string? nftUri;
        public string? nftContractAddress;
        public SupportedChains? nftChain;
    }

    [Serializable]
    public class ChargeReqBody
    {
        public ChargeReqBody() : this(default!, default!, default!, default!, default!, default!, 0.0, null) { }

        public ChargeReqBody(string tokenAddress, string symbol, List<ChargeItem> items,
                             SupportedChains chain, string successUrl, string cancelUrl,
                             double amount = 0.0, string? toAddress = null)
        {
            this.tokenAddress = tokenAddress;
            this.symbol = symbol;
            this.items = items ?? new List<ChargeItem>(); // Ensure items is not null
            this.chain = chain;
            this.successUrl = successUrl;
            this.cancelUrl = cancelUrl;
            this.amount = amount;
            this.toAddress = toAddress;
        }

        public string tokenAddress;
        public string symbol;
        public List<ChargeItem> items;
        public SupportedChains chain;
        public string successUrl;
        public string cancelUrl;

        // Optional
        public double amount;
        public string? toAddress;
    }
#nullable disable

    [Serializable]
    public class ChargeResponse
    {
        public string paymentUrl;
        public string chargeId;

        public override string ToString()
        {
            return $"Charge Response: \nPayment Url: {paymentUrl}\nCharge Id: {chargeId}";
        }
    }

    [Serializable]
    public class PayResponse
    {
        public TxStatus status;

        // TODO I have no idea what type this is, server code is not clear
        // public Route route;

        public override string ToString()
        {
            return $"Pay Response: \nTx Status: {status}";
        }
    }

    [Serializable]
    public class Transaction
    {
        public Transaction() { }

        public Transaction(string toAddress, SupportedChains chain, string symbol, double amount, string tokenAddress)
        {
            this.toAddress = toAddress;
            this.chain = chain;
            this.symbol = symbol;
            this.amount = amount;
            this.tokenAddress = tokenAddress;
        }

        public string toAddress;
        public SupportedChains chain;
        public string symbol;
        public double amount;
        public string tokenAddress;
    }
}