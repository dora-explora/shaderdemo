#include <raylib.h>
#include <stdlib.h>

int main(void) {
    const int screenWidth = 1920;
    const int screenHeight = 1080;

    InitWindow(screenWidth, screenHeight, "Shader Demo");

    Shader shader = LoadShader(0, "./shader.fs");
    
    int timeLoc = GetShaderLocation(shader, "time");
    int seedLoc = GetShaderLocation(shader, "seed");
    float time = 0;
    float screenSize[2] = { (float) GetScreenWidth(), (float) GetScreenHeight() };
    SetShaderValue(shader, GetShaderLocation(shader, "size"), &screenSize, SHADER_UNIFORM_VEC2);

    Texture2D bgTexture = LoadTexture("assets/rainworld.jpg");

    SetTargetFPS(60);

    while (!WindowShouldClose())
    {
        time += GetFrameTime();
        SetShaderValue(shader, timeLoc, &time, SHADER_UNIFORM_FLOAT);
        float seed = (float) rand() / RAND_MAX;
        SetShaderValue(shader, seedLoc, &seed, SHADER_UNIFORM_FLOAT);
        // TODO: Update your variables here
        
        BeginDrawing();
        
        ClearBackground(RAYWHITE);

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