#include <raylib.h>
#include <stdlib.h>

int main(void) {
    InitWindow(1920, 1080, "Shader Demo");

    Shader shader = LoadShader(0, "./shader.fs");
    
    int timeLoc = GetShaderLocation(shader, "time");
    float time = 0;
    int seedLoc = GetShaderLocation(shader, "seed");
    int frameLoc = GetShaderLocation(shader, "frame");
    int frame = 0;

    Texture2D bgTexture = LoadTexture("assets/bg.png");

    SetTargetFPS(60);

    while (!WindowShouldClose())
    {
        time += GetFrameTime();
        SetShaderValue(shader, timeLoc, &time, SHADER_UNIFORM_FLOAT);
        frame++;
        SetShaderValue(shader, frameLoc, &frame, SHADER_UNIFORM_INT);
        float seed = (float) rand() / RAND_MAX;
        SetShaderValue(shader, seedLoc, &seed, SHADER_UNIFORM_FLOAT);
        
        BeginDrawing();

        BeginShaderMode(shader);
        DrawTexture(bgTexture, 0, 0, WHITE);
        EndShaderMode();
                    
        DrawFPS(10, 10);
        EndDrawing();
    }

    UnloadShader(shader);
    UnloadTexture(bgTexture);

    CloseWindow();
    return 0;
}