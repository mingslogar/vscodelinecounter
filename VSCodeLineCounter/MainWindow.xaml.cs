using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using VSCodeLineCounter.Parser.Project;
using VSCodeLineCounter.Parser.Solution;

namespace VSCodeLineCounter
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			solutionPathBox.Text = VSCodeLineCounter.Properties.Settings.Default.path;

			if (VSCodeLineCounter.Properties.Settings.Default.skip != null)
			{
				foreach (string each in VSCodeLineCounter.Properties.Settings.Default.skip)
					skipBox.Text += each + "\r\n";

				skipBox.Text = skipBox.Text.TrimEnd();
			}

			(files.View as GridView).Columns.CollectionChanged += Columns_CollectionChanged;

			Closing += MainWindow_Closing;
		}

		private void MainWindow_Closing(object sender, CancelEventArgs e)
		{
			VSCodeLineCounter.Properties.Settings.Default.path = solutionPathBox.Text;

			StringCollection skipCollection = new StringCollection();
			string[] skip = skipBox.Text.Split('\n');

			foreach (string each in skip)
				skipCollection.Add(each.Trim());

			VSCodeLineCounter.Properties.Settings.Default.skip = skipCollection;
			VSCodeLineCounter.Properties.Settings.Default.Save();
		}

		private void browseButton_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Filter = "Microsoft Visual Studio Solution|*.sln";
			if (ofd.ShowDialog(this) == true)
			{
				solutionPathBox.Text = ofd.FileName;
			}
		}

		private void parseButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				skipBox.IsEnabled = solutionPathBox.IsEnabled = browseButton.IsEnabled = parseButton.IsEnabled = false;
				parseButton.Content = "Loading...";

				string solutionFile = solutionPathBox.Text;
				string[] skip = skipBox.Text.Split('\n');

				Thread thread = new Thread(() => { Parse(solutionFile, skip); });
				thread.IsBackground = true;
				thread.Start();
			}
			catch (Exception exc)
			{
				MessageBox.Show("Error parsing solution:\r\n\r\n" + exc.ToString());
			}
		}

		private void Parse(string solutionFile, string[] skip)
		{
			VsSolution solution = VsSolution.Parse(File.ReadAllLines(solutionFile));

			string solutionPath = solutionFile;
			solutionPath = solutionPath.Remove(solutionPath.LastIndexOf('\\'));

			skip = TrimToLower(skip);

			List<ListBoxItemData> allFiles = new List<ListBoxItemData>();

			int totalLines = 0;
			int totalItems = 0;
			long totalSize = 0;

			foreach (VsSolutionProjectPart each in solution.LinkedProjects)
			{
				if (!Contains(skip, each.Name.ToLower().Trim()))
				{
					string projectPath = solutionPath + "\\" + each.Path;

					if (File.Exists(projectPath))
					{
						VsProject proj = VsProject.Parse(projectPath.Remove(projectPath.LastIndexOf('\\')), File.ReadAllText(projectPath));
						projectPath = projectPath.Remove(projectPath.LastIndexOf('\\'));

						ProcessProject(allFiles, ref totalLines, ref totalItems, ref totalSize, each, projectPath, proj);
					}
				}
			}

			Dispatcher.BeginInvoke(() =>
			{
				files.ItemsSource = allFiles;
				skipBox.IsEnabled = solutionPathBox.IsEnabled = browseButton.IsEnabled = parseButton.IsEnabled = true;
				parseButton.Content = "_Parse";
				lineCount.Text = totalLines.ToString() + " line" + (totalLines != 1 ? "s" : "");
				itemCount.Text = totalItems.ToString() + " item" + (totalItems != 1 ? "s" : "");
				itemSize.Text = IOHelpers.FormatFileSize(totalSize);
			});
		}

		private void ProcessProject(List<ListBoxItemData> allFiles, ref int totalLines, ref int totalItems, ref long totalSize, VsSolutionProjectPart each, string projectPath, VsProject proj)
		{
			foreach (string file in proj.IncludedFiles)
			{
				string filename = projectPath + "\\" + file;
				FileInfo info = new FileInfo(filename);

				bool alreadyIndexed = false;

				foreach (ListBoxItemData lbid in allFiles)
					if (lbid.Path + "\\" + lbid.Name == info.FullName)
					{
						alreadyIndexed = true;
						lbid.Project += ", " + each.Name;
						break;
					}

				if (!alreadyIndexed)
				{
					int lines = File.ReadAllLines(filename).Length;

					ListBoxItemData lbid = new ListBoxItemData(info.Name, info.DirectoryName,
						info.LastWriteTime, FileType(filename), IOHelpers.FormatFileSize(info.Length),
						lines, each.Name);

					totalLines += lines;
					totalItems++;
					totalSize += info.Length;

					allFiles.Add(lbid);
				}
			}

			foreach (VsProject project in proj.IncludedProjects)
				ProcessProject(allFiles, ref totalLines, ref totalItems, ref totalSize, each, projectPath, project);
		}

		private string FileType(string filename)
		{
			NativeMethods.SHFILEINFO fInfo = new NativeMethods.SHFILEINFO();

			uint dwFileAttributes = NativeMethods.FILE_ATTRIBUTE.FILE_ATTRIBUTE_NORMAL;
			uint uFlags = (uint)(NativeMethods.SHGFI.SHGFI_TYPENAME | NativeMethods.SHGFI.SHGFI_USEFILEATTRIBUTES);

			NativeMethods.SHGetFileInfo(filename, dwFileAttributes, ref fInfo, (uint)Marshal.SizeOf(fInfo), uFlags);

			return fInfo.szTypeName;
		}

		private string[] TrimToLower(string[] array)
		{
			int count = array.Length;

			for (int i = 0; i < count; i++)
				array[i] = array[i].ToLower().Trim();

			return array;
		}

		private bool Contains(string[] array, string element)
		{
			foreach (string each in array)
				if (each == element)
					return true;

			return false;
		}

		private void Columns_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Move)
			{
				if (e.NewStartingIndex == 0 || e.OldStartingIndex == 0)
				{
					GridViewColumnCollection col = sender as GridViewColumnCollection;
					(col[0].Header as GridViewColumnHeader).Padding = new Thickness(15, 0, 15, 0);

					for (int i = 1; i < col.Count; i++)
					{
						((Control)col[i].Header).Padding = new Thickness(6, 0, 6, 0);
					}
				}
			}
		}

		private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
		{
			if (files.ItemsSource == null)
				return;

			IEnumerable items = files.ItemsSource;
			string sortBy = (string)((FrameworkElement)sender).Tag;

			Thread thread = new Thread(() => { Sort(items, sortBy); });
			thread.IsBackground = true;
			thread.Start();
		}

		private void Sort(IEnumerable items, string sortBy)
		{
			List<ListBoxItemData> arrayList = (List<ListBoxItemData>)items;
			int length = arrayList.Count - 1;

			_PropertyInfo property = typeof(ListBoxItemData).GetProperty(sortBy);

			Func<object, object, int> ComparisonFunction;

			switch (sortBy)
			{
				case "Name":
				case "Type":
				case "Project":
				case "Path":
					ComparisonFunction = StringCompareTo;
					break;

				case "LineCount":
					ComparisonFunction = IntCompareTo;
					break;

				case "DateModified":
					ComparisonFunction = DateTimeCompareTo;
					break;

				case "Size":
					ComparisonFunction = SizeCompareTo;
					break;

				default:
					throw (new NotImplementedException("Unhandled sort parameter."));
			}

			while (true)
			{
				bool _swapMade = false;

				for (int i = 0; i < length; i++)
				{
					ListBoxItemData i0 = arrayList[i];
					ListBoxItemData i1 = arrayList[i + 1];

					if (ComparisonFunction(property.GetValue(i0, null), property.GetValue(i1, null)) > 0)
					{
						arrayList[i + 1] = i0;
						arrayList[i] = i1;
						_swapMade = true;
					}
				}

				if (!_swapMade)
					break;
			}

			Dispatcher.BeginInvoke(() =>
			{
				files.ItemsSource = null;
				files.ItemsSource = arrayList;
			});
		}

		private int StringCompareTo(object str1, object str2)
		{
			return ((string)str1).CompareTo((string)str2);
		}

		private int DateTimeCompareTo(object dt1, object dt2)
		{
			return -1 * ((DateTime)dt1).CompareTo((DateTime)dt2);
		}

		private int IntCompareTo(object int1, object int2)
		{
			return -1 * (int.Parse((string)int1)).CompareTo(int.Parse((string)int2));
		}

		private int SizeCompareTo(object size1, object size2)
		{
			return -1 * IOHelpers.FormatFileSize((string)size1).CompareTo(IOHelpers.FormatFileSize((string)size2));
		}
	}
}
