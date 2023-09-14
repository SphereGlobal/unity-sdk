using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnityAuthViewControllerDemo : MonoBehaviour 
{
    public Button openURL;
    public Button openURLWithRedirect;
    public Button webAuth;

    public InputField field;
    public InputField authURL;
    public InputField callback;
    public Text label;
    public string text;

    public GameObject openURLGameObject;
    public GameObject openURLWithRedirectGameObject;
    private string url = "https://0sxwyc7xw4.execute-api.us-east-1.amazonaws.com/unity_webauth_redirect";
    private static string scheme = "UnitySafariViewControllerScheme";
    private string redirect = $"redirect_uri={scheme}://auth";

    private string testURL =
        "https://assetstore.unity.com/packages/tools/network/web-sso-authentication-for-unity-ios-macos-ipados-100575";
    private void Start() {
        authURL.text = $"{url}?{redirect}";
        callback.text = scheme;
        field.text = testURL;
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        openURLGameObject.SetActive(false);
        openURLWithRedirectGameObject.SetActive(false);
#endif
     }

    public void OpenURL() {
        #if UNITY_IOS
        UnitySafariViewController.PresentSafariViewControllerWithURL(field.text,true, (url) => {
            Debug.Log("Returned url: " + url);
            text = url;
        });
        #endif
    }
    public async void OpenURLWithRedirectURL() {
        #if UNITY_IOS
        try {
            text = await UnitySafariViewController.PresentSafariViewControllerWithURLAsync($"{url}?{redirect}");
            Debug.Log("Result PresentSafariViewControllerWithURLAsync: " + text);
        } catch (Exception e) {
            text = e.Message;
            Debug.Log("Exception PresentSafariViewControllerWithURLAsync: " + e.Message);
        }
        #endif

    }
    
    public async void OpenWebAuthenticationSessionWithRedirectURL() {
        #if UNITY_IOS || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        try {
            text = await WebAuthenticaionSession.PresentWebAuthenticationSessionWithURLAsync(authURL.text, callback.text, true);
            Debug.Log("Result PresentWebAuthenticationSessionWithURLAsync: " + text);
        } catch (Exception e) {
            text = e.Message;
            Debug.Log("Exception PresentWebAuthenticationSessionWithURLAsync: " + e.Message);
        }
        #endif
    }
    
    private void Update() {
        if (text == null) return;
        if(text != label.text)
            label.text = text;

    }
}
