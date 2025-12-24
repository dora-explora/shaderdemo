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
const int SPLATTER_DISTANCE = 20;
const int SPLATTER_LENGTH = 2;
const int RAIN_SPEED = 20;
const float MOUSE_RADIUS = 40.;

vec2 shakerand(in float seed) {
    vec2 shake;
    shake.y = rand(seed + 1.) * SHAKE_STRENGTH * (brand(seed + 1.) ? -1. : 1);
    shake.y = float(int(shake.y * 1080.)) / 1080.; 
    shake.x = rand(seed) * SHAKE_STRENGTH * (brand(seed) ? -1. : 1.);
    shake.x = float(int(shake.x * 1920.)) / 1920.; 
    return shake;
}

bool moused(in int x) {
    if (x < mouse.x + MOUSE_RADIUS && x > mouse.x - MOUSE_RADIUS) {
        return true;
    }
    return false;
}

int mouseheight(in int x) {
    int offset = x - int(mouse.x);
    return int(mouse.y + sqrt(MOUSE_RADIUS*MOUSE_RADIUS + offset*offset) - 2 * MOUSE_RADIUS);
}

int floorheight(in int x) {
    int height;
    if (x < 200) { height = 1000; }
    else if (x < 800) { 
        height = 1000 - int(smoothstep(0., 1., float(x - 200)/600.) * 200.); 
    } else if (x < 1500) { height = 800; }
    else if (x < 1600) {
        height = 800 + 2 * (x - 1500);
    } else { height = 1000; }
    if (moused(x)) {
        int mouseheight = mouseheight(x);
        if (height > mouseheight) { return mouseheight; }
    }
    return height;
}

float floorangle(in int x) {
    if (moused(x) && mouseheight(x) == floorheight(x)) {
        float offset = x - mouse.x;
        return degrees(acos(offset / MOUSE_RADIUS)) - 90.;
    }
    if (x < 200) { return 0.; }
    else if (x < 800) { 
        float t = float(x - 200)/600.;
        return 90. - degrees(atan(1. / (6. * (t - t*t))));
    } else if (x < 1500) { return 0.; }
    else if (x < 1600) { return -60.; }
    else { return 0.; }
}

bool rainrand(in int x, in int seed) { // random chance for rained
    if (sinrand(rand(sinrand(float(x) / 1920. + float(seed) * 0.76521)) + 0.5) < RAIN_CHANCE) { // this took me so god damn long
        return true;
    }
    return false;
}

ivec2 rainrotation(in int x) {
    float angle = floorangle(x);
    if (angle < -30.) {
        return ivec2(1, 4);
    } else if (angle < -24.) {
        return ivec2(1, 3);
    } else if (angle < -10.) {
        return ivec2(1, 2);
    } else if (angle < 10.) {
        return ivec2(1, 1);
    } else if (angle < 24.) {
        return ivec2(2, 1);
    } else if (angle < 30.) {
        return ivec2(3, 1);
    } else {
        return ivec2(4, 1);
    }
}

bool rained(in int x, in int y, in int frame) {
    int seed = int(frame * RAIN_SPEED - y) % 967;
    return rainrand(x, seed);
}

bool splatterable(in ivec2 ipos) {
    int height = floorheight(ipos.x);
    if(ipos.y > height) { return false; }
    if (
        ipos.x < mouse.x + MOUSE_RADIUS + SPLATTER_DISTANCE && 
        ipos.x > mouse.x - MOUSE_RADIUS - SPLATTER_DISTANCE &&
        ipos.y < mouse.y &&
        ipos.y > mouse.y - MOUSE_RADIUS - SPLATTER_DISTANCE
    ) { return true; }
    if (ipos.y < height && ipos.y > height - SPLATTER_DISTANCE) { return true; }
    return false;
}

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
        if (rained(x, y, frame)) {
            return true;
        }
    }
    return false;
}

float rain(in ivec2 ipos, in int frame) {
    if (ipos.y > floorheight(ipos.x)) {
        return 0.;
    }

    for (int i = 0; i < RAIN_LENGTH; i++) {
        if (rained(ipos.x, ipos.y + i, frame)) {
            return float(RAIN_LENGTH - i) / RAIN_LENGTH;
        }
    }

    if (splatterable(ipos)) {
        for (int i = 0; i < SPLATTER_LENGTH; i++) {
            if (splatter(ipos, frame - i)) {
                return float(SPLATTER_LENGTH + 1 - i)/3.;
            }
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
        float rain_strength = rain(ipos, frame);
        color = mix(color, vec4(0.6, 0.8, 1., 1.), rain_strength);
    }

    // // for splatterable testing
    // if (splatterable(ipos)) {
    //     color = mix(color, vec4(1.), 0.2);
    // }
    // // for floorheight() and floorangle() testing
    // if (ipos.y == floorheight(ipos.x)) {
    //     // color = vec4(1., 0., 0., 1.);
    //     color = vec4(floorangle(ipos.x) / 90., -floorangle(ipos.x) / 90., 1., 1.);
    // }
}