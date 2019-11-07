<Query Kind="Program">
  <Namespace>System.Runtime.InteropServices</Namespace>
  <Namespace>System.Runtime.InteropServices.ComTypes</Namespace>
</Query>

private DateTime now;

void Main()
{
	Util.AutoScrollResults = true;
	var outlook = new Application();
	var store = outlook.Session.Stores["e-mail address"];
	var deleteInSearchFolder = store.GetSearchFolders()["Delete in"];
	var deletedItems = store.GetDefaultFolder(OlDefaultFolders.olFolderDeletedItems);
	now = DateTime.Now.Date;
	
	var i = 1;
	var deleteCount = 0;
	var totalCount = deleteInSearchFolder.Items.Count;
	var errorCount = 0;
	
	while(i <= deleteInSearchFolder.Items.Count)
	{
		try
		{
			var mailItem = deleteInSearchFolder.Items[i] as MailItem;
			
			if (mailItem != null)
			{
				if(CanDelete(mailItem.Categories, mailItem.ReceivedTim))
				{
					new
					{
						From = mailItem.Sender?.Name,
						To = string.Join("; ", mailItem.Recipients.Cast<Recipient>().Select(r => r.Name)),
						Subject = mailItem.Subject,
						DateRecieved = mailItem.ReceivedTime,
						Folder = ((MAPIFolder)mailItem.Parent).Name
					}.Dump();
					//mailItme.Move(deletedItems);
					mailItem.Delete();
					deleteCount ++;
					Thread.Sleep(200);
				}
				else
				{
					i++;
				}
				continue;
			}
			
			var calendarItem = deleteInSearchFolder.Items[i] as MeetingItem;
			
			if(calendarItem != null)
			{
				if(CanDelete(calendarItem.Categories, calendarItem.ReceivedTime))
				{
					new
					{
						From = calendarItem.SenderName,
						To = string.Join("; ", calendarItem.Recipients.Cast<Recipient>().Select(r => r.Name)),
						Subject = calendarItem.Subject,
						DateRecieved = calendarItem.ReceivedTime,
						Folder = ((MAPIFolder)calendarItem.Parent).Name
					}.Dump();
					//calendarItem.Move(deletedItems);
					calendarItem.Delete();
					deleteCount++;
					Thread.Sleep(260);
				}
				else
				{
					i++;
				}
				continue;
			}
		}
		catch(System.Runtime.InteropServices.COMException)
		{
			Util.Metatext("COMException").Dump();
			errorCount++;
			i++;
			continue;
		}

		Util.Metatext($"Can not handle type {GetTypeName(deletedItems.Items[i])}").Dump();
		
		i++;
		continue;
	}

	new[] { $"Total ({totalCount})", $"Deleted ({deleteCount})", $"Errored ({errorCount})"}
		.Chart()
		.AddYSeries(new[] { (totalCount - deleteCount) - errorCount, deleteCount, errorCount}, LINQPad.Util.SeriesType.Pie)
		.Dump();
}

public bool CanDelete(string categories, DateTime receivedTime)
{
	if(categories.Contains("Do Not Delete"))
	{
		return false;
	}
	
	if (categories.Contains("Delete in 1 week"))
	{
		return receivedTime < now.AddDays(-7);
	}
	else if (categories.Contains("Delete in 2 weeks"))
	{
		return receivedTime < now.AddDays(-14);
	}
	else if (categories.Contains("Delete in 1 month"))
	{
		return receivedTime < now.AddMonths(-1);
	}
	else if (categories.Contains("Delete in 3 months"))
	{
		return receivedTime < now.AddMonths(-3);
	}
	else if (categories.Contains("Delete in 6 months"))
	{
		return receivedTime < now.AddMonths(-6);
	}
	else if (categories.Contains("Delete in 1 year"))
	{
		return receivedTime < now.AddYears(-1);
	}
	else if (categories.Contains("Delete end of day"))
	{
		return receivedTime < now;
	}
	
	return false;
}

public static string GetTypeName(object comObject)
{
	var dispatch = comObject as IDispatch;
	
	if(dispatch == null)
	{
		return null;
	}
	
	var pTypeInfo = dispatch.GetTypeInfo(0, 1033);
	
	string pBstrName;
	string pBstrDocString;
	int pdwHelpContext;
	string pBstrHelpFile;
	pTypeInfo.GetDocumentation(
		-1,
		out pBstrName,
		out pBstrDocString,
		out pdwHelpContext,
		out pBstrHelpFile);
		
		string str = pBstrName;
		if(str[0] == 95)
		{
			// remove leading '_'
			str = str.Substring(1);
		}
		
		return str;
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("00020400-0000-0000-C000-000000000046")]
public interface IDispatch
{
	int GetTypeInfoCount();
	
	[return: MarshalAs(UnmanagedType.Interface)]
	ITypeInfo GetTypeInfo(
		[In, MarshalAs(UnmanagedType.U4)] int iTInfo,
		[In, MarshalAs(UnmanagedType.U4)] int lcid);
		
	void GetIDsOfName(
		[In] ref Guid riid,
		[In, MarshalAs(UnmanagedType.LPArray)] string[] rgszNames,
		[In, MarshalAs(UnmanagedType.U4)] int cNames,
		[In, MarshalAs(UnmanagedType.U4)] int lcid,
		[Out, MarshalAs(UnmanagedType.LPArray)] int[] rgDispId);
}