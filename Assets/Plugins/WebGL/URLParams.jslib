mergeInto(LibraryManager.library, {

    GetURLParam: function(keyPtr) {
        var key = UTF8ToString(keyPtr);
        var params = new URLSearchParams(window.location.search);
        var value = params.get(key) || "";
        var bufferSize = lengthBytesUTF8(value) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(value, buffer, bufferSize);
        return buffer;
    },

    CopyTextToClipboard: function(textPtr) {
        var text = UTF8ToString(textPtr);
        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(text);
        } else {
            // Fallback for browsers without async clipboard API
            var el = document.createElement("textarea");
            el.value = text;
            el.style.position = "fixed";
            el.style.opacity = "0";
            document.body.appendChild(el);
            el.focus();
            el.select();
            try { document.execCommand("copy"); } catch (e) {}
            document.body.removeChild(el);
        }
    },

    GetPageOrigin: function() {
        var origin = window.location.origin + window.location.pathname;
        var bufferSize = lengthBytesUTF8(origin) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(origin, buffer, bufferSize);
        return buffer;
    }

});
