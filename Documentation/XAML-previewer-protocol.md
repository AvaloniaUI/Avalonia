XAML previewer is a standalone executable file shipped with Avalonia's NuGet package. It communicates with the IDE process via a tcp socket and supports various ways of presenting the rendered XAML

## Resolving previewer paths

The path could be retrieved from `AvaloniaPreviewerNetCoreToolPath` and `AvaloniaPreviewerNetFullToolPath` MSBuild properties set by `Avalonia` NuGet package.

## Running the previewer process 

Previewer requires the final app executable that has `BuildAvaloniaApp` in the class that contains the application entry point and some information required to load app dependencies. 
Let's say that said executable is named `App.dll` (.NET Core) or `App.exe` (.NET Framework).

### .NET Core:

IDE is required to run previewer with depsfile and runtimeconfig from the final app executable. Previewer should be run with app build output directory set as the current directory.

```
dotnet exec --depsfile ./App.deps.json --runtimeconfig ./App.runtimeconfig.json $(AvaloniaPreviewerNetCoreToolPath)
```

### .NET Framework

Just run the executable from `AvaloniaPreviewerNetFullToolPath` msbuild property with the application build output directory set as the current directory.

## Configuring previewer modes

If you went through the previous steps, the previewer should now print something like:
```
Usage: --transport transport_spec --session-id sid --method method app

Example: --transport tcp-bson://127.0.0.1:30243/ --session-id 123 --method avalonia-remote MyApp.exe

```


`--transport` allows you to configure the messaging transport. Currently only TCP transport with BSON-serialized messages is supported. The IDE process is supposed to listen on some port and pass `tcp-bson://{host}:{port}/` uri to the previewer

`--session-id` allows you to distinguish between previewer instances that are connecting to the same listening socket on the IDE side. Specifying that argument will make the previewer to send you session id as a part of the initial message set

`--method` sets the preview presentation method.
- `win32` previewer will send you an HWND it renders its contents to
- `avalonia-remote` previewer will act as Avalonia Remote widget protocol server and will send rendered frames using the same connection to the IDE
- `html` previewer start an http server on address specified by `--html-url` that would display rendered XAML to any browser that's compatible with websockets and HTML5 canvas

`app` - the path to the final app executable (e. g. `App.dll` (.NET Core) or `App.exe` (.NET Framework)). Should be after  other parameters


Example previewer invocation:
```bash
~/Projects/ControlCatalogStandalone/ControlCatalog.NetCore/bin/Debug/netcoreapp3.0$ dotnet exec --depsfile ControlCatalog.NetCore.deps.json --runtimeconfig ControlCatalog.NetCore.runtimeconfig.json  ~/.nuget/packages/avalonia/0.9.0/tools/netcoreapp2.0/designer/Avalonia.Designer.HostApp.dll --method avalonia-remote --transport tcp-bson://127.0.0.1:26434 ControlCatalog.NetCore.dll 
```

## `tcp-bson` wire protocol

| Payload Length (N) | Message type GUID | Payload |
|-|-|-|
| 4 bytes little endian | 16 bytes (binary representation of System.Guid) | N bytes|

Messages are encoded in BSON and are defined in:
- https://github.com/AvaloniaUI/Avalonia/blob/master/src/Avalonia.Remote.Protocol/ViewportMessages.cs
- https://github.com/AvaloniaUI/Avalonia/blob/master/src/Avalonia.Remote.Protocol/InputMessages.cs
- https://github.com/AvaloniaUI/Avalonia/blob/master/src/Avalonia.Remote.Protocol/DesignMessages.cs

A NuGet package [Avalonia.Remote.Protocol](https://www.nuget.org/packages/Avalonia.Remote.Protocol/) contains a reference implementation in `BsonTcpTransport` class.

## Protocol workflow

### Initial handshake

Previewer connects to the IDE and sends `StartDesignerSessionMessage` with session id from `--session-id` command line argument.

With `avalonia-remote` transport IDE sends ClientSupportedPixelFormatsMessage with supported pixel formats and `ClientRenderInfoMessage` with DPI settings. IDE is expected to send `ClientRenderInfoMessage` on DPI change. IDE should not send those messages with transports other than `avalonia-remote`.

### Sending XAML updates

IDE sends `UpdateXamlMessage` with:
- `Xaml` - Full XAML text
- `XamlFileProjectPath` - path to the XAML file in the project leading `/` and with `/` as path separator char. E. g. `/MyDir/MyFile.xaml`. Is required for `IUriContext.BaseUri`.
- `AssemblyPath` - the full path of the assembly that was generated from the project that contains the XAML file in the previewed application path directory

Previewer responds with a `UpdateXamlResultMessage`. If `win32` transport is used and XAML is loaded successfully, `Handle` property will contain an HWND.

If XAML is loaded successfully, previewer will update the rendered image using the current method

## Methods

### `win32`

Previewer will send HWND with UpdateXamlResultMessage, IDE is required to reparent that HWND to its own window

### `html`

IDE is required to pass an local url with a free port number in `--html-url` argument. IDE is expected to feed said url to a WebView or a standalone browser of IDE's choice

### `avalonia-remote`

IDE is required to send `ClientSupportedPixelFormatsMessage` and `ClientRenderInfoMessage` as a part of the handshake.

Previewer will send rendered XAML in `FrameMessage`s. IDE is required to respond with `FrameReceivedMessage` with the last `SequenceId` from a `FrameMessage` it has received.

If IDE wishes to allow mouse and keyboard interaction with the previewer, IDE may opt to send messages defined in https://github.com/AvaloniaUI/Avalonia/blob/master/src/Avalonia.Remote.Protocol/InputMessages.cs