double trim(double val,double th) {if(val<-th) return -th; if(val>th) return th; return val;}

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
	
	double trim(double val,double th) {if(val<-th) return -th; if(val>th) return th; return val;}
	
	public double update(double signal, double target, double dt = 0.016666) {
		double err = target - signal;
		integ += err * dt;
		double d = (err - lastErr)/dt;
		integ = trim(integ, integTrim);
		control = kP*err + kD*d + kI*integ;
		lastVal = signal;
		lastErr = err;
		return control;
	}
};

IMyRemoteControl rc = null;
List<IMyGyro> gyros = new List<IMyGyro>();
List<IMyThrust> thrusterUpList = new List<IMyThrust>();
List<IMyRemoteControl> remoteControls = new List<IMyRemoteControl>();
PID_Control pidRoll = new PID_Control();
PID_Control pidPitch = new PID_Control();
PID_Control pidYaw = new PID_Control();
PID_Control pidThrusters = new PID_Control();
PID_Control pidDx = new PID_Control();
PID_Control pidDz = new PID_Control();

public Program() {
	Runtime.UpdateFrequency = UpdateFrequency.Update1;
	GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(remoteControls);
	GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrusterUpList);
	GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyros);
	if(remoteControls.Count>0) rc = remoteControls[0];
	
	pidDx.integTrim = 0.05;
	pidDz.integTrim = 0.05;
	pidDx.kD = 0.5;
	pidDz.kD = 0.5;
	pidDx.kI = 0.0;
	pidDz.kI = 0.0;

	pidThrusters.kP = 0.2;
	pidThrusters.kD = 5.0;
	pidThrusters.kI = 0.5;
}

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

void getRollPitchGravity(out double roll, out double pitch) {
	var grav = rc.GetTotalGravity();
	var gravUpUnit = -1*grav;
	gravUpUnit.Normalize();
	getRollPitch(out roll, out pitch,gravUpUnit);
}

Vector3D calcGyroControl(Vector3D vRef, float dt = 0.016f, float targetRoll = 0.0f, float targetPitch = 0.0f) {
	double _roll, _pitch;
	getRollPitch(out _roll, out _pitch, vRef);
	float roll = (float)_roll; float pitch = (float)_pitch;

	double controlR = pidRoll.update(roll, targetRoll);
	double controlP = pidPitch.update(pitch, targetPitch);
	//controlR = trim(controlR,10.0f);
	//controlP = trim(controlP,10.0f);
	return new Vector3D(controlR, controlP, 0.0);
}

public enum PathType {
	Direct,
	UpTravelTarget,
	UpTravelBallisticRelease
};

PathType pathType = PathType.UpTravelTarget;

void updateToTarget(Vector3D targetPos)
{
	// Me
	var v_av = rc.GetShipVelocities();
	var v = v_av.LinearVelocity;
	var curSpeed = v.Length();
	var curPos = rc.GetPosition();
	
	// gravity
	var grav = rc.GetTotalGravity();
	var gravUpUnit = -1*grav;
	gravUpUnit.Normalize();
	
	// target
	var targetRelPos = targetPos - curPos;
	double targetDist = targetRelPos.Length();
	double vertDist = -1 * gravUpUnit.Dot(targetRelPos);
	double horizDist = Math.Sqrt(targetDist*targetDist - vertDist*vertDist);
	var vertVec = vertDist * gravUpUnit;
	var horizVec = vertVec + targetRelPos;

	// path / target velocity selection
	var vTarget = new Vector3D();
	switch(pathType)
	{
		case PathType.UpTravelTarget:
		{
			double heightTarget = 4000.0;
			if(horizDist > 2000.0) {
				var heightTargetVec = (heightTarget - vertDist) * gravUpUnit + 0.05 * horizVec;
				vTarget = heightTargetVec;
			} else if(true) {
				vTarget = targetRelPos;
			}
			vTarget.Normalize();
			vTarget *= 150.0;
		}
		break;
		
		case PathType.Direct:
		default:
		{
			vTarget = targetRelPos;
			vTarget.Normalize();
			vTarget *= 150.0;
		}
		break;
	}

	// target direction to achieve target velocity
	var vDiff = vTarget - v;
	var vRef = vDiff;
	
	// gyro control
	var controlRPY = calcGyroControl(vRef);
	
	controlRPY *= 1.0;
	
	bool bYawControl = true;
	if(bYawControl) {
		double yawSpeed = v_av.AngularVelocity.Dot(rc.WorldMatrix.Up);
		controlRPY.Z = pidYaw.update(yawSpeed, 0.0);
		controlRPY.Z *= -1.0;
	}
	
	for(int i=0;i<gyros.Count;i++) {
		gyros[i].GyroOverride = true;
		gyros[i].Roll = (float)controlRPY.X;
		gyros[i].Pitch = (float)controlRPY.Y;
		gyros[i].Yaw = (float)controlRPY.Z;
	}
	
	// thruster control
	//double cosV = v.Dot(vTarget);
	
	float thrusterOverrideVal = 1.0f;
	//if(curSpeed > 95.0) thrusterOverrideVal = 0.1f;

	for(int i=0;i<thrusterUpList.Count;i++) {
		thrusterUpList[i].ThrustOverridePercentage = thrusterOverrideVal;
	}
}

void userControlOrStabilize()
{
	// Me
	var v_av = rc.GetShipVelocities();
	var v = v_av.LinearVelocity;
	var curSpeed = v.Length();
	var curPos = rc.GetPosition();
	
	// gravity
	var grav = rc.GetTotalGravity();
	var gravUpUnit = -1*grav;
	gravUpUnit.Normalize();

	var vRef = gravUpUnit;
	double moveTh = 0.1;
	double dx = 0.0;
	double dz = 0.0;
	
	var F = rc.WorldMatrix.Forward;
	var R = rc.WorldMatrix.Right;
	var U = rc.WorldMatrix.Up;
	double _vX = v.Dot(R);
	double _vY = v.Dot(U);
	double _vZ = v.Dot(F);

	double gravVel = gravUpUnit.Dot(v);

	if(Math.Abs(rc.MoveIndicator.X)>moveTh) {
	  dx = -1.0 * rc.MoveIndicator.X;
	} else {
	  dx = pidDx.update(_vX,0.0);
	}

	if(Math.Abs(rc.MoveIndicator.Z)>moveTh) {
	  dz = rc.MoveIndicator.Z;
	} else {
	  dz = pidDz.update(_vZ,0.0);
	}

	dx = trim(dx,0.6); // dx, dz : local, vRef global (world)
	dz = trim(dz,0.6);

	vRef += rc.WorldMatrix.Right * dx;
	vRef += rc.WorldMatrix.Forward * dz;
	vRef.Normalize();
	
	var controlRPY = calcGyroControl(vRef);
	
	bool bYawControl = true;
	if(Math.Abs(rc.RotationIndicator.Y) > moveTh) {
		double val = 0.5;
		controlRPY.Z = (rc.RotationIndicator.Y > 0.0)?(val):(-val);
		bYawControl = false;
	}
	if(bYawControl) {
		double yawSpeed = v_av.AngularVelocity.Dot(rc.WorldMatrix.Up);
		controlRPY.Z = pidYaw.update(yawSpeed, 0.0);
		controlRPY.Z *= -1.0;
	}
	
	for(int i=0;i<gyros.Count;i++) {
		gyros[i].GyroOverride = true;
		gyros[i].Roll = (float)controlRPY.X;
		gyros[i].Pitch = (float)controlRPY.Y;
		gyros[i].Yaw = (float)controlRPY.Z;
	}
	
	//double thrusterOverrideVal = pidThrusters.update(gravVel,0.0);
	double elev = 0.0;
	rc.TryGetPlanetElevation(MyPlanetElevation.Surface,out elev);
	double thrusterOverrideVal = pidThrusters.update(elev,100.0);
	if(thrusterOverrideVal > 1.0) thrusterOverrideVal = 1.0;
	if(thrusterOverrideVal < 0.001) thrusterOverrideVal = 0.001;

	//if(curSpeed > 95.0) thrusterOverrideVal = 0.1f;

	for(int i=0;i<thrusterUpList.Count;i++) {
		thrusterUpList[i].ThrustOverridePercentage = (float)thrusterOverrideVal;
	}
}

public void Main(string argument, UpdateType updateSource)
{
	userControlOrStabilize();
	//updateToTarget(new Vector3D(54474.60,-26995.93,7156.81));
	//53538.92, -26695.54, 12130.25
	// Enemy_54474.60,-26995.93,7156.81
	
	//rc.ControlThrusters = false;
	//rc.DampenersOverride = false;
}
