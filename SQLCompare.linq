<Query Kind="Program">
  <NuGetReference>Dapper</NuGetReference>
  <Namespace>Dapper</Namespace>
  <Namespace>System.Collections.Concurrent</Namespace>
</Query>

//#define TRACE

public const string SOURCE_SERVER = "";
public const string SOURCE_DATABASE = "";
public const bool SOURCE_USE_WINDOWS_CREDENTIALS = false;
public const string SOURCE_USERNAME = "";
public const string TARGET_SERVER = "";
public const string TARGET_DATABASE = "";
public const bool TARGET_USE_WINDOWS_CREDENTIALS = false;
public const string TARGET_USERNAME = "";

const string indexQuery = "SELECT COL_NAME(ic.object_id, ic.column_id) AS ColumnName FROM sys.indexes i INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id WHERE i.object_id = @object_id AND i.is_primary_key = 1";

public IDisposable keepRunning;

public void Main()
{
	using (var connections = CreateConnections())
	{
		if (connections == null) return;
		Util.Cleanup += (sender, e) => connections.Dispose();
		ListTables(connections);
	}
	this.keepRunning = Util.KeepRunning();
}

public SourceTargetConnections CreateConnections()
{
	var sourceConnection = CreateConnection(SOURCE_SERVER, SOURCE_DATABASE, SOURCE_USE_WINDOWS_CREDENTIALS, SOURCE_USERNAME);
	if (sourceConnection == null) return null;
	var targetConnection = CreateConnection(TARGET_SERVER, TARGET_DATABASE, TARGET_USE_WINDOWS_CREDENTIALS, TARGET_USERNAME);
	if (targetConnection == null) return null;

	try
	{
		sourceConnection.Open();
	}
	catch (Exception e)
	{
		Util.SetPassword($"Enter password for SQL user \"{SOURCE_USERNAME}\"", null);
		throw new Exception("Source database connection failed. See Inner Exception.", e);
	}

	try
	{
		targetConnection.Open();
	}
	catch (Exception e)
	{
		sourceConnection.Close();
		Util.SetPassword($"Enter password for SQL user \"{TARGET_USERNAME}\"", null);
		throw new Exception("Target database connection failed. See Inner Exception.", e);
	}

	return new SourceTargetConnections() { SourceConnection = sourceConnection, TargetConnection = targetConnection };
}

public SqlConnection CreateConnection(string server, string database, bool useWindowsCredentials, string username)
{
	var connections = new SourceTargetConnections();
	var sourceConnectionString = $"Server={server};Database={database};";
	if (useWindowsCredentials == false && !string.IsNullOrEmpty(username))
	{
		var password = Util.GetPassword($"Enter password for SQL user \"{username}\"");
		if (password == null) return null;
		sourceConnectionString += $"User Id={username};Password={password};";
	}
	else
	{
		sourceConnectionString += $"Trusted_Connection=True";
	}

	return new SqlConnection(sourceConnectionString);
}

public void ListTables(SourceTargetConnections connections)
{
	const string tableQuery = @"SELECT t.object_id, s.name + '.' + t.name FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id";
	var sourceTables = connections.SourceConnection.Query<(int objectId, string tableName)>(tableQuery);
	var targetTables = connections.TargetConnection.Query<(int objectId, string tableName)>(tableQuery);

	var commonTableNames = sourceTables.Select(t => t.tableName).Intersect(targetTables.Select(t => t.tableName)).OrderBy(t => t);
	var commonTablesSelectors = new List<TableItem>();
	commonTablesSelectors.Add(new TableItem { TableName = "SELECT ALL", Selector = new DumpContainer(new LINQPad.Controls.CheckBox(onClick: c => SelectAllItems(commonTablesSelectors, c.Checked)))});
	foreach (var commonTableName in commonTableNames)
	{
		var tableItem = new TableItem {
			SourceTableId = sourceTables.Single(t => t.tableName == commonTableName).objectId,
			TargetTableId = targetTables.Single(t => t.tableName == commonTableName).objectId,
			TableName = commonTableName };
		tableItem.Selector = new DumpContainer(new LINQPad.Controls.CheckBox(onClick: c => tableItem.Selected = c.Checked));
		commonTablesSelectors.Add(tableItem);
	}

	commonTablesSelectors.Dump("Table selection list");
	new LINQPad.Controls.Button("Compare", b => CompareTableData(commonTablesSelectors)).Dump();
}

public void SelectAllItems(List<TableItem> tableItems, bool select)
{
	foreach (var tableItem in tableItems.Skip(1))
	{
		((LINQPad.Controls.CheckBox)tableItem.Selector.Content).Checked = select;
		tableItem.Selected = select;
	}
}

public class SourceTargetConnections : IDisposable
{
	public SqlConnection SourceConnection { get; set; }
	public SqlConnection TargetConnection { get; set; }

	public void Dispose()
	{
		if (this.SourceConnection != null)
			this.SourceConnection.Dispose();

		if (this.TargetConnection != null)
			this.TargetConnection.Dispose();
	}
}

public void CompareTableData(List<TableItem> tablesToCompare)
{
	var compareSw = new Stopwatch();
	compareSw.Start();
	CreateProgressBars(tablesToCompare.Where(t => t.Selected));

	var reportAll = new List<ReportItem>();
	var syncSqlAll = new StringBuilder();

	var parallelOptions = new System.Threading.Tasks.ParallelOptions() { MaxDegreeOfParallelism = 4 };
	System.Threading.Tasks.Parallel.ForEach(tablesToCompare.Where(t => t.Selected), parallelOptions, table =>
		{
			var report = new List<ReportItem>();
			var syncSql = new StringBuilder();
			var connections = CreateConnections();
			
			var commonIndexes = GetTableIndexes(connections, table, report);
			if (commonIndexes is null)
				return;

			var commonKeys = CompareKeysAndReturnMatchingKeys(table, commonIndexes, connections, report, syncSql);
			var commonKeysCount = commonKeys.Count();

			var i = 0;
			foreach (IDictionary<string, object> row in commonKeys)
			{
				(var sourceRow, var targetRow) = GetRows(commonIndexes, row, table, connections);
				var differenceSql = new StringBuilder();

				foreach (KeyValuePair<string, object> column in sourceRow)
				{
					var source = column.Value;
					var target = targetRow[column.Key];
					$"Column: {column.Key},  Source value: {source}, Target value: {target}".DumpTrace("Value");

					if (!(source is null && target is null) && ((source is null && !(target is null)) || !source.Equals(target)))
					{
						if (differenceSql.Length == 0)
							differenceSql.AppendFormat("UPDATE {0} SET {1} = {2}", table.TableName, column.Key, FormatSqlValue(source));
						else
							differenceSql.AppendFormat(", {0} = {1}", column.Key, FormatSqlValue(source));
					}
				}

				if (differenceSql.Length > 0)
				{
					differenceSql.Append(" ");
					var whereClause = CreateWhereClause(commonIndexes.ToDictionary(c => c, c => row[c]));
					differenceSql.Append(whereClause);
					differenceSql.Append("\r\n\r\n");
					syncSql.Append(differenceSql.ToString());
					report.Add(new ReportItem() { TableName = table.TableName, DifferenceType = UserQuery.ReportItem.DifferenceTypes.DifferentValues, Difference = Util.Dif(sourceRow, targetRow) });
				}

				var progressBar = (Util.ProgressBar)table.Selector.Content;
				i++;
				var percent = (int)(((decimal)i / commonKeysCount) * 100);
				progressBar.Percent = percent;
			}
			
			lock (reportAll)
				reportAll.AddRange(report);
			lock (syncSqlAll)
				syncSqlAll.Append(syncSql.ToString());
		}
	);

	reportAll.Dump("Report");
	if(syncSqlAll.Length > 0)
		PanelManager.DisplaySyntaxColoredText(syncSqlAll.ToString(), SyntaxLanguageStyle.SQL, "Generated SQL");
	compareSw.Stop();
	compareSw.Dump();
	this.keepRunning.Dispose();
}

public void CreateProgressBars(IEnumerable<TableItem> tables)
{
	foreach (var table in tables)
	{
		table.Selector.Content = new Util.ProgressBar();
	}
}

public IEnumerable<string> GetTableIndexes(SourceTargetConnections connections, TableItem table, IList<ReportItem> report)
{
	var sourceIndexColumns = connections.SourceConnection.Query<string>(indexQuery, new { object_id = table.SourceTableId });
	sourceIndexColumns.DumpTrace("sourceIndexColumns");
	var targetIndexColumns = connections.TargetConnection.Query<string>(indexQuery, new { object_id = table.TargetTableId });
	targetIndexColumns.DumpTrace("targetIndexColumns");
	var commonIndexes = sourceIndexColumns.Intersect(targetIndexColumns);
	if(sourceIndexColumns.Count() == 0 && targetIndexColumns.Count() == 0)
	{
		report.Add(new ReportItem() { TableName = table.TableName, DifferenceType = UserQuery.ReportItem.DifferenceTypes.NoIndex });
		return null;
	}
	else if (commonIndexes.Count() < sourceIndexColumns.Count() || commonIndexes.Count() < targetIndexColumns.Count())
	{
		report.Add(new ReportItem() { TableName = table.TableName, DifferenceType = UserQuery.ReportItem.DifferenceTypes.IndexMismatch });
		return null;
	}
	
	return commonIndexes;
}

public IEnumerable<object> CompareKeysAndReturnMatchingKeys(TableItem table, IEnumerable<string> commonIndexes, SourceTargetConnections connections, IList<ReportItem> report, StringBuilder syncSql)
{
	var query = $"SELECT {string.Join(", ", commonIndexes)} FROM {table.TableName}";
	var sourceKeys = connections.SourceConnection.Query<object>(query);
	sourceKeys.DumpTrace("sourceKeys");
	var targetKeys = connections.TargetConnection.Query<object>(query);
	targetKeys.DumpTrace("targetKeys");

	var keysOnlyInSource = sourceKeys.Except(targetKeys, new DapperRowComparer()).Cast<IDictionary<string, object>>().ToList().DumpTrace("keysNotInTarget");
	if(keysOnlyInSource.Count > 0)
	{
		syncSql.AppendFormat("SET IDENTITY_INSERT {0} ON\r\n\r\n", table.TableName);
		keysOnlyInSource.ForEach(t => ReportKeyOnlyInSource(t, report, table, connections, syncSql, commonIndexes));
		syncSql.AppendFormat("SET IDENTITY_INSERT {0} OFF\r\n\r\n", table.TableName);
	}
	
	var keysOnlyInTarget = targetKeys.Except(sourceKeys, new DapperRowComparer()).Cast<IDictionary<string, object>>().ToList().DumpTrace("keysNotInSource");
	keysOnlyInTarget.ForEach(s => ReportKeyOnlyInTarget(s, report, table, connections, syncSql, commonIndexes));
	
	var commonKeys = sourceKeys.Intersect(targetKeys, new DapperRowComparer());
	commonKeys.DumpTrace("Common keys");
	
	return commonKeys;
}

public (IDictionary<string, object>, IDictionary<string, object>) GetRows(IEnumerable<string> commonIndexes, IDictionary<string, object> row, TableItem table, SourceTargetConnections connections)
{
	
	var whereClause = CreateWhereClause(commonIndexes.ToDictionary(c => c, c => row[c]));

	var rowQuery = $"SELECT * FROM {table.TableName} {whereClause}";
	IDictionary<string, object> sourceRow = connections.SourceConnection.QuerySingle<dynamic>(rowQuery);
	sourceRow.DumpTrace("source row");
	IDictionary<string, object> targetRow = connections.TargetConnection.QuerySingle<dynamic>(rowQuery);
	targetRow.DumpTrace("target row");
	
	return (sourceRow, targetRow);
}

public void ReportKeyOnlyInSource(IDictionary<string, object> key, IList<ReportItem> report, TableItem table, SourceTargetConnections connections, StringBuilder syncSql, IEnumerable<string> commonIndexes)
{
	report.Add(new ReportItem() { TableName = table.TableName, DifferenceType = UserQuery.ReportItem.DifferenceTypes.OnlyExistsInSource, Difference = key });
/*
	var whereClause = new StringBuilder();
	var iterationIndex = 0;
	foreach (var commonIndex in commonIndexes)
	{
		if (iterationIndex > 0)
			whereClause.Append(" AND ");
		whereClause.Append(commonIndex);
		whereClause.Append(" = ");
		whereClause.Append(((IEnumerable)key).Cast<KeyValuePair<string, object>>().Single(sc => sc.Key == commonIndex).Value);
		iterationIndex++;
	}
	*/
	var whereClause = CreateWhereClause(commonIndexes.ToDictionary(c => c, c => key[c]));

	var rowQuery = $"SELECT * FROM {table.TableName} {whereClause.ToString()}";
	IDictionary<string, object> sourceRow = connections.SourceConnection.QuerySingle<dynamic>(rowQuery);
	
	var insertSql = new StringBuilder();
	insertSql.AppendFormat("INSERT INTO {0} (", table.TableName);
	foreach(var sourceColumn in sourceRow)
	{
		insertSql.Append(sourceColumn.Key);
		insertSql.Append(", ");
	}
	insertSql.Remove(insertSql.Length - 2, 2);
	insertSql.Append(") VALUES (");
	foreach (var sourceColumn in sourceRow)
	{
		var sourceValue = FormatSqlValue(sourceColumn.Value);
		insertSql.AppendFormat("{0}, ", sourceValue);
	}
	insertSql.Remove(insertSql.Length - 2, 2);
	insertSql.AppendLine(");\r\n");
	
	syncSql.Append(insertSql.ToString());
}

public void ReportKeyOnlyInTarget(IDictionary<string, object> key, IList<ReportItem> report, TableItem table, SourceTargetConnections connections, StringBuilder syncSql, IEnumerable<string> commonIndexes)
{
	report.Add(new ReportItem() { TableName = table.TableName, DifferenceType = UserQuery.ReportItem.DifferenceTypes.OnlyExistsInTarget, Difference = key });

	var deleteSql = new StringBuilder();
	
	deleteSql.AppendFormat("DELETE FROM {0} ", table.TableName);
	
	var whereClause = CreateWhereClause(commonIndexes.ToDictionary(c => c, c => key[c]));
	
	deleteSql.AppendFormat("{0};\r\n\r\n", whereClause);

	syncSql.Append(deleteSql.ToString());
}

public string FormatSqlValue(object value)
{
	if (value is string)
		return $"'{((string)value).Replace("'", "''")}'";
	else if (value is DateTime)
		return ($"'{((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss")}'");
	else if (value is bool)
		return ((bool)value) ? "1" : "0";
	else if (value is null)
		return "NULL";
	else
		return value.ToString();
}

public string CreateWhereClause(IDictionary<string, object> keysAndValues)
{
	const string opening = "WHERE ";
	var whereClause = new StringBuilder(opening);
	foreach (var keyAndValue in keysAndValues)
	{
		if (whereClause.Length > opening.Length)
			whereClause.Append(" AND ");
		whereClause.Append(keyAndValue.Key);
		whereClause.Append(" = ");
		whereClause.Append(keyAndValue.Value);
	}
	
	return whereClause.ToString();
}

public class TableItem
{
	public int SourceTableId { get; set; }
	
	public int TargetTableId { get; set; }

	public string TableName { get; set; }

	public DumpContainer Selector { get; set; }

	public bool Selected { get; set; }

	object ToDump() => Util.ToExpando(this, exclude: "Selected,SourceTableId,TargetTableId");
}

public class ReportItem
{
	public enum DifferenceTypes
	{
		DifferentValues,
		OnlyExistsInSource,
		OnlyExistsInTarget,
		IndexMismatch,
		NoIndex
	}

	public DifferenceTypes DifferenceType { get; set; }

	public string TableName { get; set; }
	
	public object Difference {get; set;}
}

public class DapperRowComparer : IEqualityComparer<object>
{
	public new bool Equals(object x, object y)
	{
		if (x is null && !(y is null))
			return false;
		if (x is null) // y must be null
			return true;

		var enumerableX = x as IEnumerable<KeyValuePair<string, object>>;
		var enumerableY = y as IEnumerable<KeyValuePair<string, object>>;

		if (enumerableX is null || enumerableY is null)
			return x.Equals(y);

		foreach (var itemX in enumerableX)
		{
			var xValue = itemX.Value;
			var yValue = enumerableY.Single(e => e.Key == itemX.Key).Value;

			if (!xValue.Equals(yValue))
			{
				return false;
			}
		}

		return true;
	}

	public int GetHashCode(object obj)
	{
		var enumerableObj = obj as IEnumerable;
		if (enumerableObj is null)
			return obj.GetHashCode();
		var hash = 0;

		foreach (KeyValuePair<string, object> item in enumerableObj)
		{
			hash = hash ^ item.Value.GetHashCode();
		}

		return hash;
	}
}