all: shaderdemo

shaderdemo: main.c
	gcc main.c -o build/shaderdemo -lraylib

run: shaderdemo
	build/shaderdemo