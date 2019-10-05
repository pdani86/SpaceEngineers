string bootRadioTag = "BOOT_RADIO";
public Program()
{
	Runtime.UpdateFrequency = UpdateFrequency.Update10;
}

public void Save()
{
}

void broadcastBootRequest()
{
	long nowMs = DateTime.Now.ToFileTimeUtc() / 10000;
	long lastBroadcastTimeMs = long.Parse(Storage);
	if (lastBroadcastTimeMs + 2000 > nowMs) return;
	string msg = "DONTCARE";
	IGC.SendBroadcastMessage(bootRadioTag, msg, TransmissionDistance.TransmissionDistanceMax);
}

void listenBootResponse()
{
	var uni = IGC.UnicastListener;
	if (!uni.HasPendingMessage) return;
	var msg = uni.AcceptMessage();
	if (msg.Tag != bootRadioTag) return;
	// TODO: check sender??
	int ix = int.Parse(msg.Data.ToString());
	Me.CustomData = ix.ToString();
}

void enableThrustGyro(bool en)
{
	var listThrust = new List<IMyThrust>();
	var listGyro = new List<IMyGyro>();
	GridTerminalSystem.GetBlocksOfType<IMyThrust>(listThrust);
	GridTerminalSystem.GetBlocksOfType<IMyGyro>(listGyro);
	for (int i = 0; i < listThrust.Count; i++) { listThrust[i].Enabled = en; }
	for (int i = 0; i < listGyro.Count; i++) { listGyro[i].Enabled = en; }
}

bool buildBoot()
{
	if (Me.CustomData == "")
	{
		broadcastBootRequest();
		listenBootResponse();
		if (Me.CustomData != "") return true;
	}
	return false;
}

void onBootReady()
{
	var rc = (IMyRemoteControl)GridTerminalSystem.GetBlockWithName("RC.Main");
	if (rc == null) return;
	rc.ClearWaypoints();
	var grav = rc.GetTotalGravity();
	var gravDir = grav;
	gravDir.Normalize();

	var coord_0 = new VRageMath.Vector3D(53574.53, -26640.24, 12128.82);
	var coord_1 = new VRageMath.Vector3D(53605.52, -26608.60, 12113.01);
	var newWP = new VRageMath.Vector3D(coord_0);
	var diff = coord_1 - coord_0;
	var serial = int.Parse(Me.CustomData);
	newWP += diff * (serial);
	var newWP0 = newWP + -10.0f * gravDir;

	rc.AddWaypoint(new MyWaypointInfo("What's UP?!", newWP0));
	rc.AddWaypoint(new MyWaypointInfo("What's DOWN?!", newWP));

	rc.SetDockingMode(true);
	rc.SpeedLimit = 3.0f;
	rc.FlightMode = FlightMode.OneWay;
	rc.Direction = VRageMath.Base6Directions.Direction.Forward;
	rc.SetAutoPilotEnabled(true);
}

public void Main(string argument, UpdateType updateSource)

{
	if (Me.CustomData == "") if (buildBoot()) onBootReady();
	var merge = (IMyShipMergeBlock)GridTerminalSystem.GetBlockWithName("Merge.Base");
	if (merge == null) return;
	enableThrustGyro(!merge.IsConnected);
}