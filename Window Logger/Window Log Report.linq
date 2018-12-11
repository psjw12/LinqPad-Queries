<Query Kind="Program">
  <Connection>
    <ID>7b920123-8af5-4d3a-83dc-a2541a09abfc</ID>
    <Persist>true</Persist>
    <Driver Assembly="IQDriver" PublicKeyToken="5b59726538a49684">IQDriver.IQDriver</Driver>
    <Provider>System.Data.SQLite</Provider>
    <CustomCxString>Data Source=D:\Users\Paul\db\window logger.db;FailIfMissing=True</CustomCxString>
    <AttachFileName>D:\Users\Paul\db\window logger.db</AttachFileName>
    <DriverData>
      <StripUnderscores>false</StripUnderscores>
      <QuietenAllCaps>false</QuietenAllCaps>
    </DriverData>
  </Connection>
</Query>

void Main()
{
	//Util.NewProcess = true;
	var date = DateTime.Now.Date;
	
	var entries = ActiveWindowProcessLogs.Where(l => l.Date == date.ToString("yyyy-MM-dd")).ToList();
	
	chartByProcess(date, entries);
	//chartByWindow(date, entries);
}

void chartByProcess(DateTime date, IEnumerable<ActiveWindowProcessLog> entries)
{
	var groupedEntries = entries
						 .GroupBy(e => e.ProcessPath)
						 .Select(e => new
						 {
						 	ProcessName = new FileInfo(e.Key).Name,
							Count = e.Count(),
							Windows = e.Where(w => w.WindowTitle != null)
									   .GroupBy(w => w.WindowTitle)
									   .Select(w => new { WindowTitle = w.Key, Count = w.Count() })
									   .OrderByDescending(w => w.Count)
						 })
						 .OrderByDescending(e => e.Count).Dump(2);
						 
	groupedEntries.Chart(e => e.ProcessName, e => e.Count, LINQPad.Util.SeriesType.Pie).Dump();
}

void chartByWindow(DateTime date, IEnumerable<ActiveWindowProcessLog> entries)
{
	var groupedEntries = entries
						 .GroupBy(e => e.ProcessPath + e.WindowTitle)
						 .Select(e => new
						 {
						 	ProcessName = new FileInfo(e.First().ProcessPath).Name,
							Count = e.Count(),
							WindowName = e.First().WindowTitle
						 })
						 .OrderByDescending(e => e.Count);
						 
						 var take = 10;
						 var chartEntries = groupedEntries.Take(take);
						 chartEntries = chartEntries.Append(new { ProcessName = "", Count = groupedEntries.Skip(take).Sum(c => c.Count), WindowName = "Others"});
						 chartEntries.Dump();
						 chartEntries.Chart(e => e.WindowName, e => e.Count, LINQPad.Util.SeriesType.Pie).Dump();
}


