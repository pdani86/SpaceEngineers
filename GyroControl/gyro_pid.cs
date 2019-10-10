IMyRemoteControl rc = null;
List<IMyGyro> gyros = new List<IMyGyro>();
List<IMyThrust> thrusterUpList = new List<IMyThrust>();
List<IMyRemoteControl> remoteControls = new List<IMyRemoteControl>();
IMyTerminalBlock pidStoreObject = null;

public Program() {
	Runtime.UpdateFrequency = UpdateFrequency.Update1;
	
	GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(remoteControls);
	GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrusterUpList);
	GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyros);
	pidStoreObject = (IMyTerminalBlock)gyros[0];
	
	if(remoteControls.Count>0) rc = remoteControls[0];
}

float trim(float val,float th) {if(val<-th) return -th; if(val>th) return th; return val;}

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
	pitch = Math.Acos(cosFG) - Math.PI/2.0;
	roll = Math.Acos(cosRG) - Math.PI/2.0;
}

void getRollPitchGravity(out double roll, out double pitch) {
	var grav = rc.GetTotalGravity();
	var gravUpUnit = -1*grav;
	gravUpUnit.Normalize();
	getRollPitch(out roll, out pitch,gravUpUnit);
}

Vector3D calcGyroControl(Vector3D vRef, float dt = 0.016f, float targetRoll = 0.0f, float targetPitch = 0.0f) {
	float kP = 2.0f;
	float kI = 0.2f;
	float kD = 4.0f;
	
	float lastR = 0.0f;
	float lastP = 0.0f;
	float integR = 0.0f;
	float integP = 0.0f;
	
	if(pidStoreObject.CustomData!="") {
		var list = pidStoreObject.CustomData.Split(',');
		lastR = float.Parse(list[0]);
		lastP = float.Parse(list[1]);
		// no Yaw
		integR = float.Parse(list[3]);
		integP = float.Parse(list[4]);
		// no Yaw
	} else {
		pidStoreObject.CustomData = "0.0,0.0,0.0,0.0,0.0,0.0";
	}

	double _roll, _pitch;
	getRollPitch(out _roll, out _pitch, vRef);
	float roll = (float)_roll; float pitch = (float)_pitch;

	float eR = targetRoll-(float)roll;
	float eP = targetPitch-(float)pitch;

	integR += eR*dt;
	integP += eP*dt;

	float eR_old = targetRoll - lastR; // TODO: store lastER instead lastR
	float eP_old = targetPitch - lastP;

	float dR = (eR - eR_old)/dt;
	float dP = (eP - eP_old)/dt;

	integR = trim(integR,2.0f);
	integP = trim(integP,2.0f);

	string storeInfo = roll.ToString() + "," + pitch.ToString() + ",0.0," + integR + "," + integP + ",0.0";
	pidStoreObject.CustomData = storeInfo;

	float controlR = kP*eR + kD*dR + kI*integR;
	float controlP = kP*eP + kD*dP + kI*integP;
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
	//double cosV = v.Dot();
	
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
