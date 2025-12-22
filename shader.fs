#version 330

in vec2 fragTexCoord;
in vec4 fragColor;

out vec4 color;

uniform float time;
uniform float seed;
uniform int frame;

uniform sampler2D bg;
uniform int heights[1920];

float rand(in float seed) {
    seed = fract(seed * 443.897);
    seed *= seed + 33.33;
    seed *= seed + seed;
    return fract(seed);
}

float sinrand(in float seed) {
    return fract(sin(seed) * 43758.5453);
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
const float RAIN_CHANCE = 0.003;
const int RAIN_LENGTH = 20;
const int SPLATTER_LENGTH = 20;
const int RAIN_SPEED = 20;

vec2 shakerand(in float seed) {
    vec2 shake;
    shake.y = rand(seed + 1.) * SHAKE_STRENGTH * (brand(seed + 1.) ? -1. : 1.);
    shake.y = float(int(shake.y * 1080.)) / 1080.; 
    shake.x = rand(seed) * SHAKE_STRENGTH * (brand(seed) ? -1. : 1.);
    shake.x = float(int(shake.x * 1920.)) / 1920.; 
    return shake;
}

int floorheight(in float x) {
    return 1000;
}

bool rainrand(in float x, in int seed) { // random chance for rain
    if (sinrand(rand(sinrand(x + float(seed) * 0.76521)) + 0.5) < RAIN_CHANCE) { // this took me so god damn long
        return true;
    } else {
        return false;
    }
}

float rain(in vec2 pos, in int frame) {
    int height = floorheight(pos.x);
    if (pos.y * 1080 > height) {
        return 0.;
    }
    for (int i = 0; i < RAIN_LENGTH; i++) {
        if (rainrand(pos.x, (frame * RAIN_SPEED - int(pos.y * 1080.) - i) % 1000)) {
            return float(RAIN_LENGTH - i) / RAIN_LENGTH;
        }
    }
    if (height - int(pos.y * 1080.) < SPLATTER_LENGTH) {
        for (int i = 0; i < SPLATTER_LENGTH; i++) {
            int depth = height * 2 - int(pos.y * 1080.) - i;
            int seed = ((frame - SPLATTER_LENGTH) * RAIN_SPEED - depth * 5) % 1000;
            float offset = float(depth - height)/1920. + float(i)/1920.;
            float angled = tan(radians(45));
            if (rainrand(pos.x + offset * angled, seed) || rainrand(pos.x - offset * angled, seed)) {
                return float(i) / SPLATTER_LENGTH;
            }
        }
    }
    return 0.;
}

void main() {
    vec2 pos = fragTexCoord;

    // pos += shakerand(seed); // screen shake
    // if (pos.y > 1. || pos.y < 0.) { // if pos.y is offscreen,
    //     color = vec4(0., 0., 0., 1.); // make the color black
    //     return;
    // }

    color = texture(bg, pos);

    float rain_strength = rain(pos, frame);
    color = mix(color, vec4(0.6, 0.8, 1., 1.), rain_strength);
}