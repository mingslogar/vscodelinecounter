using System;

namespace VSCodeLineCounter
{
	class ListBoxItemData
	{
		public ListBoxItemData(string name, string path, DateTime dateModified, string type, string size, int lineCount, string project)
		{
			_name = name;
			_path = path;
			_dateModified = dateModified;
			_type = type;
			_size = size;
			_lineCount = lineCount;
			_project = project;
		}

		private string _name;
		private string _path;
		private DateTime _dateModified;
		private string _type;
		private string _size;
		private int _lineCount;
		private string _project;

		public string Name
		{
			get { return _name; }
		}

		public string Path
		{
			get { return _path; }
		}

		public DateTime DateModified
		{
			get { return _dateModified; }
		}

		public string Type
		{
			get { return _type; }
		}

		public string Size
		{
			get { return _size; }
		}

		public string LineCount
		{
			get { return _lineCount.ToString(); }
		}

		public string Project
		{
			get { return _project; }
			set { _project = value; }
		}
	}
}
