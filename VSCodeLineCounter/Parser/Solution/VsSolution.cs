using System;

namespace VSCodeLineCounter.Parser.Solution
{
	class VsSolution
	{
		public VsSolution()
		{

		}

		private VsSolutionProjectPart[] _linkedProjects;

		public VsSolutionProjectPart[] LinkedProjects
		{
			get { return _linkedProjects; }
		}

		public static VsSolution Parse(string[] data)
		{
			try
			{
				VsSolution solution = new VsSolution();
				solution._linkedProjects = new VsSolutionProjectPart[0];

				foreach (string each in data)
				{
					if (each.StartsWith("Project(\"{"))
					{
						Array.Resize(ref solution._linkedProjects, solution._linkedProjects.Length + 1);
						solution._linkedProjects[solution._linkedProjects.Length - 1] = VsSolutionProjectPart.Parse(each);
					}
				}

				return solution;
			}
			catch
			{
				throw (new FormatException("Solution data was not in correct format."));
			}
		}
	}
}
