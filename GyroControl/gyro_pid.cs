public Program() { Runtime.UpdateFrequency = UpdateFrequency.Update1; }

void printVector(IMyTextPanel panel, string prefix, Vector3D v)
{
	var str = "";
	str += prefix + " ";
	str += v.X.ToString("0.00") + ", ";
	str += v.Y.ToString("0.00") + ", ";
	str += v.Z.ToString("0.00");
	str += "\r\n";
	panel.WriteText(str, true);
}

IMyRemoteControl rc = null;
IMyGyro gyro = null;
IMyTextPanel lcd = null;

void printVelocities()
{
	var v = rc.GetShipVelocities();
	printVector(lcd, "V", v.LinearVelocity);
	printVector(lcd, "aV", v.AngularVelocity);
}

void printOrientation()
{
	Vector3D pF,pU,pR;
	getOrientationVectors(out pF,out pU,out pR);
	printVector(lcd, "F", pF);
	printVector(lcd, "R", pR);
	printVector(lcd, "U", pU);
	double roll, pitch;
	getRollPitch(out roll, out pitch);
	roll *= 180.0/Math.PI;
	pitch *= 180.0/Math.PI;
	lcd.WriteText("roll, pitch: " + roll.ToString("0.000") + ", " + pitch.ToString("0.000") + "\r\n",true);
}

void getOrientationVectors(out  Vector3D F, out Vector3D U, out Vector3D R) {
    var pOrig = Me.CubeGrid.GridIntegerToWorld(new Vector3I(0, 0, 0));
	var pF = Me.CubeGrid.GridIntegerToWorld(new Vector3I(1, 0, 0)) - pOrig;
	var pR = Me.CubeGrid.GridIntegerToWorld(new Vector3I(0, 0, 1)) - pOrig;
	var pU = Me.CubeGrid.GridIntegerToWorld(new Vector3I(0, 1, 0)) - pOrig;
	pF.Normalize();
	pR.Normalize();
	pU.Normalize();
    F = pF;
    R = pR;
    U = pU;
}

void getRollPitch(out double roll, out double pitch) {
	Vector3D F,U,R;
	getOrientationVectors(out F,out U,out R);
	var grav = rc.GetTotalGravity();
	var gravUpUnit = -1*grav;
	gravUpUnit.Normalize();

	double cosFG = F.Dot(gravUpUnit);
	double cosRG = R.Dot(gravUpUnit);
	pitch = Math.Acos(cosFG) - Math.PI/2.0;
	roll = Math.Acos(cosRG) - Math.PI/2.0;
}

float trim(float val,float th) {if(val<-th) return -th; if(val>th) return th; return val;}

void updateGyro(float targetRoll = 0.0f, float targetPitch = 0.0f, float dt = 0.016f) {
	float kP = 0.5f;
	float kI = 0.05f;
	float kD = 5.0f;
	
	float lastR = 0.0f;
	float lastP = 0.0f;
	float integR = 0.0f;
	float integP = 0.0f;
	if(gyro.CustomData!="") {
		var list = gyro.CustomData.Split(',');
		lastR = float.Parse(list[0]);
		lastP = float.Parse(list[1]);
		// no Yaw
		integR = float.Parse(list[3]);
		integP = float.Parse(list[4]);
		// no Yaw
	} else {
		gyro.CustomData = "0.0,0.0,0.0,0.0,0.0,0.0";
	}

	double _roll, _pitch;
	getRollPitch(out _roll, out _pitch);
	float roll = (float)_roll; float pitch = (float)_pitch;

	float eR = targetRoll-(float)roll;
	float eP = targetPitch-(float)pitch;

	integR += eR*dt;
	integP += eP*dt;

	float eR_old = targetRoll - lastR; // TODO: store lastER instead lastR
	float eP_old = targetPitch - lastP;

	float dR = (eR - eR_old)/dt;
	float dP = (eP - eP_old)/dt;

	integR = trim(integR,4.0f);
	integP = trim(integP,4.0f);

	string storeInfo = roll.ToString() + "," + pitch.ToString() + ",0.0," + integR + "," + integP + ",0.0";
	gyro.CustomData = storeInfo;

	float controlR = kP*eR + kD*dR + kI*integR;
	float controlP = kP*eP + kD*dP + kI*integP;
	controlP *= -1;
	//controlR = trim(controlR,10.0f);
	//controlP = trim(controlP,10.0f);
	gyro.Roll = controlR;
	gyro.Pitch = controlP;

	if(lcd != null) {
		lcd.WriteText("PID: "+  kP.ToString("0.0000") + ", "+ kI.ToString("0.0000") + ", " + kD.ToString("0.0000") + "\r\n",true);
		lcd.WriteText("Err: "+ eR.ToString("0.00") + ", "+ eP.ToString("0.00") + "\r\n",true);
		lcd.WriteText("dErr: "+ dR.ToString("0.00") + ", "+ dP.ToString("0.00") + "\r\n",true);
		lcd.WriteText("Int: "+ integR.ToString("0.00") + ", "+ integP.ToString("0.00") + "\r\n",true);
		lcd.WriteText("Ctrl: "+ (controlR*180.0/Math.PI).ToString("0.00") + ", "+ (controlP*180.0/Math.PI).ToString("0.00") + "\r\n",true);
	}
}


public void Main(string argument, UpdateType updateSource)
{
	rc = GridTerminalSystem.GetBlockWithName("RC.Main") as IMyRemoteControl;
	gyro = GridTerminalSystem.GetBlockWithName("Gyroscope") as IMyGyro;
	lcd = GridTerminalSystem.GetBlockWithName("LCD") as IMyTextPanel;
	lcd.WriteText("");
	printVelocities();
	printOrientation();
	updateGyro();
}

