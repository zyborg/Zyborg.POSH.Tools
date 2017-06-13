using Newtonsoft.Json;
using RazorLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace Zyborg.POSH.DocGen
{
	[Cmdlet(VerbsData.Publish, "HyperDocs")]
    public class PublishHyperDocs : PSCmdlet
    {
		private IRazorLightEngine _engine;

		[Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
		public PSModuleInfo[] Module
		{ get; set; }

		protected override void BeginProcessing()
		{
			_engine = EngineFactory.CreateEmbedded(typeof(PublishHyperDocs));
		}

		protected override void ProcessRecord()
		{
			foreach (var m in Module)
			{
				var foreachModuleJson = _engine.Parse("templates.foreach-$module", m);
				var foreachModule = JsonConvert.DeserializeObject<Dictionary<string, string>>(foreachModuleJson);

				foreach (var t in foreachModule)
				{
					var fName = t.Key.Replace("%module%", m.Name);
					var tName = t.Value;

					var fBody = _engine.Parse(tName, m);

					WriteObject(new
					{
						FileName = fName,
						FileBody = fBody,
					});
				}
			}
		}
	}
}
