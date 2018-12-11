<Query Kind="Statements" />

var windowsInstaller = Type.GetTypeFromProgID("WindowsInstaller.Installer");
dynamic msi = Activator.CreateInstance(windowsInstaller);

dynamic products = msi.Products;
var productList = new List<string>();
var patchPaths = new List<string>();

foreach(var comProductCode in products)
{
	string stringProductCoode = comProductCode;
	productList.Add(stringProductCoode);
	dynamic patches = msi.Patches(comProductCode);
	foreach (var comPatchCode in patches)
	{
		string location = msi.PatchInfo(comPatchCode, "LocalPackage");
		patchPaths.Add(location.ToLower());
	}
}

var installerFolderPaths = Directory.GetDirectories(@"C:\windows\Installer\", "{*}", SearchOption.TopDirectoryOnly);
var installerFolders = installerFolderPaths.Select(ifp => new DirectoryInfo(ifp)).ToList();

var orphanInstallerFolders = installerFolders.Where(x => !productList.Contains(x.Name)).Select(x => x.Name);

var patchFiles = Directory.GetFiles(@"C:\windows\Installer\", "*.msp", SearchOption.TopDirectoryOnly);

var orphanPatchFiles = patchFiles.Where(f => !patchPaths.Contains(f.ToLower()));
var orphanPatchFilesSize = orphanPatchFiles.Sum(o => new FileInfo(o).Length);
var orphans = orphanInstallerFolders.Union(orphanPatchFiles);

(orphanPatchFilesSize / 1024 / 1024).Dump("Total Size (MB)");
orphans.Dump("orphans");

/*
foreach (var file in orphans)
{
	var fileInfo = new FileInfo(file);
	fileInfo.IsReadOnly = false;
	fileInfo.Delete();
	$"Deleted {file}".Dump();
}
*/