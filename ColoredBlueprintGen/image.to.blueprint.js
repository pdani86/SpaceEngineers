function rgb2hsv (r, g, b) {
    let rabs, gabs, babs, rr, gg, bb, h, s, v, diff, diffc, percentRoundFn;
    rabs = r / 255;
    gabs = g / 255;
    babs = b / 255;
    v = Math.max(rabs, gabs, babs),
    diff = v - Math.min(rabs, gabs, babs);
    diffc = c => (v - c) / 6 / diff + 1 / 2;
    //percentRoundFn = num => Math.round(num * 100) / 100;
    if (diff == 0) {
        h = s = 0;
    } else {
        s = diff / v;
        rr = diffc(rabs);
        gg = diffc(gabs);
        bb = diffc(babs);

        if (rabs === v) {
            h = bb - gg;
        } else if (gabs === v) {
            h = (1 / 3) + rr - bb;
        } else if (babs === v) {
            h = (2 / 3) + gg - rr;
        }
        if (h < 0) {
            h += 1;
        }else if (h > 1) {
            h -= 1;
        }
    }
    return {
        h: h,
        s: s,
        v: v
    };
}

var preStr = '<?xml version="1.0"?>' +
	'<Definitions xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">' +
  '<ShipBlueprints><ShipBlueprint xsi:type="MyObjectBuilder_ShipBlueprintDefinition">' +
  '<Id Type="MyObjectBuilder_ShipBlueprintDefinition" Subtype="Large Grid 8078" />' +
  '<DisplayName>---</DisplayName>' +
  '<CubeGrids><CubeGrid><SubtypeName /><EntityId>108433617595477518</EntityId><PersistentFlags>CastShadows InScene</PersistentFlags>' +
  '<PositionAndOrientation><Position x="0" y="0" z="0" /><Forward x="1" y="0" z="0" /><Up x="0" y="0" z="1" /><Orientation><X>1</X><Y>0</Y><Z>0</Z><W>1</W></Orientation></PositionAndOrientation>' +
  '<LocalPositionAndOrientation xsi:nil="true" />' +
  '<GridSizeEnum>Large</GridSizeEnum><CubeBlocks>' + '\r\n';

var postStr = "</CubeBlocks></CubeGrid></CubeGrids></ShipBlueprint></ShipBlueprints></Definitions>" + '\r\n';

function getBlockStr(x, y, hsv) {
	var str = 
	'<MyObjectBuilder_CubeBlock xsi:type="MyObjectBuilder_CubeBlock">' +
              '<SubtypeName>LargeBlockArmorBlock</SubtypeName>' +
              '<Min x="' + x + '" y="' + y +'" z="0" />' +
              '<ColorMaskHSV x="' + hsv.h +'" y="' + hsv.s + '" z="' + hsv.v + '" />' +
          '</MyObjectBuilder_CubeBlock>';
	return str;
}


function init() {
	var img = document.getElementById("img");
	var canvas = document.getElementById("canvas");
	var text = document.getElementById("textarea");
	var w = img.naturalWidth;
	var h = img.naturalHeight;
	canvas.width = w;
	canvas.height = h;
	var ctx = canvas.getContext("2d");
	ctx.drawImage(img, 0, 0, w, h);
	var imgData = ctx.getImageData(0,0,w,h);
	var data = imgData.data;
	
	var textStr = "";
	for(var y = 0; y < h; y++) {
		for(var x = 0; x < w; x++) {
			var ix = 4 * (y*w + x);
			var rgb = [data[ix], data[ix+1], data[ix+2]];
			var hsv = rgb2hsv(rgb[0], rgb[1], rgb[2]);
			hsv.s -= 0.8;
			hsv.v -= 0.45;
			textStr += getBlockStr(x, y, hsv) + "\r\n";
		}
	}
	ctx.putImageData(imgData, 0, 0);
	
	
	textarea.value = preStr + textStr + postStr;
}
		