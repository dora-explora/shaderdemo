**This project is contained entirely within one shader.**

It's a 1920x1080 demo scene with rain, lightning, fire, smoke, and more!

<img style="width: 50%" src="screenshot.png"/>

## Installation and Usage

If you're on x64 Linux, there's an archive with a binary and assets available in the latest release.

Otherwise, install Raylib from their [github](https://github.com/raysan5/raylib/releases/latest) or their [website](https://raylib.com).
Then, clone the repository with `git`, and run `make run` in the repository to begin!

Once you're in, move the mouse around and see the rain interact with it!

## Design

This entire project was made within a **GLSL shader**, which is absolutely stateless. This means that, other than the frame counter, there is no concept of time absolutely no memory from one frame to the next. However, using deterministic random functions, I could make the illusion of randomness.

Since this is a shader, that also means that every pixel runs the exact same program every frame. The only difference is the parameters: the pixels position, the mouses position, the current frame, and a few textures. Through lots and lots of branching, each pixel independently chooses which color to be each frame, making an illusion of objects, motion, and general continuity where there is none.

GLSL provides no random functions, so every "random" object and color in the scene is actually noise made from floating point inaccuracies. I got this concept from [the book of shaders](https://thebookofshaders.com/10/), a great resource if you want to learn shader programming.
