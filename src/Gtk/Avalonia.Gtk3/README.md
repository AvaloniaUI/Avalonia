P/Invoke based GTK3 backend
===========================

Code is EXPERIMENTAL at this point. It also needs Direct2D/Skia for rendering.

Windows GTK3 binaries aren't included in the repo, you need to download them from https://sourceforge.net/projects/gtk3win/

On Linux it should work out of the box with system-provided GTK3. On OSX you should be able to wire GTK3 using DYLD_LIBRARY_PATH environment variable.