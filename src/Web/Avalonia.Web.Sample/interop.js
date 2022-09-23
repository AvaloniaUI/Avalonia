var LibraryExample = {
    // Internal functions
    $EXAMPLE: {
        internal_func: function () {
        }
    },
    example_initialize: function () {
        globalThis.AvaloniaGL = GL
    }
}

autoAddDeps(LibraryExample, '$EXAMPLE')
mergeInto(LibraryManager.library, LibraryExample)
