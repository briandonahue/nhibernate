//
// NHibernate.Mapping.Attributes.Generator
// This product is under the terms of the GNU Lesser General Public License.
//
[assembly: log4net.Config.XmlConfiguratorAttribute(Watch=true)]

namespace NHibernate.Mapping.Attributes.Generator
{
	/// <summary>
	/// Contains the main entry point.
	/// </summary>
	internal sealed class Program
	{
		/// <summary> Log (debug) infos, warnings and errors </summary>
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(
			System.Reflection.MethodBase.GetCurrentMethod().DeclaringType );


		#region Words + NameConformer
		private static string[] words = new string[]
		{
"access",
"alias",
"all",
"any",
"array",
"assembly",
"attribute",
"auto",
"bag",
"batch",
"before",
"by",
"cache",
"cascade",
"check",
"class",
"collection",
"column",
"component",
"composite",
"constrained",
"content",
"default",
"delete",
"dirty",
"discriminator",
"dynamic",
"element",
"enum",
"explicit",
"extends",
"false",
"fetch",
"force",
"foreign",
"format",
"formula",
"generator",
"generic",
"helper",
"hibernate",
"id",
"implicit",
"import",
"index",
"inherit",
"insert",
"inverse",
"jcs",
"join",
"joined",
"key",
"lazy",
"length",
"list",
"lock",
"many",
"map",
"mapping",
"meta",
"mode",
"mutable",
"name",
"namespace",
"nested",
"non",
"none",
"not",
"null",
"object",
"one",
"only",
"optimistic",
"order",
"orphan",
"outer",
"param",
"parent",
"patterns",
"persister",
"polymorphism",
"primitive",
"property",
"proxy",
"query",
"read",
"ref",
"rename",
"return",
"save",
"schema",
"select",
"set",
"size",
"sort",
"specified",
"sql",
"strategy",
"strict",
"style",
"subclass",
"synchronize",
"table",
"timestamp",
"to",
"true",
"type",
"unique",
"unsaved",
"unspecified",
"update",
"usage",
"value",
"version",
"where",
"write",
		};

		private static Refly.NameConformer conformer = new Refly.NameConformer(words);

		public static Refly.NameConformer Conformer
		{
			get { return conformer; }
		}
		#endregion


		private static System.Xml.Schema.XmlSchema schema = null;

		public static System.Xml.Schema.XmlSchema Schema
		{
			get { return schema; }
		}



		/// <summary> Instanciation forbidden </summary>
		private Program()
		{
			throw new System.NotSupportedException();
		}


		/// <summary> The main entry point for the application. </summary>
		[System.STAThread]
		static void Main()
		{
			try
			{
				log.Info("Generation of NHibernate.Mapping.Attributes");

				// Open the Schema (in /NHMA/ directory)
				System.Xml.XmlTextReader reader = new System.Xml.XmlTextReader("../../../nhibernate-mapping-2.0.xsd");
				schema = System.Xml.Schema.XmlSchema.Read(reader, null);

				Refly.CodeDom.NamespaceDeclaration nd = new Refly.CodeDom.NamespaceDeclaration("NHibernate.Mapping.Attributes", conformer);
				nd.Imports.Clear(); // remove "using System;"
				conformer.Capitalize = true;
				Refly.CodeDom.ClassDeclaration hbmWriter = nd.AddClass("HbmWriter");
				hbmWriter.Attributes = System.Reflection.TypeAttributes.Public;
				hbmWriter.Doc.Summary.AddText(" Write a XmlSchemaElement from attributes in a System.Type. "); // Create the <summary />

				Refly.CodeDom.FieldDeclaration fdDefaultHelper = hbmWriter.AddField("HbmWriterHelper", "DefaultHelper");
				fdDefaultHelper.InitExpression = new Refly.CodeDom.Expressions.SnippetExpression("new HbmWriterHelperEx()");
				// Add its public property with a comment
				Refly.CodeDom.PropertyDeclaration pdDefaultHelper = hbmWriter.AddProperty(fdDefaultHelper, true, true, false);
				pdDefaultHelper.Doc.Summary.AddText(" Gets or sets the HbmWriterHelper used by HbmWriter "); // Create the <summary />

				Refly.CodeDom.FieldDeclaration fd = hbmWriter.AddField(typeof(System.Collections.Hashtable), "Patterns");
				// Add its public property with a comment
				Refly.CodeDom.PropertyDeclaration pd = hbmWriter.AddProperty(fd, false, true, false);
				pd.Get.Add( new Refly.CodeDom.Expressions.SnippetExpression(@"if(_patterns==null)
				{
					_patterns = new System.Collections.Hashtable();
					_patterns.Add(@""Nullables.Nullable(\w+), Nullables"", ""Nullables.NHibernate.Nullable$1Type, Nullables.NHibernate"");
					_patterns.Add(@""System.Data.SqlTypes.Sql(\w+), System.Data"", ""NHibernate.UserTypes.SqlTypes.Sql$1Type, NHibernate.UserTypes.SqlTypes"");
				}
				return _patterns;") );
				pd.Doc.Summary.AddText(" Gets or sets the Patterns to convert properties types (the key is the pattern string and the value is the replacement string) "); // Create the <summary />

				HbmWriterGenerator.FillFindAttributedMembers(hbmWriter.AddMethod("FindAttributedMembers"));
				HbmWriterGenerator.FillGetSortedAttributes(hbmWriter.AddMethod("GetSortedAttributes"));
				HbmWriterGenerator.FillIsNextElement(hbmWriter.AddMethod("IsNextElement"), schema.Items);
				HbmWriterGenerator.FillGetXmlEnumValue(hbmWriter.AddMethod("GetXmlEnumValue"));
				HbmWriterGenerator.FillWriteUserDefinedContent( hbmWriter.AddMethod("WriteUserDefinedContent"),
					hbmWriter.AddMethod("WriteUserDefinedContent") );


				log.Info("Browse Schema.Items (Count=" + schema.Items.Count + ")");
				foreach(System.Xml.Schema.XmlSchemaObject obj in schema.Items)
				{
					if(obj is System.Xml.Schema.XmlSchemaAttributeGroup)
					{
						// ignore
						log.Debug("Ignore AttributeGroup: " + (obj as System.Xml.Schema.XmlSchemaAttributeGroup).Name);
					}
					else if(obj is System.Xml.Schema.XmlSchemaSimpleType)
					{
						System.Xml.Schema.XmlSchemaSimpleType elt = obj as System.Xml.Schema.XmlSchemaSimpleType;
						log.Debug("Generate Enumeration for SimpleType: " + elt.Name);
						AttributeAndEnumGenerator.GenerateEnumeration(elt, nd.AddEnum(Utils.Capitalize(elt.Name), false));
					}
					else if(obj is System.Xml.Schema.XmlSchemaElement)
					{
						System.Xml.Schema.XmlSchemaElement elt = obj as System.Xml.Schema.XmlSchemaElement;
						System.Xml.Schema.XmlSchemaComplexType type = null;
						if(!elt.SchemaTypeName.IsEmpty) // eg:  <xs:element name="cache" type="cacheType" />
							foreach(System.Xml.Schema.XmlSchemaObject o in schema.Items)
							{
								System.Xml.Schema.XmlSchemaComplexType t = o as System.Xml.Schema.XmlSchemaComplexType;
								if(t != null && t.Name == elt.SchemaTypeName.Name)
								{
									type = t;
									break;
								}
							}
						string eltName = Utils.Capitalize(elt.Name);
						log.Debug("Generate Attribute and ElementWriter for Element: " + elt.Name);
						AttributeAndEnumGenerator.GenerateAttribute(elt, nd.AddClass(eltName + "Attribute"), type);
						HbmWriterGenerator.GenerateElementWriter(elt, eltName, hbmWriter.AddMethod("Write" + eltName), type);
						if(Utils.IsRoot(eltName))
							HbmWriterGenerator.FillWriteNestedTypes(eltName, hbmWriter.AddMethod("WriteNested" + eltName + "Types"));
					}
					else if(obj is System.Xml.Schema.XmlSchemaComplexType)
					{
						System.Xml.Schema.XmlSchemaComplexType elt = obj as System.Xml.Schema.XmlSchemaComplexType;
						log.Debug("Don't generate ComplexType: " + elt.Name); // like <query> and <sql-query>
					}
					else
						log.Warn("Unknow Object: " + obj.ToString());
				}


				// Generate the source code
				// Note: NameConformer.WordSplit() has been replaced in Refly.
				Refly.CodeDom.CodeGenerator gen = new Refly.CodeDom.CodeGenerator();
				gen.Options.IndentString = "	"; // Tab
				gen.CreateFolders = false;
				#region Copyright
				gen.Copyright = string.Format(@"
 NHibernate.Mapping.Attributes
 This product is under the terms of the GNU Lesser General Public License.


------------------------------------------------------------------------------
 <autogenerated>
     This code was generated by a tool.
     Runtime Version: {0}

     Changes to this file may cause incorrect behavior and will be lost if 
     the code is regenerated.
 </autogenerated>
------------------------------------------------------------------------------


 This source code was auto-generated by Refly, Version={1} (modified).
",
					System.Environment.Version.ToString(),
					gen.GetType().Assembly.GetName(false).Version.ToString());
				#endregion

				log.Info("CodeGenerator.GenerateCode()... Classes=" + nd.Classes.Count + ", Enums=" + nd.Enums.Count);
				gen.GenerateCode("..\\..\\..", nd);
				log.Info("Done !");
			}
			catch(System.Exception ex)
			{
				log.Error("Unexpected Exception", ex);
			}
			catch
			{
				log.Error("Unexpected non-CLSCompliant Exception");
			}
		}
	}
}
