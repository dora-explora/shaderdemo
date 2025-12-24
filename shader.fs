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

float fmod(in float n, in float m) {
    float x = n;
    while (x > m) { x -= m; }
    return x;
}

const float SHAKE_STRENGTH = .006;
const float RAIN_STRENGTH = .03;
const float RAIN_CHANCE = 0.003;
const int RAIN_LENGTH = 20;
const int SPLATTER_DISTANCE = 20;
const int SPLATTER_LENGTH = 2;
const int RAIN_SPEED = 20;
const float MOUSE_RADIUS = 40.;
const float LIGHTNING_PERIOD = 1000.;
// const float LIGHTNING_PERIOD = 1.;
const int LIGHTNING_DECAY = 10;

vec2 shakerand(in float seed) { // random offset of pos for screen shake (outdated, probalby going to remove)
    vec2 shake;
    shake.y = rand(seed + 1.) * SHAKE_STRENGTH * (brand(seed + 1.) ? -1. : 1);
    shake.y = float(int(shake.y * 1080.)) / 1080.; 
    shake.x = rand(seed) * SHAKE_STRENGTH * (brand(seed) ? -1. : 1.);
    shake.x = float(int(shake.x * 1920.)) / 1920.; 
    return shake;
}

int mouseheight(in int x, in ivec2 imouse) { // height of top of the mouse at some x position
    int offset = x - imouse.x;
    return imouse.y + int(sqrt(MOUSE_RADIUS*MOUSE_RADIUS + offset*offset) - 2 * MOUSE_RADIUS);
}

int floorheight(in int x) { // height of the floor at some x position
    if (x < 200) { 
        return 1000;
    } else if (x < 800) { 
        return 1000 - int(smoothstep(0., 1., float(x - 200)/600.) * 200.); 
    } else if (x < 1500) { 
        return 800;
    } else if (x < 1600) {
        return 800 + 2 * (x - 1500);
    } else { 
        return 1000;
    }
}

int height(in int x, in ivec2 imouse) { // height of the highest surface at some x position
    int height = floorheight(x);
    if (x < imouse.x + MOUSE_RADIUS && x > imouse.x - MOUSE_RADIUS) {
        int mouseheight = mouseheight(x, imouse);
        if (height > mouseheight) { return mouseheight; }
    }
    return height;
}

float angle(in int x, in ivec2 imouse) { // angle of the highest surface at some x position
    if (x < imouse.x + MOUSE_RADIUS && x > imouse.x - MOUSE_RADIUS) {
        int offset = x - imouse.x;
        return degrees(acos(float(offset) / MOUSE_RADIUS)) - 90.;
    }
    if (x < 200) {
        return 0.;
    } else if (x < 800) { 
        float t = float(x - 200)/600.;
        return 90. - degrees(atan(1. / (6. * (t - t*t))));
    } else if (x < 1500) { 
    return 0.;
    } else if (x < 1600) {
        return -60.;
    } else {
        return 0.;
    }
}

bool rainrand(in int x, in int seed) { // random chance for rain at some x and seed
    if (sinrand(rand(sinrand(float(x) / 1920. + float(seed) * 0.76521)) + 0.5) < RAIN_CHANCE) { // this took me so god damn long
        return true;
    }
    return false;
}

ivec2 splatterrotation(in int x, in ivec2 imouse) { // slope of rain splatter as some x
    float angle = angle(x, imouse);
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

bool rained(in int x, in int y, in int frame) { // whether a certain pixel has rain at some frame
    int seed = int(frame * RAIN_SPEED - y) % 967;
    return rainrand(x, seed);
}

bool splatterable(in ivec2 ipos, in ivec2 imouse) { // whether a certain pixel is in range of splatter
    int height = height(ipos.x, imouse);
    if(ipos.y > height) { return false; }
    if (
        ipos.x < imouse.x + MOUSE_RADIUS + SPLATTER_DISTANCE && 
        ipos.x > imouse.x - MOUSE_RADIUS - SPLATTER_DISTANCE &&
        ipos.y < imouse.y &&
        ipos.y > imouse.y - MOUSE_RADIUS - SPLATTER_DISTANCE
    ) { return true; }
    if (ipos.y < height && ipos.y > height - SPLATTER_DISTANCE) { return true; }
    return false;
}

bool splatter(in ivec2 ipos, in int frame, in ivec2 imouse) { // whether a certain pixel has rain splatter
    int bound = SPLATTER_DISTANCE * 3;
    for (int i = -bound; i < bound; i++) {
        if (i == 0 ) { continue; } // break on middle case

        int x = ipos.x + i; // x position of unobscured droplet

        int height = height(x, imouse); // floor height of unobscured droplet's column
        if (distance(vec2(x, height), ipos) > SPLATTER_DISTANCE) { continue; } // if splatter would be too far, break

        ivec2 rotation = splatterrotation(x, imouse); // floor rotation of unobscured droplets' column (ivec2 of simple ratio)
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

float rain(in ivec2 ipos, in int frame, in ivec2 imouse) { // strength of rain or splatter at a certain pixel on some frame
    if (ipos.y > height(ipos.x, imouse)) {
        return 0.;
    }

    for (int i = 0; i < RAIN_LENGTH; i++) {
        if (rained(ipos.x, ipos.y + i, frame)) {
            return float(RAIN_LENGTH - i) / RAIN_LENGTH;
        }
    }

    if (splatterable(ipos, imouse)) {
        for (int i = 0; i < SPLATTER_LENGTH; i++) {
            if (splatter(ipos, frame - i, imouse)) {
                return float(SPLATTER_LENGTH + 1 - i)/3.;
            }
        }
    }
    return 0.;
}

float mousestrength(in ivec2 ipos, in ivec2 imouse) { // strength of mouse at a certain pixel
    if (distance(ipos, imouse) < MOUSE_RADIUS) {
        return 1.;
    }
    return 0.;
}

bool lightrand(in int frame) { // whether or not some frame has lightning
    if (sinrand(fmod(float(frame) / LIGHTNING_PERIOD, LIGHTNING_PERIOD * 5.25714)) < 1./LIGHTNING_PERIOD) {
        return true;
    }
    return false;
}

float lightbg() { // strength of background lightning on this frame
    for (int i = 0; i < LIGHTNING_DECAY; i++) {
        if (lightrand(frame - i)) {
            return float(LIGHTNING_DECAY - i) / float(10 * LIGHTNING_DECAY);
        }
    }
    return 0.;
}

bool lightwalk(in int inp) { // whether or not the lightning flips direction at some y position (??)
    if (rand(fmod(float(inp), 1022.8942)) < 0.05) {
        return true;
    } else {
        return false;
    }
}

float light(in ivec2 ipos, in ivec2 imouse) { // strenght of lightning bolt at a certain pixel this frame
    if (!lightrand(frame)) { return 0.; }
    int xoffset = abs(ipos.x - imouse.x);
    int yoffset = abs(ipos.y - imouse.y);
    if (xoffset * 2 > yoffset || ipos.y >= imouse.y) { return 0.; }
    int x = int(sinrand(seed) * imouse.y / 2 + imouse.x - imouse.y/4);
    bool direction = brand(seed);
    for (int i = 0; i < ipos.y; i += 2) {
        if (x - imouse.x == (imouse.y - i)/2) {
            direction = false;
        } else if (imouse.x - x == (imouse.y - i)/2) {
            direction = true;
        } else if (lightwalk(frame + ipos.y - i/2)) { 
            direction = !direction;
        }
        if (direction) { x++; }
        else { x--; }
    }
    if (ipos.x != x) { return 0.; }
    return 1.;
}

void main() {
    vec2 pos = fragTexCoord;

    // pos += shakerand(seed); // screen shake
    // if (pos.y > 1. || pos.y < 0.) { // if pos.y is offscreen,
        // color = vec4(0., 0., 0., 1.); // make the color black
        // return;
    // }

    ivec2 ipos = ivec2(pos.x * 1920., pos.y * 1080.);

    ivec2 imouse = ivec2(int(mouse.x), clamp(int(mouse.y), 0, floorheight(int(mouse.x)) - MOUSE_RADIUS));

    color = texture(bg, pos);

    float mouse_strength = mousestrength(ipos, imouse);
    if (mouse_strength > 0.) {
        color = mix(color, vec4(0.5, 0.3, 0.3, 1.), mouse_strength);
    } else {
        float rain_strength = rain(ipos, frame, imouse);
        color = mix(color, vec4(0.6, 0.8, 1., 1.), rain_strength);
        float lightning_strength = lightbg();// + light(ipos, imouse);
        color = mix(color, vec4(1.), lightning_strength);
    }

    // // for splatterable testing
    // if (splatterable(ipos)) {
    //     color = mix(color, vec4(1.), 0.2);
    // }
    // for height() and angle() testing
    // if (ipos.y == height(ipos.x, imouse)) {
    //     // color = vec4(1., 0., 0., 1.);
    //     color = vec4(angle(ipos.x, imouse) / 90., -angle(ipos.x, imouse) / 90., 1., 1.);
    // }
}