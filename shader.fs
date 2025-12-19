#version 330

in vec2 fragTexCoord;
in vec4 fragColor;

out vec4 finalColor;

uniform vec2 size;

void main() {
    finalColor = vec4(fragTexCoord.x, fragTexCoord.y, 0., 1.);
}