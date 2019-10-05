IMyGridTerminalSystem gts = null;

string bootRadioTag = "BOOT_RADIO";
Dictionary<long, int> db = new Dictionary<long, int>();
int maxId = 0;

public Program()
{
	gts = GridTerminalSystem;
	var listener = IGC.RegisterBroadcastListener(bootRadioTag);
	listener.SetMessageCallback("");
	Runtime.UpdateFrequency = UpdateFrequency.Update100;

}

void loadDb()
{
	db = new Dictionary<long, int>();
	var list = Storage.Split(';');
	for (int i = 0; i < list.Length / 2; i++)
	{
		string keyStr = list[2 * i];
		string valStr = list[2 * i + 1];
		if (keyStr == "" || valStr == "") continue;
		int val = 0;
		db.Add(long.Parse(keyStr), val = int.Parse(valStr));
		if (val > maxId) maxId = val;
	}
}

void saveDb()
{
	var str = "";
	foreach (var pair in db)
	{
		str += pair.Key.ToString() + ";" + pair.Value.ToString() + ";";
	}
	Storage = str;
}

public void Save()
{
	saveDb();

}
void poll()
{
	var listener = IGC.RegisterBroadcastListener(bootRadioTag);
	if (!listener.HasPendingMessage) return;
	loadDb();
	MyIGCMessage message = listener.AcceptMessage();
	int responseId = -1;
	if (db.ContainsKey(message.Source)) {
		responseId = db[message.Source];
	} else {
		responseId = maxId + 1;
		db.Add(message.Source, responseId);
		saveDb();
	}
	if (responseId >= 0) IGC.SendUnicastMessage(message.Source, bootRadioTag, responseId.ToString());
}

public void Main(string argument, UpdateType updateSource)
{
	Echo(argument + updateSource.ToString());
	switch (updateSource)
	{
		case UpdateType.IGC:
			Echo("IGC");
			poll();
			break;
		case UpdateType.Antenna:
			break;
		default:
			
			break;

	}
	poll();
	/*var lcd = GridTerminalSystem.GetBlockWithName("LCD.Builder.Up.Left") as IMyTextPanel;
	string str = "";
	foreach (var pair in db) { str += pair.Key.ToString() + " -> " + pair.Value.ToString() + "\r\n"; }
	lcd.WriteText(str);*/
}