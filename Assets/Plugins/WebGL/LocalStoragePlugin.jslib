mergeInto(LibraryManager.library, {

    GetWebLocalStorage: function (keyPtr) {
        var key = UTF8ToString(keyPtr);
        var value = localStorage.getItem(key);
        if (value === null) return 0;

        var bufferSize = lengthBytesUTF8(value) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(value, buffer, bufferSize);
        return buffer;
    }

});
