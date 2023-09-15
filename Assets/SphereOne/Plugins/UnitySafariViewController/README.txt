# UnityAuthViewControllerDemo
## Description
### iOS features 
* SFSafariViewController
  * UnitySafariViewController is a native iOS plugin that allows opening URLs with SFSafariViewController.
  * Supports SFSafariViewController to show web pages.
* ASWebAuthenticationSession
  * Supports web-based and SSO authentication with both ASWebAuthenticationSession and SFSafariViewController. Very important since many companies switch to this way of authentication to improve security. 

### macOS features
* ASWebAuthenticationSession
  * Supports web-based and SSO authentication with both ASWebAuthenticationSession and SFSafariViewController. Very important since many companies switch to this way of authentication to improve security.

  
## Usage 
* SFSafariViewController: iOS Only 
The plugin is easy to use, simply import the package, and call the function with two parameters: the URL to open and Action with a `redirect_url` parameter. ```UnitySafariViewController.PresentSafariViewControllerWithURL("https://example.com", true, (redirect_uri)=> {   });``` or async version ```UnitySafariViewController.PresentSafariViewControllerWithURLAsync("https://example.com", true);``` 

* ASWebAuthenticationSession: mac OS and iOS 
An example is available with web-based redirection to see the demo drop `UnitySafariViewControllerDemo.prefab` into the empty scene. Then build an iOS project on your device.
Use this function to authenticate on macOS and iOS `WebAuthenticaionSession.PresentWebAuthenticationSessionWithURL(url, callbackScheme, callback)` or async version 'WebAuthenticaionSession.PresentWebAuthenticationSessionWithURLAsync'
