project(
    'avalonia.skia',
    ['c','cpp']
)

sk_dep = dependency('skia', static: true)

cpp = meson.get_compiler('cpp')
version_script = 'libavalonia.skia.version'
vflag = '-Wl,--version-script,@0@/@1@'.format(meson.current_source_dir(), version_script)

avalonia_skia_lib = shared_library(
   'avalonia.skia',
   ['avalonia.skia.cpp'],
   dependencies: [sk_dep],
   link_args: vflag,
   install: true,
)

testprog = executable(
    'avalonia.skia.testprog',
    ['testprog.c'],
    link_with: avalonia_skia_lib,
    install: true
)
