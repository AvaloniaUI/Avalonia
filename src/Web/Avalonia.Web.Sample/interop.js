var LibraryExample = {
    // Internal functions
    $EXAMPLE: {
        internal_func: function () {
        }
    },
    example_initialize: function () {
        window["avalonia-helper-GL"] = GL
    }
}

autoAddDeps(LibraryExample, '$EXAMPLE')
mergeInto(LibraryManager.library, LibraryExample)
