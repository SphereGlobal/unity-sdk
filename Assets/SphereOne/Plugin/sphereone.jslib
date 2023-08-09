var plugin = {
  OpenWindow: function (url) {
    // window.open(UTF8ToString(url), '_blank')
    var w = window.open(UTF8ToString(url), 'Popup', 'width=500, height=700')

    const interval = setInterval(function () {
      // Sometimes w.location.href.includes is undefined. Not sure why
      try {
        if (w.location && w.location.href) {
          if (w.location.href.includes('code=')) {
            window.unityInstance.SendMessage(
              'SphereOneManager',
              'CALLBACK_PopupLoginSuccess',
              w.location.href
            )
            clearInterval(interval)
            return
          } else if (w.location.href.includes('error=')) {
            window.unityInstance.SendMessage(
              'SphereOneManager',
              'CALLBACK_PopupLoginError',
              w.location.href
            )
            clearInterval(interval)
            return
          }
        }
      } catch (e) {
        console.log(e)
      }
    }, 20)
  },

  CloseWindow: function () {
    window.close()
  },

  SendLogoutMsg: function () {
    window.bridge.logout()
  },

  CreateSlideout: function (src, backgroundFilter) {
    window.bridge.createSlideout(UTF8ToString(src), UTF8ToString(backgroundFilter))
  },

  ToggleSphereOneSlideout: function () {
    window.bridge.toggleSlideout()
  },

  RequestCredentialFromSlideout: function () {
    window.bridge.requestCredentialFromSlideout()
  },
}

mergeInto(LibraryManager.library, plugin)
