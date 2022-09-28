#include <emscripten.h>

void av_log(int flags, const char* format) {
    emscripten_log(flags, format);
}

void av_debugger() {
    emscripten_debugger();
}
