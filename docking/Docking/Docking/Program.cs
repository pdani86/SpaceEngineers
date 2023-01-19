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
    /*
    class PositionControl
    {
        void update(Vector3D worldCoordTarget, double dt) { }
    };

    class OrientationControl
    {
        void update(Vector3D rpyTarget, double dt) { }
    };

    class ManeuverControl
    {
        PositionControl positionControl;
        OrientationControl orientationControl;

        void update(Vector3D posTarget, Vector3D rpyTarget, double dt) { }
    }*/

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

        foreach (var thruster in thrusters)
        {
            thruster.ThrustOverride = 0.0f;
        }

        pidRoll.kI = 0.0f;
        pidPitch.kI = 0.0f;
        pidYaw.kI = 0.0f;

        float pidThruster_kI = 1.0f;
        float pidThruster_integTrim = 1.0f;
        float pidThruster_kD = 0.1f;

        pidFwd.integTrim = pidThruster_integTrim;
        pidRight.integTrim = pidThruster_integTrim;
        pidUp.integTrim = pidThruster_integTrim;

        pidFwd.kI = pidThruster_kI;
        pidRight.kI = pidThruster_kI;
        pidUp.kI = pidThruster_kI;

        pidFwd.kD = pidThruster_kD;
        pidRight.kD = pidThruster_kD;
        pidUp.kD = pidThruster_kD;


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
        
        
        foreach(var thruster in thrusters)
        {
            //thruster.Orientation
            //thruster.ThrustOverride
            //MyAPIGateway.Utilities.ShowMessage("S ", "dir"+(int)thruster.Orientation.Forward);
            var thrustOrient = Base6Directions.GetOppositeDirection(thruster.Orientation.Forward);
            var rcF = rc.Orientation.Forward;
            var rcU = rc.Orientation.Up;
            var rcB = Base6Directions.GetOppositeDirection(rcF);
            var rcL = Base6Directions.GetLeft(rcU, rcF);
            var rcR = Base6Directions.GetOppositeDirection(rcL);
            var rcD = Base6Directions.GetOppositeDirection(rcU);
            if (thrustOrient == rcF)
            {
                thrustersF.Add(thruster);
            } else if(thrustOrient == rcB) {
                thrustersB.Add(thruster);
            }
            else if (thrustOrient == rcR)
            {
                thrustersR.Add(thruster);
            }
            else if (thrustOrient == rcL)
            {
                thrustersL.Add(thruster);
            }
            else if (thrustOrient == rcU)
            {
                thrustersU.Add(thruster);
            }
            else if (thrustOrient == rcD)
            {
                thrustersD.Add(thruster);
            }
        }

        _r0Name = thrustersR[0].CustomName;
        _l0Name = thrustersL[0].CustomName;
        _u0Name = thrustersU[0].CustomName;
        _d0Name = thrustersD[0].CustomName;
    }

    class ManeuverThrusters
    {
        void collectManeuverThrusters(IMyRemoteControl rc, List<IMyThrust> thrusters)
        {
            foreach (var thruster in thrusters)
            {
                var thrustOrient = Base6Directions.GetOppositeDirection(thruster.Orientation.Forward);
                var rcF = rc.Orientation.Forward;
                var rcU = rc.Orientation.Up;
                var rcB = Base6Directions.GetOppositeDirection(rcF);
                var rcL = Base6Directions.GetLeft(rcU, rcF);
                var rcR = Base6Directions.GetOppositeDirection(rcL);
                var rcD = Base6Directions.GetOppositeDirection(rcU);
                if (thrustOrient == rcF)
                {
                    thrustersF.Add(thruster);
                }
                else if (thrustOrient == rcB)
                {
                    thrustersB.Add(thruster);
                }
                else if (thrustOrient == rcR)
                {
                    thrustersR.Add(thruster);
                }
                else if (thrustOrient == rcL)
                {
                    thrustersL.Add(thruster);
                }
                else if (thrustOrient == rcU)
                {
                    thrustersU.Add(thruster);
                }
                else if (thrustOrient == rcD)
                {
                    thrustersD.Add(thruster);
                }

            }
        }

        static double getStoppingVmaxForMaxAcc(double maxAcc, double dist)
        {
            return (float)Math.Sqrt((2 * maxAcc) / Math.Abs(dist));
        }

        static void setThrustersPercentage(List<IMyThrust> list, float perc)
        {
            foreach (var thruster in list)
            {
                thruster.ThrustOverridePercentage = perc;
            }
        }

        //List<IMyThrust> thrusters = new List<IMyThrust>();
        List<IMyThrust> thrustersF = new List<IMyThrust>();
        List<IMyThrust> thrustersB = new List<IMyThrust>();
        List<IMyThrust> thrustersU = new List<IMyThrust>();
        List<IMyThrust> thrustersD = new List<IMyThrust>();
        List<IMyThrust> thrustersR = new List<IMyThrust>();
        List<IMyThrust> thrustersL = new List<IMyThrust>();
        
    };

    struct Metrics
    {
        public Metrics(IMyRemoteControl rc, Vector3D connectorPos, MatrixD targetSystem /*, Vector3D targetPos*/)
        {
            /*
            var v_av = rc.GetShipVelocities();
            var v = v_av.LinearVelocity;
            var curSpeed = v.Length();
            var curPos = rc.GetPosition();
            */
            shipMass = rc.CalculateShipMass().PhysicalMass;
            
            var targetPos = targetSystem.Translation;

            shipWorldMatrix = rc.WorldMatrix;
            targetWorldMatrix = targetSystem;

            var worldToShipMat = Matrix.Invert(shipWorldMatrix);
            var worldToTargetMat = Matrix.Invert(targetWorldMatrix);

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
            var pitchCos = Ut.Dot(F);
            pitchAngle = Math.PI / 2 - Math.Acos(pitchCos);
            yawAngle = Math.PI / 2 - Math.Atan2(f.Dot(Ft), f.Dot(Rt));
            rollAngle = Math.Atan2(utProjF.Dot(R), utProjF.Dot(U));

            targetDelta = Vector3D.Rotate(targetVec, worldToShipMat);
            shipVelWorld = rc.GetShipVelocities().LinearVelocity;
            shipVelLocal = Vector3D.Rotate(shipVelWorld, worldToShipMat);
        }

        public float shipMass;
        
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

        if(_lastOrientOk)
        {
            str += "ORIENTED ";
        }
        if (_lastAlignmentOk)
        {
            str += "ALIGNED ";   
        }
        if (connector.Status == MyShipConnectorStatus.Connectable)
        {
            str += "CONNECTABLE ";
        }
        str += "\n";
        /*str += "POS\n";
        str += vectorToString(metrics.worldShipPos);
        str += "FORWARD - UP\n";
        str += vectorToString(metrics.shipWorldMatrix.Forward);
        str += vectorToString(metrics.shipWorldMatrix.Up);
        */
        str += "SHIP FWD - TGT FWD\n";
        str += vectorToString(metrics.shipWorldMatrix.Forward) + vectorToString(metrics.targetWorldMatrix.Forward);
        str += "SHIP UP - TGT UP\n";
        str += vectorToString(metrics.shipWorldMatrix.Up) + vectorToString(metrics.targetWorldMatrix.Up);

        str += "PITCH - YAW - ROLL\n";
        str += (metrics.pitchAngle*180.0/Math.PI).ToString("0.000"); str += ", ";
        str += (metrics.yawAngle * 180.0 / Math.PI).ToString("0.000"); str += ", ";
        str += (metrics.rollAngle * 180.0 / Math.PI).ToString("0.000"); str += "\n";

        str += "tgtDelta " + vectorToString(metrics.targetDelta); //str += "\n";
        str += "velLocal " + vectorToString(metrics.shipVelLocal); //str += "\n";
        str += "vCtrlTgt " + vectorToString(_lastVControl); //str += "\n";
        str += "vCtrl " + vectorToString(_lastVControl_pid); //str += "\n";

        str += "maxVel: " + vectorToString(_lastStoppingVmax);
        str += "maxAcc: " + _lastMaxAcc.ToString("0.000") + "\n";
        

        //str += "RL0: " + _r0Name + ", " + _l0Name + "\n";
        //str += "UD0: " + _u0Name + ", " + _d0Name + "\n";
        //str += "th " + thrusters.Count + " | " + thrustersF.Count + "," + thrustersB.Count + "," + thrustersR.Count + "," + thrustersL.Count + "," + thrustersU.Count + "," + thrustersD.Count;
        //str += "\n";
        lcd.WriteText(str);
    }

    void setThrustersPercentage(List<IMyThrust> list, float perc)
    {
        foreach (var thruster in list)
        {
            thruster.ThrustOverridePercentage = perc;
        }
    }

    

    void controlLocalVel(Metrics metrics, Vector3D vel, float maxPerc = 0.02f)
    {
        var fwd = pidFwd.update(metrics.shipVelLocal.Z, vel.Z);
        var right = pidRight.update(metrics.shipVelLocal.X, vel.X);
        var up = pidUp.update(metrics.shipVelLocal.Y, vel.Y);

        setThrustersPercentage(thrustersU, 0.0f);
        setThrustersPercentage(thrustersD, 0.0f);
        setThrustersPercentage(thrustersR, 0.0f);
        setThrustersPercentage(thrustersL, 0.0f);

        _lastVControl_pid.X = right;
        _lastVControl_pid.Y = up;

        List<IMyThrust> thrustersLRToSet = (right > 0.0) ? (thrustersR) : (thrustersL);
        List<IMyThrust> thrustersUDToSet = (up > 0.0) ? (thrustersU) : (thrustersD);

        right = trim(right, maxPerc);
        up = trim(up, maxPerc);
        setThrustersPercentage(thrustersLRToSet, (float)Math.Abs(right));
        setThrustersPercentage(thrustersUDToSet, (float)Math.Abs(up));
    }

    public static void Main()
    {
        var program = new Program();
        //program.Main("", UpdateType.Update1);
    }


    double getStoppingVmaxForMaxAcc(double maxAcc, double dist)
    {
        return (float)Math.Sqrt((2*maxAcc)/ Math.Abs(dist));
    }
    /*
    double getStoppingWmaxForMaxAngAcc(double maxAngAcc, double deltaAngle)
    {
        return getStoppingVmaxForMaxAcc(maxAngAcc, deltaAngle);
    }

    double getWorstAngularAcc()
    {
        return 1.0;
    }*/

    Vector3D getStoppingVmaxForMaxAcc(double maxAcc, Vector3D dist)
    {
        return new Vector3D(
            getStoppingVmaxForMaxAcc(maxAcc, dist.X),
            getStoppingVmaxForMaxAcc(maxAcc, dist.Y),
            getStoppingVmaxForMaxAcc(maxAcc, dist.Z)
            );
    }

    public void Main(string argument, UpdateType updateSource)
    {
        Vector3D halfBlockFWD = connector.WorldMatrix.Forward * 1.25;
        var connectorPos = connector.GetPosition() + halfBlockFWD;
        MatrixD targetSystem = MatrixD.Identity;

        //targetSystem.Right = new Vector3D(0.75, -0.25, 0.61);
        //targetSystem.Up = new Vector3D(-0.22, 0.77, 0.59);

        targetSystem.Right = new Vector3D(-0.75, 0.25, -0.61);
        targetSystem.Up = new Vector3D(0.22, -0.77, -0.59);

        targetSystem.Forward = new Vector3D(0.62, 0.58, -0.53);
        //targetSystem.Translation = new Vector3D(-32.60, 66.82, -62.67);
        double connSafetyMargin = 0.2;
        targetSystem.Translation = new Vector3D(-32.60, 66.82, -62.67) + targetSystem.Forward * -1.0 * (1.25 + connSafetyMargin);

        var metrics = new Metrics(rc, connectorPos, targetSystem);

        updateLcd(metrics);

        pidYaw.update(metrics.yawAngle, 0.0);
        pidPitch.update(metrics.pitchAngle, 0.0);
        pidRoll.update(metrics.rollAngle, 0.0);

        gyro.Yaw = (float)pidYaw.control;
        gyro.Pitch = (float)pidPitch.control;
        gyro.Roll = (float)pidRoll.control;

        double angleThreshold = 0.2 * Math.PI / 180.0;
        bool orientationOk =
            (Math.Abs(metrics.rollAngle) < angleThreshold) &&
            (Math.Abs(metrics.pitchAngle) < angleThreshold) &&
            (Math.Abs(metrics.yawAngle) < angleThreshold);
        _lastOrientOk = orientationOk;
        if (orientationOk)
        {
            gyro.Yaw = 0.0f;
            gyro.Roll = 0.0f;
            gyro.Pitch = 0.0f;
        } else {
            return;
        }

        float minThrust = thrusters[0].MaxEffectiveThrust;
        foreach (var thruster in thrusters)
        {
            var thrust = thruster.MaxEffectiveThrust;
            if (thrust < minThrust) minThrust = thrust;
        }
        float maxAcc = minThrust / metrics.shipMass;
        _lastMaxAcc = maxAcc;

        var stoppingVmax = getStoppingVmaxForMaxAcc(maxAcc, metrics.targetDelta);
        _lastStoppingVmax = stoppingVmax;

        double alignmentThreshold = 0.05;
        bool xAlignmentOk = (Math.Abs(metrics.targetDelta.X) < alignmentThreshold);
        bool yAlignmentOk = (Math.Abs(metrics.targetDelta.Y) < alignmentThreshold);
        bool alignmentOk = xAlignmentOk && yAlignmentOk;

        float signX = xAlignmentOk ? 0.0f : ((metrics.targetDelta.X < 0.0f) ? (-1.0f) : (1.0f));
        float signY = yAlignmentOk ? 0.0f : ((metrics.targetDelta.Y < 0.0f) ? (-1.0f) : (1.0f));
        _lastAlignmentOk = alignmentOk;
        if (!alignmentOk) {
            float alignVelX = (Math.Abs(metrics.targetDelta.X) < 1.0) ? 0.05f : (float)stoppingVmax.X;
            float alignVelY = (Math.Abs(metrics.targetDelta.Y) < 1.0) ? 0.05f : (float)stoppingVmax.Y;

            Vector3D v = new Vector3D(alignVelX * signX, alignVelY * signY, 0.0f);
            _lastVControl = v;
            controlLocalVel(metrics, v, 1.0f);
        } else {
            //controlLocalVel(metrics, new Vector3D(0.0, 0.0, ));
        }
    }

    Vector3D _lastVControl = new Vector3D();
    Vector3D _lastVControl_pid = new Vector3D();
    bool _lastAlignmentOk = false;
    bool _lastOrientOk = false;
    float _lastMaxAcc = 0.0f;
    Vector3D _lastStoppingVmax = new Vector3D();
    string _r0Name = "";
    string _l0Name = "";
    string _u0Name = "";
    string _d0Name = "";

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

