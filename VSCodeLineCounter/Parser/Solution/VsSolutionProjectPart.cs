using System;

namespace VSCodeLineCounter.Parser.Solution
{
	class VsSolutionProjectPart
	{
		public VsSolutionProjectPart()
		{

		}

		private string _guid;
		private string _name;
		private string _path;

		public string GUID
		{
			get { return _guid; }
		}

		public string Name
		{
			get { return _name; }
		}

		public string Path
		{
			get { return _path; }
		}

		/// <summary>
		/// Parses a project entry in a solution file.
		/// </summary>
		/// <param name="data">Project("Solution_GUID") = "Project_Name", "Project_Path", "Project_GUID"</param>
		/// <returns></returns>
		public static VsSolutionProjectPart Parse(string data)
		{
			try
			{
				VsSolutionProjectPart proj = new VsSolutionProjectPart();

				data = data.Substring(data.IndexOf("=") + 1);
				string[] split = data.Split(',');

				proj._name = split[0].Trim().Trim('\"');
				proj._path = split[1].Trim().Trim('\"');
				proj._guid = split[2].Trim().Trim('\"');

				return proj;
			}
			catch
			{
				throw (new FormatException("Project data was not in correct format."));
			}
		}
	}
}
