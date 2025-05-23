﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает главную форму програмы
	/// </summary>
	public partial class TablesMergerForm: Form
		{
		// Исходные таблицы
		private List<List<List<double>>> dataTables = [];
		private List<List<string>> columnNames = [];
		private List<uint> abscissasColumnsNumbers = [];

		private List<SourceTableRow> dataRows = [];

		// Собранная таблица
		private List<List<double>> mergedTable = [];
		private List<string> mergedColumnNames = [];

		// Флаги обработки
		private int mergeType;
		private bool success = false;

		/// <summary>
		/// Конструктор. Запускает интерфейс мерджера
		/// </summary>
		public TablesMergerForm ()
			{
			// Инициализация окна
			InitializeComponent ();

			// Настройка контролов
			SFDialog.FilterIndex = 3;

			ProcessingResults.Items.Add ("Tables merger" + RDLocale.GetText ("TablesMerger_LaunchedAt") +
				DateTime.Now.ToString ("dd.MM.yyyy HH:mm"));
			ProcessingResults.SelectedIndex = ProcessingResults.Items.Count - 1;

			// Запуск
			this.Text = ProgramDescription.AssemblyTitle;
			LocalizeForm ();

			this.ShowDialog ();
			}

		// Локализация интерфейса
		private void LocalizeForm ()
			{
			// Локализация
			OFDialog.Filter = RDLocale.GetText (this.Name + "_OFDialog_F");
			OFDialog.Title = RDLocale.GetText ("GeomagDataDrawerForm_OFDialog");
			SFDialog.Filter = RDLocale.GetText (this.Name + "_SFDialog_F");
			SFDialog.Title = RDLocale.GetText ("GeomagDataDrawerForm_SFDialog");

			while (MergeType.Items.Count < 2)
				{
				MergeType.Items.Add ("");
				MergeType.SelectedIndex = 0;
				}
			for (int i = 0; i < MergeType.Items.Count; i++)
				MergeType.Items[i] = RDLocale.GetText ("TablesMergerForm_MergeType" + i.ToString ());

			RDLocale.SetControlsText (this);
			BExit.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Button_Exit);
			}

		// Добавление файлов в обработку
		private void AddFiles_Click (object sender, EventArgs e)
			{
			OFDialog.ShowDialog ();
			}

		private void OFDialog_FileOk (object sender, CancelEventArgs e)
			{
			// Запрос параметров обработки файлов
			UnknownFileParametersSelector ufps = new UnknownFileParametersSelector (2, true);
			if (ufps.Cancelled)
				return;

			ColumnsNamesSelector cns = new ColumnsNamesSelector (0);
			if (cns.Cancelled)
				return;

			// Для каждого файла
			for (int i = 0; i < OFDialog.FileNames.Length; i++)
				{
				// Формирование таблицы данных
				DiagramData dd = new DiagramData (OFDialog.FileNames[i], ufps.DataColumnsCount, cns.SkippedRowsCount);
				if (dd.InitResult != DiagramDataInitResults.Ok)
					{
					ProcessingResults.Items.Add (DiagramData.GetDataLoadError (dd.InitResult,
						OFDialog.FileNames[i]));
					continue;
					}

				// Добавление в списки
				dataTables.Add (dd.GetData ());
				columnNames.Add ([]);
				for (uint c = 0; c < dd.DataColumnsCount; c++)
					columnNames[columnNames.Count - 1].Add (dd.GetDataColumnName (c));

				abscissasColumnsNumbers.Add (ufps.AbscissasColumn);
				FileNamesList.Items.Add (OFDialog.FileNames[i]);

				ProcessingResults.Items.Add (string.Format (RDLocale.GetText ("FileAddCompleted"),
					Path.GetFileName (OFDialog.FileNames[i]),
					dataTables[dataTables.Count - 1].Count,
					dataTables[dataTables.Count - 1][0].Count));
				}

			// Завершено
			ProcessingResults.SelectedIndex = ProcessingResults.Items.Count - 1;
			}

		// Сброс списка файлов
		private void ClearFiles_Click (object sender, EventArgs e)
			{
			FileNamesList.Items.Clear ();
			dataTables.Clear ();
			columnNames.Clear ();
			abscissasColumnsNumbers.Clear ();

			ProcessingResults.Items.Add (RDLocale.GetText ("FilesListReset"));
			ProcessingResults.SelectedIndex = ProcessingResults.Items.Count - 1;
			}

		// Запуск обработки
		private void BeginProcessing_Click (object sender, EventArgs e)
			{
			// Контроль
			if (dataTables.Count < 2)
				{
				RDInterface.LocalizedMessageBox (RDMessageTypes.Warning_Center, "NotEnoughFilesToMerge");
				return;
				}

			// Подготовка
			AddFiles.Enabled = ClearFiles.Enabled = MergeType.Enabled = BeginProcessing.Enabled =
				SaveResult.Enabled = BExit.Enabled = success = false;
			dataRows.Clear ();
			mergedTable.Clear ();
			mergedColumnNames.Clear ();
			mergeType = MergeType.SelectedIndex;

			// Запуск 
			RDInterface.RunWork (ExecuteMerge, null, null,
				RDRunWorkFlags.CaptionInTheMiddle | RDRunWorkFlags.AllowOperationAbort);

			// Завершено
			AddFiles.Enabled = ClearFiles.Enabled = MergeType.Enabled = BeginProcessing.Enabled =
				BExit.Enabled = true;
			if (success)
				{
				ProcessingResults.Items.Add (string.Format (RDLocale.GetText ("TablesMergeCompleted"),
					mergedTable.Count, mergedTable[0].Count));
				ProcessingResults.SelectedIndex = ProcessingResults.Items.Count - 1;
				SaveResult.Enabled = true;
				}
			}

		// Процесс, выполняющий объединение
		private void ExecuteMerge (object sender, DoWorkEventArgs e)
			{
			// Перегонка данных
			mergedColumnNames.Add ("x");
			BackgroundWorker bw = (BackgroundWorker)sender;

			for (int f = 0; f < dataTables.Count; f++)
				{
				// Сборка строк
				for (int r = 0; r < dataTables[f][(int)abscissasColumnsNumbers[f]].Count; r++)
					{
					// Возврат прогресса
					bw.ReportProgress ((int)(r * HardWorkExecutor.ProgressBarSize /
						dataTables[f][(int)abscissasColumnsNumbers[f]].Count),
						string.Format (RDLocale.GetText ("TablesAssembling"), f + 1, r));

					// Создание строки
					dataRows.Add (new SourceTableRow ((uint)f, dataTables[f][(int)abscissasColumnsNumbers[f]][r]));

					for (int c = 0; c < dataTables[f].Count; c++)
						{
						// Пропуск столбца абсцисс
						if (c == abscissasColumnsNumbers[f])
							continue;

						// Добавление ординат
						dataRows[dataRows.Count - 1].AddOrdinate (dataTables[f][c][r]);

						// Завершение работы, если получено требование от диалога
						if (bw.CancellationPending)
							{
							e.Cancel = true;
							return;
							}
						}
					}

				// Сборка имён столбцов
				for (int c = 0; c < columnNames[f].Count; c++)
					{
					if (c != (int)abscissasColumnsNumbers[f])
						mergedColumnNames.Add (columnNames[f][c]);
					}
				}

			// Сортировка
			bw.ReportProgress ((int)HardWorkExecutor.ProgressBarSize, RDLocale.GetText ("TablesSorting"));
			dataRows.Sort ();

			// Сборка итоговой таблицы
			double currentAbscissa = double.NaN;
			for (int r = 0; r < dataRows.Count; r++)
				{
				// Возврат прогресса
				bw.ReportProgress ((int)(r * HardWorkExecutor.ProgressBarSize / dataRows.Count),
					string.Format (RDLocale.GetText ("TablesMerging"), r));

				// Добавление строк в таблицу
				if (currentAbscissa != dataRows[r].X)
					{
					currentAbscissa = dataRows[r].X;
					mergedTable.Add ([]);
					mergedTable[mergedTable.Count - 1].Add (dataRows[r].X);
					}

				// Сборка строк
				for (uint t = 0; t < dataTables.Count; t++)
					{
					// i может отличаться от r в случае пропусков строк
					int i = dataRows.IndexOf (new SourceTableRow (t, currentAbscissa));

					// Требуется дозаполнение
					if (i < 0)
						{
						switch (mergeType)
							{
							// Удаление неполных строк
							case 0:
								// Следующая схема вызовет выход из цикла обработки строк с данной ординатой
								mergedTable.RemoveAt (mergedTable.Count - 1);
								t = (uint)dataTables.Count - 1;
								break;

							// Заполнение нулями
							case 1:
								for (int c = 1; c < dataTables[(int)t].Count; c++)
									mergedTable[mergedTable.Count - 1].Add (0);

								break;
							}
						}

					// Объект найден
					else
						{
						mergedTable[mergedTable.Count - 1].AddRange (dataRows[i].Y);
						}
					}

				// Пропуск возможных дублей строк
				r = dataRows.FindLastIndex (x => x.X == currentAbscissa);
				}

			// Финальный контроль
			if (mergedTable.Count < 2)
				{
				RDInterface.LocalizedMessageBox (RDMessageTypes.Warning_Center, "TablesMergeError");
				return;
				}

			// Завершено
			e.Result = null;
			success = true;
			}

		// Выход из программы
		private void BExit_Click (object sender, EventArgs e)
			{
			this.Close ();
			}

		private void TableMergerForm_FormClosing (object sender, FormClosingEventArgs e)
			{
			e.Cancel = (RDInterface.LocalizedMessageBox (RDMessageTypes.Warning_Center, "TablesMerger_Exit",
				RDLDefaultTexts.Button_Yes, RDLDefaultTexts.Button_No) != RDMessageButtons.ButtonOne);
			}

		// Сохранение таблицы
		private void SaveResult_Click (object sender, EventArgs e)
			{
			SFDialog.ShowDialog ();
			}

		private void SFDialog_FileOk (object sender, CancelEventArgs e)
			{
			DiagramData dd = new DiagramData (mergedTable, mergedColumnNames);
			if (dd.SaveDataFile (SFDialog.FileName, (DataOutputTypes)(SFDialog.FilterIndex + 1), true) != 0)
				{
				RDInterface.MessageBox (RDMessageTypes.Warning_Center,
					string.Format (RDLocale.GetDefaultText (RDLDefaultTexts.Message_SaveFailure_Fmt),
					SFDialog.FileName));
				return;
				}

			ProcessingResults.Items.Add (RDLocale.GetText ("TablesMerger_FileSaved"));
			ProcessingResults.SelectedIndex = ProcessingResults.Items.Count - 1;
			}
		}
	}
