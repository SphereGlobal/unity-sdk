var plugin = {
  OpenWindow: function (url) {
    var popupWindow = window.open(
      UTF8ToString(url),
      'Popup',
      'width=500, height=700'
    );

    const interval = setInterval(function () {
      // Sometimes popupWindow.location.href.includes is undefined. Not sure why
      try {
        if (popupWindow.location && popupWindow.location.href) {
          if (popupWindow.location.href.includes('code=')) {
            window.unityInstance.SendMessage(
              'SphereOneManager',
              'CALLBACK_PopupLoginSuccess',
              popupWindow.location.href
            );
            clearInterval(interval);
            return;
          } else if (popupWindow.location.href.includes('error=')) {
            window.unityInstance.SendMessage(
              'SphereOneManager',
              'CALLBACK_PopupLoginError',
              popupWindow.location.href
            );
            clearInterval(interval);
            return;
          }
        }
      } catch (e) {
        console.log(e);
      }
    }, 20);
  },

  SendLogoutMsg: function () {
    window.bridge.logout();
  },

  CreateSlideout: function (src, backgroundFilter) {
    window.bridge.createSlideout(
      UTF8ToString(src),
      UTF8ToString(backgroundFilter)
    );
  },

  ToggleSphereOneSlideout: function () {
    window.bridge.toggleSlideout();
  },

  RequestCredentialFromSlideout: function () {
    window.bridge.requestCredentialFromSlideout();
  },

  OpenAddPinCodePopup: function (url) {
    const width = 450;
    const height = 350;
    const left = (window.innerWidth - width) / 2 + window.screenX;
    const top = (window.innerHeight - height) / 2 + window.screenY;
    const options = `width=${width},height=${height},top=${top},left=${left}`;

    const popup = window.open(
      UTF8ToString(url),
      'Add Pin Code',
      options
    );
  },

  OpenPinCodePopup: function(url) {
    const width = 450;
    const height = 350;
    const left = (window.innerWidth - width) / 2 + window.screenX;
    const top = (window.innerHeight - height) / 2 + window.screenY;
    const options = `width=${width},height=${height},top=${top},left=${left}`;

    const popup = window.open(
      UTF8ToString(url),
      'Sphereone Pin Code',
      options
    );
  },
};

mergeInto(LibraryManager.library, plugin);
