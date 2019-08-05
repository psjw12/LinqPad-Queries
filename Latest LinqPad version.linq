<Query Kind="Program">
  <Namespace>System.Net</Namespace>
</Query>

const string downloadFolder = @"C:\Users\Paul\Downloads\LinqPad";
const string linqPadDownloadUrl = @"http://www.linqpad.net/GetFile.aspx?LINQPad5-AnyCPU.zip";
const string linqPadBetaDownloadUrl = @"http://www.linqpad.net/GetFile.aspx?preview+LINQPad5-AnyCPU.zip";
const string cssStyling = @"<style>pre {background-color:#DEE;border:solid 1px #088; margin: 1em 0; padding: 0.3em; font-family:Consolas, monospace; font-size:92%;}
#beta {font-family: calibri; font-size:120%}</style>";

void Main()
{
	DisplayCurrentVersion();
	DisplayWebsiteVersions();
}

private static void DisplayCurrentVersion()
{
	var linqPadPath = LINQPad.Util.GetFullPath("Linqpad.exe");
	var linqPadVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(linqPadPath);
	$"{linqPadVersionInfo.FileMajorPart}.{linqPadVersionInfo.FileMinorPart}.{linqPadVersionInfo.FileBuildPart.ToString("00")}".Dump("Current version");
}

private static void DisplayWebsiteVersions()
{
	var html = new WebClient().DownloadString(@"https://www.linqpad.net/Download.aspx");
	var currentVersion = Regex.Match(html, @"(?<=Current release version: <b>)\d+\.\d+\.\d+").Value;;
	Util.OnDemand(currentVersion, () => DownloadFile(linqPadDownloadUrl, currentVersion)).Dump("Stable");
	var betaVersion = Regex.Match(html, @"(?<=Download LINQPad )\d+\.\d+\.\d+").Value;
	Util.OnDemand(betaVersion, () => DownloadFile(linqPadBetaDownloadUrl, betaVersion)).Dump("Beta");
	var betaChangelog = Regex.Match(html, @"(?<=<h4>What's New<\/h4>\W+)(?:.|\n)*?<\/ul>")?.Value;
	if (betaChangelog != null)
	{
		Util.RawHtml(cssStyling).Dump();
		Util.RawHtml(betaChangelog).Dump();
	}
}

private static Hyperlinq DownloadFile(string url, string version)
{
	var wc = new WebClient();
	var downloadPath = $@"{downloadFolder}LINQPad5-AnyCPU {version}.zip";
	if(!File.Exists(downloadPath))
		wc.DownloadFile(url, downloadPath);
	return new Hyperlinq(downloadPath, $"Open {version}");
}