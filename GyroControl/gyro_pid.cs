double trim(double val,double th) {if(val<-th) return -th; if(val>th) return th; return val;}

class PID_Control
{
	public double kP = 2.0;
	public double kI = 0.5;
	public double kD = 4.0;
	
	public double integ = 0.0;
	public double lastVal = 0.0;
	public double lastErr = 0.0;
	public double control = 0.0;
	
	double trim(double val,double th) {if(val<-th) return -th; if(val>th) return th; return val;}
	
	public double update(double signal, double target, double dt = 0.016666) {
		double err = target - signal;
		integ += err * dt;
		double d = (err - lastErr)/dt;
		integ = trim(integ,2.0f);
		control = kP*err + kD*d + kI*integ;
		lastVal = signal;
		lastErr = err;
		//control = trim(control,10.0f);
		return control;
	}
};

IMyRemoteControl rc = null;
List<IMyGyro> gyros = new List<IMyGyro>();
List<IMyThrust> thrusterUpList = new List<IMyThrust>();
List<IMyRemoteControl> remoteControls = new List<IMyRemoteControl>();
PID_Control pidRoll = new PID_Control();
PID_Control pidPitch = new PID_Control();
//PID_Control pidThrusters = new PID_Control();

public Program() {
	Runtime.UpdateFrequency = UpdateFrequency.Update1;
	GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(remoteControls);
	GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrusterUpList);
	GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyros);
	if(remoteControls.Count>0) rc = remoteControls[0];
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
	var controlRP = calcGyroControl(vRef);
	
	controlRP *= 1.0;
	
	for(int i=0;i<gyros.Count;i++) {
		gyros[i].GyroOverride = true;
		gyros[i].Roll = (float)controlRP.X;
		gyros[i].Pitch = (float)controlRP.Y;
	}
	
	// thruster control
	//double cosV = v.Dot(vTarget);
	
	float thrusterOverrideVal = 1.0f;
	//if(curSpeed > 95.0) thrusterOverrideVal = 0.1f;

	for(int i=0;i<thrusterUpList.Count;i++) {
		thrusterUpList[i].ThrustOverridePercentage = thrusterOverrideVal;
	}
}

public void Main(string argument, UpdateType updateSource)
{
	
	updateToTarget(new Vector3D(54804.56,-27195.05,11812.88));
	//53538.92, -26695.54, 12130.25
	// Enemy_54474.60,-26995.93,7156.81
	
	//rc.ControlThrusters = false;
	//rc.DampenersOverride = false;
}
