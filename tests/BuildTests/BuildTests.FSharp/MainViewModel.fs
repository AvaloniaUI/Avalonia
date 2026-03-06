namespace BuildTests

open System.Runtime.CompilerServices;

type MainViewModel() =

    member val HelloText = sprintf "Hello from %s" (if RuntimeFeature.IsDynamicCodeSupported then "JIT" else "AOT") with get, set
