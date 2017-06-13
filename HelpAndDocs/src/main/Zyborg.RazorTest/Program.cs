using RazorLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zyborg.RazorTest
{
	class Program
	{
		static void Main(string[] args)
		{
			var engine = EngineFactory.CreateEmbedded(typeof(Program));
			Console.WriteLine(engine.Parse("Test1", string.Empty));
			Console.ReadKey();
		}
	}
}
