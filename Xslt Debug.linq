<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Windows.Forms.dll</Reference>
</Query>

void Main()
{
	DrawUi();
	//Util.KeepRunning();
}

void DrawUi()
{
	
	var xmlFileLink = new LINQPad.Controls.Hyperlink("►File");
	var xmlTextLink = new LINQPad.Controls.Hyperlink("Text");
	var xmlFieldTextWrapPanel = new LINQPad.Controls.WrapPanel(new[] { xmlFileLink, xmlTextLink });
	var xmlFileText = new LINQPad.Controls.TextBox();
	var xmlFileBrowse = new LINQPad.Controls.Button("Browse");
	var xmlFileWrapPanel = new LINQPad.Controls.WrapPanel(new LINQPad.Controls.Control[] { xmlFileText, xmlFileBrowse});
	var xmlText = new LINQPad.Controls.TextArea { AutoHeight = false, Rows = 8 };
	var xmlTextWrapPanel = new LINQPad.Controls.WrapPanel(xmlText) { Visible = false };

	xmlFileLink.Click += (sender, e) => {
		xmlFileLink.Text = "►File";
		xmlTextLink.Text = "Text";
		xmlFileWrapPanel.Visible = true;
		xmlTextWrapPanel.Visible = false;
	};

	xmlTextLink.Click += (sender, e) =>
	{
		xmlFileLink.Text = "File";
		xmlTextLink.Text = "►Text";
		xmlFileWrapPanel.Visible = false;
		xmlTextWrapPanel.Visible = true;
	};
	
	xmlFileBrowse.Click += (sender, e) =>
	{
		var openFileDialog = new System.Windows.Forms.OpenFileDialog {
			CheckFileExists = true,
			CheckPathExists = true,
		    Filter = "XML Files (*.xml)|*.xml|All files (*.*)|*.*",
		    Title = "Select XML File"};

		var handle = Process.GetProcessById(Util.HostProcessID).MainWindowHandle;
		var win32Window = new System.Windows.Forms.NativeWindow();
		win32Window.AssignHandle(handle);
		
		var result = openFileDialog.ShowDialog(win32Window);
		if(result == System.Windows.Forms.DialogResult.OK)
		{
			xmlFileText.Text = openFileDialog.FileName;
		}
	};

	var xmlField = new LINQPad.Controls.FieldSet("XML File");
	xmlField.Children.Add(xmlFieldTextWrapPanel);
	xmlField.Children.Add(xmlFileWrapPanel);
	xmlField.Children.Add(xmlTextWrapPanel);
	xmlField.Dump();
}
