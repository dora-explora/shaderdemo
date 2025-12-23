#version 330

in vec2 fragTexCoord;
in vec4 fragColor;

out vec4 color;

uniform float time;
uniform float seed;
uniform int frame;
uniform vec2 mouse;

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
const int SPLATTER_DISTANCE = 10;
const int SPLATTER_LENGTH = 5;
const int RAIN_SPEED = 20;
// const int RAIN_SPEED = 2;
const float MOUSE_RADIUS = 50.;

vec2 shakerand(in float seed) {
    vec2 shake;
    shake.y = rand(seed + 1.) * SHAKE_STRENGTH * (brand(seed + 1.) ? -1. : 1);
    shake.y = float(int(shake.y * 1080.)) / 1080.; 
    shake.x = rand(seed) * SHAKE_STRENGTH * (brand(seed) ? -1. : 1.);
    shake.x = float(int(shake.x * 1920.)) / 1920.; 
    return shake;
}

int floorheight(in int x) {
    int height = 1000;
    if (x < mouse.x + MOUSE_RADIUS && x > mouse.x - MOUSE_RADIUS) {
        int offset = x - int(mouse.x);
        int mouseheight = int(mouse.y + sqrt(MOUSE_RADIUS*MOUSE_RADIUS + offset*offset) - 2 * MOUSE_RADIUS);
        if (height > mouseheight) { height = mouseheight; }
    }
    return height;
}

float floorangle(in int x) {
    if (floorheight(x) < 1000) { // optimization since the map is simple
        float offset = x - mouse.x;
        return degrees(acos(offset / MOUSE_RADIUS)) - 90.;
    }
    return 0.;
}

bool rainrand(in int x, in int seed) { // random chance for rain
    if (sinrand(rand(sinrand(float(x) / 1920. + float(seed) * 0.76521)) + 0.5) < RAIN_CHANCE) { // this took me so god damn long
        return true;
    }
    return false;
}

ivec2 rainrotation(in int x) {
    float angle = floorangle(x);
    if (angle < -31.) {
        return ivec2(1, 4);
    } else if (angle < -24.) {
        return ivec2(1, 3);
    } else if (angle < -10.) {
        return ivec2(1, 2);
    } else if (angle < 10.) {
        return ivec2(1, 1);
    } else if (angle < 24.) {
        return ivec2(2, 1);
    } else if (angle < 31.) {
        return ivec2(3, 1);
    } else {
        return ivec2(4, 1);
    }
}

bool rain(in int x, in int y, in int frame) {
    int seed = int(frame * RAIN_SPEED - y) % 967;
    return rainrand(x, seed);
}

    // if (height - int(pos.y * 1080.) < SPLATTER_LENGTH) {
    //     for (int i = 0; i < SPLATTER_LENGTH; i++) {
    //         int depth = height * 2 - int(pos.y * 1080.) - i;
    //         int seed = ((frame - SPLATTER_LENGTH) * RAIN_SPEED - depth) % 1000;
    //         float offset = float(depth - height)/1920. + float(i)/1920.;
    //         float angle = floorangle(pos.x);
    //         float rotation = tan(radians(angle));
    //         rotation = clamp(rotation, 1./5., 5.);
    //         if (rainrand(pos.x + offset / rotation, seed) || rainrand(pos.x - offset * rotation, seed)) {
    //             // return float(i) / SPLATTER_LENGTH;
    //             return 0.5;
    //         }
    //     }
    // }
bool splatter(in ivec2 ipos, in int frame) {
    int bound = SPLATTER_DISTANCE * 3;
    for (int i = -bound; i < bound; i++) {
        if (i == 0 ) { continue; } // break on middle case

        int x = ipos.x + i; // x position of unobscured droplet

        int height = floorheight(x); // floor height of unobscured droplet's column
        if (distance(vec2(x, height), ipos) > SPLATTER_DISTANCE) { continue; } // if splatter would be too far, break

        ivec2 rotation = rainrotation(x); // floor rotation of unobscured droplets' column (ivec2 of simple ratio)
        int numerator;
        int denominator;
        if (i < 0) {
            numerator = rotation.x;
            denominator = rotation.y;
        } else {
            denominator = rotation.x;
            numerator = rotation.y;
        }

        if (ipos.y + abs(i) * numerator / denominator != height) { continue; } // if not at correct y level, break

        int y = height + abs(i) * RAIN_SPEED; // y position of unobscured droplet
        if (rain(x, y, frame)) {
            return true;
        }
    }
    return false;
}

float rainstrength(in ivec2 ipos, in int frame) {
    if (ipos.y > floorheight(ipos.x)) {
        return 0.;
    }
    for (int i = 0; i < RAIN_LENGTH; i++) {
        if (rain(ipos.x, ipos.y + i, frame)) {
            return float(RAIN_LENGTH - i) / RAIN_LENGTH;
        }
    }
    for (int i = 0; i < SPLATTER_LENGTH; i++) {
        if (splatter(ipos, frame - i * RAIN_SPEED)) {
            return float(SPLATTER_DISTANCE - i)/SPLATTER_DISTANCE;
        }
    }
    return 0.;
}

float mousestrength(in ivec2 ipos) {
    if (distance(ipos, mouse) < MOUSE_RADIUS) {
        return 1.;
    }
    return 0.;
}

void main() {
    vec2 pos = fragTexCoord;

    // pos += shakerand(seed); // screen shake
    // if (pos.y > 1. || pos.y < 0.) { // if pos.y is offscreen,
        // color = vec4(0., 0., 0., 1.); // make the color black
        // return;
    // }

    ivec2 ipos = ivec2(pos.x * 1920., pos.y * 1080.);

    color = texture(bg, pos);

    float mouse_strength = mousestrength(ipos);
    if (mouse_strength > 0.) {
        color = mix(color, vec4(0.5, 0.3, 0.3, 1.), mouse_strength);
    } else {
        float rain_strength = rainstrength(ipos, frame);
        color = mix(color, vec4(0.6, 0.8, 1., 1.), rain_strength);
    }

    // for floorheight() and floorangle() testing
    // if (ipos.y == floorheight(ipos.x)) {
        // color = vec4(1., 0., 0., 0.);
        // color = vec4(floorangle(ipos.x) / 90., -floorangle(ipos.x) / 90., 0., 1.);
    // }
}