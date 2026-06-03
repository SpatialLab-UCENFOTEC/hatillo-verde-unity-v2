mergeInto(LibraryManager.library, {
    OpenURL: function(url) {
        window.open(UTF8ToString(url), "_blank");
    }
});
