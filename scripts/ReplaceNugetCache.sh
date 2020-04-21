 #!/usr/bin/env bash
 
 cp ../samples/ControlCatalog.NetCore/bin/Debug/netcoreapp3.1/Avalonia**.dll ~/.nuget/packages/avalonia/$1/lib/netcoreapp3.1/
 cp ../samples/ControlCatalog.NetCore/bin/Debug/netcoreapp3.1/Avalonia**.dll ~/.nuget/packages/avalonia/$1/lib/netstandard2.0/
 cp ../samples/ControlCatalog.NetCore/bin/Debug/netcoreapp3.1/Avalonia**.dll ~/.nuget/packages/avalonia.skia/$1/lib/netstandard2.0/
 cp ../samples/ControlCatalog.NetCore/bin/Debug/netcoreapp3.1/Avalonia**.dll ~/.nuget/packages/avalonia.native/$1/lib/netstandard2.0/
 
 
