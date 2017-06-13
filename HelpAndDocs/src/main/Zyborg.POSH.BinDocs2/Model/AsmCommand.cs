using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace Zyborg.POSH.BinDoc.Model
{
	/// <summary>
	/// Represents a single cmdlet.
	/// </summary>
	/// <remarks>
	/// This class is taken almost verbatim from
	/// <see cref="https://github.com/red-gate/XmlDoc2CmdletDoc/blob/master/XmlDoc2CmdletDoc.Core/Domain/Command.cs"
	/// >XmlDoc2CmdletDoc.Core/Domain/Command.cs</see>
	/// </remarks>
	public class AsmCommand
	{
		private readonly CmdletAttribute _attribute;

		/// <summary>
		/// Creates a new instance based on the specified cmdlet type.
		/// </summary>
		/// <param name="cmdletType">The type of the cmdlet. Must be a sub-class of <see cref="Cmdlet"/>
		/// and have a <see cref="CmdletAttribute"/>.</param>
		public AsmCommand(Type cmdletType)
		{
			if (cmdletType == null) throw new ArgumentNullException(nameof(cmdletType));
			CmdletType = cmdletType;
			_attribute = CmdletType.GetCustomAttribute<CmdletAttribute>();
			if (_attribute == null) throw new ArgumentException("Missing CmdletAttribute", nameof(cmdletType));
		}

		/// <summary>
		/// The type of the cmdlet for this command.
		/// </summary>
		public readonly Type CmdletType;

		/// <summary>
		/// The cmdlet verb.
		/// </summary>
		public string Verb { get { return _attribute.VerbName; } }

		/// <summary>
		/// The cmdlet noun.
		/// </summary>
		public string Noun { get { return _attribute.NounName; } }

		/// <summary>
		/// The cmdlet name, of the form verb-noun.
		/// </summary>
		public string Name { get { return Verb + "-" + Noun; } }

		/// <summary>
		/// The output types declared by the command.
		/// </summary>
		public IEnumerable<Type> OutputTypes
		{
			get
			{
				return CmdletType.GetCustomAttributes<OutputTypeAttribute>()
								 .SelectMany(attr => attr.Type)
								 .Select(pstype => pstype.Type)
								 .Distinct()
								 .OrderBy(type => type.FullName);
			}
		}

		/// <summary>
		/// The parameters belonging to the command.
		/// </summary>
		public IEnumerable<AsmParameter> Parameters
		{
			get
			{
				return CmdletType.GetMembers(BindingFlags.Instance | BindingFlags.Public)
					.Where(member => member.GetCustomAttributes<ParameterAttribute>().Any())
					.Select(member => new AsmParameter(CmdletType, member));
			}
		}

		/// <summary>
		/// The command's parameters that belong to the specified parameter set.
		/// </summary>
		/// <param name="parameterSetName">The name of the parameter set.</param>
		/// <returns>
		/// The command's parameters that belong to the specified parameter set.
		/// </returns>
		public IEnumerable<AsmParameter> GetParameters(string parameterSetName)
		{
			return parameterSetName == ParameterAttribute.AllParameterSets
					   ? Parameters
					   : Parameters.Where(p => p.ParameterSetNames.Contains(parameterSetName) ||
											   p.ParameterSetNames.Contains(ParameterAttribute.AllParameterSets));
		}

		/// <summary>
		/// The names of the parameter sets that the parameters belongs to.
		/// </summary>
		public IEnumerable<string> ParameterSetNames { get { return Parameters.SelectMany(p => p.ParameterSetNames).Distinct(); } }
	}
}