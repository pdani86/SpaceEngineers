
#include "spaceeng.h"

#include <cstdint>
#include <array>
#include <fstream>
#include <iostream>

static double diffc(double c, double v, double diff) { return (v - c) / 6 / diff + 1 / 2;}
extern std::array<std::array<uint8_t, 3>, 126> _valToRgbMap;

std::array<uint8_t, 3> SpaceEng::val_to_rgb(uint8_t val) {
    if(val > 0 && val<_valToRgbMap.size()) {
        return {
            _valToRgbMap[val][0],
            _valToRgbMap[val][1],
            _valToRgbMap[val][2]
        };
    }
    return {0, 0, 0};
}

std::array<double, 3> SpaceEng::rgb2hsv(double r,double g,double b) {
    double rabs, gabs, babs, rr, gg, bb, h, s, v;
    rabs = r / 255;
    gabs = g / 255;
    babs = b / 255;
    v = std::max(std::max(rabs, gabs), babs);
    double diff = v - std::min(std::min(rabs, gabs), babs);

    if (diff == 0) {
        h = s = 0;
    } else {
        s = diff / v;
        rr = diffc(rabs, v, diff);
        gg = diffc(gabs, v, diff);
        bb = diffc(babs, v, diff);

        if (rabs == v) {
            h = bb - gg;
        } else if (gabs == v) {
            h = (1 / 3) + rr - bb;
        } else if (babs == v) {
            h = (2 / 3) + gg - rr;
        }
        if (h < 0) {
            h += 1;
        }else if (h > 1) {
            h -= 1;
        }
    }
    return {h, s, v};
}

static std::string preStrSmall =
        R"(<?xml version="1.0"?>
<Definitions xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
<ShipBlueprints><ShipBlueprint xsi:type="MyObjectBuilder_ShipBlueprintDefinition">
<Id Type="MyObjectBuilder_ShipBlueprintDefinition" Subtype="-" />
<DisplayName>---</DisplayName>
<CubeGrids><CubeGrid><SubtypeName /><EntityId>108433617595477518</EntityId><PersistentFlags>CastShadows InScene</PersistentFlags>
<PositionAndOrientation><Position x="0" y="0" z="0" /><Forward x="1" y="0" z="0" /><Up x="0" y="0" z="1" /><Orientation><X>1</X><Y>0</Y><Z>0</Z><W>1</W></Orientation></PositionAndOrientation>
<LocalPositionAndOrientation xsi:nil="true" />
<GridSizeEnum>Small</GridSizeEnum><CubeBlocks>
)";

static std::string preStrLarge =
         R"(<?xml version="1.0"?>
<Definitions xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
<ShipBlueprints><ShipBlueprint xsi:type="MyObjectBuilder_ShipBlueprintDefinition">
<Id Type="MyObjectBuilder_ShipBlueprintDefinition" Subtype="-" />
<DisplayName>---</DisplayName>
<CubeGrids><CubeGrid><SubtypeName /><EntityId>108433617595477518</EntityId><PersistentFlags>CastShadows InScene</PersistentFlags>
<PositionAndOrientation><Position x="0" y="0" z="0" /><Forward x="1" y="0" z="0" /><Up x="0" y="0" z="1" /><Orientation><X>1</X><Y>0</Y><Z>0</Z><W>1</W></Orientation></PositionAndOrientation>
<LocalPositionAndOrientation xsi:nil="true" />
<GridSizeEnum>Large</GridSizeEnum><CubeBlocks>
)";

static std::string postStr = R"(</CubeBlocks></CubeGrid></CubeGrids></ShipBlueprint></ShipBlueprints></Definitions>)"
                      "\r\n";

static std::string getBlockStr(int x, int y, int z,std::array<double, 3> hsv, bool isSmall) {
    std::string str =
    std::string(R"(<MyObjectBuilder_CubeBlock xsi:type="MyObjectBuilder_CubeBlock">)") +
              "<SubtypeName>" +
              ((isSmall)?("SmallBlockArmorBlock"):("LargeBlockArmorBlock")) +
              "</SubtypeName>" +
              "<Min x=\"" + std::to_string(x) + "\" y=\"" + std::to_string(y) +"\" z=\""+std::to_string(z) + "\" />" +
              "<ColorMaskHSV x=\"" + std::to_string(hsv[0]) + "\" y=\"" + std::to_string(hsv[1]) + "\" z=\"" + std::to_string(hsv[2]) + "\" />" +
          "</MyObjectBuilder_CubeBlock>";
    return str;
}

SpaceEng::SpaceEng()
{

}

void SpaceEng::saveXML(const uint8_t* data, std::array<int, 3> dims) {
    std::cout << "SpaceEng::saveXML" << std::endl;
    //return;
    constexpr bool isSmall = true;
    std::ofstream out;
    out.open("bp.sbc", std::ios::binary);
    if(!out.is_open()) {
        std::cout << "Couldn't open file" << std::endl;;
        return;
    }
    out << ((isSmall)?(preStrSmall):(preStrLarge));
    auto zStep = dims[0] * dims[1];
    auto yStep = dims[0];
    for(int z = 0; z < dims[2]; ++z) {
        std::cout << std::to_string(z) << std::endl;
        for(int y = 0; y < dims[1]; ++y) {
            for(int x = 0; x < dims[0]; ++x) {
                int ix = z * zStep + y * yStep + x;
                auto val = data[ix];
                if(val == 0) continue;

                auto rgb = val_to_rgb(val);
                double r = 0, g = 0, b = 0;
                if(val<_valToRgbMap.size()) {
                    r = _valToRgbMap[val][0];
                    g = _valToRgbMap[val][1];
                    b = _valToRgbMap[val][2];
                } else {continue;}
                if(r==0 && g==0 && b==0) continue;

                auto hsv = rgb2_se_hsv(r, g, b);
                auto blockStr = getBlockStr(x, y, z, hsv, isSmall);
                out << blockStr << "\r\n";
            }
        }
    }
    out << postStr;
}


std::array<std::array<uint8_t, 3>, 126> _valToRgbMap = {
    {
    {0, 0, 0},
    {255, 224, 189},
    {148, 113, 176},
    {197, 146, 145},
    {240, 240, 240},
    {240, 240, 240},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {197, 140, 114},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {150, 150, 150},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {231, 203, 155},
    {183, 0, 4},
    {0,0,0},
    {0,0,0},
    {150, 150, 150},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {162, 156, 64},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {240, 240, 240},
    {240, 240, 240},
    {137, 118, 143},
    {0,0,0},
    {137, 118, 143},
    {148, 113, 176},
    {240, 240, 240},
    {197, 146, 145},
    {231, 158, 169},
    {0,0,0},
    {0,0,0},
    {240, 240, 240},
    {162, 156, 64},
    {125, 125, 125},
    {150, 150, 150},
    {197, 146, 145},
    {0,0,0},
    {0,0,0},
    {0,0,0},
    {100, 100, 100},
    {0,0,0},
    {197, 146, 145},
    {148, 113, 176},
    {0,0,0},
    {0,0,0},
    {100, 100, 100},
    {100, 100, 100},
    {137, 118, 143},
    {231, 203, 155},
    {240, 240, 240},
    {162, 156, 64},
    {100, 100, 100},
    {197, 140, 114},
    {100, 100, 100},
    {150, 150, 150},
    {100, 100, 100},
    {197, 146, 145},
    {100, 100, 100},
    {100, 100, 100},
    {100, 100, 100},
    {145, 255, 251},
    {197, 146, 145},
    {100, 100, 100},
    {0, 0, 0}, // ures volt
    {100, 100, 100},
    {148, 113, 176},
    {100, 100, 100},
    {100, 100, 100},
    {100, 100, 100},
    {145, 255, 251},
    {100, 100, 100},
    {0, 0, 0},
    {148, 113, 176},
    {148, 113, 176},
    {100, 100, 100},
    {240, 240, 240}
    }
    };
