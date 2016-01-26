#!/bin/sh
rm -rf native native.zip
mkdir -p native
cd native
if which curl
then
curl `cat ../native.url` -o native.zip
else
wget `cat ../native.url` -O native.zip
fi

unzip native.zip
chmod -R +x .
