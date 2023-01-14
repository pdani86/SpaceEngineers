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
//using System.Security.Cryptography.X509Certificates;
//using static Sandbox.Common.ObjectBuilders.Definitions.MyObjectBuilder_ParachuteDefinition.Opening;
//using Sandbox.ModAPI;


/*
void getOrientationVectors(out  Vector3D F, out Vector3D U, out Vector3D R) {
	F = rc.WorldMatrix.Forward;
	R = rc.WorldMatrix.Right;
	U = rc.WorldMatrix.Up;
}


void getRollPitch(out double roll, out double pitch,Vector3D vRef) {
	Vector3D F,U,R;
	getOrientationVectors(out F,out U,out R);
	vRef.Normalize();

	double cosFG = F.Dot(vRef);
	double cosRG = R.Dot(vRef);
	pitch = Math.PI/2.0 - Math.Acos(cosFG);
	roll = Math.PI/2.0 - Math.Acos(cosRG);
}
*/

class Program : MyGridProgram {

    class PID_Control
    {
        public double kP = 2.0;
        public double kI = 0.5;
        public double kD = 4.0;

        public double integ = 0.0;
        public double integTrim = 2.0;

        public double lastVal = 0.0;
        public double lastErr = 0.0;
        public double control = 0.0;

        double trim(double val, double th) { if (val < -th) return -th; if (val > th) return th; return val; }

        public double update(double signal, double target, double dt = 0.016666)
        {
            double err = target - signal;
            integ += err * dt;
            double d = (err - lastErr) / dt;
            integ = trim(integ, integTrim);
            control = kP * err + kD * d + kI * integ;
            lastVal = signal;
            lastErr = err;
            return control;
        }
    };

    double trim(double val, double th) { if (val < -th) return -th; if (val > th) return th; return val; }

    string vectorToString(Vector3D v)
    {
        string str = "";
        str +=
          v.X.ToString("0.0000") + ", " +
          v.Y.ToString("0.0000") + ", " +
          v.Z.ToString("0.0000") + "\n";
        return str;
    }

    public Program()
    {

        Runtime.UpdateFrequency = UpdateFrequency.Update1;
        List<IMyTextPanel> lcd_list = new List<IMyTextPanel>();
        List<IMyShipConnector> connector_list = new List<IMyShipConnector>();
        List<IMyRemoteControl> rc_list = new List<IMyRemoteControl>();
        List<IMyGyro> gyro_list = new List<IMyGyro>();
        GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(lcd_list);
        GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(connector_list);
        GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(rc_list);
        GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyro_list);
        GridTerminalSystem.GetBlocksOfType<IMyThrust>(this.thrusters);

        lcd = lcd_list[0];
        connector = connector_list[0];
        rc = rc_list[0];
        gyro = gyro_list[0];

        int ix = 0;
        foreach(var thruster in thrusters)
        {
            //thruster.Orientation
            //thruster.ThrustOverride
            //MyAPIGateway.Utilities.ShowMessage("S ", "dir"+(int)thruster.Orientation.Forward);
            ++ix;
            switch (thruster.Orientation.Forward)
            {
                case Base6Directions.Direction.Forward:
                    thrustersF.Add(thruster);
                    break;
                case Base6Directions.Direction.Backward:
                    thrustersB.Add(thruster);
                    break;
                case Base6Directions.Direction.Up:
                    thrustersU.Add(thruster);
                    break;
                case Base6Directions.Direction.Down:
                    thrustersD.Add(thruster);
                    break;
                case Base6Directions.Direction.Right:
                    thrustersR.Add(thruster);
                    break;
                case Base6Directions.Direction.Left:
                    thrustersL.Add(thruster);
                    break;
            }
        }
    }

    struct Metrics
    {
        public Metrics(IMyRemoteControl rc, Vector3D connectorPos, MatrixD targetSystem, Vector3D targetPos)
        {
            /*
            var v_av = rc.GetShipVelocities();
            var v = v_av.LinearVelocity;
            var curSpeed = v.Length();
            var curPos = rc.GetPosition();
            */
            shipWorldMatrix = rc.WorldMatrix;

            targetWorldMatrix = targetSystem;

            var F = shipWorldMatrix.Forward;
            var R = shipWorldMatrix.Right;
            var U = shipWorldMatrix.Up;

            var Ft = targetWorldMatrix.Forward;
            var Rt = targetWorldMatrix.Right;
            var Ut = targetWorldMatrix.Up;

            worldShipPos = rc.GetPosition();
            worldTargetPos = targetPos;
            targetVec = targetPos - connectorPos;
            targetDistance = targetVec.Length();

            var utProjF = Ut - (Ut.Dot(F)) * F;
            utProjF.Normalize();
            var f = F - Ut.Dot(F) * Ut;
            f.Normalize();
            var cosYaw = Ft.Dot(f);
            var pitchCos = Ut.Dot(F);
            pitchAngle = Math.PI / 2 - Math.Acos(pitchCos);
            yawAngle = Math.Atan2(cosYaw, f.Dot(Rt));
            /*yawAngle = Math.Atan2(cosYaw, f.Dot(Rt));
            yawAngle += Math.PI; if (yawAngle > Math.PI) yawAngle -= 2 * Math.PI;
            */
            yawAngle = 1.0f * yawAngle;
            rollAngle = Math.Atan2(utProjF.Dot(R), utProjF.Dot(U));
            targetDelta = Vector3D.Rotate(targetVec, rc.WorldMatrix);

            shipVelWorld = rc.GetShipVelocities().LinearVelocity;
            shipVelLocal = Vector3D.Rotate(shipVelWorld, rc.WorldMatrix);
        }

        public Vector3D worldShipPos;
        public Vector3D worldTargetPos;
        public MatrixD shipWorldMatrix;
        public MatrixD targetWorldMatrix;
        public Vector3D targetVec; // world
        public double targetDistance;

        public Vector3D shipVelWorld;
        public Vector3D shipVelLocal;

        public Vector3D targetDelta;

        public double rollAngle;
        public double pitchAngle;
        public double yawAngle;

    };

    void updateLcd(Metrics metrics)
    {
        string str = "";
        /*str += "POS\n";
        str += vectorToString(metrics.worldShipPos);
        str += "FORWARD - UP\n";
        str += vectorToString(metrics.shipWorldMatrix.Forward);
        str += vectorToString(metrics.shipWorldMatrix.Up);
        */
        str += "PITCH - YAW - ROLL\n";
        str += (metrics.pitchAngle*180.0/Math.PI).ToString("0.000");
        str += "\n";
        str += (metrics.yawAngle * 180.0 / Math.PI).ToString("0.000");
        str += "\n";
        str += (metrics.rollAngle * 180.0 / Math.PI).ToString("0.000");
        str += "\n";

        str += "tgt " + vectorToString(metrics.targetDelta);
        str += "\n";

        str += "vLoc " + vectorToString(metrics.shipVelLocal);
        str += "\n";

        str += "th " + thrusters.Count + " | " + thrustersF.Count + "," + thrustersB.Count + "," + thrustersR.Count + "," + thrustersL.Count + "," + thrustersU.Count + "," + thrustersD.Count;
        str += "\n";
        lcd.WriteText(str);
    }

    void setThrustersPercentage(List<IMyThrust> list, float perc)
    {
        foreach (var thruster in list)
        {
            thruster.ThrustOverridePercentage = perc;
        }
    }

    void controlLocalVel(Metrics metrics, Vector3D vel)
    {
        var fwd = pidFwd.update(metrics.shipVelLocal.Z, vel.Z);
        var right = pidRight.update(metrics.shipVelLocal.X, vel.X);
        var up = pidUp.update(metrics.shipVelLocal.Y, vel.Y);

        setThrustersPercentage(thrustersU, 0.0f);
        setThrustersPercentage(thrustersD, 0.0f);
        setThrustersPercentage(thrustersR, 0.0f);
        setThrustersPercentage(thrustersL, 0.0f);

        List<IMyThrust> thrustersLRToSet = (right > 0.0) ? (thrustersR) : (thrustersL);
        List<IMyThrust> thrustersUDToSet = (up < 0.0) ? (thrustersU) : (thrustersD);

        setThrustersPercentage(thrustersLRToSet, (float)Math.Abs(right));
        setThrustersPercentage(thrustersUDToSet, (float)Math.Abs(up));
    }

    public void Main(string argument, UpdateType updateSource)
    {
        Vector3D halfBlockFWD = connector.WorldMatrix.Forward * 1.25;
        var connectorPos = connector.GetPosition() + halfBlockFWD;
        var metrics = new Metrics(rc, connectorPos, MatrixD.Identity, Vector3D.Zero);

        updateLcd(metrics);

        pidYaw.update(metrics.yawAngle, 0.0);
        pidPitch.update(metrics.pitchAngle, 0.0);
        pidRoll.update(metrics.rollAngle, 0.0);

        gyro.Yaw = (float)pidYaw.control * -1.0f;
        gyro.Pitch = (float)pidPitch.control;
        gyro.Roll = (float)pidRoll.control;

        double angleThreshold = 0.2 * Math.PI / 180.0;
        bool orientationOk =
            (Math.Abs(metrics.rollAngle) < angleThreshold) &&
            (Math.Abs(metrics.pitchAngle) < angleThreshold) &&
            (Math.Abs(metrics.yawAngle) < angleThreshold);
        if(orientationOk)
        {
            gyro.Yaw = 0.0f;
            gyro.Roll = 0.0f;
            gyro.Pitch = 0.0f;
        } else {
            return;
        }

        double alignmentThreshold = 0.05;
        bool xAlignmentOk = (Math.Abs(metrics.targetDelta.X) < alignmentThreshold);
        bool yAlignmentOk = (Math.Abs(metrics.targetDelta.Y) < alignmentThreshold);
        bool alignmentOk = xAlignmentOk && yAlignmentOk;

        if(!alignmentOk) {
            float signX = xAlignmentOk ? 0.0f : ((metrics.targetDelta.X < 0.0f) ? (-1.0f) : (1.0f));
            float signY = yAlignmentOk ? 0.0f : ((metrics.targetDelta.Y < 0.0f) ? (-1.0f) : (1.0f));
            signX = -1.0f * signX;
            signY = -1.0f * signY;
            const float alignVel = 0.05f;
            Vector3D v = new Vector3D(alignVel * signX, alignVel * signY, 0.0f);
            //v = v * -1.0f;
            //controlLocalVel(metrics, v);
            //lcd.WriteText(/*lcd.GetText() + */"cvel: " + vectorToString(v));

        } else {

        }


        // var cross = targetDir.Cross(connector.WorldMatrix.Forward);

        //gyro.GyroOverride;
        //gyro.GyroPower;
    }


    IMyTextPanel lcd = null;
    IMyShipConnector connector = null;
    IMyRemoteControl rc = null;
    List<IMyThrust> thrusters = new List<IMyThrust>();
    List<IMyThrust> thrustersF = new List<IMyThrust>();
    List<IMyThrust> thrustersB = new List<IMyThrust>();
    List<IMyThrust> thrustersU = new List<IMyThrust>();
    List<IMyThrust> thrustersD = new List<IMyThrust>();
    List<IMyThrust> thrustersR = new List<IMyThrust>();
    List<IMyThrust> thrustersL = new List<IMyThrust>();
    IMyGyro gyro = null;
    
    PID_Control pidRoll = new PID_Control();
    PID_Control pidPitch = new PID_Control();
    PID_Control pidYaw = new PID_Control();
    PID_Control pidFwd = new PID_Control();
    PID_Control pidRight = new PID_Control();
    PID_Control pidUp = new PID_Control();


}

