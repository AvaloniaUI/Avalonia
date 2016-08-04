# !/bin/bash

cd "$(dirname "$0")"

tests=(Avalonia.*.UnitTests/)
result=0

for test in "${tests[@]}"; do
    mono ../testrunner/xunit.runner.console.2.1.0/tools/xunit.console.exe ${test}bin/Release/${test%/}.dll  -parallel none

    if [ $? -ne 0 ]; then result=1 ; fi
done

exit $result
