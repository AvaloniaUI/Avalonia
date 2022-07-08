# Build

Download the latest pre-built libskia from:
https://github.com/AvaloniaUI/skiabuild/releases

(Choose avalonia.skia-v0.3.0-linux-x86_64-linux-gnu-sysroot.tar.gz)

Copy this to:

```
mkdir -p libavalonia.skia/pre-built/
cp ~/Downloads/avalonia.skia-v0.3.0-linux-x86_64-linux-gnu-sysroot.tar.gz libavalonia.skia/pre-built/
cd libavalonia.skia/pre-built
tar xvf ./avalonia.skia-v0.3.0-linux-x86_64-linux-gnu-sysroot.tar.gz
```

Then build as usual:

```
cd libavalonia.skia
mkdir build
cd build

cmake -GNinja ..
ninja

```


This currently creates a test-program:

```
./avalonia.skia.testprog

ls -la

-rw-r--r-- 1 james james    10929 May  3 19:28 skia-c-example.png  

md5sum ./skia-c-example.png
8cb81a6c6ad6af7e01b274b879ca012c  ./skia-c-example.png  

```
