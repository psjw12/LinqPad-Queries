<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Windows.Forms.dll</Reference>
  <Namespace>LINQPad.Controls</Namespace>
</Query>

/*
	TODO: for v1:
		* Do simple coversation and dump output in container (manual refresh)
	TODO: for future version:
		* Remember last configuration and load on start
		* Auto update result on text or file change
		* Add support for XLST extensions (use reflection)
*/

void Main()
{
	var inputSelection = DrawUi();
}

InputSelection DrawUi()
{
	var xmlSelection = DrawInputFields("XML File", "XML Files (*.xml)|*.xml|All files (*.*)|*.*", "Select XML File");
	var xsltSelection = DrawInputFields("XSLT File", "XSLT Files (*.xslt)|*.xslt|All files (*.*)|*.*", "Select XSLT File");
	return new InputSelection(xmlSelection, xsltSelection);
}

FileSelection DrawInputFields(string sectionTitle, string fileFilter, string fileSelectionTitle)
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

		var handle = Process.GetProcessById(Util.HostProcessID).MainWindowHandle;
		var win32Window = new System.Windows.Forms.NativeWindow();
		win32Window.AssignHandle(handle);
		
		var result = openFileDialog.ShowDialog(win32Window);
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
		throw new NotImplementedException();
	}	
}

public class InputSelection
{
	public FileSelection XmlSelection {get; private set;}
	
	public FileSelection XsltSelection {get; private set;}
	
	public InputSelection(FileSelection xmlSelection, FileSelection xsltSelection)
	{
		this.XmlSelection = xmlSelection;
		this.XsltSelection = xsltSelection;
	}
}