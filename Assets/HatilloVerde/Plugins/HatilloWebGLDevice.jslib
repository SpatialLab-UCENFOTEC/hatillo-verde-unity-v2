mergeInto(LibraryManager.library, {
  HatilloIsMobileBrowser: function () {
    try {
      var ua = navigator.userAgent || navigator.vendor || '';
      return /iPhone|iPad|iPod|Android|BlackBerry|IEMobile|Opera Mini/i.test(ua) ? 1 : 0;
    } catch (e) {
      return 0;
    }
  }
});
