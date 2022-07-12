# Build Environment
You will need to install cmake to build the native part.
https://cmake.org/download/

install the latest llvm / clang from here
https://github.com/llvm/llvm-project/releases/tag/llvmorg-14.0.5

Its recommended to use CLion


## Build

Download the latest pre-built libskia from:
https://github.com/AvaloniaUI/skiabuild/releases
(Choose avalonia.skia-v0.3.4-linux-x86_64-linux-gnu-sysroot.tar.gz)

Copy this to:

```
mkdir -p libavalonia.skia/pre-built/
cp ~/Downloads/avalonia.skia-v0.3.4-linux-x86_64-linux-gnu-sysroot.tar.gz libavalonia.skia/pre-built/
cd libavalonia.skia/pre-built
tar xvf ./avalonia.skia-v0.3.4-linux-x86_64-linux-gnu-sysroot.tar.gz
```

Ensure that extracted folder structure is `pre-built/usr/local` ...

```
cd native
mkdir build
cd build
CC=clang-14 CXX=clang++-14 cmake -GNinja -DCMAKE_BUILD_TYPE=Release -DCMAKE_EXPORT_COMPILE_COMMANDS=ON ..
ninja
```

```note
During development, the ControlCatalog expects to find libavalonia.skia.so or avalonia.skia.dll under:
native/build.

To change this, edit Program.cs in ControlCatalog.NetCore
```

### Windows

To get a working build ensure:

  * Compiler is clang for MSVC runtime.
  * Not using Mingw32.
  * Currently compiling for 64-bit.

```cmd
cmake -GNinja "-DCMAKE_C_COMPILER=C:/Program Files/LLVM/bin/clang-cl.exe" "-DCMAKE_CXX_COMPILER=C:/Program Files/LLVM/bin/clang-cl.exe" -DCMAKE_C_FLAGS=-m64 -DCMAKE_CXX_FLAGS=-m64 ..
```

### Wasm

```
emcmake cmake -GNinja -DCMAKE_BUILD_TYPE=Release ..
emmake ninja
```
