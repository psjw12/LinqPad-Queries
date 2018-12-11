<Query Kind="Program">
  <Connection>
    <ID>7b920123-8af5-4d3a-83dc-a2541a09abfc</ID>
    <Persist>true</Persist>
    <Driver Assembly="IQDriver" PublicKeyToken="5b59726538a49684">IQDriver.IQDriver</Driver>
    <Provider>System.Data.SQLite</Provider>
    <CustomCxString>Data Source=D:\Users\Paul\db\window logger.db;FailIfMissing=True</CustomCxString>
    <AttachFileName>D:\Users\Paul\db\window logger.db</AttachFileName>
    <DriverData>
      <StripUnderscores>false</StripUnderscores>
      <QuietenAllCaps>false</QuietenAllCaps>
    </DriverData>
  </Connection>
  <Namespace>System.Runtime.InteropServices</Namespace>
</Query>

#define TRACE

void Main()
{
	var timer = new Timer(LogWindow, null, 0, 5000);
	Util.Cleanup += (sender, ef) => timer.Dispose();
	Util.KeepRunning();
}

void LogWindow(object state)
{
	var hwnd = GetForegroundWindow().DumpTrace("Window handle");
	if(hwnd != IntPtr.Zero)
	{
		GetWindowThreadProcessId(hwnd, out int pid)
		var process = Process.GetProcessById(pid);
		var processPath = process.MainModule.FileName.DumpTrace("Process path");
		var windowTitle = GetWindowTitle(hwnd).DumpTrace("Window Title");
		
		var tableEntry = new ActiveWindowProcessLog
		{
			ProcessPath = processPath,
			Date = DateTime.Now.ToString("yyyy-MM-dd"),
			Time = DateTime.Now.ToString("HH:mm:ss"),
			WindowTitle = windowTitle
		};
		ActiveWindowProcessLogs.InsertOnSubmit(tableEntry);
		this.SubmitChanges();
	}
}

string GetWindowTitle(IntPtr hwnd)
{
	var stringBuilder = new StringBuilder();
	stringBuilder.Capacity = 200;
	GetWindowText(hwnd, stringBuilder, stringBuilder.Capacity);
	return stringBuilder.ToString();
}

[DllImport("user32.dll")]
private static extern IntPtr GetForegroundWindow();

[DllImport("user32.dll")]
private static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out int ProcessId);

[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
private static extern IntPtr GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

