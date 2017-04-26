using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.RegularExpressions;

namespace CsTask.Data
{
	public class CsTaskFile
	{
		public string Path { get; set; }
		public string Name { get; set; }
		public string Code { get; set; }

		public SyntaxTree Syntax { get; set; }

		public List<string> ReferencedAssemblies { get; set; }

		public CsTaskFile(string path)
		{
			Path = path;
			Name = System.IO.Path.GetFileNameWithoutExtension(Path);
			Code = File.ReadAllText(path);

			//TODO: Find file in cache and pass assemblies in
			var file = CodeReader.Read(Path, Code,null, out string rawCode);
			ReferencedAssemblies = file.ReferencedAssemblies;
			Code = rawCode;
		}

	}
}
