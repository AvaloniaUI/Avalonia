#!/usr/bin/make -f

all: vert.spirv frag.spirv
.PHONY: all

vert.spirv: vert.glsl
	glslc  -fshader-stage=vert vert.glsl -o vert.spirv

frag.spirv: frag.glsl
	glslc  -fshader-stage=frag frag.glsl -o frag.spirv


