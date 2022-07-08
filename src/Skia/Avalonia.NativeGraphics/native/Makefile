BASE:=$(realpath $(dir $(lastword $(MAKEFILE_LIST))))
BUILD:=$(BASE)/build


.PHONY: all
all: libavalonia.skia.so

$(BUILD)/build.ninja:
	mkdir -p $(BUILD)
	cd $(BUILD) && cmake -GNinja $(BASE)


.PHONY: libavalonia.skia.so
libavalonia.skia.so: $(BUILD)/build.ninja
	@cd $(BUILD) && ninja

.PHONY: clean
clean:
	cd build && ninja clean

.PHONY: format
format:
	find .  -iname "*.c" -o -iname "*.h" -o -iname "*.cpp" | grep -v '^./pre-built' | grep -v '^./build' | grep -v '^./include/interop.h' | xargs clang-format -i
