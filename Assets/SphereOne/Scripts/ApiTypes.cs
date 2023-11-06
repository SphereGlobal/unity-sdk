using System;
using System.Collections.Generic;
using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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

    [JsonConverter(typeof(TolerantEnumConverter))]
    public enum RouteActionType
    {
        SWAP,
        TRANSFER,
        BRIDGE,
        Unknown
    }

    [JsonConverter(typeof(TolerantEnumConverter))]
    public enum BridgeServices
    {
        wormhole,
        lifi,
        imx,
        stealthex,
        squid,
        swft
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum TransferType
    {
        SYSTEM,
        ERC20,
        SPL,
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
        public bool isPinCodeSetup;
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
    public class UserBalance
    {
        public Balance[] balances;
        public string total;
    }

    [Serializable]
    public class TokenMetadata
    {
        public string symbol;
        public string name;
        public int decimals;
        public string address;
        public string logoURI;
        public SupportedChains chain;

        public override string ToString()
        {
            return $"symbol: {symbol}\nname: {name}\ndecimals: {decimals}\naddress: {address}\nlogoURI: {logoURI}\nchain: {chain}";
        }
    }

    [Serializable]
    public class Nft
    {
        public string img;
        public string name;
        public string address;
        public string tokenType;
    }

    [Serializable]
    public class ChargeItem
    {
        public ChargeItem() { }

        public ChargeItem(string name, string image, double amount, double quantity, string nftUri = "", string nftContractAddress = "", SupportedChains nftChain = SupportedChains.Unknown)
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
        public string nftUri;
        public string nftContractAddress;
        public SupportedChains nftChain;
    }

    [Serializable]
    public class ChargeReqBody
    {
        public ChargeReqBody() { }

        public ChargeReqBody(string tokenAddress, string symbol, List<ChargeItem> items, SupportedChains chain, string successUrl, string cancelUrl, double amount = 0.0, string toAddress = null)
        {
            this.tokenAddress = tokenAddress;
            this.symbol = symbol;
            this.items = items;
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
        public string toAddress;
    }

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
    public class ChainWallets : Dictionary<SupportedChains, Wallet>
    {}

    [Serializable]
    public class BridgeQuote
    {
        public object rawQuote; // 'any' in TypeScript is similar to 'object' in C#
        public BridgeServices service;
        public SupportedChains fromChain;
        public BigInteger fromAmount; // Assuming BigNumber maps to BigInteger
        public string fromAddress;
        public TokenMetadata fromToken;
        public SupportedChains toChain;
        public BigInteger toAmount; // Assuming BigNumber maps to BigInteger
        public string toAddress;
        public TokenMetadata toToken;
        public double estimatedTime; // Assuming number maps to double
        public double estimatedCostUSD; // Assuming number maps to double
        public BigInteger estimatedEthGas; // Assuming BigNumber maps to BigInteger
        public BigInteger estimatedMatGas; // Assuming BigNumber maps to BigInteger
        public BigInteger estimatedAvaxGas; // Assuming BigNumber maps to BigInteger
        public BigInteger estimatedArbGas; // Assuming BigNumber maps to BigInteger
        public BigInteger estimatedBscGas; // Assuming BigNumber maps to BigInteger
        public BigInteger estimatedSolGas; // Assuming BigNumber maps to BigInteger
        public BigInteger estimatedOptGas; // Assuming BigNumber maps to BigInteger
        public BigInteger estimatedEosEvmGas; // Assuming BigNumber maps to BigInteger
        public BigInteger estimatedBaseGas; // Assuming BigNumber maps to BigInteger
        public string bridgeId; // Can be null due to the '' in TypeScript
        public string depositAddress; // Can be null due to the '' in TypeScript
    }

    [Serializable]
    public class BridgeProps
    {
        public BridgeQuote quote;
        public ChainWallets wallets;
        public double userSponsoredGas;
    }

    [Serializable]
    public class BridgeResponse
    {
        public BridgeResponseData data;
        public string error;
    }

    [Serializable]
    public class BridgeResponseData
    {
        public BridgeQuote quote;
        public ChainWallets wallets;
        public string bridgeTx; // for wormhole this refers to send to bridge tx
        public string redeemTx; // wormhole only
        public string approveTx; // lifi only. Sometimes allowance is already there so no need for this
        public string postVaaTx;
        public string userOperationHash; // Only with Smart Wallets to poll TxHash later
        public TxStatus bridgeStatus;
        public TxStatus redeemStatus; // (wormhole only) when using lifi true by default
        public TxStatus approveStatus; // (lifi only) when using wormhole true by default
        public TxStatus status;
        public string sequence; // wormhole only
        public string emitterAddress; // wormhole only
        public string bridgeId; // stealthex/swft only
        public bool sponsoredFee; // We can sponsor or not with Smart Wallets
    }

    [Serializable]
    public class SwapData
    {
        public SupportedChains fromChain;
        public BigInteger fromAmount;
        public TokenMetadata fromToken;
        public string fromAddress;
        public string fromPrivateKey;
        public BigInteger toAmount;
        public TokenMetadata toToken;
        public BigInteger estimatedGas;
    }

    [Serializable]
    public class SwapResponse
    {
        public SupportedChains fromChain;
        public string fromPrivateKey;
        public string fromAddress;
        public TokenMetadata fromToken;
        public TokenMetadata toToken;
        public BigInteger toAmount;
        public BigInteger fromAmount;
        public string userOperationHash; // Only valid for smart wallets (is used for retrieving the Tx Hash after bundlers execution)
        public string approveTxHash; // always null for solana
        public string swapTxHash;
        public TxStatus approveStatus;
        public TxStatus swapStatus;
        public TxStatus status; // success when approve and status are done
        public bool sponsoredFee;
    }

    [Serializable]
    public class TransferData
    {
        public SupportedChains fromChain;
        public BigInteger fromAmount; // Assuming BigNumber maps to BigInteger
        public string fromAddress;
        public string fromPrivateKey;
        public TokenMetadata fromToken;
        public string toAddress;
        public bool smartWallet; // Nullable because of the '' in TypeScript indicating it's optional
        public string starkPrivateKey; // Can be null due to the '' in TypeScript
    }

    [Serializable]
    public class TransferResponseData
    {
        public SupportedChains fromChain;
        public BigInteger fromAmount; // Assuming BigNumber maps to BigInteger
        public string fromAddress;
        public string fromTokenAddress;
        public string toAddress;
        public string hash; // Can be null
        public string userOperationHash; // Can be null
        public string fromPrivateKey;
        public TransferType transferType;
        public TxStatus status;
        public BigInteger fee; // Nullable, assuming BigNumber maps to BigInteger
        public object rawRecipient; // 'any' in TypeScript is similar to 'object' in C#
        public bool sponsoredFee;
    }

    [Serializable]
    public class TransferResponse
    {
        public TransferResponseData data;
        public string error;
    }

    [Serializable]
    public class Estimate
    {
        public int time; // minutes
        public double costUsd;
        public BigInteger ethGas;
        public BigInteger maticGas;
        public BigInteger optGas;
        public BigInteger avaxGas;
        public BigInteger arbGas;
        public BigInteger bscGas;
        public BigInteger solGas;
        public BigInteger eosEvmGas;
        public BigInteger baseGas;
    }

    [Serializable]
    public class RouteAction
    {
        public RouteActionType type;
        public TxStatus status;
        public Estimate estimate;
        public SwapData swapData;
        public SwapResponse swapResponse;
        public TransferData transferData;
        public TransferResponse transferResponse;
        public BridgeProps bridgeData;
        public BridgeResponse bridgeResponse;
    }

    [Serializable]
    public class RouteBatch
    {
        public string description;
        public TxStatus status;
        public RouteAction[] actions;
        public Balance[] afterBalances;
        public Estimate estimate;
    }

    [Serializable]
    public class Route
    {
        public string id;
        public string toChain;
        public string toAmount;
        public string toAddress;
        public TokenMetadata toToken;
        public Estimate estimate;
        public Balance[] fromBalances;
        public TxStatus status;
        public RouteBatch[] batches;
        public string fromUid;
    }

    [Serializable]
    public class PayResponse
    {
        public TxStatus status;
        public Route route;
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

    [Serializable]
    public class TransactionsResponse
    {
        public string data; // this data comes as a JWT
        public string error; // can be null
    }

    [Serializable]
    public class GenericErrorPayload
    {
        public string code;
        public string message;
    }

    [Serializable]
    public class OnRampResponse {
        public TxStatus status;
        public string onrampLink;
    }

    [Serializable]
    public class RouteResponse {
        public TxStatus status;
        public Route route;
    }

    [Serializable]
    public class PayErrorResponse
    {
        public GenericErrorPayload error;
        public string data; // null
    }

    [Serializable]
    public class PayResponseOnRampLink {
        public GenericErrorPayload error;
        public OnRampResponse data;
    }

    [Serializable]
    public class PayResponseRouteCreated
    {
        public GenericErrorPayload error;
        public RouteResponse data;
    }

    public class PayException : Exception
    {
        public string name { get; } // Immutable after construction
        public string onrampLink { get; } // Immutable after construction
        // Constructors in C# can't have named arguments by default, so we use a normal constructor with an optional parameter.

        // Calls the base class constructor with the "message" argument
        public PayException(string message, string onrampLink = null): base(message)
        {
            this.name = "Pay Error";
            this.onrampLink = onrampLink;
        }
    }

    public class GenericErrorCodeResponse
    {
        public string code;
        public string message;
    }

    public class PayRouteEstimateResponse
    {
       public PayRouteEstimate data;
       public GenericErrorCodeResponse error;
    }

    public class PayRouteEstimate
    {
        public string txId; // transactionId
        public TxStatus status; // TxStatus
        public decimal total; // total amount
        public PayRouteTotalEstimation estimation;
        public PayRouteDestinationEstimate to;
        public long startTimestamp; // timestamp
        public long limitTimestamp; // timestamp

        public override string ToString()
        {
            return $"txId: {txId}\nstatus: {status}\ntotal: {total}\nestimation: {estimation.ToString()}\nto: {to}\nstartTimestamp: {startTimestamp}\nlimitTimestamp: {limitTimestamp}";
        }
    }

    public class PayRouteTotalEstimation
    {
        public decimal costUsd; // cost in USD
        public int timeEstimate; // in minutes
        public string gas; // gas for the transaction
        public string route; // the route batches

        public override string ToString()
        {
            return $"costUsd: {costUsd}\ntimeEstimate: {timeEstimate}\ngas: {gas}\nroute: {route}";
        }
    }

    public class PayRouteDestinationEstimate
    {
        public string toAmount; // amount to be received
        public string toAddress; // receiver address
        public string toChain; // destination chain
        public TokenMetadata toToken; // extra token metadata

        public override string ToString()
        {
            return $"toAmount: {toAmount}\ntoAddress: {toAddress}\ntoChain: {toChain}\ntoToken: {toToken.ToString()}";
        }
    }

    public class PinCodeSetupResponse
    {
        public string data;
        public string error;
    }
}