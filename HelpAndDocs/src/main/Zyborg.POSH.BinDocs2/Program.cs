using System;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

namespace Zyborg.POSH.BinDocs
{
    class Program
    {
        static void Main(string[] args)
        {
			var asmDir = @"C:\prj\zyborg\Zyborg.Vault\src\main\Zyborg.Vault.POSH\bin\Test-BinDocs";
			var asmBin = "Zyborg.Vault.POSH.dll";
			var asmPath = Path.Combine(asmDir, asmBin);
			//var xmlPath = Path.ChangeExtension(asmPath, ".xml");
			//var outPath = Path.ChangeExtension(asmPath, "dll-help.xml");

			var cfg = new GeneratorConfig(asmPath);
			var gen = new Generator(cfg);

			gen.Generate();

			//Console.ReadKey();
        }
    }
}