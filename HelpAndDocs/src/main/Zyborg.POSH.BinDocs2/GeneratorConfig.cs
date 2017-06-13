using System.IO;

namespace Zyborg.POSH.BinDocs
{
	public class GeneratorConfig
    {
		public GeneratorConfig(string asmPath,
				string asmXmlDocPath = null, string psXmlOutPath = null)
		{
			AssemblyPath = asmPath;

			if (string.IsNullOrEmpty(asmXmlDocPath))
				asmXmlDocPath = Path.ChangeExtension(asmPath, "xml");
			if (string.IsNullOrEmpty(psXmlOutPath))
				psXmlOutPath = Path.ChangeExtension(asmPath, "dll-help.xml.XXX");

			AssemblyXmlDocPath = asmXmlDocPath;
			PowerShellXmlOutPath = psXmlOutPath;
		}

		public string AssemblyPath
		{ get; }

		public string AssemblyXmlDocPath
		{ get; }

		public string PowerShellXmlOutPath
		{ get; }
	}
}
