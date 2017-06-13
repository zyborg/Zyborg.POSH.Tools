using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace Zyborg.POSH.BinDoc.Model
{
	public class AsmData
	{
		private Assembly _asm;

		public AsmData(string filePath)
		{
			var asmRoot = Path.GetDirectoryName(filePath);

			try
			{
				AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
				this.Assembly = Assembly.LoadFile(filePath);
				Commands = GetCommands(this.Assembly);
			}
			finally
			{
				AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssembly;
			}

			// Local function -- defined here so that we can register AND deregister
			Assembly ResolveAssembly(object sender, ResolveEventArgs args)
			{
				var asmName = args.Name.Split(',')[0];
				var asmPath = Path.Combine(asmRoot, asmName) + ".dll";

				return File.Exists(asmPath) ? Assembly.LoadFile(asmPath) : null;
			}
		}

		public AsmData(Assembly asm)
		{
			this.Assembly = asm;
			Commands = GetCommands(this.Assembly);
		}

		public Assembly Assembly
		{ get; }

		public IEnumerable<AsmCommand> Commands
		{ get; }

		/// <summary>
		/// Retrieves a sequence of <see cref="AsmCommand"/> instances, one for each cmdlet defined in the specified <paramref name="assembly"/>.
		/// </summary>
		/// <param name="assembly">The assembly.</param>
		/// <returns>A sequence of commands, one for each cmdlet defined in the <paramref name="assembly"/>.</returns>
		private static IEnumerable<AsmCommand> GetCommands(Assembly assembly)
		//private static IEnumerable<string> GetCommands(Assembly asm)
		{
			//return asm.GetTypes().Where(t => t.GetTypeInfo().IsPublic).Select(t => t.GetTypeInfo().BaseType.AssemblyQualifiedName);

			return assembly.GetTypes().Where(type => type.IsPublic
					&& typeof(Cmdlet).IsAssignableFrom(type)
					&& type.GetCustomAttribute<CmdletAttribute>() != null)
					.Select(type => new AsmCommand(type))
					.OrderBy(command => command.Noun)
					.ThenBy(command => command.Verb);
		}
	}
}
