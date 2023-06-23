You can debug the XAML compiler by running `Avalonia.Build.Tasks` and passing the `original.dll` file created in the `obj` directory when compiling the project normally. For example, to run the XAML compiler on Sandbox the `launchsettings.json` file would look like:

```
{
  "profiles": {
    "Avalonia.Build.Tasks": {
      "commandName": "Project",
      "executablePath": "$(SolutionDir)\\src\\Avalonia.Build.Tasks\\bin\\Debug\\netstandard2.0\\Avalonia.Build.Tasks.exe",
      "commandLineArgs": "$(SolutionDir)\\samples\\Sandbox\\obj\\Debug\\net6.0\\Avalonia\\original.dll $(SolutionDir)\\samples\\Sandbox\\bin\\Debug\\net6.0\\Sandbox.dll.refs $(SolutionDir)\\out.dll"
    }
  }
}

```