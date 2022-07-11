cmake_minimum_required(VERSION 3.20)
project(avalonia.skia)

set(CMAKE_EXPORT_COMPILE_COMMANDS on)

find_package(OpenGL REQUIRED)

if(NOT PKGROOT)
    set(PKGROOT ${CMAKE_SOURCE_DIR}/pre-built)
endif()

include(${PKGROOT}/skia.cmake)

add_library(avalonia.skia SHARED)

target_sources(avalonia.skia 
    PRIVATE
        src/factory.cpp
        src/AvgGlGpu.cpp
        src/AvgPath.cpp
        src/AvgDrawingContext.cpp
        src/AvgString.cpp
        src/AvgFontManager.cpp
        src/AvgGlyphRun.cpp
        src/AvgGlyphTypeface.cpp
        src/AvgFontShapeBuffer.cpp
        src/AvgStream.cpp
        src/AvgImage.cpp
)

target_include_directories(avalonia.skia PRIVATE ${PKGROOT}/${SKIA_PREFIX}/include/skia)
target_include_directories(avalonia.skia PRIVATE ${PKGROOT}/${SKIA_PREFIX}/include/harfbuzz)
target_include_directories(avalonia.skia PRIVATE include)


set_target_properties(avalonia.skia
    PROPERTIES
        CXX_STANDARD 17
)

target_link_directories(avalonia.skia PRIVATE ${PKGROOT}/${SKIA_LIBDIR})
target_link_libraries(avalonia.skia PRIVATE skia)
target_link_libraries(avalonia.skia PRIVATE ${PKGROOT}/${SKIA_LIBDIR}/libharfbuzz.a)

if (!WINDOWS)
target_link_libraries(avalonia.skia PRIVATE fontconfig)
endif()

link_directories(${OPENGL_LIBRARY_DIRS})
target_link_libraries(avalonia.skia PRIVATE ${OPENGL_LIBRARIES})

target_compile_definitions(avalonia.skia PRIVATE ${SKIA_COMPILE_DEFINES})

if (!WINDOWS)
target_link_options(avalonia.skia PRIVATE "-Wl,--version-script=${CMAKE_SOURCE_DIR}/libavalonia.skia.version")
target_link_options(avalonia.skia PRIVATE "-static-libgcc")
target_link_options(avalonia.skia PRIVATE "-static-libstdc++")

target_compile_options(avalonia.skia
   PRIVATE
       "-static-libstdc++"
       "-static-libgcc"
       "-Wno-subobject-linkage"
)
else()
    set_property(TARGET avalonia.skia PROPERTY MSVC_RUNTIME_LIBRARY "MultiThreaded")

    add_compile_options(
            $<$<CONFIG:>:/MT>
            $<$<CONFIG:Debug>:/MTd>
            $<$<CONFIG:Release>:/MT>
    )
endif()

install(TARGETS avalonia.skia)
