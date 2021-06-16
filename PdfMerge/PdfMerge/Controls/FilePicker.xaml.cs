using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

using Microsoft.WindowsAPICodePack.Dialogs;

namespace Utils.Controls
{
	/// <summary>
	/// Interaction logic for FilePathDialog.xaml
	/// </summary>
	public partial class FilePicker : UserControl
	{
		public bool Multiselect { get; set; }
		public bool IsFolderPicker { get; set; }
		public bool IsSaveFileDialog { get; set; }
		public string Placeholder { get; set; }

		/// <summary>
		/// Pattern: "display_name1|extension1|display_name2|extension2 ..."
		/// </summary>
		public string Filters { get; set; }
		public string DefaultDirectory { get; set; }

		public string FilePath => pathBox.Text;
		public event Action<string[]> OnFilePicked;

		public FilePicker()
		{
			InitializeComponent();
		}

		void Button_Click(object sender, RoutedEventArgs e)
		{
			DefaultDirectory = Environment.ExpandEnvironmentVariables(DefaultDirectory);

			if (IsSaveFileDialog)
			{
				if (Multiselect || IsFolderPicker)
					throw new InvalidOperationException($"{nameof(IsSaveFileDialog)} cannot be true if {nameof(IsFolderPicker)} or {nameof(Multiselect)} are true!");

				CommonSaveFileDialog dialog = new CommonSaveFileDialog { DefaultDirectory = this.DefaultDirectory };
				FilterSetup(dialog);

				if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
				{
					// Gets full path without extenion
					string output = Path.ChangeExtension(dialog.FileName, null) + "." + dialog.Filters[0].Extensions[0];
					pathBox.Text = output;
					OnFilePicked?.Invoke(Enumerable.Empty<string>().Append(output).ToArray());
				}
			}
			else
			{
				if(IsFolderPicker && Filters != null)
					throw new InvalidOperationException($"{nameof(Filters)} cannot be set if {nameof(IsFolderPicker)} is true!");

				CommonOpenFileDialog dialog = new CommonOpenFileDialog() { IsFolderPicker = this.IsFolderPicker, Multiselect = this.Multiselect, DefaultDirectory = this.DefaultDirectory };
				FilterSetup(dialog);

				if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
				{
					pathBox.Text = Multiselect ? string.Join(" ", dialog.FileNames.Select(x => x = $"\"{Path.GetFileName(x)}\"")) : dialog.FileName; ;
					OnFilePicked?.Invoke(dialog.FileNames.ToArray());
				}
			}

			void FilterSetup(CommonFileDialog dialog)
			{
				if (Filters != null)
				{
					string[] split = Filters.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
					for (int i = 0; i < split.Length; i += 2)
					{
						string displayName = split[i];
						string extensionList = split[i + 1];
						var filter = new CommonFileDialogFilter(displayName, extensionList);
						dialog.Filters.Add(filter);
					}
				}
			}
		}
		void pathBox_TextChanged(object sender, TextChangedEventArgs e) { }

		public void ChangePath(string content)
		{
			pathBox.Text = content;
		}
	}
}
