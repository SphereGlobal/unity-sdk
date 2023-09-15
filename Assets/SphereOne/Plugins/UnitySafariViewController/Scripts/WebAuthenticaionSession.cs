using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

public class WebAuthenticaionSession {
    
#if UNITY_IOS || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    
    /// <summary>
    /// Delegate to be called on authentication completion.
    /// <param name="url">URL string that redirected back after authentication process</param>
    /// <param name="error">Error from the authentication process</param>
    /// </summary>
    public delegate void WebAuthenticationSessionDidRedirectBackAction(string url, string error);
    public static Action<string, string> webAuthenticationSessionDidRedirectBackAction;
    
    #if UNITY_IOS
    [DllImport("__Internal")]
    #elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    [DllImport("UnityWebAuthenticationMacOS", CallingConvention = CallingConvention.Cdecl)]
    #endif

    private static extern void presentWebAuthenticationSessionWithURL(string url, string scheme, bool prefersEphemeralWebBrowserSession, WebAuthenticationSessionDidRedirectBackAction actionBlock);

    [AOT.MonoPInvokeCallback(typeof(WebAuthenticationSessionDidRedirectBackAction))]
    public static void  WebAuthenticationSessionDidRedirectBackActionDelegate(string url, string error) {
        webAuthenticationSessionDidRedirectBackAction(url, error);
    }
    
    /// <summary>
    /// Starts iOS/macOS authentication process.
    /// <param name="url">Authentication URL</param>
    /// <param name="scheme">App URL Scheme for the redirect url.</param>
    /// <param name="prefersEphemeralWebBrowserSession">Set Prefers Ephemeral WebBrowserSession. Note:(macOS and from iOS 13)</param>
    /// <param name="action">Callback WebAuthenticationSessionDidRedirectBackAction(url, error)</param>
    /// <remarks>Make sure that scheme is added to the app, otherwise, the authentication process will not be able to redirect back to the app.</remarks>
    /// </summary>
    public static void PresentWebAuthenticationSessionWithURL(string url, string scheme, bool prefersEphemeralWebBrowserSession, Action<string, string> action) {
        if (url.Contains("http") == false) {
            Debug.LogWarning("To open url in ASWebAuthenticationSession, the scheme should be http:// or https://, otherwise you can use Application.OpenURL(url); = " + url);
        }
        webAuthenticationSessionDidRedirectBackAction = action;
        presentWebAuthenticationSessionWithURL(url, scheme, prefersEphemeralWebBrowserSession, WebAuthenticationSessionDidRedirectBackActionDelegate);
    }

    /// <summary>
    /// Starts iOS/macOS authentication process. Async version of PresentWebAuthenticationSessionWithURL function.
    /// <param name="url">Authentication URL</param>
    /// <param name="scheme">App URL Scheme for the redirect url.</param>
    /// <param name="prefersEphemeralWebBrowserSession">Set Prefers Ephemeral WebBrowserSession. Note:(macOS and from iOS 13)</param>
    /// <returns>Result from the redirect. Otherwise throws exception with error.</returns>
    /// </summary>
    public static Task<string> PresentWebAuthenticationSessionWithURLAsync(string url, string scheme, bool prefersEphemeralWebBrowserSession) {
        if (url.Contains("http") == false) {
            Debug.LogWarning("To open url in ASWebAuthenticationSession, the scheme should be http:// or https://, otherwise you can use Application.OpenURL(url); = " + url);
        }
        var taskCompletionSource = new TaskCompletionSource<string>();
        webAuthenticationSessionDidRedirectBackAction = (result, error) => {
            if (!string.IsNullOrEmpty(error)) {
                taskCompletionSource.TrySetException(new Exception(error));
            } else {
                taskCompletionSource.TrySetResult(result);
            } 
            
        };
        presentWebAuthenticationSessionWithURL(url, scheme, prefersEphemeralWebBrowserSession, WebAuthenticationSessionDidRedirectBackActionDelegate);
        return taskCompletionSource.Task;
    }

#endif


}
