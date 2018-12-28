<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Windows.Forms.dll</Reference>
</Query>

void Main()
{
	DrawUi();
}

void DrawUi()
{
	DrawInputFields("XML File", "XML Files (*.xml)|*.xml|All files (*.*)|*.*", "Select XML File");
	DrawInputFields("XSLT File", "XSLT Files (*.xslt)|*.xslt|All files (*.*)|*.*", "Select XSLT File");
}

void DrawInputFields(string sectionTitle, string fileFilter, string fileSelectionTitle)
{
	
	var fileLink = new LINQPad.Controls.Hyperlink("►File");
	var textLink = new LINQPad.Controls.Hyperlink("Text");
	var fieldTextWrapPanel = new LINQPad.Controls.WrapPanel(new[] { fileLink, textLink });
	var fileText = new LINQPad.Controls.TextBox();
	var fileBrowse = new LINQPad.Controls.Button("Browse");
	var fileWrapPanel = new LINQPad.Controls.WrapPanel(new LINQPad.Controls.Control[] { fileText, fileBrowse});
	var inputText = new LINQPad.Controls.TextArea { AutoHeight = false, Rows = 8 };
	var textWrapPanel = new LINQPad.Controls.WrapPanel(inputText) { Visible = false };

	fileLink.Click += (sender, e) => {
		fileLink.Text = "►File";
		textLink.Text = "Text";
		fileWrapPanel.Visible = true;
		textWrapPanel.Visible = false;
	};

	textLink.Click += (sender, e) =>
	{
		fileLink.Text = "File";
		textLink.Text = "►Text";
		fileWrapPanel.Visible = false;
		textWrapPanel.Visible = true;
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

	var section = new LINQPad.Controls.FieldSet(sectionTitle);
	section.Children.Add(fieldTextWrapPanel);
	section.Children.Add(fileWrapPanel);
	section.Children.Add(textWrapPanel);
	section.Dump();
}