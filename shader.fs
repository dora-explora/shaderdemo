#version 330

in vec2 fragTexCoord;
in vec4 fragColor;

out vec4 finalColor;

uniform vec2 size;
uniform float time;
uniform float seed;

uniform sampler2D texture0;

float random (in float seed) {
    seed = fract(seed * .1031);
    seed *= seed + 33.33;
    seed *= seed + seed;
    return fract(seed);
}

bool brandom (in float seed) {
    if (random(seed) < 0.5) {
        return true;
    } else {
        return false;
    }
}

const float SHAKE_STRENGTH = 50.;
const float RAIN_STRENGTH = 30.;
const float RAIN_CHANCE = 0.06;
const float RAIN_WIDTH = 0.005;

bool rainchancerand(in float x, in float seed) { // random chance for rain
    if (random(x * 123.456789 + seed) < RAIN_CHANCE) {
        return true;
    } else {
        return false;
    }
}

float rainstrengthrand(in float x, in float seed) { // random strength for rain
    return random(x * 83.4628734 + seed) / RAIN_STRENGTH;
}

float rainrand(in float x, in float seed) {
    for (float i = 0.; i < RAIN_WIDTH; i+= 1/size.x) {
        if (rainchancerand(x + i, seed)) {
            return rainstrengthrand(x + i, seed) * ((RAIN_WIDTH - i) / RAIN_WIDTH);
        } else if (rainchancerand(x - i, seed)) {
            return rainstrengthrand(x - i, seed) * ((RAIN_WIDTH - i) / RAIN_WIDTH);
        }
    }
    return 0.;
}

vec2 shakerand(in float seed) {
    vec2 shake;
    shake.x = random(seed) / SHAKE_STRENGTH * (brandom(seed) ? -1. : 1.);
    shake.y = random(seed + 1.) / SHAKE_STRENGTH * (brandom(seed + 1.) ? -1. : 1.);
    return shake;
}

void main() {
    vec2 pos = fragTexCoord;
    
    pos.y -= rainrand(pos.x, seed); // rain
    pos += shakerand(seed); // screen shake

    if (pos.y > 1. || pos.y < 0.) { // if pos.y is offscreen,
        finalColor = vec4(0., 0., 0., 1.); // make the color black
        return;
    }
    
    vec4 rawColor = texture(texture0, pos);

    finalColor = rawColor;
}