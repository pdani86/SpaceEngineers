/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
*/
using Sandbox.Common;
using Sandbox.Game;
//using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game;
using SpaceEngineers.Game.ModAPI;
using SpaceEngineers.ObjectBuilders;
using VRage;
using VRage.Game;
using VRage.Library;
using VRageMath;
using VRage.Game.ModAPI.Ingame;

class Program2 : MyGridProgram
{
    string vectorToString(Vector3D v)
    {
        string str = "";
        str +=
          v.X.ToString("0.000") + ", " +
          v.Y.ToString("0.000") + ", " +
          v.Z.ToString("0.000") + "\n";
        return str;
    }

    List<IMyShipConnector> getConnectors()
    {
        List<IMyShipConnector> connectors = new List<IMyShipConnector>();
        GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(connectors);
        return connectors;
    }

    string getConnectorInfo(IMyShipConnector conn)
    {
        string str = "";
        
        str += conn.CustomName + " (RUF): " + conn.GetPosition().ToString("0.00") + "\n";
        str += vectorToString(conn.WorldMatrix.Right);
        str += vectorToString(conn.WorldMatrix.Up);
        str += vectorToString(conn.WorldMatrix.Forward);
        return str;
    }

    void printConnectorInfo(IMyTextPanel lcd)
    {
        string str = "";
        var connectors = getConnectors();
        foreach (var connector in connectors)
        {
            var connInfo = getConnectorInfo(connector);
            str += connInfo + "\n";
        }
        lcd.WriteText(str);
    }

    public Program2()
    {


        List<IMyTextPanel> lcd_list = new List<IMyTextPanel>();
        GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(lcd_list);

        lcd = lcd_list[0];

        printConnectorInfo(lcd);
    }

    IMyTextPanel lcd = null;

    public void Main(string argument, UpdateType updateSource)
    {

    }
}
/*
namespace Docking
{
    internal class Class1
    {
    }
}
*/