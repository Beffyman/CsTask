using CsTask.Data;
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

namespace CsTask
{
	public static class CodeReader
	{

		public static CompiledFile Read(string fileName,string Code,List<string> foundAssemblies, out string RawCode)
		{
			string Name = $"CsTask{Guid.NewGuid().ToString().Replace("-", "")}";
			RawCode = Code;
			CompiledFile file = new CompiledFile();
			file.Name = fileName;

			SyntaxTree syntax = null;
			bool retry = false;
			bool wrapStatic = false;
			file.ReferencedAssemblies = foundAssemblies ?? new List<string>();

			RETRY:

			file.ReferencedAssemblies = file.ReferencedAssemblies.Distinct().ToList();

			if (retry)//Failed and a CS0116 error, retry with wrapping in static classs
			{

				var root = syntax.GetRoot() as CompilationUnitSyntax;
				var usings = root.Usings.ToFullString();

				SyntaxList<MemberDeclarationSyntax> newMembers = new SyntaxList<MemberDeclarationSyntax>();

				var members = root.Members;
				foreach (var mem in members)
				{
					var staticToken = SyntaxFactory.Token(SyntaxKind.StaticKeyword);
					var realStaticToken = staticToken.WithTrailingTrivia(SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, " "));

					if (mem is MethodDeclarationSyntax method)
					{

						if (!method.Modifiers.Any(x => x.Text == staticToken.Text))
						{
							newMembers = newMembers.Add(method.AddModifiers(realStaticToken));
						}
						else
						{
							newMembers = newMembers.Add(method);
						}
					}
					else if (mem is PropertyDeclarationSyntax prop)
					{
						if (!prop.Modifiers.Any(x => x.Text == staticToken.Text))
						{
							newMembers = newMembers.Add(prop.AddModifiers(realStaticToken));
						}
						else
						{
							newMembers = newMembers.Add(prop);
						}
					}
					else if (mem is FieldDeclarationSyntax field)
					{
						if (!field.Modifiers.Any(x => x.Text == staticToken.Text))
						{
							newMembers = newMembers.Add(field.AddModifiers(realStaticToken));
						}
						else
						{
							newMembers = newMembers.Add(field);
						}
					}
					else
					{
						newMembers = newMembers.Add(mem);
					}
				}

				root = root.WithMembers(newMembers);

				var txt = root.GetText();
				var fullTxt = txt.ToString();
				RawCode = fullTxt;
				if (!string.IsNullOrWhiteSpace(usings))
				{
					fullTxt = fullTxt.Replace(usings, "");
				}

				if (wrapStatic)
				{
					Code =
$@"
{usings}

static class {Name}{{

{fullTxt}

}}

";
				}
				else
				{
					Code =
$@"
{usings}

{fullTxt}


";

				}

				wrapStatic = false;
				retry = false;
			}


			syntax = CSharpSyntaxTree.ParseText(Code);

			List<MetadataReference> references = new List<MetadataReference>
			{
				MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
				MetadataReference.CreateFromFile(typeof(Enumerable).GetTypeInfo().Assembly.Location)
			};

			foreach (var ass in file.ReferencedAssemblies)
			{
				Assembly loaded = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(ass));
				references.Add(MetadataReference.CreateFromFile(loaded.Location));
			}

			CSharpCompilation compilation = CSharpCompilation.Create(
				Name,
				syntaxTrees: new[] { syntax },
				references: references,
				options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

			using (var ms = new MemoryStream())
			{
				EmitResult result = compilation.Emit(ms);

				if (!result.Success)
				{
					if (result.Diagnostics.Any(x => x.ToString().Contains("CS0116")))//Failed and a CS0116 error, retry with wrapping in static classs
					{
						wrapStatic = true;
						retry = true;
					}

					IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
						diagnostic.IsWarningAsError ||
						diagnostic.Severity == DiagnosticSeverity.Error);

					if (failures.Any(x => x.Id == "CS0012"))
					{
						var infoMember = failures.FirstOrDefault().GetType().GetMembers().SingleOrDefault(x => x.Name == "get_Info") as MethodInfo;
						var argumentField = infoMember.ReturnType.GetTypeInfo().GetDeclaredField("_arguments");
						var errorAssemblies = failures.Where(x => x.Id == "CS0012").ToList();
						foreach (var ass in errorAssemblies)
						{
							object info = infoMember.Invoke(ass, null);
							object[] arguments = argumentField.GetValue(info) as object[];
							var assemblyName = arguments[1].ToString();
							file.ReferencedAssemblies.Add(assemblyName);
						}

						retry = true;
					}

					if (retry)
					{
						goto RETRY;
					}

					foreach (Diagnostic diagnostic in failures)
					{
						Console.Error.WriteLine(diagnostic.ToString());

					}
					Console.Error.WriteLine($"Exit Code = -1; \"Failed to compile\"");

#if DEBUG
					Console.ReadKey();
#endif

					Environment.Exit(-1);
				}
				else
				{
					ms.Seek(0, SeekOrigin.Begin);
					file.LoadedAssembly = AssemblyLoadContext.Default.LoadFromStream(ms);
				}
			}

			////Remove all get_, set_, k__BackingFields, and .cctor runtime members.
			file.Members = file.LoadedAssembly.DefinedTypes.SelectMany(x => x.DeclaredMembers.Where(y => !y.Name.Contains("k__BackingField"))).ToList();
			file.Members = file.Members.Where(x =>
			{
				if (x is MethodInfo mi)
				{
					return !mi.Attributes.HasFlag(MethodAttributes.SpecialName);
				}
				else if (x is ConstructorInfo ci)
				{
					return !ci.Attributes.HasFlag(MethodAttributes.SpecialName);
				}

				return true;
			}).ToList();

			return file;
		}

	}
}
