# !/bin/bash

cd "$(dirname "$0")"

tests=(Avalonia.*.UnitTests/)
exclude=("*Direct2D*/")
result=0

for del in ${exclude[@]}; do
    tests=(${tests[@]/$del})
done

for test in ${tests[@]}; do
    echo Running test $test
    mono ../testrunner/xunit.runner.console.2.1.0/tools/xunit.console.exe ${test}bin/Release/${test%/}.dll  -parallel none

    if [ $? -ne 0 ]; then result=1 ; fi
done

exit $result
