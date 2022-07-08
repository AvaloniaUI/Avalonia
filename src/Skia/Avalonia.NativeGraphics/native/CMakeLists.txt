cmake_minimum_required(VERSION 3.20)
project(avalonia.skia)
set (CMAKE_CXX_STANDARD 17)
find_package(PkgConfig)

if(NOT PKGROOT)
    set(PKGROOT ${CMAKE_SOURCE_DIR}/pre-built)
endif()

set(ENV{PKG_CONFIG_PATH} ${PKGROOT}/usr/local/lib64/pkgconfig)
set(ENV{PKG_CONFIG_LIBDIR} ${PKGROOT}/usr/lib/pkgconfig:${PKGROOT}/usr/share/pkgconfig)
set(ENV{PKG_CONFIG_SYSROOT_DIR} ${PKGROOT})

pkg_check_modules(PKG_SKIA REQUIRED IMPORTED_TARGET skia)

add_library(avalonia.skia SHARED com.h comimpl.h factory.cpp AvgGlGpu.cpp AvgGlGpu.h AvgDrawingContext.cpp AvgDrawingContext.h)
target_include_directories(avalonia.skia PRIVATE ${PKG_SKIA_INCLUDE_DIRS})
target_link_libraries(avalonia.skia PRIVATE PkgConfig::PKG_SKIA)
target_link_options(avalonia.skia PRIVATE "-Wl,--version-script=${CMAKE_SOURCE_DIR}/libavalonia.skia.version")
target_link_options(avalonia.skia PRIVATE "-static-libgcc")
target_link_options(avalonia.skia PRIVATE "-static-libstdc++")
target_compile_options(avalonia.skia PRIVATE "-static-libstdc++")
target_compile_options(avalonia.skia PRIVATE "-static-libgcc")
target_compile_definitions(avalonia.skia PUBLIC SK_GL)

install(TARGETS avalonia.skia)
