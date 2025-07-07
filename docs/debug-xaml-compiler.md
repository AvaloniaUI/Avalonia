# Debugging the XAML Compiler

The Avalonia XAML compiler can be debugged by setting an MSBuild property which triggers a debugger launch during compilation. This allows you to step through the XAML compilation process and troubleshoot issues.

To enable XAML compiler debugging, set the `AvaloniaXamlIlDebuggerLaunch` MSBuild property to `true` in your project file:

```xml
<PropertyGroup>
  <AvaloniaXamlIlDebuggerLaunch>true</AvaloniaXamlIlDebuggerLaunch>
</PropertyGroup>
```

When this property is enabled, the XAML compiler will call `Debugger.Launch()` on startup, which prompts you to attach a debugger to the compiler process.

If you're working with the Sandbox project in the Avalonia repository, you can enable debugging by simply uncommenting the property line in the [project file](https://github.com/AvaloniaUI/Avalonia/blob/56d94d64b9aa6f16200be39b3bcb17f03325b7f9/samples/Sandbox/Sandbox.csproj#L8).
