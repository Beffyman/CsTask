using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CsTask.Data
{
	public class CompiledFile
    {
		public Assembly LoadedAssembly { get; set; }

		public List<MemberInfo> Members { get; set; }
	}
}
