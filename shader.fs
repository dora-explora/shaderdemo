#version 330

in vec2 fragTexCoord;
in vec4 fragColor;

out vec4 finalColor;

uniform vec2 size;
uniform float time;

uniform sampler2D texture0;

float random (in float seed) {
    return fract(sin(seed) * 43758.5453123);
}

bool brandom (in float seed) {
    if (random(seed) < 0.5) {
        return true;
    } else {
        return false;
    }
}

const float SHAKE_STRENGTH = 50.;
const float RAIN_STRENGTH = 20.;

void main() {
    vec2 pos = fragTexCoord;
    pos.y -= random(pos.x * 1000. + time)/RAIN_STRENGTH;
    pos.x += random(time)/SHAKE_STRENGTH * (brandom(time) ? -1. : 1.);
    pos.y += random(time + 1.)/SHAKE_STRENGTH * (brandom(time + 1.) ? -1. : 1.);
    if (pos.y > 1. || pos.y < 0.) {
        finalColor = vec4(0., 0., 0., 1.);
    } else {
        finalColor = texture(texture0, pos);
    }
}