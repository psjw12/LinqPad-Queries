<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\Accessibility.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Configuration.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Deployment.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Runtime.Serialization.Formatters.Soap.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Security.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Windows.Forms.dll</Reference>
  <NuGetReference>HtmlAgilityPack</NuGetReference>
  <Namespace>System.Net</Namespace>
  <Namespace>HtmlAgilityPack</Namespace>
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
	var htmlDoc = new HtmlDocument();
	htmlDoc.LoadHtml(webString);
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
	var betaHtml = htmlDoc.GetElementbyId("beta");
	if(betaHtml != null)
	{
		Util.RawHtml(cssStyling).Dump();
		Util.RawHtml(betaHtml.OuterHtml).Dump();
	}
}

public static Hyperlinq DownloadFile(string url, string version)
{
	var wc = new WebClient();
	var downloadPath = $@"{downloadFolder}LINQPad5-AnyCPU {version}.zip";
	if(!File.Exists(downloadPath))
		wc.DownloadFile(url, downloadPath);
	return new Hyperlinq(downloadPath, $"Open {version}");
}