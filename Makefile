all: shaderdemo

shaderdemo: main.c
	gcc main.c -o build/shaderdemo -lraylib

run: shaderdemo
	build/shaderdemo
	
static:
		gcc main.c -o build/shaderdemo -Ibuild/raylib lib/libraylib.a -lm -ldl -lpthread -lrt -lX11 -lXrandr -lXinerama -lXxf86vm -lXcursor -lXfixes -lfreetype -lGL