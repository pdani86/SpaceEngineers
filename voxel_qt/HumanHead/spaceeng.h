#ifndef SPACEENG_H
#define SPACEENG_H

#include <cstdint>
#include <array>

class SpaceEng
{
public:
    SpaceEng();
    static std::array<double, 3> rgb2hsv(double r,double g,double b);
    static std::array<double, 3> toSEhsv(std::array<double, 3> hsv) {return {hsv[0], hsv[1]-0.8, hsv[2]-0.45};}
    static std::array<double, 3> rgb2_se_hsv(double r,double g,double b) {return toSEhsv(rgb2hsv(r, g, b));}
    static void saveXML(const uint8_t* data, std::array<int, 3> dims);
    static std::array<uint8_t, 3> val_to_rgb(uint8_t val);
};

#endif // SPACEENG_H
