using System;
using System.IO;
using System.Xml;

namespace VSCodeLineCounter.Parser.Project
{
	class VsProject
	{
		public VsProject()
		{

		}

		private string _guid;
		private string _path;
		private string[] _includedFiles;
		private VsProject[] _includedProjects;

		public string GUID
		{
			get { return _guid; }
		}

		public string Path
		{
			get { return _path; }
		}

		public string[] IncludedFiles
		{
			get { return _includedFiles; }
		}

		public VsProject[] IncludedProjects
		{
			get { return _includedProjects; }
		}

		public static VsProject Parse(string path, string data)
		{
			try
			{
				VsProject proj = new VsProject();
				proj._path = path;

				XmlDocument xml = new XmlDocument();
				xml.LoadXml(data);

				XmlNodeList guid = xml.GetElementsByTagName("ProjectGuid");

				if (guid != null && guid.Count > 0)
					proj._guid = guid[0].InnerXml;

				proj._includedFiles = new string[0];

				string[] nodeList = new string[] { "Compile", "Page", "None", "Resource", "ClInclude", "ClCompile" };

				foreach (string each in nodeList)
					proj._includedFiles = AppendArray(proj._includedFiles, GetIncludedFiles(xml, each));

				XmlNodeList importNodes = xml.GetElementsByTagName("Import");

				if (importNodes != null && importNodes.Count > 0)
				{
					proj._includedProjects = new VsProject[importNodes.Count];
					int counter = 0;

					foreach (XmlNode each in importNodes)
					{
						string import = path + "\\" + each.Attributes["Project"].Value;

						if (File.Exists(import))
						{
							VsProject importedProj = Parse(import.Remove(import.LastIndexOf('\\')), File.ReadAllText(import));
							proj._includedProjects[counter++] = importedProj;
						}
					}

					Array.Resize(ref proj._includedProjects, counter);
				}
				else
					proj._includedProjects = new VsProject[0];

				return proj;
			}
			catch
			{
				throw (new FormatException("Project data was not in correct format."));
			}
		}

		private static string[] AllowedFileTypes = new string[] {
			".cs", ".xaml", ".vb",
			".cc", ".cpp", ".cxx", ".c", ".h", ".hh", ".hpp", ".hxx", ".inl", ".tlh", ".tli", ".fx", ".rc",
			".htm", ".html", ".css", ".lss", ".coffee", ".ascx", ".js", ".asp", ".aspx",
			".config", ".manifest",
			".xml", ".xsd", ".xslt"
		};

		private static string[] GetIncludedFiles(XmlDocument doc, string type)
		{
			XmlNodeList files = doc.GetElementsByTagName(type);

			string[] includedFiles = new string[0];

			foreach (XmlNode each in files)
			{
				XmlAttribute includeAttrib = each.Attributes["Include"];

				if (includeAttrib != null)
				{
					string include = includeAttrib.Value;

					if (!string.IsNullOrWhiteSpace(include) && IsAllowedFileType(include))
					{
						Array.Resize(ref includedFiles, includedFiles.Length + 1);
						includedFiles[includedFiles.Length - 1] = include;

						//foreach (XmlNode child in each.ChildNodes)
						//{
						//	if (child.Name == "DependentUpon")
						//	{
						//		string text = child.InnerText;

						//		if (IsAllowedFileType(text))
						//		{
						//			Array.Resize(ref includedFiles, includedFiles.Length + 1);
						//			includedFiles[includedFiles.Length - 1] = (include.Contains("\\") ? include.Remove(include.LastIndexOf('\\')) + "\\" : "") + text;
						//		}
						//	}
						//}
					}
				}
			}

			return includedFiles;
		}

		private static bool IsAllowedFileType(string file)
		{
			foreach (string each in AllowedFileTypes)
				if (file.EndsWith(each))// && !file.StartsWith("..\\"))
					return true;

			return false;
		}

		private static string[] AppendArray(string[] array1, string[] array2)
		{
			Array.Resize(ref array1, array1.Length + array2.Length);
			array2.CopyTo(array1, array1.Length - array2.Length);
			return array1;
		}
	}
}
