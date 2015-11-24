#!/bin/sh
rm -rf native
mkdir -p native
cd native
wget `cat ../native.url` -O native.zip
unzip native.zip

