P/Invoke based GTK3 backend
===========================

Code is EXPERIMENTAL at this point. It also needs Direct2D/Skia for rendering.

Windows GTK3 binaries aren't included in the repo, you need to download them from http://www.tarnyko.net/repo/gtk3_build_system/gtk+-bundle_3.4.2-20130513_win32.zip
Then you need to extract them somewhere and add `bin` directory to PATH. Support for specifying exact path to binaries will be implemented later.

On Linux it should work out of the box with system-provided GTK3. On OSX you should be able to wire GTK3 using DYLD_LIBRARY_PATH environment variable.