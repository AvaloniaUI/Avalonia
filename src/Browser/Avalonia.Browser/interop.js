var LibraryExample = {
    // Internal functions
    $EXAMPLE: {
        internal_func: function () {
        }
    },
    InterceptGLObject: function () {
        globalThis.AvaloniaGL = GL
    }
}

autoAddDeps(LibraryExample, '$EXAMPLE')
mergeInto(LibraryManager.library, LibraryExample)
