 #!/usr/bin/env bash
 
 cp ../samples/ControlCatalog.NetCore/bin/Debug/netcoreapp1.1/Avalonia**.dll ~/.nuget/packages/avalonia/$1/lib/netcoreapp1.0/
 cp ../samples/ControlCatalog.NetCore/bin/Debug/netcoreapp1.1/Avalonia**.dll ~/.nuget/packages/avalonia/$1/lib/netstandard1.1/
 cp ../samples/ControlCatalog.NetCore/bin/Debug/netcoreapp1.1/Avalonia**.dll ~/.nuget/packages/avalonia.gtk3/$1/lib/netstandard1.1/
 cp ../samples/ControlCatalog.NetCore/bin/Debug/netcoreapp1.1/Avalonia**.dll ~/.nuget/packages/avalonia.skia.desktop/$1/lib/netstandard1.3/
 
