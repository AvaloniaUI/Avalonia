Since MonoDevelop doesn't support .NET Core at all, Visual Studio for Mac can't even load our solution and VSCode has dozens of issues with our code base, one has only two options of building and debugging AvaloniaUI on *nix machines. One is to install Rider and unload all non-netcore/netstandard projects and run tests from console. That will cost you ~$100+ for Rider license and leave you without any means of checking if every project in solution can be built.

Another option is to use Visual Studio on Windows machine, sync your sources to Linux/OSX one, build and run there and then attach remote debugger. 

## Syncing the source code

You need to be able to login to your *nix machine through SSH under *root* account. Make sure that you have root login enabled, you can check that by doing `ssh root@localhost` on your nix machine. You need to either enable root password logon or use key auth. 



### Linux

You need to install SSH server (`sudo apt-get install openssh-server` on Debian/Ubuntu) and enable root login by changing `/etc/ssh/sshd_config` file. You need to set `PermitRootLogin` to `yes` and set root password via `sudo passwd`. Alternatively you can use public key auth, which will be more secure.

### OSX

Follow instructions [here](https://support.apple.com/en-us/HT204012) to enable root user.

Enable SSH server:

![sharing](https://i.imgur.com/NfpstPD.png)
![remote login](https://i.imgur.com/bK83oFC.png)


### On  Windows machine

On Windows you need [WinSCP](https://winscp.net/download/WinSCP-5.11.2-Setup.exe)

Connect to your Linux/Mac and select some directory your sources will be uploaded to.
On the left tab navigate to the directory with your sources on the windows machine.

Open `Commands`->`Keep remote directory up to date`
Go to transfer settings and set `File mask` to `*.cs; *.csproj; *.xaml; *.props; *.targets; *.projitems; *.png; *.jpg; *.ico | bin/; obj/; TestFiles/; .git/; tools/; artifacts/; .vs/`

![transfer settings](https://i.imgur.com/V14WyY0.png)

![settings](https://i.imgur.com/DoHhjSA.png)

Click `Start` and wait for initial synchronization to complete. Keep that window open. Any changes you save from Visual Studio will be automatically synchronized to your Mac/Linux machine.

## Running and debugging

Open terminal and go to `samples/ControlCatalog.NetCore` directory, and run `dotnet restore`.

You can use `dotnet run --wait-for-attach` to run our control catalog, which will wait for you to attach debugger. If you want to do the same with your own app, you need to have something like that in your `Program.cs`:

```csharp
if (args.Contains("--wait-for-attach"))
  {
    Console.WriteLine("Attach debugger and use 'Set next statement'");
    while (true)
    {
    Thread.Sleep(100);
    if (Debugger.IsAttached)
      break;
  }
}
```

Then in Visual Studio go to `Debug`->`Attach to process...` and select `SSH` as connection type. Then set up your connection target. Make sure to login as `root`, the select the process to debug (you need `dotnet exec` one:

![debugger](https://i.imgur.com/kTXYONd.png)