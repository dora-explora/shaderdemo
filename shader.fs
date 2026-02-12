#version 330

in vec2 fragTexCoord;
in vec4 fragColor;

out vec4 color;

uniform float time;
uniform float seed;
uniform int frame;
uniform vec2 mouse;

uniform sampler2D bg;
uniform sampler2D rock;
uniform sampler2D platform;
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

bool brand(in float seed) {
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
const int SPLATTER_DISTANCE = 30;
const int SPLATTER_LENGTH = 3;
const int RAIN_SPEED = 20;
const float MOUSE_RADIUS = 40.;
const float LIGHTNING_PERIOD = 400.;
const int LIGHTNING_DECAY = 15;
const float LIGHTNING_BG_STRENGTH = 0.3;
const float LIGHTNING_WALK_CHANCE = 0.3;
const float FIRE_CHANCE = 0.05;

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

int platformpos(in int frame) {
    int aframe = frame % 1000; // "actual" frame, adjusted for period
    if (aframe < 200) {
        return int(12 * sqrt(aframe + 1.)) + 988;
    } else if (aframe < 267) {
        return int(pow(float(aframe - 191), 2.) / 40.) + 1156;
    } else if (aframe < 500) {
        return 1300;
    } else if (aframe < 700) {
        return -int(12 * sqrt(aframe - 499.)) + 1311;
    } else if (aframe < 767) {
        return -int(pow(float(aframe - 691), 2.) / 40.) + 1144;
    } else {
        return 1000;
    }
}

int platformheight(in int x, in int frame) {
    int pos = platformpos(frame);
    if (x < pos + 75 && x > pos - 75) {
        return 450;
    }
    return 1080;
}

int fireheight(in int x) {
    if (x > 1035 || x < 965) { return 1080; }
    int offset = abs(x - 1000) / 2;
    if (offset < 13) { return 780 - offset; }
    else { return 756 + offset; }
}

int height(in int x, in int frame, in ivec2 imouse) { // height of the highest surface at some x position
    int height = floorheight(x);
    height = min(height, platformheight(x, frame));
    height = min(height, fireheight(x));
    if (x < imouse.x + MOUSE_RADIUS && x > imouse.x - MOUSE_RADIUS) {
        int mouseheight = mouseheight(x, imouse);
        if (height > mouseheight) { return mouseheight; }
    }
    return height;
}

float angle(in int x, in ivec2 imouse, in int frame) { // angle of the highest surface at some x position
    int platformpos = platformpos(frame);
    if (x < platformpos + 75 && x > platformpos - 75) {
        return 0.;
    }
    if (x < imouse.x + MOUSE_RADIUS && x > imouse.x - MOUSE_RADIUS) {
        int offset = x - imouse.x;
        return degrees(acos(float(offset) / MOUSE_RADIUS)) - 90.;
    }
    if (x < 1035 && x > 965) {
        int offset = x - 1000;
        if (offset > 25) { return -30.; }
        if (offset < -25) { return 30.; }
        if (offset > 0) { return 30.; }
        if (offset < 0) { return -30.; }
        return 0.;
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

ivec2 splatterrotation(in int x, in ivec2 imouse, in int frame) { // slope of rain splatter as some x
    float angle = angle(x, imouse, frame);
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

bool splatterable(in ivec2 ipos, in ivec2 imouse, in int frame) { // whether a certain pixel is in range of splatter
    int height = height(ipos.x, frame, imouse);
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

        int height = height(x, frame, imouse); // floor height of unobscured droplet's column
        if (distance(vec2(x, height), ipos) > SPLATTER_DISTANCE) { continue; } // if splatter would be too far, break

        ivec2 rotation = splatterrotation(x, imouse, frame); // floor rotation of unobscured droplets' column (ivec2 of simple ratio)
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
    if (ipos.y > height(ipos.x, frame, imouse)) {
        return 0.;
    }

    for (int i = 0; i < RAIN_LENGTH; i++) {
        if (rained(ipos.x, ipos.y + i, frame)) {
            return float(RAIN_LENGTH - i) / RAIN_LENGTH;
        }
    }

    if (splatterable(ipos, imouse, frame)) {
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

vec4 mousecolor(in ivec2 ipos, in ivec2 imouse) {
    float x = (ipos.x - imouse.x) / (MOUSE_RADIUS * 2. + 1.) + .5;
    float y = (ipos.y - imouse.y) / (MOUSE_RADIUS * 2. + 1.) + .5;
    return texture2D(rock, vec2(x, y));
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
            return float(LIGHTNING_DECAY - i) / float(LIGHTNING_DECAY);
        }
    }
    return 0.;
}

bool lightwalk(in float inp) { // whether or not the lightning flips direction at some y position (??)
    if (rand(inp) < LIGHTNING_WALK_CHANCE) {
        return true;
    } else {
        return false;
    }
}

bool lightbolt(in ivec2 ipos, in ivec2 imouse, in int frame) { // strength of lightning bolt at a certain pixel and certain frame
    if (!lightrand(frame)) { return false; }
    int strikex = int(seed * 1920.);
    int strikey = height(strikex, frame, imouse);
    if (ipos.y >= strikey) { return false; }
    for (int i = -2; i <= 2; i++) {
        int xoffset = abs(ipos.x + i - strikex);
        int yoffset = abs(ipos.y - strikey);
        if (xoffset > yoffset) { return false; }
        int x = int(sinrand(seed) * 100 + strikex - 50);
        int direction = 1;
        for (int y = 0; y < ipos.y; y++) {
            if (x - strikex == strikey - y) {
                direction = -1;
            } else if (strikex - x == strikey - y) {
                direction = 1;
            } else if (lightwalk(fract(seed + float(y) * 0.827634))) {
                direction = direction == 1 ? -1 : 1;
            }
            x += direction;
        }
        if (ipos.x + i == x) { return true; }
    }
    return false;
}

float platformstrength(in ivec2 ipos, in int frame) {
    int pos = platformpos(frame);
    if (ipos.y < 500 && ipos.y > 450 && ipos.x < pos + 75 && ipos.x > pos - 75) {
        return 1.;
    }
    return 0.;
}

vec4 platformcolor(in ivec2 ipos, in int frame) {
    int pos = platformpos(frame);
    if (ipos.y == 476) {
        if (frame % 500 < 267) {
            if (ipos.x == pos) {
                return vec4(0., 1., 0., 1.);
            }
            if (ipos.x == pos - 5 && (frame % 1000) / 500 == 1) {
                return vec4(1., .5, 0., 1.);
            }
            if (ipos.x == pos + 5 && (frame % 1000) / 500 == 0) {
                return vec4(1., .5, 0., 1.);
            }
        } else if (frame % 500 > 420) {
            if (ipos.x == pos) {
                return vec4(1., 1., 0., 1.);
            }
        } else {
            if (ipos.x == pos) {
                return vec4(1., 0., 0., 1.);
            }
        }
    }
    float x = (ipos.x - pos) / 151. + 0.5;
    float y = (ipos.y - 450) / 51.;
    return texture(platform, vec2(x, y));
}

bool fireable(in ivec2 ipos, in ivec2 imouse) {
    if (ipos.x < 975 || ipos.x > 1025) { return false; }
    int offset = abs(ipos.x - 1000) / 2;
    if (ipos.y + offset > 780) { return false; }
    float x = (ipos.x - 1000);
    float aframe = 0.85 - (1 + sin(frame * 0.05)) * 0.15;
    float top = .5 * cos(x / 16.) + .2 * cos(x / 8.) + aframe * cos(x / 4.) + 0.8;
    if (ipos.y < 750 - int(50. * top)) { return false; }
    if (distance(ipos, imouse) < MOUSE_RADIUS) { return false; }
    int direction = (ipos.x < 1000) ? -25 : 25;
    if ((780 - offset - ipos.y)/10 > abs(ipos.x - 1000 - direction)) { return false; }
    return true;
}

bool firerand(in ivec2 ipos, in int frame) {
    float x = fmod(ipos.x / 63.9263, 928.374);
    float y = fmod(ipos.y / 987.812746, 627.67231);
    float f = fmod(frame * 1.23688, 38.8364);
    float fseed = (x + y + f) * 2.26371;
    if (rand(sinrand(fseed) + 0.5) < FIRE_CHANCE) { return true; }
    else { return false; }
}

vec4 fire(in ivec2 ipos, in ivec2 imouse, in int frame) {
    if (fireable(ipos, imouse)) {
        int offset = abs(ipos.x - 1000) / 2;
        int oy = 780 - offset;
        int diagonal = (oy - ipos.y)/10;
        if (ipos.x < 1000) { diagonal = -diagonal; }
        int ox = ipos.x + diagonal;
        int of = oy - ipos.y - frame * 2;
        if (firerand(ivec2(ox, oy), of)) {
            float x = fmod(ipos.x / 637.9263, 517.2673);
            float y = fmod(ipos.y / 987.812746, 725.2987);
            float fseed = seed + x + y;
            float height = float(780 - ipos.y) / 150.;
            float g = sinrand(fseed * 1926.26371) * 0.4 + 0.6 - height;
            bool gray = false;
            if (ipos.x < platformpos(frame) - 75) {
                if (ipos.x < imouse.x + MOUSE_RADIUS && ipos.x > imouse.x - MOUSE_RADIUS) {
                    gray = false;
                } else {
                    gray = true;
                }
            }
            if (gray) {
                return vec4(1., 1., 1., g);
            } else {
                return vec4(1., g, 0., 1.);
            }
        }
    }
    return vec4(0.);
}

void main() {
    vec2 pos = fragTexCoord;

    // pos += shakerand(seed); // screen shake
    // if (pos.y > 1. || pos.y < 0.) { // if pos.y is offscreen,
    //     color = vec4(0., 0., 0., 1.); // make the color black
    //     return;
    // }

    ivec2 ipos = ivec2(pos.x * 1920., pos.y * 1080.);

    ivec2 imouse = ivec2(int(mouse.x), clamp(int(mouse.y), 0, floorheight(int(mouse.x)) - MOUSE_RADIUS));

    color = texture(bg, pos);

    float mouse_strength = mousestrength(ipos, imouse);
    float platform_strength = platformstrength(ipos, frame);
    if (mouse_strength > 0.) {
        color = mousecolor(ipos, imouse);
    } else if (platform_strength > 0.) {
        color = platformcolor(ipos, frame);
    } else {
        vec4 fire = fire(ipos, imouse, frame);
        color = mix(color, fire, fire.a);
        float rain_strength = rain(ipos, frame, imouse);
        color = mix(color, vec4(0.6, 0.8, 1., 1.), rain_strength);
        float lightning_bg_strength = lightbg();
        color = mix(color, vec4(1.), lightning_bg_strength * LIGHTNING_BG_STRENGTH);
        if (lightbolt(ipos, imouse, frame)) { color = vec4(1., 1., 0.8, 1.); }
    }

    // // for splatterable testing
    // if (splatterable(ipos, imouse)) {
    //     color = mix(color, vec4(1.), 0.2);
    // }
    // // for height() and angle() testing
    // if (ipos.y == height(ipos.x, imouse)) {
    //     color = vec4(1., 0., 0., 1.);
    //     // color = vec4(angle(ipos.x, imouse, frame) / 90., -angle(ipos.x, imouse, frame) / 90., 1., 1.);
    // }
}
