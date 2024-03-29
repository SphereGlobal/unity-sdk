using System.Net.Mime;
using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Threading.Tasks;
using TMPro;
// Don't remove this! needed for WebGL DllImport
using System.Runtime.InteropServices;

// TODO
// - Slideout doesnt show when fullscreen
// - Finish Unity logout -> iframe logout
// - Refactor SphereOneWindowManager, break into multiple classes and prefabs (collectables gallery, wallets, balances)
// - Replace Newtonsoft.Json with Unity built in

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

        [DllImport("__Internal")]
        static extern void OpenPinCodePopup(string title, string url);
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

        const string DOMAIN = "https://auth.sphereone.xyz";

        const string AUDIENCE = "https://auth.sphereone.xyz";

        const string IFRAME_URL = "https://wallet.sphereone.xyz";

        const string PIN_CODE_URL = "https://pin.sphereone.xyz";

        [SerializeField] Environment _environment = Environment.PRODUCTION;

        [Tooltip("The mode of authentication used")]
        [SerializeField] LoginBehavior _loginMode = LoginBehavior.SLIDEOUT;

        [Tooltip("Enable/Disable SphereOneSDK event logging")]
        [SerializeField] bool _enableLogging = true;

        [Tooltip("Filter the background when the slideout is open")]
        [SerializeField] BackgroundFilter _backgroundFilter = BackgroundFilter.DARKEN;

        [SerializeField] string _sphereOneApiUrl = "https://api-olgsdff53q-uc.a.run.app";
        [SerializeField] string _clientId;

        [Tooltip("The URL of your game. This is where the Auth Provider will redirect back to.")]
        [SerializeField] string _redirectUrl;
        [Tooltip("Redirect Scheme Identifier. Must be unique for each game. If you change this, make sure to update AndroidManifest.xml in Assets/Plugins/Android")]
#pragma warning disable CS0414 // Suppress the warning.
        // This property is being set in the SphereOneManager Editor. And it is being used in `OpenPopupWindow` and `ValidateConfiguration`.
        [SerializeField] string _scheme = "sphereone";
#pragma warning restore CS0414
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

        OpenIdConfiguration _openIdConfig;
        Credentials _credentials;
        private string _wrappedDek;
        private long _wrappedDekExpiration;
        Dictionary<string, string> _headers;
        bool _forceRefreshCache = true;

        [SerializeField] TMP_Text _debugText;
        SphereOneLogger _logger;

        void Awake()
        {
            ValidateConfiguration();

            _logger = new SphereOneLogger(_enableLogging, _debugText);

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
            SetupAuthHeader();

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

            if (callbackUrl.Contains("data="))
            {
                //! NOTE: THIS IS SPECIFICALLY FOR PIN CODE, NOT FOR LOGIN. JUST REUSING THE CALLBACK.
                var data = callbackUrl.Split("data=")[1];
                var pinResponse = JsonConvert.DeserializeObject<PinCodeFormatResponse>(data).data;
                if (pinResponse.code.ToLower().Equals("dek"))
                {
                    _wrappedDek = pinResponse.share;
                    // exit early, we don't want to load the credentials
                    return;
                }
            }

            if (_loginMode != LoginBehavior.POPUP) return;

            if (IsAuthenticated) return;

            if (!callbackUrl.Contains("code="))
                return;

            if (!callbackUrl.Contains("state="))
                return;

            var code = callbackUrl.Split("code=")[1].Split("&state=")[0];
#if UNITY_IOS
            var iosState = callbackUrl.Split("&state=")[1];
            var state = iosState.Split("#")[0];
#elif UNITY_WEBGL || UNITY_ANDROID || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            var state = callbackUrl.Split("&state=")[1];
#endif

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

            WebRequestResponse res = await WebRequestHandler.Get(authUrl, null);

            if (!res.IsSuccess)
            {
                _logger.LogError($"Error fetching token: {res.Error}");
                return;
            }

            var credentials = JsonConvert.DeserializeObject<CredentialsWrapper>(res.Data).data;
            LoadCredentials(credentials);
        }

        // Do not rename this function without updating sphereone.jslib and/or bridge.js
        void CALLBACK_Logout()
        {
            Logout();
        }

        // Do not rename this function without updating sphereone.jslib and/or bridge.js
        /// <summary>
        /// This function is called by the PinCodePopup when the user has successfully entered their pin code.
        /// And it's only for WebGL build
        /// </summary>
        /// <param name="share"></param>
        void CALLBACK_SetPinCodeShare(string share)
        {
            if (share.ToLower() == "ok")
            {
                // user has successfully added a pin code
                _logger.Log("User has successfully added a pin code");
            }
            else
            {
                // set the dek with the tokenized share returned by the PinCodePopup
                _wrappedDek = share;
            }
        }

        /// <summary>
        /// Login the user by opening the auth popup window or toggling the slideout.
        /// </summary>
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

        async Task FetchOpenIdConfiguration()
        {
            // Config is already fetched
            if (_openIdConfig != null) return;

            string configUrl = $"{DOMAIN}/.well-known/openid-configuration";
            WebRequestResponse res = await WebRequestHandler.Get(configUrl, null);

            if (!res.IsSuccess)
            {
                _logger.LogError($"Error fetching OpenIdConfiguration: {res.Error}");
                return;
            }

            _openIdConfig = JsonConvert.DeserializeObject<OpenIdConfiguration>(res.Data);
        }

        async void OpenPopupWindow()
        {
            if (_loginMode != LoginBehavior.POPUP) return;

            if (IsAuthenticated) return;

            // Get OIDC configuration
            await FetchOpenIdConfiguration();

            // Generate secure random state
            var state = SphereOneUtils.SecureRandomString(24, true);
            SPrefs.SetString(LOCAL_STORAGE_STATE, state);

#if UNITY_IOS || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_ANDROID
            // Redirect URL is the same for ios and macos, hardcoded here
            _redirectUrl = $"{_scheme}://auth";
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            _redirectUrl = "http://localhost:8080/win-standalone/oauth2";
#endif

            var authorizationUrl = $"{_openIdConfig.authorization_endpoint}?response_type=code&client_id={_clientId}&state={state}&audience={AUDIENCE}&scope=openid%20profile%20email%20offline_access&redirect_uri={_redirectUrl}";

#if UNITY_WEBGL
            OpenWindow(authorizationUrl);
#elif UNITY_IOS || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            OpenWebAuthenticationSessionWithRedirectURL(authorizationUrl);
#elif UNITY_ANDROID
            AndroidChromeCustomTab.LaunchUrl(authorizationUrl);
#endif
        }

        public void AddPinCode()
        {
            if (!IsAuthenticated) return;
            var accessToken = _credentials.access_token;
#if UNITY_WEBGL
            var title = "Add Pin Code";
            var url = $"{PIN_CODE_URL}/add?accessToken={accessToken}&redirectUrl={_redirectUrl}";
            OpenPinCodePopup(title, url);
#elif UNITY_ANDROID
            var url = $"{PIN_CODE_URL}/add?accessToken={accessToken}&platform=android&redirectUrl={_redirectUrl}";
            AndroidChromeCustomTab.LaunchUrl(url);
#elif UNITY_IOS
            // Code specific to iOS (both iPhone and iPad)
            var url = $"{PIN_CODE_URL}/add?accessToken={accessToken}&platform=ios&redirectUrl={_redirectUrl}";
            OpenWebAuthenticationSessionWithRedirectURL(url);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            // Code specific to running in the Unity Editor on macOS or standalone macOS build
            var url = $"{PIN_CODE_URL}/add?accessToken={accessToken}&platform=mac&redirectUrl={_redirectUrl}";
            OpenWebAuthenticationSessionWithRedirectURL(url);
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            var url = $"{PIN_CODE_URL}/add?accessToken={accessToken}&platform=win&redirectUrl={_redirectUrl}";
            OpenWebAuthenticationSessionWithRedirectURL(url);
#endif
        }

        /// <summary>
        /// Opens a pin code entry interface for the user. The specific action taken by the pin code
        /// interface is determined by the target parameter, which should correspond to one of the
        /// predefined actions in <see cref="PincodeTargets"/>.
        /// </summary>
        /// <param name="target">The pin code target action as a string. Defaults to <see cref="PincodeTargets.SendNft"/>,
        /// which represents the action to send an NFT.</param>
        /// <remarks>
        /// <para>Scenarios:</para>
        /// <list type="bullet">
        /// <item>
        /// <description>If the user wants to make a payment and needs to enter their PIN, call <c>OpenPinCode("the-charge-id")</c>.</description>
        /// </item>
        /// <item>
        /// <description>If the user wants to send an NFT, call <c>OpenPinCode(PincodeTargets.SendNft)</c>.</description>
        /// </item>
        /// <item>
        /// <description>If the user wants to add a custodial wallet (with ReadOnly access), call <c>OpenPinCode(PincodeTargets.AddWallet)</c>.</description>
        /// </item>
        /// </list>
        /// <para>This method is platform-dependent and will open the appropriate pin code entry interface
        /// based on the current platform (WebGL, Android, iOS, macOS, Windows).</para>
        /// </remarks>
        public void OpenPinCode(string target = PincodeTargets.SendNft)
        {
            if (!IsAuthenticated) return;
            var accessToken = _credentials.access_token;
#if UNITY_WEBGL
            var title = "SphereOne Pin Code";
            var url = $"{PIN_CODE_URL}/?accessToken={accessToken}&target={target}";
            OpenPinCodePopup(title, url);
#elif UNITY_ANDROID
            var url = $"{PIN_CODE_URL}/?accessToken={accessToken}&target={target}&platform=android&redirectUrl={_redirectUrl}";
            AndroidChromeCustomTab.LaunchUrl(url);
#elif UNITY_IOS
            // Code specific to iOS (both iPhone and iPad)
            var url = $"{PIN_CODE_URL}/?accessToken={accessToken}&target={target}&platform=ios&redirectUrl={_redirectUrl}";
            OpenWebAuthenticationSessionWithRedirectURL(url);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
             // Code specific to running in the Unity Editor on macOS or standalone macOS build
            var url = $"{PIN_CODE_URL}/?accessToken={accessToken}&target={target}&platform=mac&redirectUrl={_redirectUrl}";
            OpenWebAuthenticationSessionWithRedirectURL(url);
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            var url = $"{PIN_CODE_URL}/?accessToken={accessToken}&target={target}&platform=win&redirectUrl={_redirectUrl}";
            OpenWebAuthenticationSessionWithRedirectURL(url);
#endif
        }

        // iOS and macos (uses ASWebAuthenticationSession under the hood), windows runs a local server and waits for a callback 
        async void OpenWebAuthenticationSessionWithRedirectURL(string authUrl)
        {
            try
            {
#if UNITY_IOS || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                var redirectReturnUrl = await WebAuthenticaionSession.PresentWebAuthenticationSessionWithURLAsync(authUrl, _scheme, true);

                CALLBACK_PopupLoginSuccess(redirectReturnUrl);
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                var _browser = new StandaloneBrowser();
                var browserResult =
                        await _browser.StartAsync(authUrl, _redirectUrl);
                if (browserResult.status == BrowserStatus.Success)
                {
                    // 3. Exchange authorization code for access and refresh tokens.
                    CALLBACK_PopupLoginSuccess(browserResult.redirectUrl); 
                }
#endif
            }
            catch (Exception e)
            {
                var text = e.Message;
                CALLBACK_PopupLoginError(text);
            }
        }

        /// <summary>
        /// Logout the user and clear the cache.
        /// </summary>
        public async void Logout()
        {
            if (_loginMode == LoginBehavior.SLIDEOUT)
            {
                // TODO - finish this - frontend logout has been refactored
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

                // Get OIDC configuration if its not already stored
                await FetchOpenIdConfiguration();

                // Logout OIDC
                string tokenHint = _credentials != null ? _credentials.id_token : "";
                await WebRequestHandler.Get($"{_openIdConfig.end_session_endpoint}?id_token_hint={tokenHint}");
            }

            _logger.Log("User logged out. Local cookies cleared.");

            onUserLogout?.Invoke();
        }

        /// <summary>
        /// Toggle the SphereOne slideout wallet. Does nothing in popup mode.
        /// </summary>
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
            _wrappedDekExpiration = 0;
            User = null;

            Wallets.Clear();
            Balances.Clear();
            Nfts.Clear();
            _headers.Remove("Authorization");

            SPrefs.SetString(LOCAL_STORAGE_CREDENTIALS, null);
            SPrefs.SetString(LOCAL_STORAGE_STATE, null);
            _wrappedDek = null;
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

            if (!string.IsNullOrEmpty(credentials.access_token))
            {
                RefreshUserAuthentication(credentials);
            }
            else
            {
                _logger.LogError("Error loading credentials");
                return;
            }

            _logger.Log($"User authenticated.");

            // Save token to local storage
            if (_loginMode == LoginBehavior.POPUP)
            {
                string credentialsJson = JsonConvert.SerializeObject(_credentials);
                SPrefs.SetString(LOCAL_STORAGE_CREDENTIALS, credentialsJson);
            }

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
#pragma warning disable CS4014 // Suppress the warning.
            // We don't want these async calls to be called synchronously. We want them to run in parallel.
            // So, we will ignore Unity's warning about not awaiting the async calls.
            FetchUserWallets();
            FetchUserInfo();
            FetchUserNfts();
            FetchUserBalances();
#pragma warning restore CS4014  // Re-enable the warning
        }

        async Task<string> GetWrappedDek()
        {
            if (!string.IsNullOrEmpty(_wrappedDek) && _wrappedDekExpiration * 1000 > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            {
                return _wrappedDek;
            }

            try
            {
                string url = $"{_sphereOneApiUrl}/createOrRecoverAccount";
                WebRequestResponse res = await WebRequestHandler.Post(url, "", _headers);

                if (!res.IsSuccess) throw new Exception(res.Error);

                string wrappedDekData = JsonConvert.DeserializeObject<WrappedDekFormatResponse>(res.Data).data;

                long expiration = JwtUtils.GetTokenExpirationTime(wrappedDekData) * 1000; // Convert to milliseconds;
                _wrappedDek = wrappedDekData;
                _wrappedDekExpiration = expiration;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error fetching wrapped DEK: {ex.Message}");
                throw ex;
            }

            return _wrappedDek;
        }

        /// <summary>
        /// Checks if user has a PIN already setup.
        /// </summary>
        /// <returns><see cref="bool"/></returns>
        public bool CheckIfPinCodeExists()
        {
            return User.isPinCodeSetup == true;
        }

        /// <summary>
        FormattedBatch FormatBatch(string title, List<RouteAction> actions)
        {
            FormattedBatchRender renderObj = new FormattedBatchRender
            {
                type = BatchType.TRANSFER,
                title = title,
                operations = new List<string>()
            };

            foreach (var action in actions)
            {
                if (action.transferData != null)
                {
                    renderObj.type = BatchType.TRANSFER;
                    renderObj.operations.Add(
                        $"- Transfer {HexToNumber(action.transferData.fromAmount.hex, action.transferData.fromToken.decimals)} {action.transferData.fromToken.symbol} in {action.transferData.fromChain}"
                    );
                }
                else if (action.swapData != null)
                {
                    renderObj.type = BatchType.SWAP;
                    renderObj.operations.Add(
                        $"- Swap {HexToNumber(action.swapData.fromAmount.hex, action.swapData.fromToken.decimals)} {action.swapData.fromToken.symbol} to {HexToNumber(action.swapData.toAmount.hex, action.swapData.toToken.decimals)} {action.swapData.toToken.symbol} in {action.swapData.fromChain}"
                    );
                }
                else if (action.bridgeData != null)
                {
                    renderObj.type = BatchType.BRIDGE;
                    renderObj.operations.Add(
                        $"- Bridge {HexToNumber(action.bridgeData.quote.fromAmount.hex, action.bridgeData.quote.fromToken.decimals)} {action.bridgeData.quote.fromToken.symbol} in {action.bridgeData.quote.fromToken.chain} to {HexToNumber(action.bridgeData.quote.toAmount.hex, action.bridgeData.quote.toToken.decimals)} {action.bridgeData.quote.toToken.symbol} in {action.bridgeData.quote.toToken.chain}"
                    );
                }
            }

            FormattedBatch formattedBatch = new FormattedBatch
            {
                type = renderObj.type,
                title = renderObj.title,
                operations = renderObj.operations.ToArray() // Convert List<string> to string[]
            };

            return formattedBatch;
        }

        public string HexToNumber(string hex, int decimals)
        {
            BigInteger number = ParseHexToBigInteger(hex);

            // Convert BigInteger to decimal and adjust for decimals
            decimal decimalNumber = (decimal)number / (decimal)Math.Pow(10, decimals);
            // Format the decimal number as a string
            return decimalNumber.ToString($"F{decimals}", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
        }

        private BigInteger ParseHexToBigInteger(string hex)
        {
            // Remove the "0x" prefix if it exists
            hex = hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? hex.Substring(2) : hex;
            BigInteger result = 0;

            // Iterate through the string and calculate the number
            foreach (char c in hex)
            {
                result = result * 16 + HexCharToInt(c);
            }

            return result;
        }

        private int HexCharToInt(char hexChar)
        {
            if (hexChar >= '0' && hexChar <= '9')
            {
                return hexChar - '0';
            }
            if (hexChar >= 'a' && hexChar <= 'f')
            {
                return hexChar - 'a' + 10;
            }
            if (hexChar >= 'A' && hexChar <= 'F')
            {
                return hexChar - 'A' + 10;
            }
            throw new ArgumentException("Invalid hex character: " + hexChar);
        }

        #region API functions

        /// <summary>
        /// Fetch the most recent User Info.
        /// </summary>
        /// <returns>The <see cref="User"/> object or null if there was an error.</returns>
        async public Task<User> FetchUserInfo()
        {
            if (_environment == Environment.EDITOR)
            {
                var usrJson = MockApiDataFactory.Instance.GetMockUser();
                return LoadUser(usrJson);
            }

            try
            {
                await CheckJwtExpiration();

                string url = $"{_sphereOneApiUrl}/user";
                WebRequestResponse res = await WebRequestHandler.Get(url, _headers);

                if (!res.IsSuccess)
                    throw new Exception(res.Error);

                return LoadUser(res.Data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error fetching user info: {ex.Message}");
                throw ex;
            }
        }

        User LoadUser(string json)
        {
            User = JsonConvert.DeserializeObject<UserWrapper>(json).data;

            _logger.Log($"User loaded: {User.name}");

            onUserLoaded?.Invoke(User);

            return User;
        }

        /// <summary>
        /// Fetch the most recent User Wallets.
        /// </summary>
        /// <returns>A List of <see cref="Wallet"/> objects or null if there was an error.</returns>
        async public Task<List<Wallet>> FetchUserWallets()
        {
            if (_environment == Environment.EDITOR)
            {
                return LoadWallets(MockApiDataFactory.Instance.GetMockWallets()); ;
            }

            try
            {
                await CheckJwtExpiration();

                string url = $"{_sphereOneApiUrl}/user/wallets";
                WebRequestResponse res = await WebRequestHandler.Get(url, _headers);

                if (!res.IsSuccess)
                    throw new Exception(res.Error);

                return LoadWallets(res.Data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error fetching user wallets: {ex.Message}");
                throw ex;
            }
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

        /// <summary>
        /// Fetch the most recent User Balances.
        /// </summary>
        /// <returns>A List of <see cref="Balance"/> objects or null if there was an error.</returns>
        async public Task<List<Balance>> FetchUserBalances()
        {
            if (_environment == Environment.EDITOR)
            {
                return LoadBalances(MockApiDataFactory.Instance.GetMockBalances()); ;
            }

            try
            {
                await CheckJwtExpiration();

                string url = $"{_sphereOneApiUrl}/getFundsAvailable?refreshCache={_forceRefreshCache.ToString().ToLower()}";
                WebRequestResponse res = await WebRequestHandler.Get(url, _headers);

                if (!res.IsSuccess) throw new Exception(res.Error);

                return LoadBalances(res.Data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error fetching balances: {ex.Message}");
                throw ex;
            }
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

        /// <summary>
        /// Fetch the most recent User Nfts.
        /// </summary>
        /// <returns>A List of <see cref="Nft"/> objects or null if there was an error.</returns>
        async public Task<List<Nft>> FetchUserNfts()
        {
            if (_environment == Environment.EDITOR)
            {
                return LoadNfts(MockApiDataFactory.Instance.GetMockNfts()); ;
            }

            try
            {
                await CheckJwtExpiration();

                string url = $"{_sphereOneApiUrl}/getNftsAvailable";
                WebRequestResponse res = await WebRequestHandler.Get(url, _headers);

                if (!res.IsSuccess) throw new Exception(res.Error);

                return LoadNfts(res.Data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error fetching NFTs: {ex.Message}");
                return null;
            }
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
        /// Create a charge to be paid later.
        /// </summary>
        /// <param name="chargeReq"></param>
        /// <param name="isTest">Not required. Determines if API Key is test or production. By default, it is false.</param>
        /// <param name="isDirectTransfer">Not required. By default, it is false.</param>
        /// <returns>The <see cref="ChargeResponse"/> object or null if there was an error.</returns>
#nullable enable
        async public Task<ChargeResponse> CreateCharge(ChargeReqBody chargeReq, bool isTest = false,
                bool isDirectTransfer = false, CallSmartContractProps? callSmartContractProps = null)
        {
            try
            {
                if (_environment == Environment.EDITOR)
                {
                    // TODO fake charge mock data
                    return null;
                }
                // remove old wrappedDek whenever a new charge is created
                _wrappedDek = null;

                var body = new CreateChargeReqBodyWrapper(chargeReq, isTest, isDirectTransfer, callSmartContractProps);
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                var bodySerialized = JsonConvert.SerializeObject(body, settings);

                string url = $"{_sphereOneApiUrl}/createCharge";
                WebRequestResponse response = await WebRequestHandler.Post(url, bodySerialized, _headers);
                if (!response.IsSuccess)
                {
                    throw new Exception(response.Error);
                }

                ChargeResponse chargeResponse = JsonConvert.DeserializeObject<CreateChargeResponseWrapper>(response.Data).data;
                _logger.Log($"Charge Created: {chargeResponse}");

                return chargeResponse;
            }
            catch (Exception e)
            {
                _logger.LogError($"There was an error creating your transaction, error: {e.Message}");
                throw e;
            }
        }
#nullable disable

        /// <summary>
        /// Pay a charge created with <see cref="CreateCharge"/>
        /// </summary>
        /// <param name="transactionId">The id of the charge</param>
        /// <returns>The <see cref="PayResponse"/> object or null if there was an error.</returns>
        async public Task<PayResponse> PayCharge(string transactionId)
        {
            try
            {
                if (_environment == Environment.EDITOR)
                {
                    // TODO fake charge mock data
                    return null;
                }

                await CheckJwtExpiration();

                var dek = _wrappedDek;

                if (dek == null)
                {
                    throw new Exception("There was an error getting the wrapped dek");
                }

                var body = new PayChargeReqBody(dek, transactionId);
                var bodySerialized = JsonConvert.SerializeObject(body);

                string url = $"{_sphereOneApiUrl}/pay";
                WebRequestResponse response = await WebRequestHandler.Post(url, bodySerialized, _headers);

                if (!response.IsSuccess)
                {
                    PayResponseOnRampLink onRampResponse = JsonConvert.DeserializeObject<PayResponseOnRampLink>(response.Data);
                    if (onRampResponse.error.code == "empty-balances" ||
                        onRampResponse.error.code == "insufficient-balances" ||
                        onRampResponse.error.message.Contains("Not sufficient funds to bridge"))
                    {
                        string onrampLink = onRampResponse.data?.onrampLink;
                        throw new PayError("insufficient balances", onrampLink);
                    }
                    else
                    {
                        PayErrorResponse errorResponse = JsonConvert.DeserializeObject<PayErrorResponse>(response.Error);
                        throw new Exception($"Payment failed: {errorResponse.error.code ?? errorResponse.error.message}");
                    }
                }
                else
                {
                    PayResponse payResponse = JsonConvert.DeserializeObject<PayResponse>(response.Data);
                    _logger.Log(payResponse.ToString());

                    // after done, delete it
                    _wrappedDek = null;

                    return payResponse;
                }
            }
            catch (PayError e)
            {
                _logger.LogError($"There was an error paying your transaction. User needs to perform onramp with OnrampLink in Error Response: {e.Message}");
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError($"There was an error paying your transaction, error: {e.Message}");
                throw;
            }
        }

        void SetupAuthHeader()
        {
            _headers.Clear();
            _headers.Add("x-api-key", _apiKey);
            _headers.Add("sphere-one-source", "unity-sdk");
            _headers.Add("sphere-one-client-id", _clientId);
        }

        void RefreshUserAuthentication(Credentials credentials)
        {
            _credentials = credentials;
            _headers.Remove("Authorization");
            _headers.Add("Authorization", $"Bearer {_credentials.access_token}");
        }

        async Task CheckJwtExpiration()
        {
            if (!string.IsNullOrEmpty(_credentials.access_token) && JwtUtils.IsTokenExpired(_credentials.access_token))
            {
                // Token is out of date

                _logger.Log("JWT Token is expired, attempting to refresh.");

                await RefreshToken();
            }
            else
            {
                // Token valid or doesn't exist
            }
        }

        public async Task RefreshToken()
        {

            RefreshTokenReqBody body = new RefreshTokenReqBody(_credentials.refresh_token, _clientId);
            var form = body.ToForm();
            string refreshUrl = $"{DOMAIN}/oauth2/token";
            var res = await WebRequestHandler.Post(refreshUrl, form, _headers);
            var refreshCredentials = JsonConvert.DeserializeObject<Credentials>(res.Data);
            if (!string.IsNullOrEmpty(refreshCredentials.access_token))
            {
                RefreshUserAuthentication(refreshCredentials);
            }
            else
            {
                _logger.LogError("Error refreshing credentials");
            }
        }

        public void ValidateConfiguration()
        {
            string pre = "SphereOneSDK: Invalid Configuration. ";

            if (!Application.isEditor && _environment == Environment.EDITOR)
                throw new Exception(pre + "Environment EDITOR can only be used in the editor. You must switch to PRODUCTION before building.");

            if (string.IsNullOrEmpty(_sphereOneApiUrl))
                throw new Exception(pre + "Sphere One Api URL is required");

            if (!SphereOneUtils.IsUrlValid(_sphereOneApiUrl))
                throw new Exception(pre + "Sphere One Api URL invalid");

            if (string.IsNullOrEmpty(_apiKey))
                throw new Exception(pre + "Api Key is required");

            if (_loginMode == LoginBehavior.POPUP)
            {
                // Validate Setup
                if (string.IsNullOrEmpty(_clientId))
                    throw new Exception(pre + "Client Id is required");

#if UNITY_WEBGL
                if (string.IsNullOrEmpty(_redirectUrl))
                    throw new Exception(pre + "Redirect URL is required");

                if (!SphereOneUtils.IsUrlValid(_redirectUrl))
                    throw new Exception(pre + "Redirect URL invalid.");
#elif UNITY_IOS || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_ANDROID
                if (string.IsNullOrEmpty(_scheme))
                    throw new Exception(pre + "Scheme is required");
#endif
            }
            else if (_loginMode == LoginBehavior.SLIDEOUT)
            {

            }
        }

        /// <summary>
        /// Get the estimated route for a transaction.
        /// </summary>
        /// <param name="transactionId"></param>
        /// <returns>The <see cref="PayRouteEstimate"/> object or null if there was an error.</returns>
        public async Task<PayRouteEstimate> GetRouteEstimation(string transactionId)
        {
            try
            {
                if (_environment == Environment.EDITOR)
                {
                    // TODO fake charge mock data
                    return null;
                }

                await CheckJwtExpiration();

                var serializedBody = JsonConvert.SerializeObject(new
                {
                    transactionId = transactionId
                });
                WebRequestResponse response = await WebRequestHandler.Post($"{_sphereOneApiUrl}/pay/route", serializedBody, _headers);
                if (!response.IsSuccess)
                {
                    OnRampErrorFormatResponse errorRes = JsonConvert.DeserializeObject<OnRampErrorFormatResponse>(response.Data);
                    if (errorRes.error.code == "empty-balances" || errorRes.error.code == "insufficient-balances")
                    {
                        OnRampResponse onrampData = errorRes.data;
                        string onrampLink = onrampData.onrampLink;
                        throw new RouteEstimateError("There was an error calculating route for transaction because user doesn't have enough funds to get a proper estimation. User needs to perform onramp with OnrampLink in Error Response.", onrampLink);
                    }
                    else
                    {
                        throw new Exception(errorRes.error.message);
                    }
                }
                else
                {
                    var data = JsonConvert.DeserializeObject<PayRouteEstimateResponse>(response.Data).data;
                    List<RouteBatch> parsedRouteList = JsonConvert.DeserializeObject<List<RouteBatch>>(data.estimation.route);
                    FormattedBatch[] batches = parsedRouteList
                        .Select(b => FormatBatch(b.description, b.actions.ToList()))
                        .ToArray();
                    PayRouteEstimate newData = new PayRouteEstimate(data)
                    {
                        estimation = new PayRouteTotalEstimation(data.estimation)
                        {
                            routeParsed = batches
                        }
                    };
                    _logger.Log($"Route Estimation: {newData.ToString()}");
                    return newData;
                }
            }
            catch (RouteEstimateError e)
            {
                _logger.LogError($"{e.Message}");
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError($"There was an error calculating route for your transaction, error: {e.Message}");
                throw;
            }
        }

        public async Task<TransferNftResponse> TransferNft(NftDataParams data)
        {
            try
            {
                await CheckJwtExpiration();

                var dek = _wrappedDek;
                if (dek == null)
                {
                    throw new Exception("There was an error getting the wrapped dek");
                }

                var bodySerialized = JsonConvert.SerializeObject(new
                {
                    data.fromAddress,
                    data.toAddress,
                    data.chain,
                    data.nftTokenAddress,
                    data.tokenId,
                    data.reason,
                    wrappedDek = dek
                });

                string url = $"{_sphereOneApiUrl}/transferNft";
                WebRequestResponse response = await WebRequestHandler.Post(url, bodySerialized, _headers);

                if (!response.IsSuccess)
                {
                    throw new Exception(response.Error);
                }
                else
                {
                    TransferNftResponse nftTransferResult = JsonConvert.DeserializeObject<TransferNftResponseWrapper>(response.Data).data;
                    _logger.Log($"NFT Transferred: {nftTransferResult.approveTxHash}");
                    return nftTransferResult;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"There was an error transferring your NFT, error: {e.Message}");
                throw e;
            }
            finally
            {
                // after done, delete it
                _wrappedDek = null;
            }
        }

        #endregion
    }

    [Serializable]
    class OpenIdConfiguration
    {
        public string issuer;
        public string authorization_endpoint;
        public string token_endpoint;
        public string end_session_endpoint;
        public string userinfo_endpoint;
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

        // For Debugging, Testing
        public override string ToString()
        {
            return $"access_token: {access_token}\nrefresh_token: {refresh_token}\nid_token: {id_token}\nscope: {scope}\nexpires_in: {expires_in}\ntoken_type: {token_type}";
        }
    }

    [Serializable]
    class RefreshTokenReqBody
    {
        public string grant_type;
        public string refresh_token;
        public string client_id;

        public RefreshTokenReqBody(string refresh_token, string client_id)
        {
            grant_type = "refresh_token";
            this.refresh_token = refresh_token;
            this.client_id = client_id;
        }

        // Refresh does not work as JSON
        public WWWForm ToForm()
        {
            WWWForm form = new WWWForm();
            form.AddField("grant_type", grant_type);
            form.AddField("refresh_token", refresh_token);
            form.AddField("client_id", client_id);
            return form;
        }

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

#nullable enable
    [Serializable]
    class CreateChargeReqBodyWrapper
    {
        public CreateChargeReqBodyWrapper(ChargeReqBody chargeData, bool isTest, bool isDirectTransfer, CallSmartContractProps? callSmartContractProps = null)
        {
            this.isTest = isTest;
            this.isDirectTransfer = isDirectTransfer;
            this.chargeData = chargeData;
            this.callSmartContractProps = callSmartContractProps;
        }

        public bool isTest;
        public bool isDirectTransfer;
        public ChargeReqBody chargeData;
        public CallSmartContractProps? callSmartContractProps;
    }
#nullable disable

    [Serializable]
    class CreateChargeResponseWrapper : ApiResponseWrapper
    {
        public ChargeResponse data;
    }

    [Serializable]
    class RouteEstimationBodyWrapper
    {
        public RouteEstimationBodyWrapper(string data)
        {
            this.transactionId = data;
        }

        public string transactionId;
    }

    [Serializable]
    class PinCodeReqBodyWrapper
    {
        public PinCodeReqBodyWrapper(string pin)
        {
            this.pinCode = pin;
        }

        public string pinCode;
    }

    public class WrappedDekFormatResponse
    {
        public string data { get; set; }
        public string error { get; set; }
    }

    public class PinCodeFormatResponse
    {
        public PinCodeData data { get; set; }
        public string error { get; set; }
    }

    public class PinCodeData
    {
        // DEK, PIN, 
        public string code { get; set; }
        public string share { get; set; }

        // can be null or "OK"
        public string status { get; set; }
    }

    [Serializable]
    public class TransferNftResponseWrapper
    {
        public TransferNftResponse data { get; set; }
        public string error { get; set; }
    }

    [Serializable]
    public class TransferNftResponse
    {
        public string approveTxHash { get; set; }
        public string userOperationHash { get; set; }

        public override string ToString()
        {
            return $"approveTxHash: {approveTxHash},\nuserOperationHash: {userOperationHash}";
        }
    }
}