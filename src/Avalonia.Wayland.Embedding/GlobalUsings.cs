// The NWayland generator emits client-proxy bindings whose base type (WlProxy) and the
// IWlTargetQueue parameter live in the *root* NWayland namespace and are referenced unqualified.
// Standard protocols compile because they are generated under NWayland.Protocols.*, so C# name
// resolution walks up to NWayland; our custom protocol lives under
// Avalonia.Wayland.Embedding.Protocol, which isn't nested under NWayland — so make it global.
global using NWayland;
