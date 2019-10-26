<Query Kind="Program">
  <Namespace>System.Net</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

const string downloadFolder = @"D:\Users\Paul\Downloads\LinqPad\";
const string linqPad5Page = @"https://www.linqpad.net/LINQPad5.aspx";
const string linqPad6Page = @"https://www.linqpad.net/LINQPad6.aspx";
const string cssStyling = @"<style>pre {background-color:#DEE;border:solid 1px #088; margin: 1em 0; padding: 0.3em; font-family:Consolas, monospace; font-size:92%;}
#beta {font-family: calibri; font-size:120%}</style>";

private Output output = new Output();

async Task Main()
{
	Util.RawHtml(cssStyling).Dump();
	output.DumpAll();
	var currentVersionTask = Task.Run(() => DisplayCurrentVersion(output));
	var linqPad5Task = Task.Run(() => GetLinqPad5Versions(output));
	var linqPad6Task = Task.Run(() => GetLinqPad6Versions(output));

	await Task.WhenAll(currentVersionTask, linqPad5Task, linqPad6Task);
}

private static void DisplayCurrentVersion(Output output)
{
	var linqPadPath = Process.GetCurrentProcess().Modules.OfType<ProcessModule>().SingleOrDefault(pm => pm.FileName.EndsWith("LINQPad.Runtime.dll"))?.FileName ??
		LINQPad.Util.GetFullPath("LINQPad.exe");
	var linqPadVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(linqPadPath);
	output.CurrentVersion.Content = $"{linqPadVersionInfo.FileMajorPart}.{linqPadVersionInfo.FileMinorPart}.{linqPadVersionInfo.FileBuildPart}";
}

private void GetLinqPad5Versions(Output output)
{
	var html = new WebClient().DownloadString(linqPad5Page);
	var stableVersion = Regex.Match(html, @"(?<=<h4>What's New in )\d+\.\d+(\.\d+)?").Value;
	var stableDownloadUrl = Regex.Match(html, @"(?<=<a (style="""" )?href="").*?(?="")").Value;
	var stableChangeLog = Regex.Match(html, @"(?<=What's New in \d+\.\d+(\.\d+)?</h4>\s*)<ul>(?:.|\n)*?</ul>(?=\s*<h4>)").Value;
	output.Version5Stable.Content = Util.HorizontalRun(true,  
		Util.OnDemand(stableVersion, () => DownloadFile(stableDownloadUrl, stableVersion)),
		Util.OnDemand("Changelog", () => Util.RawHtml(stableChangeLog)));
	html = html.Substring(html.IndexOf("LINQPad 5 - Latest Beta"));
	var betaVersion = Regex.Match(html, @"(?<=Download LINQPad )\d+\.\d+(\.\d+)?(?= \(Any CPU)").Value;
	var betaDownloadUrl = Regex.Match(html, @"(?<=<a style="""" href="").*?AnyCPU.zip(?="">)").Value;
	var betaChangelog = Regex.Match(html, @"(?<=What's new:</p>\s*)<ul>(?:.|\n)*?</ul>(?=\s*<p)").Value;
	output.Version5Beta.Content = Util.HorizontalRun(true, 
		Util.OnDemand(betaVersion, () => DownloadFile(betaDownloadUrl, betaVersion)),
		Util.OnDemand("Changelog", () => Util.RawHtml(betaChangelog)));
}

private void GetLinqPad6Versions(Output output)
{
	var html = new WebClient().DownloadString(linqPad6Page);
	var stableVersion = Regex.Match(html, @"(?<=<h3>)\d+\.\d+(\.\d+)?(?= Release Notes)").Value;
	var stableDownloadUrl = Regex.Match(html, @"(?<=<a (style="""" )?href="").*?(?="">Download LINQPad 6 - installer)").Value;
	var stableChangelog = Regex.Match(html, @"(?<=New Features in LINQPad 6</h3>\s*)<ul>(?:.|\n)*?</ul>(?=\s*<h3>)").Value;
	output.Version6Stable.Content = Util.HorizontalRun(true, 
		Util.OnDemand(stableVersion, () => DownloadFile(stableDownloadUrl, stableVersion)),
		Util.OnDemand("Changelog", () => Util.RawHtml(stableChangelog)));
	html = html.Substring(html.IndexOf("LINQPad 6 - Latest Beta"));
	var betaVersion = Regex.Match(html, @"(?<=- version )\d+\.\d+(\.\d+)?").Value;
	var betaDownloadUrl = Regex.Match(html, @"(?<=<a style="""" href="").*?-Beta.zip(?="">)").Value;
	var betaChangelog = Regex.Match(html, @"(?<=Latest Beta</h2>\s*)(?:.|\n)*?(?=\s*<p style)").Value;
	output.Version6Beta.Content = Util.HorizontalRun(true, 
	Util.OnDemand(betaVersion, () => DownloadFile(betaDownloadUrl, betaVersion)),
	Util.OnDemand("Changelog", () => Util.RawHtml(betaChangelog)));
}

private static Hyperlinq DownloadFile(string url, string version)
{
	var wc = new WebClient();
	var downloadPath = $@"{downloadFolder}LINQPad5-AnyCPU {version}.zip";
	if(!File.Exists(downloadPath))
		wc.DownloadFile(url, downloadPath);
	return new Hyperlinq(downloadPath, $"Open {version}");
}

public class Output
{
	public DumpContainer CurrentVersion = new DumpContainer("Loading...");
	public DumpContainer Version5Stable = new DumpContainer("Loading...");
	public DumpContainer Version5Beta = new DumpContainer("Loading...");
	public DumpContainer Version6Stable = new DumpContainer("Loading...");
	public DumpContainer Version6Beta = new DumpContainer("Loading...");
	
	public void DumpAll()
	{
		CurrentVersion.Dump("Current Version");
		Version5Stable.Dump("LinqPad 5 Stable");
		Version5Beta.Dump("LinqPad 5 Beta");
		Version6Stable.Dump("LinqPad 6 Stable");
		Version6Beta.Dump("LinqPad 6 Beta");
	}
}