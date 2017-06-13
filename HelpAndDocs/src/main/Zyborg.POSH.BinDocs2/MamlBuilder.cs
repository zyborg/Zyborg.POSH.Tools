using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Zyborg.POSH.BinDoc.Model;
using static Zyborg.POSH.BinDoc.Model.XmlData;

namespace Zyborg.POSH.BinDocs
{
	public class MamlBuilder
	{
		// MAML for PowerShell Ref:
		//    https://msdn.microsoft.com/en-us/library/bb525433(v=vs.85).aspx

		public static readonly XNamespace MshNs = XNamespace.Get("http://msh");
		public static readonly XNamespace MamlNs = XNamespace.Get("http://schemas.microsoft.com/maml/2004/10");
		public static readonly XNamespace DevNs = XNamespace.Get("http://schemas.microsoft.com/maml/dev/2004/10");
		public static readonly XNamespace CommandNs = XNamespace.Get("http://schemas.microsoft.com/maml/dev/command/2004/10");

		private XDocument _out;
		private XElement _outItems;

		public MamlBuilder()
		{
			// <helpItems xmlns="http://msh" schema="maml">
			_outItems = new XElement(MshNs + "helpitems",
					new XAttribute("schema", "maml"),
					new XAttribute(XNamespace.Xmlns + "maml", MamlNs),
					new XAttribute(XNamespace.Xmlns + "dev", DevNs),
					new XAttribute(XNamespace.Xmlns + "cmd", CommandNs));

			_out = new XDocument(new XDeclaration("1.0", "utf-8", null), _outItems);
		}

		public void Save(string filePath)
		{
			_out.Save(filePath);
		}

		public void AddCommand(AsmCommand cmd, XmlTypeData xtd)
		{
			var elmCommand = new XElement(CommandNs + "command");
			AddDetails(elmCommand, cmd, xtd);
			AddSyntax(elmCommand, cmd, xtd);

			_outItems.Add(new XComment($"Command:  {cmd.Name} - {cmd.CmdletType}"));
			_outItems.Add(elmCommand);
		}

		private void AddDetails(XElement elmCommand, AsmCommand cmd, XmlTypeData xtd)
		{
			var elmName = new XElement(CommandNs + "name", cmd.Name);
			var elmVerb = new XElement(CommandNs + "verb", cmd.Verb);
			var elmNoun = new XElement(CommandNs + "noun", cmd.Noun);
			var elmSynopsis = ToMamlDescription(xtd?.Summary);
			var elmDetails = new XElement(CommandNs + "details", elmName, elmVerb, elmNoun, elmSynopsis);

			var elmDescription = ToMamlDescription(xtd?.Remarks);

			elmCommand.Add(elmDetails, elmDescription);
		}

		private void AddSyntax(XElement elmCommand, AsmCommand cmd, XmlTypeData xtd)
		{
			var elmSyntax = new XElement(CommandNs + "syntax");
			var paramSetNames = cmd.ParameterSetNames.ToArray();
			if (paramSetNames.Length > 1)
				paramSetNames = paramSetNames.Where(n => n != ParameterAttribute.AllParameterSets).ToArray();

			foreach (var psn in paramSetNames)
			{
				var elmSyntaxItem = new XElement(CommandNs + "syntaxItem",
						new XElement(MamlNs + "name", cmd.Name));
				var orderedParams = cmd.GetParameters(psn)
						.OrderBy(p => p.GetPosition(psn))
						.ThenBy(p => p.IsRequired(psn) ? "0" : "1")
						.ThenBy(p => p.Name);

				foreach (var p in orderedParams)
				{
					var elmSyntaxItemParam = ToMamlParameter(p, psn, xtd);

					elmSyntaxItem.Add(new XComment($"Parameter:  {p.Name}"));
					elmSyntaxItem.Add(elmSyntaxItemParam);
				}

				elmSyntax.Add(new XComment($"Parameter Set Name:  {psn}"));
				elmSyntax.Add(elmSyntaxItem);
			}

			elmCommand.Add(elmSyntax);
		}

		public static XElement ToMamlDescription(params IEnumerable<XNode>[] nodes)
		{
			if (nodes == null)
				return null;

			var elm = new XElement(MamlNs + "description");

			foreach (var n in nodes)
			{
				if (n == null)
					continue;

				foreach (var x in n)
				{
					if (x.NodeType == XmlNodeType.Text)
					{
						elm.Add(new XElement(MamlNs + "para", x));
					}
					if (x is XElement xe && xe.Name == "para")
					{
						// Create a copy
						var elmPara = new XElement(xe);
						// Change the FQN
						elmPara.Name = MamlNs + "para";
						// Strip out any attributes
						elmPara.RemoveAttributes();

						elm.Add(elmPara);
					}
				}
			}

			return elm;
		}

		public static XElement ToMamlParameter(AsmParameter param, string paramSetName, XmlTypeData xtd)
		{
			var elm = new XElement(CommandNs + "parameter",
					new XAttribute("required", param.IsRequired(paramSetName)),
					new XAttribute("globbing", param.SupportsGlobbing(paramSetName)),
					new XAttribute("pipelineInput", param.GetIsPipelineAttribute(paramSetName)),
					new XAttribute("position", param.GetPosition(paramSetName)),
					new XElement(MamlNs + "name", param.Name));

			elm.Add(ToMamlParameterDescription(param, xtd));
			elm.Add(new XElement(CommandNs + "parameterValue",
					new XAttribute("required", true),
					GetSimpleTypeName(param.ParameterType));

			return elm;
		}

		public static XElement ToMamlParameterDescription(AsmParameter param, XmlTypeData xtd)
		{
			var xpd = xtd?.GetProperty(param.MemberInfo.Name);
			return xpd?.Summary != null || xpd?.Remarks != null
				? ToMamlDescription(xpd.Summary, xpd.Remarks)
				: null;
		}


		/*
		/// <summary>
		/// Generates a <em>&lt;command:command&gt;</em> element for the specified command.
		/// </summary>
		/// <param name="commentReader"></param>
		/// <param name="command">The command.</param>
		/// <param name="reportWarning">Function used to log warnings.</param>
		/// <returns>A <em>&lt;command:command&gt;</em> element that represents the <paramref name="command"/>.</returns>
		private XElement GenerateCommandElement(ICommentReader commentReader, Command command, ReportWarning reportWarning)
		{
			return new XElement(CommandNs + "command",
								new XAttribute(XNamespace.Xmlns + "maml", MamlNs),
								new XAttribute(XNamespace.Xmlns + "command", CommandNs),
								new XAttribute(XNamespace.Xmlns + "dev", DevNs),
								GenerateDetailsElement(commentReader, command, reportWarning),
								GenerateDescriptionElement(commentReader, command, reportWarning),
								GenerateSyntaxElement(commentReader, command, reportWarning),
								GenerateParametersElement(commentReader, command, reportWarning),
								GenerateInputTypesElement(commentReader, command, reportWarning),
								GenerateReturnValuesElement(commentReader, command, reportWarning),
								GenerateAlertSetElement(commentReader, command, reportWarning),
								GenerateExamplesElement(commentReader, command, reportWarning),
								GenerateRelatedLinksElement(commentReader, command, reportWarning));
		}

		/// <summary>
		/// Generates the <em>&lt;command:syntax&gt;</em> element for a command.
		/// </summary>
		/// <param name="commentReader">Provides access to the XML Doc comments.</param>
		/// <param name="command">The command.</param>
		/// <param name="reportWarning">Function used to log warnings.</param>
		/// <returns>A <em>&lt;command:syntax&gt;</em> element for the <paramref name="command"/>.</returns>
		private XElement GenerateSyntaxElement(ICommentReader commentReader, Command command, ReportWarning reportWarning)
		{
			var syntaxElement = new XElement(CommandNs + "syntax");
			IEnumerable<string> parameterSetNames = command.ParameterSetNames.ToList();
			if (parameterSetNames.Count() > 1)
			{
				parameterSetNames = parameterSetNames.Where(name => name != ParameterAttribute.AllParameterSets);
			}
			foreach (var parameterSetName in parameterSetNames)
			{
				syntaxElement.Add(GenerateComment("Parameter set: " + parameterSetName));
				syntaxElement.Add(GenerateSyntaxItemElement(commentReader, command, parameterSetName, reportWarning));
			}
			return syntaxElement;
		}

		/// <summary>
		/// Generates the <em>&lt;command:syntaxItem&gt;</em> element for a specific parameter set of a command.
		/// </summary>
		/// <param name="commentReader">Provides access to the XML Doc comments.</param>
		/// <param name="command">The command.</param>
		/// <param name="parameterSetName">The parameter set name.</param>
		/// <param name="reportWarning">Function used to log warnings.</param>
		/// <returns>A <em>&lt;command:syntaxItem&gt;</em> element for the specific <paramref name="parameterSetName"/> of the <paramref name="command"/>.</returns>
		private XElement GenerateSyntaxItemElement(ICommentReader commentReader, Command command, string parameterSetName, ReportWarning reportWarning)
		{
			var syntaxItemElement = new XElement(CommandNs + "syntaxItem",
												 new XElement(MamlNs + "name", command.Name));
			foreach (var parameter in command.GetParameters(parameterSetName).OrderBy(p => p.GetPosition(parameterSetName)).
																			  ThenBy(p => p.IsRequired(parameterSetName) ? "0" : "1").
																			  ThenBy(p => p.Name))
			{
				syntaxItemElement.Add(GenerateComment("Parameter: " + parameter.Name));
				syntaxItemElement.Add(GenerateParameterElement(commentReader, parameter, parameterSetName, reportWarning));
			}
			return syntaxItemElement;
		}

		/// <summary>
		/// Generates the <em>&lt;command:parameters&gt;</em> element for a command.
		/// </summary>
		/// <param name="commentReader">Provides access to the XML Doc comments.</param>
		/// <param name="command">The command.</param>
		/// <param name="reportWarning">Function used to log warnings.</param>
		/// <returns>A <em>&lt;command:parameters&gt;</em> element for the <paramref name="command"/>.</returns>
		private XElement GenerateParametersElement(ICommentReader commentReader, Command command, ReportWarning reportWarning)
		{
			var parametersElement = new XElement(CommandNs + "parameters");
			foreach (var parameter in command.Parameters)
			{
				parametersElement.Add(GenerateComment("Parameter: " + parameter.Name));
				parametersElement.Add(GenerateParameterElement(commentReader, parameter, ParameterAttribute.AllParameterSets, reportWarning));
				GenerateAliasElements(commentReader, reportWarning, parameter, parametersElement);
			}
			return parametersElement;
		}

		// Because the proper aliases generated in GenerateParameterElement are not manifested by Get-Help,
		// this simply duplicates parameters that have aliases, substituting in the alias name.
		// Thus, one could do Get-Help xyz -param actualName or Get-Help xyz -param aliasName
		private void GenerateAliasElements(ICommentReader commentReader, ReportWarning reportWarning, Parameter parameter, XElement parametersElement)
		{
			foreach (var alias in parameter.Aliases)
			{
				var parameterElement = GenerateParameterElement(commentReader, parameter, ParameterAttribute.AllParameterSets, reportWarning);
				parametersElement.Add(parameterElement);
				var nameElement = (XElement)(parameterElement.Nodes().First(n => ((XElement)n).Name == MamlNs + "name"));
				nameElement.Value = alias;
				var descriptionElement = (XElement)(parameterElement.Nodes().FirstOrDefault(n => ((XElement)n).Name == MamlNs + "description"));
				if (descriptionElement == null)
				{
					descriptionElement = new XElement(MamlNs + "description");
					parameterElement.Add(descriptionElement);
				}
				descriptionElement.Add(new XElement(MamlNs + "para", $"This is an alias of the {parameter.Name} parameter."));
			}
		}

		/// <summary>
		/// Generates a <em>&lt;command:parameter&gt;</em> element for a single parameter.
		/// </summary>
		/// <param name="commentReader">Provides access to the XML Doc comments.</param>
		/// <param name="parameter">The parameter.</param>
		/// <param name="parameterSetName">The specific parameter set name, or <see cref="ParameterAttribute.AllParameterSets"/>.</param>
		/// <param name="reportWarning">Function used to log warnings.</param>
		/// <returns>A <em>&lt;command:parameter&gt;</em> element for the <paramref name="parameter"/>.</returns>
		private XElement GenerateParameterElement(ICommentReader commentReader, Parameter parameter, string parameterSetName, ReportWarning reportWarning)
		{
			var element = new XElement(CommandNs + "parameter",
								new XAttribute("required", parameter.IsRequired(parameterSetName)),
								new XAttribute("globbing", parameter.SupportsGlobbing(parameterSetName)),
								new XAttribute("pipelineInput", parameter.GetIsPipelineAttribute(parameterSetName)),
								new XAttribute("position", parameter.GetPosition(parameterSetName)),
								new XElement(MamlNs + "name", parameter.Name),
								GenerateDescriptionElement(commentReader, parameter, reportWarning),
								commentReader.GetParameterValueElement(parameter, reportWarning),
								GenerateTypeElement(commentReader, parameter.ParameterType, true, reportWarning),
								commentReader.GetParameterDefaultValueElement(parameter, reportWarning),
								GetParameterEnumeratedValuesElement(parameter));
			var aliasNames = parameter.Aliases.ToList();
			if (aliasNames.Count > 0)
			{
				element.Add(new XAttribute("aliases", string.Join(",", aliasNames)));
			}
			return element;
		}

		/// <summary>
		/// Fetch the description from the ICommentReader.
		/// If the parameter is an Enum, add to the description a list of its legal values.
		/// </summary>
		private static XElement GenerateDescriptionElement(ICommentReader commentReader, Parameter parameter, ReportWarning reportWarning)
		{
			var descriptionElement = commentReader.GetParameterDescriptionElement(parameter, reportWarning);
			if (parameter.EnumValues.Any())
			{
				if (descriptionElement == null)
				{
					descriptionElement = new XElement(MamlNs + "description");
				}
				descriptionElement.Add(
					new XElement(MamlNs + "para",
								 "Possible values: " + string.Join(", ", parameter.EnumValues)));
			}
			return descriptionElement;
		}

		/// <summary>
		/// Generates a <em>&lt;command:parameterValueGroup&gt;</em> element for a parameter
		/// in order to display enum choices in the cmdlet's syntax section.
		/// </summary>
		private XElement GetParameterEnumeratedValuesElement(Parameter parameter)
		{
			var enumValues = parameter.EnumValues.ToList();
			if (enumValues.Any())
			{
				var parameterValueGroupElement = new XElement(CommandNs + "parameterValueGroup");
				foreach (var enumValue in enumValues)
				{
					parameterValueGroupElement.Add(GenerateParameterEnumeratedValueElement(enumValue));
				}
				return parameterValueGroupElement;
			}
			return null;
		}

		/// <summary>
		/// Generates a <em>&lt;command:parameterValue&gt;</em> element for a single enum value.
		/// </summary>
		private XElement GenerateParameterEnumeratedValueElement(string enumValue)
		{
			// These hard-coded attributes were copied from what PowerShell's own core cmdlets use
			return new XElement(CommandNs + "parameterValue",
								new XAttribute("required", false),
								new XAttribute("variableLength", false),
								enumValue);
		}

		/// <summary>
		/// Generates the <em>&lt;command:inputTypes&gt;</em> element for a command.
		/// </summary>
		/// <param name="commentReader">Provides access to the XML Doc comments.</param>
		/// <param name="command">The command.</param>
		/// <param name="reportWarning">Function used to log warnings.</param>
		/// <returns>A <em>&lt;command:inputTypes&gt;</em> element for the <paramref name="command"/>.</returns>
		private XElement GenerateInputTypesElement(ICommentReader commentReader, Command command, ReportWarning reportWarning)
		{
			var inputTypesElement = new XElement(CommandNs + "inputTypes");
			var pipelineParameters = command.GetParameters(ParameterAttribute.AllParameterSets)
											.Where(p => p.IsPipeline(ParameterAttribute.AllParameterSets));
			foreach (var parameter in pipelineParameters)
			{
				inputTypesElement.Add(GenerateInputTypeElement(commentReader, parameter, reportWarning));
			}
			return inputTypesElement;
		}

		/// <summary>
		/// Generates the <em>&lt;command:inputType&gt;</em> element for a pipeline parameter.
		/// </summary>
		/// <param name="commentReader">Provides access to the XML Doc comments.</param>
		/// <param name="parameter">The parameter.</param>
		/// <param name="reportWarning">Function used to log warnings.</param>
		/// <returns>A <em>&lt;command:inputType&gt;</em> element for the <paramref name="parameter"/>'s type.</returns>
		private XElement GenerateInputTypeElement(ICommentReader commentReader, Parameter parameter, ReportWarning reportWarning)
		{
			var inputTypeDescription = commentReader.GetInputTypeDescriptionElement(parameter, reportWarning);
			return new XElement(CommandNs + "inputType",
								GenerateTypeElement(commentReader, parameter.ParameterType, inputTypeDescription == null, reportWarning),
								inputTypeDescription);
		}

		/// <summary>
		/// Generates the <em>&lt;command:returnValues&gt;</em> element for a command.
		/// </summary>
		/// <param name="commentReader">Provides access to the XML Doc comments.</param>
		/// <param name="command">The command.</param>
		/// <param name="reportWarning">Function used to log warnings.</param>
		/// <returns>A <em>&lt;command:returnValues&gt;</em> element for the <paramref name="command"/>.</returns>
		private XElement GenerateReturnValuesElement(ICommentReader commentReader, Command command, ReportWarning reportWarning)
		{
			var returnValueElement = new XElement(CommandNs + "returnValues");
			foreach (var type in command.OutputTypes)
			{
				returnValueElement.Add(GenerateComment("OutputType: " + (type == typeof(void) ? "None" : type.Name)));
				var returnValueDescription = commentReader.GetOutputTypeDescriptionElement(command, type, reportWarning);
				returnValueElement.Add(new XElement(CommandNs + "returnValue",
													GenerateTypeElement(commentReader, type, returnValueDescription == null, reportWarning),
													returnValueDescription));
			}
			return returnValueElement;
		}

		/// <summary>
		/// Generates the <em>&lt;maml:alertSet&gt;</em> element for a command.
		/// </summary>
		/// <param name="commentReader">Provides access to the XML Doc comments.</param>
		/// <param name="command">The command.</param>
		/// <param name="reportWarning">Function used to log warnings.</param>
		/// <returns>A <em>&lt;maml:alertSet&gt;</em> element for the <paramref name="command"/>.</returns>
		private XElement GenerateAlertSetElement(ICommentReader commentReader, Command command, ReportWarning reportWarning)
		{
			return commentReader.GetCommandAlertSetElement(command, reportWarning);
		}

		/// <summary>
		/// Generates the <em>&lt;command:examples&gt;</em> element for a command.
		/// </summary>
		/// <param name="commentReader">Provides access to the XML Doc comments.</param>
		/// <param name="command">The command.</param>
		/// <param name="reportWarning">Function used to log warnings.</param>
		/// <returns>A <em>&lt;command:examples&gt;</em> element for the <paramref name="command"/>.</returns>
		private XElement GenerateExamplesElement(ICommentReader commentReader, Command command, ReportWarning reportWarning)
		{
			return commentReader.GetCommandExamplesElement(command, reportWarning);
		}

		/// <summary>
		/// Generates the <em>&lt;maml:relatedLinks&gt;</em> element for a command.
		/// </summary>
		/// <param name="commentReader">Provides access to the XML Doc comments.</param>
		/// <param name="command">The command.</param>
		/// <param name="reportWarning">Function used to log warnings.</param>
		/// <returns>A <em>&lt;maml:relatedLinks&gt;</em> element for the <paramref name="command"/>.</returns>
		private XElement GenerateRelatedLinksElement(ICommentReader commentReader, Command command, ReportWarning reportWarning)
		{
			return commentReader.GetCommandRelatedLinksElement(command, reportWarning);
		}

		/// <summary>
		/// Generates a <em>&lt;dev:type&gt;</em> element for a type.
		/// </summary>
		/// <param name="commentReader">Provides access to the XML Doc comments.</param>
		/// <param name="type">The type for which a corresopnding <em>&lt;dev:type&gt;</em> element is required.</param>
		/// <param name="includeMamlDescription">Indicates whether or not a <em>&lt;maml:description&gt;</em> element should be
		/// included for the type. A description can be obtained from the type's XML Doc comment, but it is useful to suppress it if
		/// a more context-specific description is available where the <em>&lt;dev:type&gt;</em> element is actually used.</param>
		/// <param name="reportWarning">Function used to log warnings.</param>
		/// <returns>A <em>&lt;dev:type&gt;</em> element for the specified <paramref name="type"/>.</returns>
		private XElement GenerateTypeElement(ICommentReader commentReader, Type type, bool includeMamlDescription, ReportWarning reportWarning)
		{
			return new XElement(DevNs + "type",
								new XElement(MamlNs + "name", type == typeof(void) ? "None" : type.FullName),
								new XElement(MamlNs + "uri"),
								includeMamlDescription ? commentReader.GetTypeDescriptionElement(type, reportWarning) : null);
		}

		/// <summary>
		/// Creates a comment.
		/// </summary>
		/// <param name="text">The text of the comment.</param>
		/// <returns>An <see cref="XComment"/> instance based on the specified <paramref name="text"/>.</returns>
		private XComment GenerateComment(string text) { return new XComment($" {text} "); }
		*/

		private static readonly IDictionary<Type, string> PredefinedSimpleTypeNames =
		new Dictionary<Type, string>
		{
			[typeof(object)]  /**/= "object",
			[typeof(string)]  /**/= "string",
			[typeof(bool)]    /**/= "bool",
			[typeof(byte)]    /**/= "byte",
			[typeof(char)]    /**/= "char",
			[typeof(short)]   /**/= "short",
			[typeof(ushort)]  /**/= "ushort",
			[typeof(int)]     /**/= "int",
			[typeof(uint)]    /**/= "uint",
			[typeof(long)]    /**/= "long",
			[typeof(ulong)]   /**/= "ulong",
			[typeof(float)]   /**/= "float",
			[typeof(double)]  /**/= "double",
		};

		private static string GetSimpleTypeName(Type type)
		{
			if (type.IsArray)
			{
				return GetSimpleTypeName(type.GetElementType()) + "[]";
			}

			string result;
			if (PredefinedSimpleTypeNames.TryGetValue(type, out result))
			{
				return result;
			}
			return type.Name;
		}
	}
}
