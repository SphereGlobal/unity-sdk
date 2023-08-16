using System;
using System.Numerics;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using System.Threading.Tasks;
using JetBrains.Annotations;

// TODO
// - Slideout doesnt show when fullscreen
// - Finish Unity logout -> iframe logout (needs frontend refactor)
// - Refactor SphereOneWindowManager, break into multiple classes and prefabs (collectables gallery, wallets, balances)
// - Replace Newtonsoft.Json with Unity built in
// - Popup Auth mode: auto refresh token when expired

namespace SphereOne
{
    [RequireComponent(typeof(MockApiDataFactory))]
    public class SphereOneManager : MonoBehaviour
    {
#if UNITY_WEBGL
        [DllImport("__Internal")]
        static extern string OpenWindow(string url);

        [DllImport("__Internal")]
        static extern void SendLogoutMsg();

        [DllImport("__Internal")]
        static extern void CreateSlideout(string src, string backgroundFilter);

        [DllImport("__Internal")]
        static extern void ToggleSphereOneSlideout();

        [DllImport("__Internal")]
        static extern void RequestCredentialFromSlideout();
#endif

        public static SphereOneManager Instance { get; set; }

        public delegate void OnUserLoaded(User user);
        public static OnUserLoaded onUserLoaded;

        public delegate void OnUserLogout();
        public static OnUserLogout onUserLogout;

        public delegate void OnUserWalletsLoaded(List<Wallet> wallets);
        public static OnUserWalletsLoaded onUserWalletsLoaded;

        public delegate void OnUserBalancesLoaded(List<Balance> balances);
        public static OnUserBalancesLoaded onUserBalancesLoaded;

        public delegate void OnUserNftsLoaded(List<Nft> nfts);
        public static OnUserNftsLoaded onUserNftsLoaded;

        const string LOCAL_STORAGE_CREDENTIALS = "sphere_one_credentials";
        const string LOCAL_STORAGE_STATE = "sphere_one_state";

        const string DOMAIN = "https://relaxed-kirch-zjpimqs5qe.projects.oryapis.com";
        const string AUDIENCE = "https://relaxed-kirch-zjpimqs5qe.projects.oryapis.com";
        const string IFRAME_URL = "https://wallet.sphereone.xyz";

        [SerializeField] Environment _environment = Environment.PRODUCTION;

        [Tooltip("The mode of authentication used")]
        [SerializeField] LoginBehavior _loginMode = LoginBehavior.SLIDEOUT;

        [Tooltip("Enable/Disable SphereOneSDK event logging")]
        [SerializeField] bool _enableLogging = true;

        [Tooltip("Filter the background when the slideout is open")]
        [SerializeField] BackgroundFilter _backgroundFilter = BackgroundFilter.DARKEN;

        // localhost: http://127.0.0.1:5001/sphereone-testing/us-central1/api
        [SerializeField] string _sphereOneApiUrl = "https://api-olgsdff53q-uc.a.run.app";
        [SerializeField] string _clientId;

        [Tooltip("The URL of your game. This is where the Auth Provider will redirect back to.")]
        [SerializeField] string _redirectUrl;
        [SerializeField] string _apiKey;

        public User User;
        public List<Wallet> Wallets;
        public List<Balance> Balances;
        public List<Nft> Nfts;

        // divide by 1000000 to get dollar amount
        public BigInteger TotalBalance;

        public bool IsAuthenticated
        {
            get
            {
                if (_credentials != null && JwtUtils.IsTokenValid(_credentials.access_token)) return true;

                return false;
            }
        }

        public LoginBehavior LoginMode { get { return _loginMode; } }
        public Environment Environment { get { return _environment; } }

        Credentials _credentials;
        string _wrappedDek;
        Dictionary<string, string> _headers;
        bool _forceRefreshCache = true;

        SphereOneLogger _logger;

        void Awake()
        {
            ValidateConfiguration();

            _logger = new SphereOneLogger(_enableLogging);

            _headers = new Dictionary<string, string>();
            Wallets = new List<Wallet>();
            Balances = new List<Balance>();
            Nfts = new List<Nft>();

            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                _logger.LogWarning("Two SphereOneManager instances were found, removing this one.");
                Destroy(gameObject);
                return;
            }

            if (_loginMode == LoginBehavior.SLIDEOUT)
            {
                if (!Application.isEditor)
                {
#if UNITY_WEBGL
                    CreateSlideout(IFRAME_URL, _backgroundFilter.ToString());
#endif
                }
            }
        }

        void Start()
        {
            if (_environment == Environment.EDITOR)
            {
                // Load Mock credentials
                var mockCredsJson = MockApiDataFactory.Instance.GetMockCredentials();
                var credentials = JsonConvert.DeserializeObject<Credentials>(mockCredsJson);
                LoadCredentials(credentials);
                return;
            }

            switch (_loginMode)
            {
                case LoginBehavior.POPUP:
                    TryLoadTokenFromLocalStorage();
                    break;

                case LoginBehavior.SLIDEOUT:
                    TryLoadTokenFromSlideout();
                    break;

                default:
                    _logger.LogError($"LoginMode: {_loginMode} not implemented");
                    break;
            }
        }

        // Any function prefixed with CALLBACK is a function that will be called 
        //    by javascript code using window.unityInstance.SendMessage
        // These functions cannot be renamed without first updating sphereone.jslib and/or bridge.js

        // Do not rename this function without updating sphereone.jslib and/or bridge.js
        void CALLBACK_CredentialFromSlideout(string credentialsJson)
        {
            if (_loginMode != LoginBehavior.SLIDEOUT) return;

            if (IsAuthenticated) return;

            if (_enableLogging)
                _logger.Log($"Received token from iframe.");

            var credentials = JsonConvert.DeserializeObject<Credentials>(credentialsJson);

            LoadCredentials(credentials);
        }

        // Do not rename this function without updating sphereone.jslib and/or bridge.js
        void CALLBACK_PopupLoginError(string callbackUrl)
        {
            if (_loginMode != LoginBehavior.POPUP) return;

            // TODO handle this error
            _logger.LogError($"Login Failed: {callbackUrl}");
        }

        // Do not rename this function without updating sphereone.jslib and/or bridge.js
        async void CALLBACK_PopupLoginSuccess(string callbackUrl)
        {
            if (_loginMode != LoginBehavior.POPUP) return;

            if (IsAuthenticated) return;

            if (!callbackUrl.Contains("code="))
                return;

            if (!callbackUrl.Contains("state="))
                return;

            var code = callbackUrl.Split("code=")[1].Split("&state=")[0];
            var state = callbackUrl.Split("&state=")[1];

            var savedState = SPrefs.GetString(LOCAL_STORAGE_STATE);

            if (state != savedState)
            {
                // This could be a CSRF attack
                // https://auth0.com/docs/secure/attack-protection/state-parameters#csrf-attacks
                _logger.LogError($"State mismatch: {state} != {savedState}. Cannot login. Possible CSRF attack.");
                return;
            }

            var authUrl = $"{_sphereOneApiUrl}/auth";
            authUrl += $"?code={code}";
            authUrl += $"&clientId={_clientId}";
            authUrl += $"&redirectUri={_redirectUrl}";
            authUrl += $"&state={state}";

            var res = await WebRequestHandler.Get(authUrl, null);

            if (res == WebRequestHandler.REQUEST_ERR)
                return;

            var credentials = JsonConvert.DeserializeObject<CredentialsWrapper>(res).data;

            LoadCredentials(credentials);
        }

        // Do not rename this function without updating sphereone.jslib and/or bridge.js
        void CALLBACK_Logout()
        {
            Logout();
        }

        public void Login()
        {
            if (IsAuthenticated)
            {
                _logger.LogWarning("Trying to login, however the user is already logged in.");
                return;
            }

            switch (_loginMode)
            {
                case LoginBehavior.POPUP:
                    OpenPopupWindow();
                    break;

                case LoginBehavior.SLIDEOUT:
                    ToggleSlideout();
                    break;

                default:
                    _logger.LogError($"LoginMode: {_loginMode} not implemented");
                    break;
            }
        }

        async void OpenPopupWindow()
        {
            if (_loginMode != LoginBehavior.POPUP) return;

            if (IsAuthenticated) return;

            // Get configuration
            string configUrl = $"{DOMAIN}/.well-known/openid-configuration";
            var res = await WebRequestHandler.Get(configUrl, null);

            if (res == WebRequestHandler.REQUEST_ERR)
                return;

            var openIdConfig = JsonConvert.DeserializeObject<OpenIdConfiguration>(res);

            // Generate secure random state
            var state = SphereOneUtils.SecureRandomString(24, true);
            SPrefs.SetString(LOCAL_STORAGE_STATE, state);

            var url = $"{openIdConfig.authorization_endpoint}?response_type=code&client_id={_clientId}&state={state}&redirect_uri={_redirectUrl}&audience={AUDIENCE}&scope=openid%20profile%20email%20offline_access";

            if (!Application.isEditor)
            {
#if UNITY_WEBGL
                OpenWindow(url);
#endif
            }
        }

        public void Logout()
        {
            if (_loginMode == LoginBehavior.SLIDEOUT)
            {
                // TODO - frontend (app) needs a logout refactor first
                // See app -> RootNavigator.tsx
                _logger.LogError("Logout currently not implemented for SLIDEOUT mode.");
                return;
            }

            ClearUserData();

            if (!Application.isEditor)
            {
#if UNITY_WEBGL
                if (_loginMode == LoginBehavior.SLIDEOUT)
                    SendLogoutMsg();
#endif
            }

            _logger.Log("User logged out. Local cookies cleared.");

            onUserLogout?.Invoke();
        }

        public void ToggleSlideout()
        {
            if (_loginMode != LoginBehavior.SLIDEOUT) return;

            if (!Application.isEditor)
            {
#if UNITY_WEBGL
                ToggleSphereOneSlideout();
#endif
            }
        }

        void ClearUserData()
        {
            _credentials = null;
            _wrappedDek = null;
            User = null;

            Wallets.Clear();
            Balances.Clear();
            Nfts.Clear();
            _headers.Clear();

            SPrefs.SetString(LOCAL_STORAGE_CREDENTIALS, null);
            SPrefs.SetString(LOCAL_STORAGE_STATE, null);
        }

        void TryLoadTokenFromLocalStorage()
        {
            // Only store credentials locally in popup mode
            if (_loginMode != LoginBehavior.POPUP) return;

            var savedCredentialsJson = SPrefs.GetString(LOCAL_STORAGE_CREDENTIALS);
            var savedCredentials = JsonConvert.DeserializeObject<Credentials>(savedCredentialsJson);

            LoadCredentials(savedCredentials);
        }

        void LoadCredentials(Credentials credentials)
        {
            if (IsAuthenticated) return;

            if (credentials == null)
                return;

            _credentials = credentials;

            _logger.Log($"User authenticated.");

            // Save token to local storage
            if (_loginMode == LoginBehavior.POPUP)
            {
                string credentialsJson = JsonConvert.SerializeObject(_credentials);
                SPrefs.SetString(LOCAL_STORAGE_CREDENTIALS, credentialsJson);
            }

            SetupAuthHeader();
            FetchAllData();
        }

        void TryLoadTokenFromSlideout()
        {
            if (_loginMode != LoginBehavior.SLIDEOUT) return;

            if (!Application.isEditor)
            {
#if UNITY_WEBGL
                RequestCredentialFromSlideout();
#endif
            }
        }

        void FetchAllData()
        {
            FetchUserWallets();
            FetchUserInfo();
            FetchUserNfts();
            FetchUserBalances();
        }

        async Task<string> GetWrappedDek()
        {
            if (_wrappedDek != null)
                return _wrappedDek;

            string url = $"{_sphereOneApiUrl}/createOrRecoverAccount";
            var res = await WebRequestHandler.Post(url, null, _headers);

            if (res == WebRequestHandler.REQUEST_ERR)
                return null;

            _wrappedDek = JsonConvert.DeserializeObject<WrappedDekWrapper>(res).data;

            return _wrappedDek;
        }

        // API functions
        async public Task<User> FetchUserInfo()
        {
            if (_environment == Environment.EDITOR)
            {
                var usrJson = MockApiDataFactory.Instance.GetMockUser();
                return LoadUser(usrJson);
            }

            CheckJwtExpiration();

            string url = $"{_sphereOneApiUrl}/user";
            var res = await WebRequestHandler.Get(url, _headers);

            if (res == WebRequestHandler.REQUEST_ERR)
                return null;

            return LoadUser(res);
        }

        User LoadUser(string json)
        {
            User = JsonConvert.DeserializeObject<UserWrapper>(json).data;

            _logger.Log($"User loaded: {User.name}");

            onUserLoaded?.Invoke(User);

            return User;
        }

        async public Task<List<Wallet>> FetchUserWallets()
        {
            if (_environment == Environment.EDITOR)
            {
                return LoadWallets(MockApiDataFactory.Instance.GetMockWallets()); ;
            }

            CheckJwtExpiration();

            string url = $"{_sphereOneApiUrl}/user/wallets";
            var res = await WebRequestHandler.Get(url, _headers);

            if (res == WebRequestHandler.REQUEST_ERR)
                return null;

            return LoadWallets(res);
        }

        List<Wallet> LoadWallets(string json)
        {
            Wallets = JsonConvert.DeserializeObject<WalletWrapper>(json).data;

            foreach (var w in Wallets)
            {
                _logger.Log($"Wallet loaded: {w.address}");
            }

            onUserWalletsLoaded?.Invoke(Wallets);

            return Wallets;
        }

        async public Task<List<Balance>> FetchUserBalances()
        {
            if (_environment == Environment.EDITOR)
            {
                return LoadBalances(MockApiDataFactory.Instance.GetMockBalances()); ;
            }

            CheckJwtExpiration();

            string url = $"{_sphereOneApiUrl}/getFundsAvailable?refreshCache={_forceRefreshCache.ToString().ToLower()}";
            var res = await WebRequestHandler.Get(url, _headers);

            if (res == WebRequestHandler.REQUEST_ERR)
                return null;

            return LoadBalances(res);
        }

        List<Balance> LoadBalances(string json)
        {
            var data = JsonConvert.DeserializeObject<BalancesWrapper>(json).data;
            Balances = data.balances;

            TotalBalance = BigInteger.Parse(data.total);

            // Only refresh cache on the first load
            _forceRefreshCache = false;

            onUserBalancesLoaded?.Invoke(Balances);

            return Balances;
        }

        async public Task<List<Nft>> FetchUserNfts()
        {
            if (_environment == Environment.EDITOR)
            {
                return LoadNfts(MockApiDataFactory.Instance.GetMockNfts()); ;
            }

            CheckJwtExpiration();

            string url = $"{_sphereOneApiUrl}/getNftsAvailable";
            var res = await WebRequestHandler.Get(url, _headers);

            if (res == WebRequestHandler.REQUEST_ERR)
                return null;

            return LoadNfts(res);
        }

        List<Nft> LoadNfts(string json)
        {
            Nfts = JsonConvert.DeserializeObject<NftWrapper>(json).data;

            foreach (var nft in Nfts)
            {
                _logger.Log($"NFT loaded: {nft.name}");
            }

            onUserNftsLoaded?.Invoke(Nfts);

            return Nfts;
        }

        /// <summary>
        /// Create a charge to be paid later
        /// </summary>
        /// <param name="chargeReq"></param>
        /// <param name="isTest">Not required. Determines if API Key is test or production. By default, it is false.</param>
        /// <returns>The ChargeResponse object or null.</returns>
        async public Task<ChargeResponse> CreateCharge(ChargeReqBody chargeReq, bool isTest = false)
        {
            if (_environment == Environment.EDITOR)
            {
                // TODO fake charge mock data
                return null;
            }

            CheckJwtExpiration();

            var body = new CreateChargeReqBodyWrapper(chargeReq, isTest);
            var bodySerialized = JsonConvert.SerializeObject(body);

            string url = $"{_sphereOneApiUrl}/createCharge";
            var res = await WebRequestHandler.Post(url, bodySerialized, _headers);

            if (res == WebRequestHandler.REQUEST_ERR)
                return null;

            var chargeResponse = JsonConvert.DeserializeObject<CreateChargeResponseWrapper>(res).data;

            return chargeResponse;
        }

        async public Task<PayResponse> PayCharge(string transactionId)
        {
            if (_environment == Environment.EDITOR)
            {
                // TODO fake charge mock data
                return null;
            }

            CheckJwtExpiration();

            var dek = await GetWrappedDek();

            if (dek == null)
                return null;

            var body = new PayChargeReqBody(dek, transactionId);
            var bodySerialized = JsonConvert.SerializeObject(body);

            string url = $"{_sphereOneApiUrl}/pay";
            var res = await WebRequestHandler.Post(url, bodySerialized, _headers);

            if (res == WebRequestHandler.REQUEST_ERR)
                return null;

            var payResponse = JsonConvert.DeserializeObject<PayResponseWrapper>(res).data;

            return payResponse;
        }

        async public Task<PayResponse> Pay(Transaction transaction)
        {
            if (_environment == Environment.EDITOR)
            {
                // TODO fake charge mock data
                return null;
            }

            CheckJwtExpiration();

            var dek = await GetWrappedDek();

            if (dek == null)
                return null;

            var bodySerialized = JsonConvert.SerializeObject(transaction);

            string url = $"{_sphereOneApiUrl}/pay";
            var res = await WebRequestHandler.Post(url, bodySerialized, _headers);

            if (res == WebRequestHandler.REQUEST_ERR)
                return null;

            var payResponse = JsonConvert.DeserializeObject<PayResponseWrapper>(res).data;

            return payResponse;
        }

        void SetupAuthHeader()
        {
            _headers.Clear();
            _headers.Add("Authorization", $"Bearer {_credentials.access_token}");
            _headers.Add("x-api-key", _apiKey);
            _headers.Add("sphere-one-source", "unity-sdk");
            _headers.Add("sphere-one-client-id", _clientId);
        }

        void CheckJwtExpiration()
        {
            if (JwtUtils.IsTokenExpired(_credentials.access_token))
            {
                // Token is out of date

                _logger.Log("JWT Token is out of date");

                RefreshToken();
            }
            else
            {
                // Token valid
            }
        }

        // TODO
        void RefreshToken()
        {

        }

        public void ValidateConfiguration()
        {
            string pre = "SphereOneSDK: Invalid Configuration. ";

            if (!Application.isEditor)
            {
                if (Application.platform != RuntimePlatform.WebGLPlayer)
                    throw new Exception(pre + "Only WebGL is currently supported.");
            }

            if (!Application.isEditor && _environment == Environment.EDITOR)
                throw new Exception(pre + "Environment EDITOR can only be used in the editor. You must switch to PRODUCTION before building.");

            if (string.IsNullOrEmpty(_sphereOneApiUrl))
                throw new Exception(pre + "Sphere One Api URL is required");

            if (!SphereOneUtils.IsUrlValid(_sphereOneApiUrl))
                throw new Exception(pre + "Sphere One Api URL invalid.");

            if (string.IsNullOrEmpty(_apiKey))
                throw new Exception(pre + "Api Key is required");

            if (_loginMode == LoginBehavior.POPUP)
            {
                // Validate Setup
                if (string.IsNullOrEmpty(_clientId))
                    throw new Exception(pre + "Client Id is required");

                if (string.IsNullOrEmpty(_redirectUrl))
                    throw new Exception(pre + "Redirect URL is required");

                if (!SphereOneUtils.IsUrlValid(_redirectUrl))
                    throw new Exception(pre + "Redirect URL invalid.");
            }
            else if (_loginMode == LoginBehavior.SLIDEOUT)
            {

            }
        }
    }

    [Serializable]
    class OpenIdConfiguration
    {
        public string issuer;
        public string authorization_endpoint;
        public string token_endpoint;
        // ...
    }

    [Serializable]
    abstract class ApiResponseWrapper
    {
        public string error;
    }

    [Serializable]
    class UserWrapper : ApiResponseWrapper
    {
        public User data;
    }

    [Serializable]
    class WalletWrapper : ApiResponseWrapper
    {
        public List<Wallet> data;
    }

    [Serializable]
    class BalancesWrapper : ApiResponseWrapper
    {
        public BalancesWithTotal data;
    }

    [Serializable]
    class BalancesWithTotal
    {
        public string total;
        public List<Balance> balances;
    }

    [Serializable]
    class NftWrapper : ApiResponseWrapper
    {
        public List<Nft> data;
    }

    [Serializable]
    class CredentialsWrapper : ApiResponseWrapper
    {
        public Credentials data;
    }

    [Serializable]
    class Credentials
    {
        public string access_token;
        public string refresh_token;
        public string id_token;
        public string scope;
        public string expires_in;
        public string token_type;
    }

    [Serializable]
    class WrappedDekWrapper : ApiResponseWrapper
    {
        public string data;
    }

    [Serializable]
    class PayChargeWrapper
    {
        public PayChargeWrapper(PayChargeReqBody data)
        {
            this.data = data;
        }

        public PayChargeReqBody data;
    }

    [Serializable]
    class PayChargeReqBody
    {
        public PayChargeReqBody(string wrappedDek, string transactionId)
        {
            this.wrappedDek = wrappedDek;
            this.transactionId = transactionId;
        }

        public string wrappedDek;
        public string transactionId;
    }

    [Serializable]
    class PayResponseWrapper : ApiResponseWrapper
    {
        public PayResponse data;
    }

    [Serializable]
    class CreateChargeReqBodyWrapper
    {
        public CreateChargeReqBodyWrapper(ChargeReqBody chargeData, bool isTest)
        {
            this.isTest = isTest;
            this.chargeData = chargeData;
        }

        public bool isTest;
        public ChargeReqBody chargeData;
    }

    [Serializable]
    class CreateChargeResponseWrapper : ApiResponseWrapper
    {
        public ChargeResponse data;
    }
}