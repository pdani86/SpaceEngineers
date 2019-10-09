
IMyTextPanel lcdHangar = null;
IMyTextPanel lcdHangar2Engineering = null;
IMyTextPanel lcdEngineering = null;
IMyTextPanel lcdSmallChamber = null;
IMyTextPanel lcdStairway12_1 = null;

IMyTextPanel lcdOxygenSummary = null;

IMyAirVent ventHangar = null;
IMyAirVent ventHangarEngineering = null;
IMyAirVent ventEngineering = null;
IMyAirVent ventSmallChamber = null;
IMyAirVent ventStairway12_1 = null;

bool areNeighbours(IMyCubeBlock cube1, IMyCubeBlock cube2) {
    var p1 = cube1.Position;
    var p2 = cube2.Position;
    int dx = Math.Abs(p1.X-p2.X);
    int dy = Math.Abs(p1.Y-p2.Y);
    int dz = Math.Abs(p1.Z-p2.Z);
    if(dx+dy+dz == 1) return true;
    return false;
}

List<List<IMyAirtightHangarDoor>> collectAndGroupRenameHangarDoors() {
    List<IMyAirtightHangarDoor> list = new List<IMyAirtightHangarDoor>();
    GridTerminalSystem.GetBlocksOfType<IMyAirtightHangarDoor>(list);
    List<List<IMyAirtightHangarDoor>> result = new List<List<IMyAirtightHangarDoor>>();

    while(list.Count != 0) {
         var cur = list[0];
         list.Remove(cur);
         var curGroup = new List<IMyAirtightHangarDoor>();
         curGroup.Add(cur);
         int groupIx = 0;
         while(groupIx<curGroup.Count) {
             cur = curGroup[groupIx];
             for(int i=0;i<list.Count;i++) {
                 if(areNeighbours(cur,list[i])) {
                      curGroup.Add(list[i]);
                      list.Remove(list[i]);
                      i++;
                 }
             }
             groupIx++;
         }
         result.Add(curGroup);
    }
    
    // rename
    for(int i=0;i<result.Count;i++) {
        var curGroup = result[i];
        string curPrefix = "HangarDoor."+i.ToString();
        for(int k=0;k<curGroup.Count;k++) {
            curGroup[k].CustomName = curPrefix + "." + k;
        }
    }
    return result;
}

struct VentedRoomConnection {
    VentedRoom other;
    //List<...> doors;
}

struct VentedRoom {
    string name;
    List<IMyAirVent> vents;
    List<VentedRoomConnection> connections;
    List<IMyTextPanel> pressureLCDs;

    public void updateLCDs() {
        if(pressureLCDs == null || pressureLCDs.Count==0) return;
        string str = "";
        if(vents.Count>0) {
            str = (100.0*vents[0].GetOxygenLevel()).ToString("0");
        } else {
            str = "N/A";
        }
        for(int i=0;i<pressureLCDs.Count;i++) {pressureLCDs[i].WriteText(str);}
    }
}

List<VentedRoom> ventedRooms = new List<VentedRoom>();

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update100;

//collectAndGroupRenameHangarDoors();

   lcdHangar = GridTerminalSystem.GetBlockWithName("LCD.P.Hangar") as IMyTextPanel;
   lcdHangar2Engineering = GridTerminalSystem.GetBlockWithName("LCD.P.HangarEngineering") as IMyTextPanel;
   lcdEngineering = GridTerminalSystem.GetBlockWithName("LCD.P.Engineering") as IMyTextPanel;
   lcdSmallChamber = GridTerminalSystem.GetBlockWithName("LCD.P.SmallChamber") as IMyTextPanel;
   lcdStairway12_1 = GridTerminalSystem.GetBlockWithName("LCD.P.Stairway12.1") as IMyTextPanel;

   lcdOxygenSummary = GridTerminalSystem.GetBlockWithName("LCD.OxygenSummary") as IMyTextPanel;

   ventHangar = GridTerminalSystem.GetBlockWithName("AirVent.Hangar.1") as IMyAirVent;
   ventHangarEngineering = GridTerminalSystem.GetBlockWithName("AirVent.Chamber.Hangar") as IMyAirVent;
   ventEngineering = GridTerminalSystem.GetBlockWithName("AirVent.Engineering.1") as IMyAirVent;
   ventSmallChamber = GridTerminalSystem.GetBlockWithName("AirVent.SmallChamber.Hangar") as IMyAirVent;
   ventStairway12_1 = GridTerminalSystem.GetBlockWithName("AirVent.Stairway.1st.2nd") as IMyAirVent;
}

public void Save() {}

void updatePressureLCDs() {
    int pressHangar = (int)(ventHangar.GetOxygenLevel()*100.0);
    int pressHangarEngineering = (int)(ventHangarEngineering.GetOxygenLevel()*100.0);
    int pressEngineering = (int)(ventEngineering.GetOxygenLevel()*100.0);
    int pressSmallChamber = (int)(ventSmallChamber.GetOxygenLevel()*100.0);
    int pressStairway12_1 = (int)(ventStairway12_1.GetOxygenLevel()*100.0);

    lcdHangar.WriteText(pressHangar.ToString() + " %");
    lcdHangar2Engineering.WriteText(pressHangarEngineering.ToString() + " %");
    lcdSmallChamber.WriteText(pressSmallChamber.ToString() + " %");
    lcdStairway12_1.WriteText(pressStairway12_1 + " %");

    var str = "PRESSURES\r\n";
         str+="----------------\r\n";
    str += "Engineering: " + pressEngineering + " %\r\n";
    str += "Press. Chamber: " + pressHangarEngineering + " %\r\n";
    str += "Hangar: " + pressHangar + " %\r\n";
    str += "Small Chamber: " + pressSmallChamber + " %\r\n";
    str += "Stairway 1-2 [1]: " + pressStairway12_1 + " %\r\n";

    lcdEngineering.WriteText(str);
}

void updateOxygenSummaryLCD() {
    var list = new List<IMyGasTank>();
    GridTerminalSystem.GetBlocksOfType<IMyGasTank>(list);
    string str = "";
    double sum = 0.0f;
    double sumCapacity = 0.0f;
    for(int i=0;i<list.Count;i++) {
        var cur = list[i];
        sumCapacity += cur.Capacity;
        sum += cur.FilledRatio * cur.Capacity;
        str += "Tank["+i+"]: "+(100.0*cur.FilledRatio).ToString("0.00") + " %" + "\r\n";
    }
    double totalRatio = sum / sumCapacity;
    string strSum = "SUM: " + (100.0*totalRatio).ToString("0.00") + " % of " + (sumCapacity/1000000.0).ToString("0") + "ML\r\n";
    string strSpacer = "--------------------\r\n";
    str = "OXYGEN TANK SUMMARY\r\n" + strSpacer + strSum + strSpacer + str;
    lcdOxygenSummary.WriteText("Oxygen Tanks\r\n");
    lcdOxygenSummary.WriteText(str);
}

public void Main(string argument, UpdateType updateSource)
{
    updatePressureLCDs();
    updateOxygenSummaryLCD();
}

