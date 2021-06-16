using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Pdf;
using iText.IO.Image;

using WPFCustomMessageBox;

using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;

namespace PdfMerge
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		string[] m_PdfsToMerge;
		string m_OutputFile;
		ProgressWindow m_ProgressWindow;
		BackgroundWorker m_Worker;
		bool m_Cancelled;

		public MainWindow()
		{
			InitializeComponent();
		}

		void FilePicker_FilesPicked(string[] files)
		{
			m_PdfsToMerge = files;
			picturesInfo.Content = $"Wybrano {files.Length} pliki .pdf";
			mergeBtn.IsEnabled = CanMerge();
		}
		void FilePicker_OutputFolderPicked(string[] file)
		{
			m_OutputFile = file[0];
			mergeBtn.IsEnabled = CanMerge();
		}
		bool CanMerge()
		{
			if (m_PdfsToMerge == null || m_PdfsToMerge.Length == 0 || string.IsNullOrEmpty(m_OutputFile))
				return false;

			foreach (var file in m_PdfsToMerge)
			{
				if (!File.Exists(file))
					return false;
			}

			return true;
		}

		void MergeBtn_Click(object sender, RoutedEventArgs e)
		{
			m_Worker = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
			m_Worker.DoWork += Worker_DoWork;
			m_Worker.ProgressChanged += Worker_ProgressChanged;
			m_Worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
			m_Worker.RunWorkerAsync();
		}
		void Worker_DoWork(object sender, DoWorkEventArgs e)
		{
			Dispatcher.BeginInvoke(new Action(() =>
			{
				m_ProgressWindow = new ProgressWindow();
				m_ProgressWindow.Owner = this;
				m_ProgressWindow.ShowDialog();
			}), DispatcherPriority.Background);

			m_Cancelled = false;
			m_Worker.ReportProgress(0, ("Rozpoczynanie...", ""));

			// Write to output file
			using (var pdfWriter = new PdfWriter(m_OutputFile))
			{
				using (var pdfMerged = new PdfDocument(pdfWriter))
				{
					// Cancelling state
					if (m_Worker.CancellationPending)
					{
						m_Worker.ReportProgress(-1, ("Anulowanie...", ""));
						Dispatcher.BeginInvoke(new Action(() => m_ProgressWindow.cancelBtn.IsEnabled = false), DispatcherPriority.Background);

						if (pdfMerged.GetNumberOfPages() > 0) pdfMerged.Close();
						pdfWriter.Dispose();

						m_Worker.ReportProgress(-2, ("Anulowano", ""));
						m_Cancelled = true;
						return;
					}

					// Merge pdfs
					for (int i = 0; i < m_PdfsToMerge.Length; i++)
					{
						using (var reader = new PdfReader(m_PdfsToMerge[i]))
						{
							using (var pdfDoc = new PdfDocument(reader))
							{
								pdfDoc.CopyPagesTo(1, pdfDoc.GetNumberOfPages(), pdfMerged);
							}
						}
						m_Worker.ReportProgress(Convert.ToInt32((float)(i + 1) / m_PdfsToMerge.Length * 100f), ($"Scalanie {i + 1} z {m_PdfsToMerge.Length}:", Path.GetFileName(m_PdfsToMerge[i])));
					}
				}
			}

			// Ending state
			Dispatcher.BeginInvoke(new Action(() => m_ProgressWindow.cancelBtn.IsEnabled = false), DispatcherPriority.Background);
			m_Worker.ReportProgress(100, ("Usuwanie z pamięci tymczasowej...", ""));
		}
		void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			Dispatcher.BeginInvoke(new Action(() =>
			{
				var progress = ((string, string))e.UserState;

				if (e.ProgressPercentage == 100 || e.ProgressPercentage == -1) m_ProgressWindow.progressBar.IsIndeterminate = true;

				m_ProgressWindow.progressBar.Value = e.ProgressPercentage;
				m_ProgressWindow.progressInfo1.Content = progress.Item1;
				m_ProgressWindow.progressInfo2.Content = progress.Item2;
			}), DispatcherPriority.Background);
		}
		void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			// Close progress window
			Dispatcher.BeginInvoke(new Action(() =>
			{
				m_ProgressWindow.Close();
			}), DispatcherPriority.Background);

			if (m_Cancelled)
				return;

			// Show dialog after converting
			var result = CustomMessageBox.ShowOKCancel("Zakończono scalanie", "Informacja", "Otwórz plik .pdf", "Zamknij", MessageBoxImage.Information);

			if (result == MessageBoxResult.OK)
			{
				Process.Start(m_OutputFile);
			}
		}
		public void CancelConvert()
		{
			m_Worker.CancelAsync();
		}
	}
}