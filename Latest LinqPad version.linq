<Query Kind="Program">
  <Namespace>System.Net</Namespace>
</Query>

const string downloadFolder = @"C:\Users\Paul\Downloads\LinqPad";
const string linqPadDownloadUrl = @"http://www.linqpad.net/GetFile.aspx?LINQPad5-AnyCPU.zip";
const string linqPadBetaDownloadUrl = @"http://www.linqpad.net/GetFile.aspx?preview+LINQPad5-AnyCPU.zip";

void Main()
{
	DisplayCurrentVersion();
	DisplayWebsiteVersions();
}

public static void DisplayCurrentVersion()
{
	var linqPadPath = LINQPad.Util.GetFullPath("Linqpad.exe");
	var linqPadVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(linqPadPath);
	$"{linqPadVersionInfo.FileMajorPart}.{linqPadVersionInfo.FileMinorPart}.{linqPadVersionInfo.FileBuildPart.ToString("00")}".Dump("Current version");
}

public static void DisplayWebsiteVersions()
{
	var wc = new WebClient();
	var webString = wc.DownloadString(@"http://www.linqpad.net/Download.aspx");
	var lines = webString.Split('\n');
	var index = @"				Current release version: <b>";
	var versionLine = lines.Single(l => l.Contains(index));
	versionLine = versionLine.Substring(index.Length);
	var versionString = versionLine.Substring(0, versionLine.IndexOf("</b>"));
	Util.OnDemand(versionString, () => DownloadFile(linqPadDownloadUrl, versionString)).Dump("Stable");
	var betaVersionLine = lines.Single(l => l.Contains(@"<a style="""" href=""https://www.linqpad.net/GetFile.aspx?preview+LINQPad5-AnyCPU.zip"">"));
	var betaIndex = "Download LINQPad ";
	betaVersionLine = betaVersionLine.Substring(betaVersionLine.IndexOf(betaIndex) + betaIndex.Length);
	var betaVersionString = betaVersionLine.Substring(0, betaVersionLine.IndexOf(' '));
	Util.OnDemand(betaVersionString, () => DownloadFile(linqPadBetaDownloadUrl, betaVersionString)).Dump("Beta");
}

public static Hyperlinq DownloadFile(string url, string version)
{
	var wc = new WebClient();
	var downloadPath = $@"{downloadFolder}LINQPad5-AnyCPU {version}.zip";
	if(!File.Exists(downloadPath))
		wc.DownloadFile(url, downloadPath);
	return new Hyperlinq(downloadPath, $"Open {version}");
}