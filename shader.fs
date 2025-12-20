#version 330

in vec2 fragTexCoord;
in vec4 fragColor;

out vec4 color;

uniform vec2 size;
uniform float time;
uniform float seed;
uniform int frame;

uniform sampler2D bg;

float rand (in float seed) {
    seed = fract(seed * .1031);
    seed *= seed + 33.33;
    seed *= seed + seed;
    return fract(seed);
}

bool brand (in float seed) {
    if (rand(seed) < 0.5) {
        return true;
    } else {
        return false;
    }
}

const float SHAKE_STRENGTH = .006;
const float RAIN_STRENGTH = .03;
const float RAIN_CHANCE = 0.0006;
const float RAIN_LENGTH = 0.05;
const float RAIN_SPEED = 20.;

bool rainrand(in float x, in float seed) { // random chance for rain
    if (rand(x * 1920. + seed) < RAIN_CHANCE) {
        return true;
    } else {
        return false;
    }
}

vec2 shakerand(in float seed) {
    vec2 shake;
    shake.y = rand(seed + 1.) * SHAKE_STRENGTH * (brand(seed + 1.) ? -1. : 1.);
    shake.x = rand(seed) * SHAKE_STRENGTH * (brand(seed) ? -1. : 1.);
    return shake;
}

float rain(in vec2 pos, in int frame, in float height) {
    for (float i = 0; i < RAIN_LENGTH; i += 1 / height) {
        if (rainrand(pos.x, frame * RAIN_SPEED / height - pos.y - i)) {
            return (RAIN_LENGTH - i) / RAIN_LENGTH;
        }
    }
    return 0.;
}

void main() {
    vec2 pos = fragTexCoord;

    float rain_strength = rain(pos, frame, size.y);
    
    // pos += shakerand(seed); // screen shake
    // if (pos.y > 1. || pos.y < 0.) { // if pos.y is offscreen,
    //     color = vec4(0., 0., 0., 1.); // make the color black
    //     return;
    // }


    color = texture(bg, pos);

    color = mix(color, vec4(0.6, 0.8, 1., 1.), rain_strength);
}