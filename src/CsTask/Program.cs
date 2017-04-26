using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CsTask.Data;
using System.Reflection;
using Newtonsoft.Json;

namespace CsTask
{
	public static class Program
	{
		public static string CurrentDirectory => Directory.GetCurrentDirectory();
		public static Dictionary<int, string> Commands { get; set; } = new Dictionary<int, string>();
		public static List<CsTaskFile> Files { get; set; } = new List<CsTaskFile>();

		private static JsonSerializerSettings JsonSettings = new JsonSerializerSettings
		{
			Formatting = Formatting.Indented,
			NullValueHandling = NullValueHandling.Include
		};

		private const string PipeArrow = "|";
		private static MemberInfo PipeMethod
		{
			get
			{
				var type = typeof(Program);
				var field = type.GetTypeInfo().DeclaredFields.SingleOrDefault(x=>x.Name == nameof(Program.PipeArrow));
				return field;
			}
		}


		static void Main(string[] args)
		{
			int i = 0;
			args.ToList().ForEach(x => Commands.Add(i++, x));


			LoadFiles();

			CompiledFile compiledFile = CompileAssembly();

			Dictionary<string, (int order, MemberInfo member)> commandMembers = FindCommands(compiledFile);

			List<MemberInfo> commandRunOrder = commandMembers.Select(x => x.Value).OrderBy(x => x.order).Select(x => x.member).ToList();

			object previousVariable = null;
			bool pipe = false;

			foreach (var command in commandRunOrder)
			{
				try
				{

					if (command is MethodInfo mi)
					{
						object output;
						if (pipe == true)
						{
							output = mi.Invoke(null, new object[] { previousVariable });
						}
						else
						{
							output = mi.Invoke(null, null);
						}

						if (output != null)
						{
							previousVariable = output;
							Console.WriteLine(GetStringOutput(output));
						}
					}
					else if (command is PropertyInfo pi)
					{
						var output = pi.GetValue(null);
						previousVariable = output;
						Console.WriteLine(GetStringOutput(output));
					}
					else if (command is FieldInfo fi)
					{
						if(command == PipeMethod)
						{
							pipe = true;
							continue;
						}

						var output = fi.GetValue(null);
						previousVariable = output;
						Console.WriteLine(GetStringOutput(output));
					}

					pipe = false;
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine(ex.Message);
					Console.Error.WriteLine($"Exit Code = -3; \"Command {command.Name} could not be run\"");

#if DEBUG
					Console.ReadKey();
#endif

					Environment.Exit(-3);
				}
			}
#if DEBUG
			Console.ReadKey();
#endif
		}

		private static string GetStringOutput(object obj)
		{
			if(obj is string s)
			{
				return s;
			}
			else
			{
				return JsonConvert.SerializeObject(obj, JsonSettings);
			}
		}

		private static void LoadFiles()
		{
			var taskFiles = Directory.GetFiles(CurrentDirectory, "*.csx");
			foreach (var file in taskFiles)
			{
				Files.Add(new CsTaskFile(file));
			}
		}

		private static CompiledFile CompileAssembly()
		{
			string joinedCode = string.Join(Environment.NewLine, Files.Select(x => x.RawCode));
			return CodeReader.Read(joinedCode, out string rawCode);
		}

		private static Dictionary<string, (int order, MemberInfo member)> FindCommands(CompiledFile compiledFile)
		{
			Dictionary<string, (int order, MemberInfo member)> members = new Dictionary<string, (int order, MemberInfo member)>();

			foreach (var command in Commands)
			{
				if(command.Value == PipeArrow)
				{
					members.Add(command.Value, (command.Key, PipeMethod));
				}
				else
				{
					foreach (var file in Files)
					{
						var memberCommand = compiledFile.Members.SingleOrDefault(x => StringEquals(x.Name, command.Value));
						if (memberCommand != null)
						{
							if (members.ContainsKey(command.Value))
							{
								Console.Error.WriteLine($"Exit Code = -2; \"Duplicate command exists of name: {command}\"");
								Environment.Exit(-2);
							}

							members.Add(command.Value, (command.Key, memberCommand));
						}
					}
				}
			}

			return members;
		}

		private static bool StringEquals(string str1, string str2)
		{
			if (str1.Length == str2.Length)
			{
				return (str1.Equals(str2, StringComparison.CurrentCultureIgnoreCase));
			}
			return false;
		}
	}
}