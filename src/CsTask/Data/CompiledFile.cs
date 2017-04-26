using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CsTask.Data
{
	public class CompiledFile
    {
		public string Name { get; set; }

		[JsonIgnore]
		public Assembly LoadedAssembly { get; set; }

		[JsonIgnore]
		public List<MemberInfo> Members { get; set; }

		public List<string> ReferencedAssemblies { get; set; }
	}
}
