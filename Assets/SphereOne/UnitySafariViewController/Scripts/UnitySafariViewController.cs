using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

public class UnitySafariViewController {

    #if UNITY_IOS
    [DllImport("__Internal")]
    private static extern void dissmissSafariViewController();

    public delegate void SafariDidRedirectBackAction(string url);
    public static Action<string> safariDidRedirectBackAction;
    [DllImport("__Internal")]
    private static extern void presentSafariViewControllerWithURL(string url, bool isModalView, SafariDidRedirectBackAction actionBlock);

    [AOT.MonoPInvokeCallback(typeof(SafariDidRedirectBackAction))]
    public static void SafariDidRedirectBackActionDelegate(string url) {
        safariDidRedirectBackAction(url);
    }

    public static void DissmissSafariViewController() {
        dissmissSafariViewController();
    }

    public static void PresentSafariViewControllerWithURL(string url, bool isModalView = true, Action<string> action = null) {
        if (url.Contains("http") == false) {
            Debug.LogWarning("To open url in SFSafariViewController, the scheme should be http:// or https://, otherwise you can use Application.OpenURL(url); = " + url);
        }
        safariDidRedirectBackAction = action;
        
        presentSafariViewControllerWithURL(url, isModalView, SafariDidRedirectBackActionDelegate);

    }
    
    /// <summary>
    /// Opens SFSafariViewController on iOS. Async version of PresentSafariViewControllerWithURL function.
    /// <param name="url">URL to open.</param>
    /// <returns>Result from the redirect. If result is null, it throws exception with error.</returns>
    /// </summary>
    public static Task<string> PresentSafariViewControllerWithURLAsync(string url, bool isModalView = true) {
        if (url.Contains("http") == false) {
            Debug.LogWarning("To open url in SFSafariViewController, the scheme should be http:// or https://, otherwise you can use Application.OpenURL(url); = " + url);
        }
        var taskCompletionSource = new TaskCompletionSource<string>();
        safariDidRedirectBackAction = (result) => {
            if (string.IsNullOrEmpty(result)) {
                taskCompletionSource.TrySetException(new Exception("Result is null"));
            } else {
                taskCompletionSource.TrySetResult(result);
            } 
            
        };
        presentSafariViewControllerWithURL(url, isModalView, SafariDidRedirectBackActionDelegate);
        return taskCompletionSource.Task;
    }
    
    
    #endif

}
