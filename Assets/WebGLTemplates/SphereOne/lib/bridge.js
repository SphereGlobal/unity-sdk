// This should be replaced with the published domain
//   const targetOrigin = "https://wallet.sphereone.xyz"
const TARGET_ORIGIN = '*'

const SDK_BRIDGE_METHOD = {
  RETURN_CREDENTIALS: 'RETURN-CREDENTIALS',
  REQUEST_CREDENTIALS: 'REQUEST-CREDENTIALS',
  LOGOUT: 'LOGOUT',
}

class Bridge {
  darkenBackground = false
  blurBackground = false

  constructor() {}

  requestCredentialFromSlideout() {
    var iframe = document.getElementById('slideout-iframe')

    const message = JSON.stringify({
      method: SDK_BRIDGE_METHOD.REQUEST_CREDENTIALS,
    })

    iframe.contentWindow.postMessage(message, TARGET_ORIGIN)
  }

  logout() {
    var iframe = document.getElementById('slideout-iframe')

    const message = JSON.stringify({
      method: SDK_BRIDGE_METHOD.LOGOUT,
    })

    iframe.contentWindow.postMessage(message, TARGET_ORIGIN)
  }

  createSlideout(src, backgroundFilter) {
    switch (backgroundFilter) {
      case 'DARKEN':
        this.darkenBackground = true
        break

      case 'BLUR':
        this.blurBackground = true
        break
    }

    document.getElementById(
      'slideout-container'
    ).innerHTML += `<div id="slideout"><iframe id="slideout-iframe" src="${src}" allow="fullscreen"></iframe></div>`
  }

  toggleSlideout() {
    var slideout = document.getElementById('slideout')

    // slideout will not exists in popup AuthMode
    if (!slideout) return

    slideout.classList.toggle('open')

    var bg = document.getElementById('app-container')

    if (this.darkenBackground) bg.classList.toggle('darken-overlay')
    if (this.blurBackground) bg.classList.toggle('blur-overlay')

    bg.classList.toggle('no-click')
  }
}

var bridge = new Bridge()

// Listen for response from iframe
window.addEventListener('message', function (event) {
  const pinCodeData = event.data;
  if (pinCodeData.data.code.toLowerCase() === 'dek') {
    window.unityInstance.SendMessage(
      'SphereOneManager',
      'CALLBACK_SetPinCodeShare',
      pinCodeData.data.share
    );
  }

  if (typeof event.data !== 'string') return

  const data = JSON.parse(event.data)

  if (data.method === SDK_BRIDGE_METHOD.RETURN_CREDENTIALS) {
    window.unityInstance.SendMessage(
      'SphereOneManager',
      'CALLBACK_CredentialFromSlideout',
      JSON.stringify(data.credentials)
    )
  } else if (data.method === SDK_BRIDGE_METHOD.LOGOUT) {
    window.unityInstance.SendMessage('SphereOneManager', 'CALLBACK_Logout')
  }
})
