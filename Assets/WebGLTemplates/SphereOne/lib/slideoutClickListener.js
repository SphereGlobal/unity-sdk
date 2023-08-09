document.addEventListener('click', function (e) {
  var slideout = document.getElementById('slideout')

  // slideout will not exists in popup AuthMode
  if (!slideout) return

  // close the slideout when user clicks the background (when id === null)
  if (e.target.getAttribute('id') !== null) return

  if (slideout.classList.contains('open')) {
    bridge.toggleSlideout()
  }
})
