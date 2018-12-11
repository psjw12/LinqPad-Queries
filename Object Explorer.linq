<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Web.dll</Reference>
</Query>

void Main()
{
	const string path = @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\mscorlib.dll";
	
	var a = Assembly.LoadFile(path);
	
	var t = a.GetTypes();
	
	var n = t.Where(x => x.Namespace != null).Select(b => b.Namespace).Distinct().OrderBy(b => b);

	var ns = new List<Namespace>(
		n.Select(b => new Namespace()
		{
			Name = b,
			Classes = new List<Class>(
				t.Where(y => y.Namespace == b && y.IsPublic)
				.Select(c => new Class
				{
					Name = c.ContainsGenericParameters
					? $"{c.Name.Substring(0, c.Name.IndexOf("`"))}<{string.Join("`", c.GetGenericArguments().Select(p => p.Name))}>"
					: c.Name,
					Methods = new List<Method>(
						c.GetMethods()
						.Where(ci => ci.IsPublic && !ci.IsSpecialName)
						.Select(y => new Method()
						{
							Name = y.Name,
							Arguements = new List<Arguement>(y.GetParameters().Select(p => new Arguement { Name = p.Name, Type = p.ParameterType.Name })),
							IsStatic = y.IsStatic,
							GenericArguements = y.GetGenericArguments().Select(p => p.Name)
						})
						.OrderBy(y => y.Name)
					),
					Properties = new List<Property>(
						c.GetProperties()
						.Where(p => p.GetAccessors(false).Count() > 0)
						.Select(p => new Property { Name = p.Name, Type = p.PropertyType.Name, IsStatic = p.GetGetMethod().IsStatic })
						.OrderBy(p => p.Name)),
					BaseType = c.BaseType?.Name,
					Interfaces = c.GetInterfaces().Select(i => i.Name)
				}
			).OrderBy(m => m.Name))}
		).OrderBy(b => b.Name)
		.Where(b => b.Classes.Count > 0));
	ns.Dump(1);
}

public class Namespace
{
	public string Name {get; set;}
	public List<Class> Classes {get; set;}
}

public class Class
{
	public string Name { get; set; }
	public List<Method> Methods { get; set; }
	public List<Property> Properties { get; set; }
	public string BaseType { get; set; }
	public IEnumerable<string> Interfaces {get; set;}
}

public class Method
{
	public string Name { get; set; }
	public List<Arguement> Arguements { get; set; }
	public bool IsStatic { get; set; }
	public IEnumerable<string> GenericArguements {get; set;}

	object ToDump()
	{
		var isStatic = this.IsStatic ? @"<span style=""color:gray;font-style:italics"">static </span>" : string.Empty;
		var isGeneric = GenericArguements.Count() > 0 ? $"&lt;{Encode(string.Join(",", GenericArguements))}&gt;" : string.Empty;
		return new { Name = Util.RawHtml($@"{isStatic}<span style=""color:green"">{Name}</span>{isGeneric}({String.Join(", ", Arguements.Select(a => $@"<span style=""color:navy"">{Encode(a.Type)}</span> {a.Name}"))})") };
	}
}

public class Arguement
{
	public string Name { get; set; }
	public string Type { get; set; }
}

public class Property
{
	public string Name { get; set; }
	public string Type { get; set; }
	public bool IsStatic {get; set;}
	
	object ToDump()
	{
		var isStatic = IsStatic ? @"<span style=""color:gray;font-style:italics"">static </span>" : string.Empty;
		return new
		{
			Name = Util.RawHtml($@"{isStatic}<span style=""color:green"">{Name}</span> : <span style=""color:navy"">{Type}</span>")
		};
	}
}

static string Encode(string s) => System.Web.HttpUtility.HtmlEncode(s);