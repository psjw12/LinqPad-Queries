<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Windows.Forms.dll</Reference>
  <Namespace>LINQPad.Controls</Namespace>
  <Namespace>NativeWindow = System.Windows.Forms.NativeWindow</Namespace>
  <Namespace>System.Xml.Xsl</Namespace>
</Query>

/*
	TODO:
		* Auto update result on text or file change
		* Add support for XLST extensions (use reflection)
			* Needs to load assembly to bind to
			* Need to either specify or select class to load (must they have no parameters? Do they have a attribute?)
			* Need to specify namespace
			* Needs including in state save
*/

private DumpContainer xmlOuput = new DumpContainer();

void Main()
{
	var inputSelection = DrawUi();
	ScriptState.Load(inputSelection);
}

InputSelection DrawUi()
{
	var handle = Process.GetProcessById(Util.HostProcessID).MainWindowHandle;
	var linqPadWindow = new NativeWindow();
	linqPadWindow.AssignHandle(handle);	
	var xmlSelection = DrawInputFields("XML File", "XML Files (*.xml)|*.xml|All files (*.*)|*.*", "Select XML File", linqPadWindow);
	var xsltSelection = DrawInputFields("XSLT File", "XSLT Files (*.xslt)|*.xslt|All files (*.*)|*.*", "Select XSLT File", linqPadWindow);
	var xsltExtensionsSelection = DrawXstlExtensionsInput(linqPadWindow);
	var inputSelection = new InputSelection(xmlSelection, xsltSelection, xsltExtensionsSelection);
	new Button("Transform", b => Transform(inputSelection)).Dump();
	xmlOuput.Dump("XML Output");
	return inputSelection;
}

FileSelection DrawInputFields(string sectionTitle, string fileFilter, string fileSelectionTitle, NativeWindow linqPadWindow)
{
	var fileLink = new Hyperlink("►File");
	var textLink = new Hyperlink("Text");
	var fieldTextWrapPanel = new WrapPanel(new[] { fileLink, textLink });
	var fileText = new TextBox();
	var fileBrowse = new Button("Browse");
	var fileWrapPanel = new WrapPanel(new Control[] { fileText, fileBrowse});
	var inputText = new TextArea { AutoHeight = false, Rows = 8 };
	var textWrapPanel = new WrapPanel(inputText) { Visible = false };
	var fileSelection = new FileSelection(fileText, inputText);

	Action switchToFile = () =>
	{
		fileLink.Text = "►File";
		textLink.Text = "Text";
		fileWrapPanel.Visible = true;
		textWrapPanel.Visible = false;
	};
	
	Action switchToText = () =>
	{
		fileLink.Text = "File";
		textLink.Text = "►Text";
		fileWrapPanel.Visible = false;
		textWrapPanel.Visible = true;
	};

	fileLink.Click += (sender, e) => {
		switchToFile();
		fileSelection.Source = UserQuery.FileSelection.Sources.Path;
	};

	textLink.Click += (sender, e) =>
	{
		switchToText();
		fileSelection.Source = UserQuery.FileSelection.Sources.Text;
	};
	
	fileBrowse.Click += (sender, e) =>
	{
		var openFileDialog = new System.Windows.Forms.OpenFileDialog {
			CheckFileExists = true,
			CheckPathExists = true,
		    Filter = fileFilter,
		    Title = fileSelectionTitle};
		
		var result = openFileDialog.ShowDialog(linqPadWindow);
		if(result == System.Windows.Forms.DialogResult.OK)
		{
			fileText.Text = openFileDialog.FileName;
		}
	};

	var section = new FieldSet(sectionTitle);
	section.Children.Add(fieldTextWrapPanel);
	section.Children.Add(fileWrapPanel);
	section.Children.Add(textWrapPanel);
	section.Dump();
	
	fileSelection.SourceChanged += (source, e) => {
		if(fileSelection.Source == UserQuery.FileSelection.Sources.Path)
			switchToFile();
		else
			switchToText();
	};
	
	return fileSelection;
}

IList<XsltExtension> DrawXstlExtensionsInput(NativeWindow linqPadWindow)
{
	var headerRow = new TableRow(new[] { new Span("Assembly"), new Span("Class"), new Span("Namespace URI"), new Span() });
	var tableRows = new List<TableRow>();
	var extensionDumpContainer = new DumpContainer().Dump();
	Action renderExtensionTable = null;
	var openFileDialog = new System.Windows.Forms.OpenFileDialog
	{
		CheckFileExists = true,
		CheckPathExists = true,
		Filter = "Dynamic Link Library (*.dll)|*.dll",
		Title = "Select .NET Assembly"
	};
	var addButton = new Button("Add", b =>
	{
		TableRow tableRow = null;
		var extensionPathTextbox = new TextBox(width: "20em");
		var typeDataListBox = new DataListBox() { Width = "20em" };
		extensionPathTextbox.TextInput += (sender, e) => { typeDataListBox.Options = GetClassesInAssembly(extensionPathTextbox.Text); };
		var browseButton = new Button("Browse", b2 => {
			var dialogResult = openFileDialog.ShowDialog(linqPadWindow);
			if (dialogResult == System.Windows.Forms.DialogResult.OK)
			{
				extensionPathTextbox.Text = openFileDialog.FileName;
				typeDataListBox.Options = GetClassesInAssembly(extensionPathTextbox.Text);
			}				
		}); 
		var removeButton = new Button("Remove", b2 => {
			tableRows.Remove(tableRow);
			renderExtensionTable();
		});
		tableRow = new TableRow(new Control[] { new WrapPanel(new Control[] { extensionPathTextbox, browseButton }), typeDataListBox, new TextBox(width: "20em"), removeButton});
		tableRows.Add(tableRow);
		renderExtensionTable();
	});
	renderExtensionTable = () =>
	{
		var extensionsTable = new Table(new[] { headerRow }, true);
		extensionsTable.Rows.AddRange(tableRows);
		var extensionFieldSet = new FieldSet("XSLT Extensions", new Control[] { extensionsTable, addButton });
		extensionDumpContainer.Content = extensionFieldSet;
	};
	renderExtensionTable();
	return null;
}

string[] GetClassesInAssembly(string assemblyPath)
{
	if(!File.Exists(assemblyPath))
		return new string[0];
		
	try {
		var assembly = Assembly.LoadFile(assemblyPath);
		var types = assembly.GetTypes();
		var noConstructorParametersTypes = types.Where(t => t.GetConstructors().Any(c => c.GetParameters().Count() == 0));
		return noConstructorParametersTypes.Select(t => t.FullName).ToArray();
	}
	catch (Exception)
	{
		return new string[0];
	}
}

void Transform(InputSelection inputSelection)
{
	ScriptState.Save(inputSelection);
	var xslt = new XslCompiledTransform(true);
	using (var xsltFile = inputSelection.XsltSelection.GetFile())
	using (var xmlFile = inputSelection.XmlSelection.GetFile())
	using (var stream = new MemoryStream())
	{
		xslt.Load(xsltFile);
		xslt.Transform(inputSelection.XmlSelection.GetFile(), new XsltArgumentList(), stream);
		stream.Position = 0;
		var xml = XDocument.Load(stream);
		this.xmlOuput.Content = xml;
	}
}

public class FileSelection
{
	public enum Sources
	{
		Path = 1,
		Text = 2
	}

	private Sources _source = Sources.Path;
	public Sources Source
	{
		get
		{
			return this._source;
		}
		set
		{
			this._source = value;
			SourceChanged(this, new EventArgs());
		}
	}

	public string FilePath
	{
		get
		{
			return this.pathTextControl.Text;
		}
		set {
			this.pathTextControl.Text = value;
		}
	}

	public string Text
	{
		get
		{
			return this.xmlTextControl.Text;
		}
		set
		{
			this.xmlTextControl.Text = value;
		}
	}
	
	public event EventHandler SourceChanged;
	
	private ITextControl pathTextControl;
	private ITextControl xmlTextControl;
	
	public FileSelection(ITextControl pathTextControl, ITextControl xmlTextControl)
	{
		this.pathTextControl = pathTextControl;
		this.xmlTextControl = xmlTextControl;
	}

	public XmlReader GetFile()
	{
		if (this.Source == Sources.Path)
		{
			using (var stream = new FileStream(this.FilePath, FileMode.Open))
			{
				var streamReader = new System.IO.StreamReader(stream);
				var xml = streamReader.ReadToEnd();
				var stringReader = new StringReader(xml);
				var reader = new XmlTextReader(stringReader);
				return reader;
			}
		}
		else
		{
			var stringReader = new StringReader(this.Text);
			var xmlReader = new XmlTextReader(stringReader);
			return xmlReader;
		}
	}	
}

public class XsltExtension
{
	
}

public class InputSelection
{
	public FileSelection XmlSelection {get; private set;}
	
	public FileSelection XsltSelection {get; private set;}
	
	public IList<XsltExtension> XsltExtensions {get; private set;}
	
	public InputSelection(FileSelection xmlSelection, FileSelection xsltSelection, IList<XsltExtension> xsltExtensions)
	{
		this.XmlSelection = xmlSelection;
		this.XsltSelection = xsltSelection;
		this.XsltExtensions = xsltExtensions;
	}
}

public static class ScriptState
{
	public static string SavePath =>
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LINQPad", "XsltDebug.xml");
	
	public static void Save(InputSelection inputSelection)
	{
		var saveXml = new XDocument(new XElement("XsltDebugState", new[] {
		new XElement("XmlSelection", new[] {
			new XElement("Source", inputSelection.XmlSelection.Source),
			new XElement("FilePath", inputSelection.XmlSelection.FilePath),
			new XElement("Text", new XCData(inputSelection.XmlSelection.Text))
		}),
		new XElement("XsltSelection", new[] {
			new XElement("Source", inputSelection.XsltSelection.Source),
			new XElement("FilePath", inputSelection.XsltSelection.FilePath),
			new XElement("Text", new XCData(inputSelection.XsltSelection.Text))})
		}));
		saveXml.Declaration = new XDeclaration("1.0", "utf-8", null);		
		saveXml.Save(SavePath);
	}
	
	public static void Load(InputSelection inputSelection)
	{
		if(!File.Exists(SavePath))
			return;
			
		var saveXml = XDocument.Load(SavePath);
		var xmlSelection = saveXml.Root.Element("XmlSelection");
		inputSelection.XmlSelection.Source = (FileSelection.Sources)Enum.Parse(typeof(FileSelection.Sources), xmlSelection.Element("Source").Value);
		inputSelection.XmlSelection.FilePath = xmlSelection.Element("FilePath").Value;
		inputSelection.XmlSelection.Text = xmlSelection.Element("Text").Value;
		var xsltSelection = saveXml.Root.Element("XsltSelection");
		inputSelection.XsltSelection.Source = (FileSelection.Sources)Enum.Parse(typeof(FileSelection.Sources), xsltSelection.Element("Source").Value);
		inputSelection.XsltSelection.FilePath = xsltSelection.Element("FilePath").Value;
		inputSelection.XsltSelection.Text = xsltSelection.Element("Text").Value;
	}
}