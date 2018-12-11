<Query Kind="Program" />

void Main()
{
	Search("Enter term here");
}

public void Search(string term)
{
	if (term == null)
		throw new ArgumentNullException(nameof(term));

	if (term == string.Empty)
		return;

	var columns = SearchColumns(term);
	columns.Dump("Columns");

	var tables = SearchTables(term);
	tables.Dump("Tables");

	var sprocs = SearchStoredProcedures(term);
	sprocs.Dump("Stored procedures");
}

public IEnumerable<object> SearchColumns(string term)
{
	var cols = sys.Columns.Join(sys.Tables, c => c.Object_id, t => t.Object_id, (c, t) => new { column = c, table = t }).Where(c => c.column.Name.Contains(term));

	return cols.Select(ct => new { ColunmName = HighlightMatch(ct.column.Name, term), TableName = $"{sys.Schemas.Single(s => s.Schema_id == ct.table.Schema_id).Name}.{ct.table.Name}" });
}

public IEnumerable<object> SearchTables(string term)
{
	var tables = sys.Tables.Where(t => t.Name.Contains(term));

	return tables.Select(t => HighlightMatch($"{sys.Schemas.Single(s => s.Schema_id == t.Schema_id).Name}.{t.Name}", term));
}

public IEnumerable<object> SearchStoredProcedures(string term)
{
	var sprocs = sys.Sql_modules.Join(sys.Objects, o => o.Object_id, i => i.Object_id, (o, i) => new { i.Name, o.Definition, i.Schema_id }).Where(s => s.Name.Contains(term) || s.Definition.Contains(term));

	return sprocs.Select(s => new
	{
		SprocName = HighlightMatch($"{sys.Schemas.Single(sc => sc.Schema_id == s.Schema_id).Name}.{s.Name}", term),
		Body = HighlightMatch(s.Definition, term)
	});
}

object HighlightMatch(string termToSearch, string searchTerm)
{
	var searchIndex = termToSearch.IndexOf(searchTerm, StringComparison.InvariantCultureIgnoreCase);
	if (searchIndex < 0)
	{
		return termToSearch;
	}

	var html = new StringBuilder();
	while (searchIndex > -1)
	{
		html.Append(termToSearch, 0, searchIndex);
		html.Append("<span class=\"highlight\">");
		html.Append(termToSearch, searchIndex, searchTerm.Length);
		html.Append("</span>");
		termToSearch = termToSearch.Substring(searchIndex + searchTerm.Length);
		searchIndex = termToSearch.IndexOf(searchTerm, StringComparison.InvariantCultureIgnoreCase);
	}
	html.Append(termToSearch);
	html.Replace("\r\n", "<br>");
	
	return Util.RawHtml(html.ToString());
}