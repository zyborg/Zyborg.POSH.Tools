using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Zyborg.POSH.BinDoc.Model
{
	public class XmlData
	{
		private XDocument _xmlDoc;
		private XElement _elmRoot;
		private XElement _elmAsmName;
		private XElement _elmMembers;

		// Member#name sample patterns:
		//  Types (classes, interfaces, structs):
		//    "T:Zyborg.Vault.POSH.GetAuthToken" - class GetAuthToken
		//    "T:Zyborg.Vault.POSH.GetAuthToken.TokenInfo" - nested class TokenInfo
		//  Property:
		//    "P:Zyborg.Vault.POSH.DismountAuditProvider.MountName"
		//  Field:
		//    "F:Zyborg.Vault.POSH.VaultBaseCmdlet._client" - field _client
		//  Method:
		//    "M:Zyborg.Vault.POSH.VaultBaseCmdlet.ResolveVaultClient" - method ResolveVaultClient
		//  Constructor:
		//    "M:Zyborg.Vault.POSH.VaultSession.#ctor(System.String,System.String)"
		//      - constructor for class VaultSession with sig (string, string)

		internal static Dictionary<Regex, (Type, Type)> XmlDocPrefixes = new Dictionary<Regex, (Type, Type)>
		{
			[new Regex("T:(?<Name>.+)")] = (typeof(Type), typeof(XmlTypeData)),
			[new Regex("P:(?<Name>.+)")] = (typeof(PropertyInfo), typeof(XmlPropertyData)),
		};

		internal static Dictionary<Type, string> XmlDocMemberPrefixes = new Dictionary<Type, string>
		{
			[typeof(Type)] = "T:",
			[typeof(EventInfo)] = "E:",
			[typeof(PropertyInfo)] = "P:",
			[typeof(FieldInfo)] = "F:",
			[typeof(MethodInfo)] = "M:",
			//[typeof(ConstructorInfo)] = "M:",
		};

		private Dictionary<string, XmlTypeData> _byType = new Dictionary<string, XmlTypeData>();
		private Dictionary<string, XmlPropertyData> _byProp = new Dictionary<string, XmlPropertyData>();

		public XmlData(string filePath) : this(XDocument.Load(filePath))
		{ }

		public XmlData(XDocument xmlDoc)
		{
			_xmlDoc = xmlDoc;
			Parse();
		}

		public XmlTypeData GetType(string fullName)
		{
			return _byType.TryGetValue(fullName, out var t) ? t : null;
		}

		public XmlPropertyData GetProperty(string fullName)
		{
			return _byProp.TryGetValue(fullName, out var p) ? p : null;
		}

		public IEnumerable<XmlPropertyData> GetPropertiesForType(string typeName)
		{
			return _byProp.Values.Where(x => x.TypeName == typeName);
		}

		private void Parse()
		{
			if ((_elmRoot = _xmlDoc.Element("doc")) == null)
				throw new Exception("missing root element 'doc'");
			if ((_elmAsmName = _xmlDoc.XPathSelectElement("/doc/assembly/name")) == null)
				throw new Exception("missing assembly name path '/doc/assembly/name'");
			if ((_elmMembers = _xmlDoc.XPathSelectElement("/doc/members")) == null)
				throw new Exception("missing assembly members path '/doc/members'");

			foreach (var m in _elmMembers.XPathSelectElements("member"))
			{
				var mName = m.Attribute("name")?.Value;
				if (string.IsNullOrEmpty(mName))
					throw new Exception("member attribute 'name' is missing or empty");

				if (mName.StartsWith("T:"))
				{
					var tName = mName.Substring(2);
					var xtd = new XmlTypeData(this, m, tName);
					_byType[xtd.Name] = xtd;
				}
				else if (mName.StartsWith(XmlDocMemberPrefixes[typeof(PropertyInfo)]))
				{
					var pName = mName.Substring(2);
					var xpd = new XmlPropertyData(this, m, pName);
					_byProp[xpd.Name] = xpd;
				}

				// We ignore all other member types for now (maybe forever)
			}
		}

		public class XmlElementData
		{
			private XmlData _dataParent;

			protected XmlElementData(XmlData dataParent, XElement element, string name)
			{
				Parent = dataParent;
				Element = element;
				Name = name;

				Func<XNode, bool> wherePara = x =>
						x.NodeType == XmlNodeType.Text
						|| x.NodeType == XmlNodeType.Whitespace
						|| x.NodeType == XmlNodeType.SignificantWhitespace
						|| (x.NodeType == XmlNodeType.Element
							&& ((XElement)x).Name == "para");

				Summary = element.Element("summary")?.Nodes().Where(wherePara).ToArray();
				Remarks = element.Element("remarks")?.Nodes().Where(wherePara).ToArray();

				//RawComment1 = element.ToString(SaveOptions.DisableFormatting);
				//RawComment2 = string.Join("", element.DescendantNodes().Select(x => x.ToString(SaveOptions.DisableFormatting)));
			}

			public XmlData Parent
			{ get; }

			public XElement Element
			{ get; }

			public string Name
			{ get; }

			public XNode[] Summary
			{ get; }

			public XNode[] Remarks
			{ get; }
		}

		public class XmlTypeData : XmlElementData
		{
			internal XmlTypeData(XmlData dataParent, XElement element, string name)
				: base(dataParent, element, name)
			{ }

			public XmlPropertyData GetProperty(string name)
			{
				string fullName = $"{this.Name}.{name}";
				return Parent.GetProperty(fullName);
			}
		}

		public class XmlPropertyData : XmlElementData
		{
			internal XmlPropertyData(XmlData dataParent, XElement element, string name)
				: base(dataParent, element, name)
			{
				var lastDot = name.LastIndexOf('.');
				if (lastDot <= 0)
					throw new Exception("malformed or missing property name");

				TypeName = name.Substring(0, lastDot);
				PropertyName = name.Substring(lastDot + 1);
			}

			public string TypeName
			{ get; }

			public string PropertyName
			{ get; }
		}
	}
}
