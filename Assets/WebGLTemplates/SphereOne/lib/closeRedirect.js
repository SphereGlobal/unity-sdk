const currentUrl = window.location.href
// const searchParams = new URL(currentUrl).searchParams
// const code = searchParams.get('code')

// If URL contains "code" or "error", this is the redirected login popup.
// We are done with this window at this point, so close it
if (currentUrl.includes('code=') || currentUrl.includes('error=')) {
  // Needed a small delay here for the primary window to have time to call window.unityInstance.SendMessage
  setInterval(function () {
    // window.close()
    open(location, '_self').close()
  }, 100)
}
