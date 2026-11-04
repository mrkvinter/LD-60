mergeInto(LibraryManager.library, {
    IsPointerLocked: function () {
        return document.pointerLockElement ? 1 : 0;
    }
});
