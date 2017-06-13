using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using Zyborg.POSH.BinDoc.Model;
using static Zyborg.POSH.BinDoc.Model.XmlData;

namespace Zyborg.POSH.BinDocs
{
	public delegate void ReportWarning(MemberInfo target, string warningText);

	public class Generator
    {
		public Generator(GeneratorConfig config)
		{
			if (config == null)
				throw new ArgumentNullException(nameof(config));

			Config = config;
		}

		public GeneratorConfig Config
		{ get; }

		public void Generate()
		{
			var asmData = new AsmData(Config.AssemblyPath);
			var xmlData = new XmlData(Config.AssemblyXmlDocPath);

			var maml = new MamlBuilder();

			foreach (var c in asmData.Commands)
			{
				Console.WriteLine("----------------------------------");
				Console.WriteLine($"{c.Name} - {c.CmdletType.FullName}");
				maml.AddCommand(c, xmlData.GetType(c.CmdletType.FullName));
			}

			maml.Save(Config.PowerShellXmlOutPath);
		}
	}
}
