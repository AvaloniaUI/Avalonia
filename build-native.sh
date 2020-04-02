# /bin/sh

mkdir native-build
cd native-build
cmake -DCMAKE_BUILD_TYPE=$1 ../native
cmake --build . --target install