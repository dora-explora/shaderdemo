#include <raylib.h>

int main(void) {
    const int screenWidth = 1920;
    const int screenHeight = 1080;

    InitWindow(screenWidth, screenHeight, "Shader Demo");

    Shader shader = LoadShader(0, "./shader.fs");
    float screenSize[2] = { (float) GetScreenWidth(), (float) GetScreenHeight() };
    SetShaderValue(shader, GetShaderLocation(shader, "size"), &screenSize, SHADER_UNIFORM_VEC2);

    // make sure to add the background, middle, and foreground textures (once drawn/given placeholders)
    Image imBlank = GenImageColor(screenWidth, screenHeight, BLANK);
    Texture2D blankTexture = LoadTextureFromImage(imBlank);
    UnloadImage(imBlank);

    SetTargetFPS(60);

    while (!WindowShouldClose())
    {
        // TODO: Update your variables here
        
        BeginDrawing();
        
        ClearBackground(RAYWHITE);

        BeginShaderMode(shader);
        DrawTexture(blankTexture, 0, 0, WHITE);
        EndShaderMode();
                    
        DrawFPS(10, 10);
        EndDrawing();
    }

    UnloadShader(shader);
    UnloadTexture(blankTexture);

    CloseWindow();
    return 0;
}