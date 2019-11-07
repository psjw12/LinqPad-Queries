<Query Kind="Program">
  <Namespace>System.Runtime.InteropServices</Namespace>
  <Namespace>System.Runtime.InteropServices.ComTypes</Namespace>
</Query>

private DateTime threeMonthsAgo;

void Main()
{
	Util.AutoScrollResults = true;
	var outlook = new Application();
	var store = outlook.Session.Stores["paul.watson@swiftcover.com"];
	var deletedItems = store.GetDefaultFolder(OlDefaultFolders.olFolderDeletedItems);
	threeMonthsAgo = DateTime.Now.Date.AddMonths(-3);

	var i = 1;
	var deleteCount = 0;
	var totalCount = deletedItems.Items.Count;
	var errorCount = 0;

	while (i <= deletedItems.Items.Count)
	{
		try
		{
			var mailItem = deletedItems.Items[i] as MailItem;

			if (mailItem != null)
			{
				var lastModificationTime = (DateTime)((ItemProperty)mailItem.ItemProperties["LastModificationTime"]).Value;
				if (CanDeleteMailItem(mailItem, lastModificationTime))
				{
					new
					{
						From = mailItem.Sender?.Name,
						To = string.Join("; ", mailItem.Recipients.Cast<Recipients>().Select(r => r.Name)),
						Subject = mailItem.Subject,
						DateReceived = mailItem.ReceivedTime,
						LastModificationTime = lastModificationTime,
						Type = nameof(MailItem),
						Categories = mailItem.Categories

					}.Dump();
					mailItem.Delete();
					deleteCount++;
					Thread.Sleep(200);
				}
				else
				{
					i++
				}
				continue;
			}

			var meetingItem = deletedItems.Item[i] as MeetingItem;

			if (meetingItem != null)
			{
				var lastModificationTime = (DateTime)((ItemProperty)meetingItem.ItemProperties["LastModificationTime"]).Value;
				if (!(meetingItem.Categories?.Contains("No Pernament Delete") ?? true)
				&& !(meetingItem.Categories?.Contains("Do Not Delete") ?? true)
				&& lastModificationTime < threeMonthsAgo)
				{
					new
					{
						From = meetingItem.SenderName,
						To = string.Join("; ", meetingItem.Recipients.Cast<Recipient>().Select(r => r.Name)),
						Subject = meetingItem.Subject,
						DateReceived = meetingItem.ReceivedTime,
						LastModificationtime = lastModificationTime,
						Type = nameof(MeetingItem),
						Categories = meetingItem.Categories

					}.Dump();
					meetingItem.Delete();
					deleteCount++;
					Thread.Sleep(200);
				}
				else
				{
					i++;
				}
				continue;
			}

			var appointmentItem = deletedItems.Item[i] as AppointmentItem;

			if (appointmentItem != null)
			{
				var lastModificationTime = (DateTime)((ItemProperty)appointmentItem.ItemProperties["LastModificationTime"]).Value;
				if (!(appointmentItem.Categories?.Contains("No Pernament Delete") ?? true)
				&& !(appointmentItem.Categories?.Contains("Do Not Delete") ?? true)
				&& lastModificationTime < threeMonthsAgo)
				{
					new
					{
						To = string.Join("; ", appointmentItem.Recipients.Cast<Recipient>().Select(r => r.Name)),
						Subject = appointmentItem.Subject,
						LastModificationtime = lastModificationTime,
						Type = nameof(AppointmentItem),
						Categories = appointmentItem.Categories

					}.Dump();
					appointmentItem.Delete();
					deleteCount++;
					Thread.Sleep(200);
				}
				else
				{
					i++;
				}
				continue;
			}
		}
		catch (System.Runtime.InteropServices.COMException)
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

	new[] { $"Total ({totalCount})", $"Deleted ({deleteCount})", $"Errored ({errorCount})" }
		.Chart()
		.AddYSeries(new[] { (totalCount - deleteCount) - errorCount, deleteCount, errorCount}, LINQPad.Util.SeriesType.Pie)
		.Dump();
}

private bool CanDeleteMailItem(MailItem mailItem, DateTime lastModificationTime)
{
	if((mailItem.Categories?.Contains("No Pernament Delete") ?? false) ||
	(mailItem.Categories?.Contains("Do Not Delete") ?? false))
		return false;
		
	if(lastModificationTime > threeMonthsAgo)
		return false;
		
	if(lastModificationTime.Date == mailItem.ReceivedTime.Date &&
	(mailItem.Categories?.Contains("Delete in ") ?? false))
	{
		return CanDeleteAfterCategory(mailItem.Categories, lastModificationTime);
	}
	
	return true;
}

public bool CanDeleteAfterCategory(string categories, DateTime lastModificationTime)
{
	if (categories.Contains("Delete in 1 week"))
	{
		return lastModificationTime < threeMonthsAgo.AddDays(-7);
	}
	else if (categories.Contains("Delete in 2 weeks"))
	{
		return lastModificationTime < threeMonthsAgo.AddDays(-14);
	}
	else if (categories.Contains("Delete in 1 month"))
	{
		return lastModificationTime < threeMonthsAgo.AddMonths(-1);
	}
	else if (categories.Contains("Delete in 3 months"))
	{
		return lastModificationTime < threeMonthsAgo.AddMonths(-3);
	}
	else if (categories.Contains("Delete in 6 months"))
	{
		return lastModificationTime < threeMonthsAgo.AddMonths(-6);
	}
	else if (categories.Contains("Delete in 1 year"))
	{
		return lastModificationTime < threeMonthsAgo.AddYears(-1);
	}
	else if (categories.Contains("Delete end of day"))
	{
		return lastModificationTime < threeMonthsAgo;
	}

	return true;
}

public static string GetTypeName(object comObject)
{
	var dispatch = comObject as IDispatch;

	if (dispatch == null)
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
	if (str[0] == 95)
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