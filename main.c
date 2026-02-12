#include <raylib.h>

int main(void) {
    SetTraceLogLevel(LOG_WARNING);
    InitWindow(1920, 1080, "Shader Demo");

    Shader shader = LoadShader(0, "./shader.fs");

    int frame = 0;
    int frameLoc = GetShaderLocation(shader, "frame");
    Vector2 mouse;
    int mouseLoc = GetShaderLocation(shader, "mouse");
    Texture2D rockTexture = LoadTexture("assets/rock.png");
    int rockLoc = GetShaderLocation(shader, "rock");
    Texture2D platformTexture = LoadTexture("assets/platform.png");
    int platformLoc = GetShaderLocation(shader, "platform");

    Texture2D bgTexture = LoadTexture("assets/bg.png");

    SetTargetFPS(60);

    while (!WindowShouldClose())
    {
        frame++;
        SetShaderValue(shader, frameLoc, &frame, SHADER_UNIFORM_INT);
        mouse = GetMousePosition();
        SetShaderValue(shader, mouseLoc, &mouse, SHADER_UNIFORM_VEC2);

        BeginDrawing();

        BeginShaderMode(shader);
        SetShaderValueTexture(shader, rockLoc, rockTexture);
        SetShaderValueTexture(shader, platformLoc, platformTexture);
        DrawTexture(bgTexture, 0, 0, WHITE);
        EndShaderMode();

        // DrawFPS(10, 10);
        EndDrawing();
    }

    UnloadShader(shader);
    UnloadTexture(bgTexture);
    UnloadTexture(rockTexture);
    UnloadTexture(platformTexture);

    CloseWindow();
    return 0;
}
