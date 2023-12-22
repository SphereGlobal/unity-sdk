using System;
using System.Collections.Generic;
using System.Linq;
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
        KLAYTN,
        DFK,
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

    [JsonConverter(typeof(StringEnumConverter))]
    public enum BatchType
    {
        TRANSFER,
        SWAP,
        BRIDGE
    }

    public struct PincodeTargets
    {
        public const string AddWallet = "ADD_WALLET";
        public const string SendNft = "SEND_NFT";
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
    public class ChainWallets : Dictionary<SupportedChains, Wallet>
    { }

    [Serializable]
    public class BridgeQuote
    {
        public object rawQuote; // 'any' in TypeScript is similar to 'object' in C#
        public BridgeServices service;
        public SupportedChains fromChain;
        public BigNumberObj fromAmount; // Assuming BigNumber maps to BigInteger
        public string fromAddress;
        public TokenMetadata fromToken;
        public SupportedChains toChain;
        public BigNumberObj toAmount; // Assuming BigNumber maps to BigInteger
        public string toAddress;
        public TokenMetadata toToken;
        public double estimatedTime; // Assuming number maps to double
        public double estimatedCostUSD; // Assuming number maps to double
        public BigNumberObj estimatedEthGas; // Assuming BigNumber maps to BigInteger
        public BigNumberObj estimatedMatGas; // Assuming BigNumber maps to BigInteger
        public BigNumberObj estimatedAvaxGas; // Assuming BigNumber maps to BigInteger
        public BigNumberObj estimatedArbGas; // Assuming BigNumber maps to BigInteger
        public BigNumberObj estimatedBscGas; // Assuming BigNumber maps to BigInteger
        public BigNumberObj estimatedSolGas; // Assuming BigNumber maps to BigInteger
        public BigNumberObj estimatedOptGas; // Assuming BigNumber maps to BigInteger
        public BigNumberObj estimatedEosEvmGas; // Assuming BigNumber maps to BigInteger
        public BigNumberObj estimatedBaseGas; // Assuming BigNumber maps to BigInteger
        public BigNumberObj estimatedKlaytnGas; // Assuming BigNumber maps to BigInteger
        public BigNumberObj estimatedDfkGas; // Assuming BigNumber maps to BigInteger
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
        public BigNumberObj fromAmount;
        public TokenMetadata fromToken;
        public string fromAddress;
        public string fromPrivateKey;
        public BigNumberObj toAmount;
        public TokenMetadata toToken;
        public BigNumberObj estimatedGas;
    }

    [Serializable]
    public class SwapResponse
    {
        public SupportedChains fromChain;
        public string fromPrivateKey;
        public string fromAddress;
        public TokenMetadata fromToken;
        public TokenMetadata toToken;
        public BigNumberObj toAmount;
        public BigNumberObj fromAmount;
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
        public BigNumberObj fromAmount; // Assuming BigNumber maps to BigInteger
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
        public BigNumberObj fromAmount; // Assuming BigNumber maps to BigInteger
        public string fromAddress;
        public string fromTokenAddress;
        public string toAddress;
        public string hash; // Can be null
        public string userOperationHash; // Can be null
        public string fromPrivateKey;
        public TransferType transferType;
        public TxStatus status;
        public BigNumberObj fee; // Nullable, assuming BigNumber maps to BigInteger
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
        public BigNumberObj ethGas;
        public BigNumberObj maticGas;
        public BigNumberObj optGas;
        public BigNumberObj avaxGas;
        public BigNumberObj arbGas;
        public BigNumberObj bscGas;
        public BigNumberObj solGas;
        public BigNumberObj eosEvmGas;
        public BigNumberObj baseGas;
        public BigNumberObj klaytnGas;
        public BigNumberObj dfkGas;
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

        public Transaction(string toAddress, SupportedChains chain, string symbol,
                            double amount, string tokenAddress)
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
    public class OnRampResponse
    {
        public TxStatus status;
        public string onrampLink;
    }

    [Serializable]
    public class RouteResponse
    {
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
    public class PayResponseOnRampLink
    {
        public GenericErrorPayload error;
        public OnRampResponse data;
    }

    [Serializable]
    public class OnRampErrorFormatResponse : PayResponseOnRampLink
    {
    }

    [Serializable]
    public class PayResponseRouteCreated
    {
        public GenericErrorPayload error;
        public RouteResponse data;
    }

    [Serializable]
    public class PayException : Exception
    {
        public string name { get; } // Immutable after construction
        public string onrampLink { get; } // Immutable after construction
        // Constructors in C# can't have named arguments by default, so we use a normal constructor with an optional parameter.

        // Calls the base class constructor with the "message" argument
        public PayException(string message, string onrampLink = null) : base(message)
        {
            this.name = "Pay Error";
            this.onrampLink = onrampLink;
        }
    }

    [Serializable]
    public class GenericErrorCodeResponse
    {
        public string code;
        public string message;
    }

    [Serializable]
    public class PayRouteEstimateResponse
    {
        public PayRouteEstimate data { get; set; }
        public GenericErrorCodeResponse error { get; set; }
    }

    [Serializable]
    public class PayRouteEstimate
    {
        public string txId { get; set; } // transactionId
        public TxStatus status { get; set; } // TxStatus
        public decimal total { get; set; } // total amount initially received, not including other costs
        public decimal totalUsd { get; set; } // total amount initially received, in USD, not including other costs
        public PayRouteTotalEstimation estimation { get; set; }
        public PayRouteDestinationEstimate to { get; set; }
        public long startTimestamp { get; set; } // timestamp
        public long limitTimestamp { get; set; } // timestamp

        public PayRouteEstimate() { }

        public PayRouteEstimate(string txId, TxStatus status, decimal total, decimal totalUsd, PayRouteTotalEstimation estimation,
            PayRouteDestinationEstimate to, long startTimestamp, long limitTimestamp)
        {
            this.txId = txId;
            this.status = status;
            this.total = total;
            this.totalUsd = totalUsd;
            this.estimation = estimation;
            this.to = to;
            this.startTimestamp = startTimestamp;
            this.limitTimestamp = limitTimestamp;
        }

        public PayRouteEstimate(PayRouteEstimate newData)
        {
            this.txId = newData.txId;
            this.status = newData.status;
            this.total = newData.total;
            this.totalUsd = newData.totalUsd;
            this.estimation = newData.estimation;
            this.to = newData.to;
            this.startTimestamp = newData.startTimestamp;
            this.limitTimestamp = newData.limitTimestamp;
        }

        public override string ToString()
        {
            return $"txId: {txId}\nstatus: {status}\ntotal: {total}\nestimation: {estimation.ToString()}\nto: {to}\nstartTimestamp: {startTimestamp}\nlimitTimestamp: {limitTimestamp}";
        }
    }

    [Serializable]
    public class PayRouteTotalEstimation
    {
        public decimal costUsd { get; set; } // cost in USD
        public int timeEstimate { get; set; } // in minutes
        public string gas { get; set; } // gas for the transaction
        public string route; // the route batches

        public FormattedBatch[] routeParsed { get; set; } // the route batches that will be executed

        public PayRouteTotalEstimation() { }

        public PayRouteTotalEstimation(decimal costUsd, int timeEstimate, string gas, string route, FormattedBatch[] routeParsed = null)
        {
            this.costUsd = costUsd;
            this.timeEstimate = timeEstimate;
            this.gas = gas;
            this.route = route;
            this.routeParsed = routeParsed;
        }

        public PayRouteTotalEstimation(PayRouteTotalEstimation newData)
        {
            this.costUsd = newData.costUsd;
            this.timeEstimate = newData.timeEstimate;
            this.gas = newData.gas;
            this.route = newData.route;
            this.routeParsed = newData.routeParsed;
        }

        public override string ToString()
        {
            var routeParsedContent = routeParsed == null || routeParsed.Length == 0
                 ? ""
                 : string.Join("\n", routeParsed.Select(batch => batch.ToString()));

            return $"costUsd: {costUsd}\ntimeEstimate: {timeEstimate}\ngas: {gas}\nroute: {routeParsedContent}";
        }
    }

    [Serializable]
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

    [Serializable]
    public class PinCodeSetupResponse
    {
        public string data;
        public string error;
    }

    [Serializable]
    public class FormattedBatch
    {
        public BatchType type { get; set; }
        public string title { get; set; }
        public string[] operations { get; set; }

        public FormattedBatch() { }

        public FormattedBatch(BatchType type, string title, string[] operations)
        {
            this.type = type;
            this.title = title;
            this.operations = operations;
        }

        public FormattedBatch(FormattedBatch newData)
        {
            this.type = newData.type;
            this.title = newData.title;
            this.operations = newData.operations;
        }

        public override string ToString()
        {
            // Check if the operations array is null or empty
            var operationsContent = operations == null || operations.Length == 0
                ? ""
                : string.Join("\n- ", operations);
            return $"type: {type}\ntitle: {title}\noperations: {operationsContent}";
        }
    }

    public class FormattedBatchRender
    {
        public BatchType type { get; set; }
        public string title { get; set; }
        public List<string> operations { get; set; }
    }

    [Serializable]
    public class ParsedRoute
    {
        public string id;
        public List<RouteBatch> batches;
        public string status;
        public TokenMetadata toToken;
        public string toAddress;
        public string toChain;
        public string toAmount;
        public Estimate estimate;
        public string fromUid;
    }

    public class HandleCallback
    {
        public Action<object[]> SuccessCallback { get; set; }
        public Action<object[]> FailCallback { get; set; }
        public Action<object[]> CancelCallback { get; set; }
    }

    public class PayError : Exception
    {
        public string onrampLink { get; private set; }

        public PayError(string message, string onrampLink = null) : base(message)
        {
            this.onrampLink = onrampLink;
        }

        // The 'Name' property is provided by the base 'Exception' class, so it's not necessary to declare it here.
        // In C#, the 'Name' property of an exception is typically 'GetType().Name', which would return "PayError" for this class.
    }

    public class RouteEstimateError : Exception
    {
        public string onrampLink { get; private set; }

        public RouteEstimateError(string message, string onrampLink = null) : base(message)
        {
            this.onrampLink = onrampLink;
        }
    }

    [Serializable]
    public class BigNumberObj
    {
        public string hex { get; set; }
        public string type { get; set; }
    }

    [Serializable]
    public class TokenizedShare
    {
        public string DEK { get; set; }
        public string error { get; set; }
    }

    [Serializable]
    public class NftDataParams
    {
        public string fromAddress { get; set; }
        public string toAddress { get; set; }
        public SupportedChains chain { get; set; }
        public string nftTokenAddress { get; set; }
        public string tokenId { get; set; }
        public string reason { get; set; }
    }
}