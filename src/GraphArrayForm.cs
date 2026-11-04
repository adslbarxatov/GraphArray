using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает главную форму программы
	/// </summary>
	public partial class GraphArrayForm: Form
		{
		// Создание класса-представителя данных диаграммы (фиктивная инициализация)
		private DiagramData dd = new DiagramData ("", DataInputTypes.Unknown, 0);

		// Состояние загрузки значений параметров в контролы
		private bool loading = false, selectionMode = false;

		// Загрузчик маркеров
		private MarkersLoader ml = new MarkersLoader ();

		// Основное поле отрисовки программы
		private Graphics drawField;

		/*// Текущий список выбранных кривых
		private List<int> selectedInidces = [];*/

		/*// Состояние загрузки списка выбранных кривых
		private bool selecting = false;*/

		// Промежуточные координаты указателя мыши
		private int oldMouseX, oldMouseY;

		// Список индексов выбранных объектов
		private List<int> selectedIndices = [];
		private int selectedIndex = -1;

		// Отступ списка объектов в пункте меню
		private byte objectsOffset = 3;

		#region Настройка и функционирование главной формы

		/// <summary>
		/// Конструктор главной формы программы
		/// </summary>
		/// <param name="SentFileName">Имя файла, полученное от операционной системе при запуске программы</param>
		/// <param name="SentFileType">Предопределённый тип полученного файла</param>
		public GraphArrayForm (string SentFileName, DataInputTypes SentFileType)
			{
			// Инициализация и локализация формы
			InitializeComponent ();

			// Блокировка формата отображения чисел (для фиксации на NaN, десятичной точке и т. п.;
			// иначе в редакторе невозможно выставить точки разрыва)
			Application.CurrentCulture = RDLocale.GetCulture (RDLanguages.en_us);

			// Настройка списков
			for (int i = 0; i < 4; i++)
				{
				OxPlacementCombo.Items.Add (i.ToString ());
				OyPlacementCombo.Items.Add (i.ToString ());

				if (i < 3)
					{
					OxFormatCombo.Items.Add (i.ToString ());
					OyFormatCombo.Items.Add (i.ToString ());
					}
				if (i < 3)
					{
					LineStyleCombo.Items.Add (i.ToString ());
					}
				}

			LocalizeForm ();

			// Установка заголовка программы и параметров окна
			this.Text = RDGenerics.DefaultAssemblyVisibleName;
			this.MinimumSize = new Size ((int)ConfigAccessor.MinWidth, (int)ConfigAccessor.MinHeight);
			RDGenerics.LoadWindowDimensions (this);

			if (!RDGenerics.AppHasAccessRights (false, false))
				this.Text += RDLocale.GetDefaultText (RDLDefaultTexts.Message_LimitedFunctionality);

			// Потеря фокуса полем настройки
			MainTabControl_Leave (MainTabControl, null);

			#region Настройка контролов

			// Диалоги
			StyleFontDialog.MaxSize = (int)DiagramStyle.MaxFontSize;
			StyleFontDialog.MinSize = (int)DiagramStyle.MinFontSize;

			// Общие параметры
			LeftOffset.Minimum = TopOffset.Minimum = 0;
			LeftOffset.Maximum = DiagramStyle.MaxImageWidth;
			TopOffset.Maximum = DiagramStyle.MaxImageHeight;

			loading = true;
			ImageWidth.Minimum = DiagramStyle.MinImageWidth;
			ImageHeight.Minimum = DiagramStyle.MinImageHeight;
			ImageWidth.Maximum = DiagramStyle.MaxImageWidth;
			ImageHeight.Maximum = DiagramStyle.MaxImageHeight;
			loading = false;

			LineName.MaxLength = (int)DiagramStyle.MaxLineNameLength;

			// Настройки осей
			OxPrimaryDiv.Minimum = DiagramStyle.MinPrimaryDivisions;
			OxPrimaryDiv.Maximum = DiagramStyle.MaxPrimaryDivisions;
			OyPrimaryDiv.Minimum = DiagramStyle.MinPrimaryDivisions;
			OyPrimaryDiv.Maximum = DiagramStyle.MaxPrimaryDivisions;
			OxSecondaryDiv.Minimum = DiagramStyle.MinSecondaryDivisions;
			OxSecondaryDiv.Maximum = DiagramStyle.MaxSecondaryDivisions;
			OySecondaryDiv.Minimum = DiagramStyle.MinSecondaryDivisions;
			OySecondaryDiv.Maximum = DiagramStyle.MaxSecondaryDivisions;
			AxesWidth.Minimum = DiagramStyle.MinLineWidth;
			AxesWidth.Maximum = DiagramStyle.MaxLineWidth;

			// Настройки сетки
			GridWidth.Minimum = DiagramStyle.MinLineWidth;
			GridWidth.Maximum = DiagramStyle.MaxLineWidth;

			// Настройки линии кривой
			LineWidth.Minimum = DiagramStyle.MinLineWidth;
			LineWidth.Maximum = DiagramStyle.MaxLineWidth;
			LineMarker.Minimum = 1;
			LineMarker.Maximum = ml.MarkersCount;

			// Настройка эффекта выделения
			MainSelector.Parent = DiagramBox;

			// Подключение обработки колёсика мыши
			DiagramBox.MouseWheel += DiagramBox_MouseWheel;

			#endregion

			#region Загрузка диаграммы

			if (SentFileType == DataInputTypes.Unspecified)
				{
				// Загрузка стандартного файла данных при старте
				if (ConfigAccessor.ForceUsingBackupDataFile)
					{
					dd = new DiagramData (RDGenerics.GetStoragePath (true) + ConfigAccessor.BackupDataFileName,
						DataInputTypes.GDD, 0);
					}
				}
			else
				{
				// Тестовое открытие файла данных
				DiagramData ddt;
				if (SentFileType == DataInputTypes.Unknown)
					ddt = new DiagramData (SentFileName, 2, 0);
				else
					ddt = new DiagramData (SentFileName, SentFileType, 0);

				// Контроль результата (до настоящей загрузки результат BrokenFile допустим по признаку имён столбцов)
				if ((ddt.InitResult != DiagramDataInitResults.Ok) && (ddt.InitResult !=
					DiagramDataInitResults.BrokenFile))
					{
					RDInterface.MessageBox (RDMessageFlags.Warning,
						DiagramData.GetDataLoadError (ddt.InitResult, SentFileName));
					return;
					}

				// Обновление параметров загрузки
				if (!CheckFileLoadingParameters (SentFileType))
					return;

				// Контрольная загрузка файла, переданного из операционной системы
				if (SentFileType == DataInputTypes.Unknown)
					dd = new DiagramData (SentFileName, ConfigAccessor.ExpectedColumnsCount, ConfigAccessor.SkippedLinesCount);
				else
					dd = new DiagramData (SentFileName, SentFileType, ConfigAccessor.SkippedLinesCount);
				}

			// Контроль результата
			if (dd.InitResult == DiagramDataInitResults.Ok)
				{
				/*for (int i = 0; i < dd.LinesCount; i++)
					{
					LineNamesList.Items.Add (dd.GetDataColumnName (dd.GetStyle (i).YColumnNumber) + " @ " +
						dd.GetDataColumnName (dd.GetStyle (i).XColumnNumber));
					}
				for (int i = 0; i < dd.AdditionalObjectsCount; i++)
					{
					LineNamesList.Items.Add (dd.GetStyle (i + (int)dd.LinesCount).LineName);
					}
				if (LineNamesList.Items.Count != 0)
					{
					LineNamesList.SelectedItems.Clear ();
					LineNamesList.SelectedIndex = 0;
					}*/
				for (int i = 0; i < dd.LinesCount; i++)
					{
					DiagramStyle ds = dd.GetStyle (i);
					/*string name = dd.GetDataColumnName (ds.YColumnNumber) +
						" @ " + dd.GetDataColumnName (ds.XColumnNumber);
					ToolStripMenuItem item = (ToolStripMenuItem)MList. DropDownItems.Add (name, null,
						LineNames_ SelectedIndexChanged);
					item.Check OnClick = true;*/
					AddObject (dd.GetDataColumnName (ds.YColumnNumber) +
						" @ " + dd.GetDataColumnName (ds.XColumnNumber));
					}
				for (int i = 0; i < dd.AdditionalObjectsCount; i++)
					{
					/*string name = dd.GetStyle (i + (int)dd.LinesCount).LineName;
					ToolStripMenuItem item = (ToolStripMenuItem)MList. DropDownItems.Add (name, null,
						LineNames_S electedIndexChanged);
					item.Check OnClick = true;*/
					AddObject (dd.GetStyle (i + (int)dd.LinesCount).LineName);
					}
				if (MList.DropDownItems.Count > objectsOffset)
					SelectIndex (0);

				ChangeControlsState (true);

				if (ConfigAccessor.ForceShowDiagram && (SentFileType != DataInputTypes.GDD) &&
					(SentFileType != DataInputTypes.Unspecified))
					{
					AddFirstColumns ();
					}

				Redraw ();
				}
			else if (SentFileType != DataInputTypes.Unspecified)
				{
				RDInterface.MessageBox (RDMessageFlags.Warning,
					DiagramData.GetDataLoadError (dd.InitResult, SentFileName));
				}

			#endregion
			}

		// Метод выполняет локализацию формы
		private void LocalizeForm ()
			{
			// Поля настройки
			for (int i = 0; i < MainTabControl.TabPages.Count; i++)
				{
				string s;
				for (int j = 0; j < MainTabControl.TabPages[i].Controls.Count; j++)
					{
					Control ctrl = MainTabControl.TabPages[i].Controls[j];
					s = RDLocale.GetText (MainTabControl.TabPages[i].Name + "_" + ctrl.Name);
					if (!string.IsNullOrWhiteSpace (s))
						ctrl.Text = s;
					}

				ConfigAccessor.SetControlsText (MainTabControl.TabPages[i], MainToolTip);
				}

			AxeColorFromCurveButton.Text = TextColorFromCurveButton.Text;
			MainToolTip.SetToolTip (AxeColorFromCurveButton, MainToolTip.GetToolTip (TextColorFromCurveButton));

			// Меню программы
			MFile.Text = RDLocale.GetText (GraphArrayFormMenuStrip.Name + "_" + MFile.Name);
			ConfigAccessor.SetControlsText (MFile);
			MExit.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Button_Exit);

			MOperations.Text = RDLocale.GetText (GraphArrayFormMenuStrip.Name + "_" + MOperations.Name);
			ConfigAccessor.SetControlsText (MOperations);
			MUpperHelp.Text = RDLocale.GetText (GraphArrayFormMenuStrip.Name + "_" + MUpperHelp.Name);
			ConfigAccessor.SetControlsText (MUpperHelp);
			MAdditional.Text = RDLocale.GetText (GraphArrayFormMenuStrip.Name + "_" + MAdditional.Name);
			ConfigAccessor.SetControlsText (MAdditional);
			MLanguage2.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Control_InterfaceLanguage);

			RDLocale.SetControlText (MList);
			RDLocale.SetControlText (MSelectAll);
			RDLocale.SetControlText (MDeselectAll);

			// Кнопки управления
			ConfigAccessor.SetControlsText (this, MainToolTip);

			// Панель настроек
			for (int i = 0; i < MainTabControl.TabPages.Count; i++)
				{
				string s = RDLocale.GetText (MainTabControl.Name + "_" + MainTabControl.TabPages[i].Name + "_TT");
				if (!string.IsNullOrWhiteSpace (s))
					MainTabControl.TabPages[i].ToolTipText = s;
				}

			// Контролы и диалоги
			OFDialog.Filter = string.Format (RDLocale.GetText (this.Name + "_OFDialog_F"),
				ProgramDescription.AssemblyMainName, DiagramData.AppDataExtension2);
			SFDialog.Filter = string.Format (RDLocale.GetText (this.Name + "_SFDialog_F"),
				ProgramDescription.AssemblyMainName, DiagramData.AppDataExtension2);
			OpenImageDialog.Filter = RDLocale.GetText (this.Name + "_OIDialog_F");

			LoadStyleDialog.Filter = SaveStyleDialog.Filter = string.Format (RDLocale.GetText (this.Name +
				"_StyleDialog_F"), DiagramData.AppStyleExtension2);

			MainToolTip.ToolTipTitle = RDLocale.GetText (this.Name + "_MainToolTip");

			for (int i = 0; i < 4; i++)
				{
				OxPlacementCombo.Items[i] = RDLocale.GetText (this.Name + "_OxPlacement_" + i.ToString ());
				OyPlacementCombo.Items[i] = RDLocale.GetText (this.Name + "_OyPlacement_" + i.ToString ());

				if (i < 3)
					{
					OxFormatCombo.Items[i] = RDLocale.GetText (this.Name + "_OxFormat_" + i.ToString ());
					OyFormatCombo.Items[i] = RDLocale.GetText (this.Name + "_OxFormat_" + i.ToString ());
					}
				if (i < 3)
					{
					LineStyleCombo.Items[i] = RDLocale.GetText (this.Name + "_LineStyle_" + i.ToString ());
					}
				}
			}

		// Метод вызывает формы настройки параметров загрузки файлов данных
		// Возвращает false, если была нажата кнопка Cancel
		private static bool CheckFileLoadingParameters (DataInputTypes InputType)
			{
			// Обработка случая извлечения данных
			UnknownFileParametersSelector ufps;
			if (InputType == DataInputTypes.Unknown)
				{
				ufps = new UnknownFileParametersSelector (ConfigAccessor.ExpectedColumnsCount, false);
				if (ufps.Cancelled)
					return false;

				// Сохранение изменившегося значения
				ConfigAccessor.ExpectedColumnsCount = ufps.DataColumnsCount;
				}

			// Обработка случаев, требующих указания количества строк, используемых для поиска имён столбцов данных
			ColumnsNamesSelector cns;
			if (InputType != DataInputTypes.GDD)
				{
				cns = new ColumnsNamesSelector (ConfigAccessor.SkippedLinesCount);

				if (cns.Cancelled)
					return false;

				// Сохранение изменившегося значения
				ConfigAccessor.SkippedLinesCount = cns.SkippedRowsCount;
				}

			return true;
			}

		// Обработка изменения размера окна
		private void MainForm_Resize (object sender, EventArgs e)
			{
			// Изменение размера поля отрисовки
			HorScroll.Width = DiagramBox.Width = this.Width - 55;
			VertScroll.Height = DiagramBox.Height = this.Height - 65 - DiagramBox.Top;

			VertScroll.Left = this.Width - 35;
			HorScroll.Top = this.Height - 45 - HorScroll.Height;

			// Перерисовка
			Redraw ();
			}

		// Сворачивание панелей
		private void MainForm_Click (object sender, EventArgs e)
			{
			MainTabControl_Leave (MainTabControl, null);
			/*LineNamesList_Leave (LineNamesList, null);*/
			}

		// Активация/деактивация контролов при старте программы/закрытии диаграммы
		private void ChangeControlsState (bool NewState)
			{
			// Окно программы и меню
			if (DiagramBox.Enabled != NewState)
				{
				AddColumn.Enabled = MAddColumn.Enabled = MAddColumnCmd.Enabled = MMergeLines.Enabled =

				MHideShow.Enabled = DeleteColumn.Enabled = MReplaceColumn.Enabled = MDeleteColumn.Enabled =

				/*LineNamesList.Enabled*/
				MList.Enabled = DrawLine.Enabled =
				DiagramBox.Enabled = HorScroll.Enabled = VertScroll.Enabled =
				MLoadStyle.Enabled = MSaveStyle.Enabled = MResetStyle.Enabled = MSaveTemplate.Enabled =
				MSaveDataFile.Enabled = MSaveDiagramImage.Enabled = MRedactData.Enabled = NewState;

				MRestoreTemplate.Enabled = MReplaceTemplate.Enabled = NewState &&
					RDGenerics.AppHasAccessRights (false, false);
				}

			GetSelectedIndices ();

			/*bool isAnObject = LineNamesList.SelectedIndex >= dd.LinesCount;*/
			bool isAnObject = (selectedIndex >= dd.LinesCount);
			DiagramAdditionalObjects objectType = (isAnObject ? dd.GetObjectType ((uint)/*LineNamesList.SelectedIndex*/selectedIndex -
				dd.LinesCount) : DiagramAdditionalObjects.Text);
			/*bool hasItems = (LineNamesList.Items.Count != 0);*/
			bool hasItems = (MList.DropDownItems.Count > objectsOffset);

			// Основные
			AutoTextOffset.Enabled = SwitchXY.Enabled = (NewState && !isAnObject && hasItems);

			SwapX.Enabled = SwapY.Enabled = Label01.Enabled = MaxX.Enabled = MinX.Enabled = MaxY.Enabled =
				MinY.Enabled = MergeRangesButton.Enabled = (NewState && hasItems && !isAnObject);

			Label02.Enabled = Label03.Enabled = Label04.Enabled = Label05.Enabled =
			Label06.Enabled = LeftOffset.Enabled = TopOffset.Enabled = ImageHeight.Enabled = ImageWidth.Enabled =
			AlignAsRow.Enabled = AlignAsColumn.Enabled = AlignLeftTopButton.Enabled =
				AlignFieldButton.Enabled = (NewState && hasItems);

			// Оси
			OxSecondaryDiv.Enabled = OySecondaryDiv.Enabled = OxPlacementCombo.Enabled = OyPlacementCombo.Enabled =
			AutoDivisions.Enabled = AxesWidth.Enabled = AxesColor.Enabled = AxesColorTurnOff.Enabled =
			OxFormatCombo.Enabled = OyFormatCombo.Enabled = (NewState && !isAnObject && hasItems);

			OxPrimaryDiv.Enabled = OyPrimaryDiv.Enabled = (!AutoDivisions.Checked && hasItems);

			// Шрифты
			AxesFont.Enabled = AxesFontColor.Enabled = AxeColorFromCurveButton.Enabled =
				(NewState && !isAnObject && hasItems);

			Label27.Enabled = TextFont.Enabled = TextFontColor.Enabled = TextColorFromCurveButton.Enabled =
				(NewState && hasItems && (!isAnObject || isAnObject && (objectType == DiagramAdditionalObjects.Text)));

			// Текстовая подпись
			Label07.Enabled = Label08.Enabled = Label09.Enabled = Label10.Enabled =
			OxTextOffset.Enabled = OyTextOffset.Enabled = LineName.Enabled =
			LineNameLeftOffset.Enabled = LineNameTopOffset.Enabled = (!AutoTextOffset.Checked && hasItems);

			if (isAnObject && (objectType == DiagramAdditionalObjects.Text))
				Label09.Enabled = LineName.Enabled = (NewState && hasItems);

			// Сетка
			GridWidth.Enabled = GridPrimaryColor.Enabled = GridSecondaryColor.Enabled =
			GridColorTurnOff.Enabled = (NewState && !isAnObject && hasItems);

			// Линия
			LineMarker.Enabled = (LineStyleCombo.SelectedIndex > 0) && hasItems;

			LineStyleCombo.Enabled = (NewState && !isAnObject && hasItems);

			Label28.Enabled = Label29.Enabled = LineWidth.Enabled = (NewState && hasItems &&
				(!isAnObject || isAnObject &&
				((objectType == DiagramAdditionalObjects.LineH) ||
				(objectType == DiagramAdditionalObjects.LineV) ||
				(objectType == DiagramAdditionalObjects.LineNWtoSE) ||
				(objectType == DiagramAdditionalObjects.LineSWtoNE) ||
				(objectType == DiagramAdditionalObjects.Rectangle) ||
				(objectType == DiagramAdditionalObjects.Ellipse))));

			LineColor.Enabled = (NewState && hasItems && (!isAnObject || isAnObject &&
				((objectType == DiagramAdditionalObjects.LineH) ||
				(objectType == DiagramAdditionalObjects.LineV) ||
				(objectType == DiagramAdditionalObjects.LineNWtoSE) ||
				(objectType == DiagramAdditionalObjects.LineSWtoNE) ||
				(objectType == DiagramAdditionalObjects.Rectangle) ||
				(objectType == DiagramAdditionalObjects.Ellipse) ||
				(objectType == DiagramAdditionalObjects.FilledRectangle) ||
				(objectType == DiagramAdditionalObjects.FilledEllipse))));

			// Лейблы
			Label11.Enabled = Label12.Enabled = Label13.Enabled = Label14.Enabled = Label15.Enabled =
			Label16.Enabled = Label17.Enabled = Label18.Enabled = Label19.Enabled = Label20.Enabled =
			Label21.Enabled = Label22.Enabled = Label23.Enabled = Label24.Enabled = Label25.Enabled =
			Label26.Enabled = Label30.Enabled = Label31.Enabled = (NewState && !isAnObject && hasItems);
			}

		// Перерисовка диаграммы
		private void Redraw ()
			{
			// Контроль на наличие источника изображения
			if (dd == null)
				return;

			// Создание изображения, если оно не было создано
			if (DiagramBox.BackgroundImage != null)
				DiagramBox.BackgroundImage.Dispose ();
				
			if (drawField != null)
				drawField.Dispose ();
				
			DiagramBox.BackgroundImage = new Bitmap ((int)dd.DiagramWidth, (int)dd.DiagramHeight);
			drawField = Graphics.FromImage (DiagramBox.BackgroundImage);

			// Пересчёт параметров полос прокрутки
			HorScroll.Maximum = ((dd.DiagramWidth - DiagramBox.Width < 0) ? 0 : (int)(dd.DiagramWidth - DiagramBox.Width));
			VertScroll.Maximum = ((dd.DiagramHeight - DiagramBox.Height < 0) ? 0 : (int)(dd.DiagramHeight - DiagramBox.Height));

			// Перерисовка
			if (dd != null)		// Предотвращение перерисовки при запуске программы
				{
				/*dd.DrawAllDiagrams (drawField, LineNamesList.SelectedIndices);*/
				dd.DrawAllDiagrams (drawField, GetSelectedIndices ());
				Image im = (Image)DiagramBox.BackgroundImage.Clone ();
				drawField.Clear (DiagramStyle.ImageBackColor);
				drawField.DrawImage (im, -HorScroll.Value, -VertScroll.Value);
				im.Dispose ();
				}
			}

		// Изменение положения бегунков
		private void HorScroll_Scroll (object sender, ScrollEventArgs e)
			{
			if (e.Type == ScrollEventType.EndScroll)
				Redraw ();
			}

		private void VertScroll_Scroll (object sender, ScrollEventArgs e)
			{
			if (e.Type == ScrollEventType.EndScroll)
				Redraw ();
			}

		// Смещение кривой "перетаскиванием" или выделение группы кривых
		private void DiagramBox_MouseUp (object sender, MouseEventArgs e)
			{
			// Выделение диаграмм
			if (selectionMode)
				{
				selectionMode = false;
				MainSelector.Visible = false;

				// Сброс списка
				/*LineNamesList.SelectedItems.Clear ();*/
				ClearSelectedIndices ();

				// Поиск подходящих кривых
				Rectangle r1 = new Rectangle (Math.Min (e.X, oldMouseX) + HorScroll.Value, Math.Min (e.Y, oldMouseY) +
					VertScroll.Value, Math.Max (e.X, oldMouseX) - Math.Min (e.X, oldMouseX),
					Math.Max (e.Y, oldMouseY) - Math.Min (e.Y, oldMouseY));

				for (int i = 0; i < MList.DropDownItems.Count - objectsOffset; i++)
					{
					DiagramStyle ds = dd.GetStyle (i);
					if (!ds.AllowDrawing)
						continue;

					Rectangle r2 = new Rectangle ((int)ds.DiagramImageLeftOffset,
						(int)ds.DiagramImageTopOffset,
						(int)ds.DiagramImageWidth, (int)ds.DiagramImageHeight);

					if (r1.IntersectsWith (r2))
						/*LineNamesList.SelectedIndices.Add (i);*/
						SelectIndex (i);
					}

				// Отображение списка
				/*LineNamesList_MouseClick (sender, e);
				LineNamesList.Select ();*/
				Redraw ();
				MList.ShowDropDown ();
				}

			// Смещение по полю
			else
				{
				// Смещение по горизонтали
				if (HorScroll.Value + oldMouseX - e.X > HorScroll.Maximum)
					HorScroll.Value = HorScroll.Maximum;
				else if (HorScroll.Value + oldMouseX - e.X < HorScroll.Minimum)
					HorScroll.Value = HorScroll.Minimum;
				else
					HorScroll.Value += (oldMouseX - e.X);
				
				// Смещение по вертикали
				if (VertScroll.Value + oldMouseY - e.Y > VertScroll.Maximum)
					VertScroll.Value = VertScroll.Maximum;
				else if (VertScroll.Value + oldMouseY - e.Y < VertScroll.Minimum)
					VertScroll.Value = VertScroll.Minimum;
				else
					VertScroll.Value += (oldMouseY - e.Y);

				// Перерисовка (не пересекается с заданием положения изображения
				// и случайными нажатиями)
				if ((oldMouseX != e.X) || (oldMouseY != e.Y))
					Redraw ();
				}
			}

		// Изменение эффекта выделения
		private void DiagramBox_MouseMove (object sender, MouseEventArgs e)
			{
			if (!selectionMode)
				return;

			MainSelector.Left = Math.Min (oldMouseX, e.X);
			MainSelector.Top = Math.Min (oldMouseY, e.Y);
			MainSelector.Width = Math.Max (oldMouseX, e.X) - MainSelector.Left;
			MainSelector.Height = Math.Max (oldMouseY, e.Y) - MainSelector.Top;
			}

		// Задание положения и размера изображения кривой, а также положения его подписей или начало выделения
		private void DiagramBox_MouseDown (object sender, MouseEventArgs e)
			{
			// Потеря фокуса
			MainTabControl_Leave (MainTabControl, null);
			/*LineNamesList_Leave (LineNamesList, null);*/

			if (e.Button == MouseButtons.Middle)
				selectionMode = true;

			// Обработка движения по полю диаграммы или выделения кривых
			if (e.Clicks == 1)
				{
				oldMouseX = e.X;
				oldMouseY = e.Y;
				}

			// Эффект выделения, если требуется
			if (selectionMode)
				{
				MainSelector.Visible = true;
				MainSelector.Left = oldMouseX;
				MainSelector.Top = oldMouseY;
				MainSelector.Width = MainSelector.Height = 0;
				}

			// Обработка позиционирования диаграмм
			if (e.Clicks == 2)
				{
				// Смещение подписей осей и диаграммы
				if (Control.ModifierKeys == Keys.Control)
					{
					// Смещение подписи диаграммы
					if (e.Button == MouseButtons.Left)
						{
						if (e.X + HorScroll.Value - LeftOffset.Value > LineNameLeftOffset.Maximum)
							LineNameLeftOffset.Value = LineNameLeftOffset.Maximum;
						else if (e.X + HorScroll.Value - LeftOffset.Value < LineNameLeftOffset.Minimum)
							LineNameLeftOffset.Value = LineNameLeftOffset.Minimum;
						else
							LineNameLeftOffset.Value = e.X + HorScroll.Value - LeftOffset.Value;

						if (e.Y + VertScroll.Value - TopOffset.Value > LineNameTopOffset.Maximum)
							LineNameTopOffset.Value = LineNameTopOffset.Maximum;
						else if (e.Y + VertScroll.Value - TopOffset.Value < LineNameTopOffset.Minimum)
							LineNameTopOffset.Value = LineNameTopOffset.Minimum;
						else
							LineNameTopOffset.Value = e.Y + VertScroll.Value - TopOffset.Value;
						}

					// Смещение подписей осей
					if (e.Button == MouseButtons.Right)
						{
						if (e.X + HorScroll.Value - LeftOffset.Value > OyTextOffset.Maximum)
							OyTextOffset.Value = OyTextOffset.Maximum;
						else if (e.X + HorScroll.Value - LeftOffset.Value < OyTextOffset.Minimum)
							OyTextOffset.Value = OyTextOffset.Minimum;
						else
							OyTextOffset.Value = e.X + HorScroll.Value - LeftOffset.Value;

						if (e.Y + VertScroll.Value - TopOffset.Value > OxTextOffset.Maximum)
							OxTextOffset.Value = OxTextOffset.Maximum;
						else if (e.Y + VertScroll.Value - TopOffset.Value < OxTextOffset.Minimum)
							OxTextOffset.Value = OxTextOffset.Minimum;
						else
							OxTextOffset.Value = e.Y + VertScroll.Value - TopOffset.Value;
						}
					}

				// Положение и размер диаграммы
				else
					{
					// Положение
					if (e.Button == MouseButtons.Left)
						{
						if (e.X + HorScroll.Value > LeftOffset.Maximum)
							LeftOffset.Value = LeftOffset.Maximum;
						else if (e.X + HorScroll.Value < LeftOffset.Minimum)
							LeftOffset.Value = LeftOffset.Minimum;
						else
							LeftOffset.Value = e.X + HorScroll.Value;

						if (e.Y + VertScroll.Value > TopOffset.Maximum)
							TopOffset.Value = TopOffset.Maximum;
						else if (e.Y + VertScroll.Value < TopOffset.Minimum)
							TopOffset.Value = TopOffset.Minimum;
						else
							TopOffset.Value = e.Y + VertScroll.Value;
						}

					// Размер
					if (e.Button == MouseButtons.Right)
						{
						if (e.X + HorScroll.Value - LeftOffset.Value > ImageWidth.Maximum)
							ImageWidth.Value = ImageWidth.Maximum;
						else if (e.X + HorScroll.Value - LeftOffset.Value < ImageWidth.Minimum)
							ImageWidth.Value = ImageWidth.Minimum;
						else
							ImageWidth.Value = e.X + HorScroll.Value - LeftOffset.Value;

						if (e.Y + VertScroll.Value - TopOffset.Value > ImageHeight.Maximum)
							ImageHeight.Value = ImageHeight.Maximum;
						else if (e.Y + VertScroll.Value - TopOffset.Value < ImageHeight.Minimum)
							ImageHeight.Value = ImageHeight.Minimum;
						else
							ImageHeight.Value = e.Y + VertScroll.Value - TopOffset.Value;
						}
					}
				}
			}

		// Смещение диапазонов и зум
		private void DiagramBox_MouseWheel (object sender, MouseEventArgs e)
			{
			int sign = Math.Sign (e.Delta);

			// Зум
			if (ModifierKeys == Keys.Control)
				{
				decimal increment = sign * Math.Min (MinX.Increment, MinY.Increment);

				// Защита от разворота диапазона
				decimal preXSign = Math.Sign(MaxX.Value - MinX.Value);
				decimal postXSign = Math.Sign (MaxX.Value - MinX.Value - 2 * increment);
				decimal preYSign = Math.Sign (MaxY.Value - MinY.Value);
				decimal postYSign = Math.Sign (MaxY.Value - MinY.Value - 2 * increment);

				if (preXSign == postXSign)
					{
					MinX.Value += increment;
					MaxX.Value -= increment;
					}

				if (preYSign == postYSign)
					{
					MinY.Value += increment;
					MaxY.Value -= increment;
					}
				}

			// Горизонтальное смещение
			else if (ModifierKeys == Keys.Shift)
				{
				decimal increment = sign * MinX.Increment;
				MinX.Value += increment;
				MaxX.Value += increment;
				}

			// Вертикальное смещение
			else
				{
				decimal increment = sign * MinY.Increment;
				MinY.Value += increment;
				MaxY.Value += increment;
				}
			}

		// Завершение работы
		private void MainForm_FormClosing (object sender, FormClosingEventArgs e)
			{
			// Перезапись автосохраняемого файла данных
			bool fub = ConfigAccessor.ForceUsingBackupDataFile;
			if (fub && (dd != null) && (dd.InitResult == DiagramDataInitResults.Ok))
				{
				// Возвращаемый результат не имеет значения
				dd.SaveDataFile (RDGenerics.GetStoragePath (true) + ConfigAccessor.BackupDataFileName,
					DataOutputTypes.GDD, true);
				}

			// Подтверждение
			if (ConfigAccessor.ForceExitConfirmation || !fub && (dd != null) && (dd.InitResult == DiagramDataInitResults.Ok))
				{
				if (RDInterface.LocalizedMessageBox ((fub ? RDMessageFlags.Question : RDMessageFlags.Warning) | RDMessageFlags.CenterText,
					fub ? "ApplicationExit" : "ApplicationExitNoBackup",
					RDLDefaultTexts.Button_YesNoFocus, RDLDefaultTexts.Button_No) == RDMessageButtons.ButtonTwo)
					{
					e.Cancel = true;	// Отмена закрытия окна
					}
				}

			// Сохранение конфигурации
			RDGenerics.SaveWindowDimensions (this);
			}

		#endregion

		#region Меню программы «Файл»

		// Выбор файла данных
		private void MOpenDataFile_Click (object sender, EventArgs e)
			{
			// Защита
			if ((dd != null) && (dd.InitResult == DiagramDataInitResults.Ok) &&
				(RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText, "AbortChanges",
				RDLDefaultTexts.Button_YesNoFocus, RDLDefaultTexts.Button_No) != RDMessageButtons.ButtonOne))
				{
				return;
				}

			OFDialog.FileName = "";
			OFDialog.ShowDialog ();
			}

		// Файл выбран
		private void OFDialog_FileOk (object sender, CancelEventArgs e)
			{
			// Тестовое открытие файла данных
			DiagramData ddt;
			if (OFDialog.FilterIndex == (int)DataInputTypes.Unknown)
				ddt = new DiagramData (OFDialog.FileName, 2, 0);
			else
				ddt = new DiagramData (OFDialog.FileName, (DataInputTypes)OFDialog.FilterIndex, 0);

			// Контроль результата (до настоящей загрузки результат BrokenFile допустим по признаку имён столбцов)
			if ((ddt.InitResult != DiagramDataInitResults.Ok) && (ddt.InitResult != DiagramDataInitResults.BrokenFile))
				{
				RDInterface.MessageBox (RDMessageFlags.Warning,
					DiagramData.GetDataLoadError (ddt.InitResult, OFDialog.FileName));
				return;
				}

			// Обновление параметров загрузки файлов
			if (!CheckFileLoadingParameters ((DataInputTypes)OFDialog.FilterIndex))
				return;

			// Контрольное открытие
			if (OFDialog.FilterIndex == (int)DataInputTypes.Unknown)
				dd = new DiagramData (OFDialog.FileName, ConfigAccessor.ExpectedColumnsCount, ConfigAccessor.SkippedLinesCount);
			else
				dd = new DiagramData (OFDialog.FileName, (DataInputTypes)OFDialog.FilterIndex, ConfigAccessor.SkippedLinesCount);

			if (dd.InitResult != DiagramDataInitResults.Ok)
				{
				// Файл точно с ошибками, или выбрано некорректное количество строк для поиска имён
				RDInterface.MessageBox (RDMessageFlags.Warning,
					DiagramData.GetDataLoadError (dd.InitResult, OFDialog.FileName));
				return;
				}

			// Сброс списка кривых
			/*LineNamesList.Items.Clear ();*/
			ClearObjects ();

			// Загрузка
			if (OFDialog.FilterIndex == (int)DataInputTypes.Unknown)
				{
				if (ConfigAccessor.ForceShowDiagram)
					AddFirstColumns ();
				}
			else
				{
				// GDD
				if (OFDialog.FilterIndex == (int)DataInputTypes.GDD)
					{
					for (int i = 0; i < dd.LinesCount; i++)
						{
						/*LineNamesList.Items.Add (dd.GetDataColumnName (dd.GetStyle (i).YColumnNumber) + " @ " +
							dd.GetDataColumnName (dd.GetStyle (i).XColumnNumber));*/
						DiagramStyle ds = dd.GetStyle (i);
						/*string name = dd.GetDataColumnName (ds.YColumnNumber) + " @ " +
							dd.GetDataColumnName (ds.XColumnNumber);
						ToolStripMenuItem item = (ToolStripMenuItem)MList. DropDownItems.Add (name, null,
							LineNames_ SelectedIndexChanged);
						item.Check OnClick = true;*/
						AddObject (dd.GetDataColumnName (ds.YColumnNumber) + " @ " +
							dd.GetDataColumnName (ds.XColumnNumber));
						}
					for (int i = 0; i < dd.AdditionalObjectsCount; i++)
						{
						/*LineNamesList.Items.Add (dd.GetStyle (i + (int)dd.LinesCount).LineName);
						string name = dd.GetStyle (i + (int)dd.LinesCount).LineName;
						ToolStripMenuItem item = (ToolStripMenuItem)MList. DropDownItems.Add (name, null,
							LineNames_ SelectedIndexChanged);
						item.Check OnClick = true;*/
						AddObject (dd.GetStyle (i + (int)dd.LinesCount).LineName);
						}
					/*if (LineNamesList.Items.Count != 0)
						{
						LineNamesList.SelectedItems.Clear ();
						LineNamesList.SelectedIndex = 0;
						}*/
					if (MList.DropDownItems.Count > objectsOffset)
						SelectIndex (0);
					}

				// Остальные
				else
					{
					if (ConfigAccessor.ForceShowDiagram)
						AddFirstColumns ();
					}
				}

			// Включение заблокированных контролов
			ChangeControlsState (true);

			// Перерисовка
			Redraw ();
			}

		// Метод добавляет (по возможности) первые столбцы на диаграмму
		private void AddFirstColumns ()
			{
			// Получение параметров
			ColumnsAdderCmd cad = new ColumnsAdderCmd (dd.DataColumnsCount, true);
			string parametersFile = RDGenerics.GetStoragePath (true) + ConfigAccessor.LineParametersFileName;

			if (!cad.LoadParametersFile (parametersFile))
				{
				if (!cad.CreateParametersFile (parametersFile))
					return;

				cad.LoadParametersFile (parametersFile);
				}

			// Контроль
			if (cad.XColumnNumber.Count == 0)
				return;

			// Добавление
			for (int i = 0; i < cad.XColumnNumber.Count; i++)
				{
				int res = dd.AddDiagram (cad.XColumnNumber[i], cad.YColumnNumber[i]);
				if ((res < 0) && (res != -3))
					{
					continue;
					}
				else if (res == -3)
					{
					RDInterface.MessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
						string.Format (RDLocale.GetText ("LinesOverloadError"), DiagramData.MaxLines));
					break;
					}

				// Обновление списка названий кривых
				/*LineNamesList.Items.Add (dd.GetDataColumnName (cad.YColumnNumber[i]) + " @ " +
					dd.GetDataColumnName (cad.XColumnNumber[i]));*/
				/*string name = dd.GetDataColumnName (cad.YColumnNumber[i]) + " @ " +
					dd.GetDataColumnName (cad.XColumnNumber[i]);
				ToolStripMenuItem item = (ToolStripMenuItem)MList. DropDownItems.Add (name, null,
					LineNames_ SelectedIndexChanged);
				item.Check OnClick = true;*/
				AddObject (dd.GetDataColumnName (cad.YColumnNumber[i]) + " @ " +
					dd.GetDataColumnName (cad.XColumnNumber[i]));

				/*LineNamesList.SelectedItems.Clear ();*/
				ClearSelectedIndices ();
				/*LineNamesList.SelectedIndex = LineNamesList.Items.Count - 1;*/
				SelectIndex (MList.DropDownItems.Count - 1 - objectsOffset);

				// Установка параметров
				ImageWidth.Value = cad.ImageWidth[i];
				ImageHeight.Value = cad.ImageHeight[i];
				LeftOffset.Value = cad.ImageLeft[i];
				TopOffset.Value = cad.ImageTop[i];

				AutoTextOffset.Checked = cad.AutoNameOffset[i];
				if (!AutoTextOffset.Checked)
					{
					LineName.Text = cad.LineName[i];
					LineNameLeftOffset.Value = cad.LineNameLeftOffset[i];
					LineNameTopOffset.Value = cad.LineNameTopOffset[i];
					}
				}
			}

		// Сохранение файла данных
		private void MSaveDataFile_Click (object sender, EventArgs e)
			{
			SFDialog.FileName = "";
			SFDialog.ShowDialog ();
			}

		private void SFDialog_FileOk (object sender, CancelEventArgs e)
			{
			// Сохранение
			if (dd.SaveDataFile (SFDialog.FileName, (DataOutputTypes)SFDialog.FilterIndex,
				ConfigAccessor.ForceSavingColumnNames) < 0)
				{
				RDInterface.MessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					string.Format (RDLocale.GetDefaultText (RDLDefaultTexts.Message_SaveFailure_Fmt),
					SFDialog.FileName));
				}
			}

		// Сохранение изображения
		private void MSaveDiagramImage_Click (object sender, EventArgs e)
			{
			_ = new SavePicture (dd, false);
			}

		// Настройки программы
		private void MProgramSettings_Click (object sender, EventArgs e)
			{
			_ = new ProgramSettings ();
			}

		// Выход из программы
		private void MExit_Click (object sender, EventArgs e)
			{
			this.Close ();
			}

		// Генерация кривой
		private void MGenerate_Click (object sender, EventArgs e)
			{
			// Защита
			if ((dd != null) && (dd.InitResult == DiagramDataInitResults.Ok) &&
				(RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
				"AbortChanges", RDLDefaultTexts.Button_YesNoFocus, RDLDefaultTexts.Button_No) !=
				RDMessageButtons.ButtonOne))
				{
				return;
				}

			// Генерация данных по формуле
			FormulaEvaluator fe = new FormulaEvaluator ();
			if (fe.Cancelled)
				{
				fe.Dispose ();
				return;
				}

			// Создание диаграммы
			dd = new DiagramData (fe.X, fe.Y, fe.ColumnsNames);
			fe.Dispose ();

			if (dd.InitResult != DiagramDataInitResults.Ok)
				throw new Exception (RDLocale.GetText ("ExceptionMessage") + " (2)");

			// Сброс списка кривых
			/*LineNamesList.Items.Clear ();*/
			ClearObjects ();

			// Включение заблокированных контролов
			ChangeControlsState (true);

			// Загрузка
			if (ConfigAccessor.ForceShowDiagram)
				AddFirstColumns ();

			// Перерисовка
			Redraw ();
			}

		// Закрытие диаграммы
		private void MClose_Click (object sender, EventArgs e)
			{
			// Защита
			if ((dd != null) && (dd.InitResult == DiagramDataInitResults.Ok) &&
				(RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
				"AbortChanges", RDLDefaultTexts.Button_YesNoFocus, RDLDefaultTexts.Button_No) !=
				RDMessageButtons.ButtonOne))
				{
				return;
				}

			// Снятие инициализации и блокировка контролов
			if (dd.InitResult != DiagramDataInitResults.Ok)
				return;

			dd = new DiagramData ("", DataInputTypes.Unknown, 0);
			/*LineNamesList.Items.Clear ();*/
			ClearObjects ();
			ChangeControlsState (false);
			Redraw ();
			}

		// Загрузка из буфера обмена
		private void MLoadFromClipboard_Click (object sender, EventArgs e)
			{
			// Защита
			if ((dd != null) && (dd.InitResult == DiagramDataInitResults.Ok) &&
				(RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
				"AbortChanges", RDLDefaultTexts.Button_YesNoFocus, RDLDefaultTexts.Button_No) !=
				RDMessageButtons.ButtonOne))
				{
				return;
				}

			// Обновление параметров загрузки файлов
			if (!CheckFileLoadingParameters (DataInputTypes.Unknown))
				return;

			// Контрольное открытие
			DiagramData ddt = new DiagramData (ConfigAccessor.ExpectedColumnsCount, ConfigAccessor.SkippedLinesCount);
			if (ddt.InitResult != DiagramDataInitResults.Ok)
				{
				// Файл точно с ошибками, или выбрано некорректное количество строк для поиска имён
				RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					"ClipboardLoadError");
				return;
				}

			// Создание диаграммы
			dd = new DiagramData (ConfigAccessor.ExpectedColumnsCount, ConfigAccessor.SkippedLinesCount);

			// Сброс списка кривых
			/*LineNamesList.Items.Clear ();*/
			ClearObjects ();

			// Включение заблокированных контролов
			ChangeControlsState (true);

			// Загрузка
			if (ConfigAccessor.ForceShowDiagram)
				AddFirstColumns ();

			// Перерисовка
			Redraw ();
			}

		// Генерация кривой
		private void MProcessImage_Click (object sender, EventArgs e)
			{
			// Защита
			if ((dd != null) && (dd.InitResult == DiagramDataInitResults.Ok) &&
				(RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
				"AbortChanges", RDLDefaultTexts.Button_YesNoFocus, RDLDefaultTexts.Button_No) !=
				RDMessageButtons.ButtonOne))
				{
				return;
				}

			// Получение изображения
			if (OpenImageDialog.ShowDialog () != DialogResult.OK)
				return;

			Bitmap b;
			try
				{
				b = (Bitmap)Image.FromFile (OpenImageDialog.FileName);
				}
			catch
				{
				RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					"NotAnImageError");
				return;
				}

			// Генерация данных по формуле
			ImageProcessor ip = new ImageProcessor (b);
			if (ip.Cancelled)
				{
				ip.Dispose ();
				return;
				}

			// Создание диаграммы
			dd = new DiagramData (ip.X, ip.Y, ip.ColumnsNames);
			ip.Dispose ();

			if (dd.InitResult != DiagramDataInitResults.Ok)
				throw new Exception (RDLocale.GetText ("ExceptionMessage") + " (3)");

			// Сброс списка кривых
			/*LineNamesList.Items.Clear ();*/
			ClearObjects ();

			// Включение заблокированных контролов
			ChangeControlsState (true);

			// Загрузка
			if (ConfigAccessor.ForceShowDiagram)
				AddFirstColumns ();

			// Перерисовка
			Redraw ();
			}

		#endregion

		#region Меню программы «Справка»

		// О программе
		private void MAbout_Click (object sender, EventArgs e)
			{
			RDInterface.ShowAbout (false);
			}

		// Изменение языка интерфейса
		private void MLanguage_Click (object sender, EventArgs e)
			{
			if (RDInterface.MessageBox ())
				LocalizeForm ();
			}

		#endregion

		#region Меню программы «Операции»

		// Загрузка стиля из файла
		private void MLoadStyle_Click (object sender, EventArgs e)
			{
			// Вызов диалога
			LoadStyleDialog.FileName = "";
			LoadStyleDialog.ShowDialog ();
			}

		private void LoadStyleDialog_FileOk (object sender, CancelEventArgs e)
			{
			// Выбор варианта использования стилей
			switch (RDInterface.LocalizedMessageBox (RDMessageFlags.Question, "StyleLoadingType",
				RDLDefaultTexts.Button_Apply, RDLDefaultTexts.Button_Add, RDLDefaultTexts.Button_Cancel))
				{
				// Применение к выделенным кривым
				case RDMessageButtons.ButtonOne:
					// Контроль
					/*if (LineNamesList.SelectedIndices.Count == 0)*/
					if (selectedIndices.Count < 1)
						{
						RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
							"LinesForLoadingStylesNSError");
						return;
						}

					// Загрузка
					if (dd.LoadStyle (LoadStyleDialog.FileName, /*LineNamesList.SelectedIndices*/GetSelectedIndices () ) < 0)
						{
						RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
							"BrokenStyleError");
						return;
						}
					break;

				// Добавление кривых
				case RDMessageButtons.ButtonTwo:
					// Загрузка
					int oldLinesCount = MList.DropDownItems.Count - objectsOffset;

					if (dd.LoadStyle (LoadStyleDialog.FileName) < 0)
						RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
							"BrokenStyleError");

					// Обновление списка названий кривых (файл стиля заведомо непустой)
					for (int i = oldLinesCount; i < dd.LinesCount; i++)
						{
						/*LineNamesList.Items.Insert (i, dd.GetDataColumnName (dd.GetStyle (i).YColumnNumber) + " @ " +
							dd.GetDataColumnName (dd.GetStyle (i).XColumnNumber));*/
						InsertObject (i, dd.GetDataColumnName (dd.GetStyle (i).YColumnNumber) + " @ " +
							dd.GetDataColumnName (dd.GetStyle (i).XColumnNumber));
						}

					/*LineNamesList.SelectedItems.Clear ();*/
					ClearSelectedIndices ();
					/*LineNamesList.SelectedIndex = (int)dd.LinesCount - 1;*/
					SelectIndex ((int)dd.LinesCount - 1);

					break;

				// Отмена
				case RDMessageButtons.ButtonThree:
					return;
				}

			// Перерисовка
			LineNames_SelectedIndexChanged (/*LineNamesList*/ null, null);
			}

		// Сохранение стиля в файл
		private void MSaveStyle_Click (object sender, EventArgs e)
			{
			// Контроль
			/*if (LineNamesList.SelectedIndices.Count == 0)*/
			if (selectedIndices.Count < 1)
				{
				RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					"LinesForSavingStylesNSError");
				return;
				}

			// Вызов диалога
			SaveStyleDialog.FileName = "";
			SaveStyleDialog.ShowDialog ();
			}

		private void SaveStyleDialog_FileOk (object sender, CancelEventArgs e)
			{
			if (dd.SaveStyle (SaveStyleDialog.FileName, GetSelectedIndices ()) < 0)
				RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					"StyleSaveError");
			}

		// Сброс стиля диаграммы
		private void MResetStyle_Click (object sender, EventArgs e)
			{
			// Контроль
			/*if (LineNamesList.SelectedIndices.Count == 0)*/
			if (selectedIndices.Count < 1)
				{
				RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					"LinesForResetStylesNSError");
				return;
				}

			if (RDInterface.LocalizedMessageBox (RDMessageFlags.Question | RDMessageFlags.CenterText,
				"StylesReset", RDLDefaultTexts.Button_YesNoFocus, RDLDefaultTexts.Button_No) ==
				RDMessageButtons.ButtonOne)
				{
				// Сброс
				for (int i = 0; i < selectedIndices.Count; i++)
					dd.ResetStyle ((uint)selectedIndices[i]);

				// Перерисовка
				LineNames_SelectedIndexChanged (/*LineNamesList*/null, null);
				}
			}

		// Сохранение шаблона добавления кривых
		private void MSaveTemplate_Click (object sender, EventArgs e)
			{
			// Инициализация
			ColumnsAdderCmd cad = new ColumnsAdderCmd (dd.DataColumnsCount, true);
			switch (cad.SaveParametersFile (dd))
				{
				case -1:
					throw new Exception (RDLocale.GetText ("ExceptionMessage") + " (9)");

				case -2:
					RDInterface.MessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
						string.Format (RDLocale.GetDefaultText (RDLDefaultTexts.Message_SaveFailure_Fmt), "?"));
					break;

				default:
					// Файл сохранён
					// Сохранение отменено
					break;
				}

			cad.Dispose ();
			}

		// Восстановление шаблона добавления кривых
		private void MReplaceTemplate_Click (object sender, EventArgs e)
			{
			if (RDInterface.LocalizedMessageBox (RDMessageFlags.Question | RDMessageFlags.CenterText,
				"TemplateReplace", RDLDefaultTexts.Button_YesNoFocus, RDLDefaultTexts.Button_No) !=
				RDMessageButtons.ButtonOne)
				{
				return;
				}

			if (!ColumnsAdderCmd.WriteParametersFile (dd, RDGenerics.GetStoragePath (true) +
				ConfigAccessor.LineParametersFileName))
				{
				RDInterface.MessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					string.Format (RDLocale.GetDefaultText (RDLDefaultTexts.Message_SaveFailure_Fmt),
					ConfigAccessor.LineParametersFileName));
				}
			}

		// Восстановление шаблона добавления кривых
		private void MRestoreTemplate_Click (object sender, EventArgs e)
			{
			if (RDInterface.LocalizedMessageBox (RDMessageFlags.Question | RDMessageFlags.CenterText,
				"TemplateReset", RDLDefaultTexts.Button_YesNoFocus, RDLDefaultTexts.Button_No) !=
				RDMessageButtons.ButtonOne)
				{
				return;
				}

			if (!ColumnsAdderCmd.CreateDefaultParametersFile (RDGenerics.GetStoragePath (true) +
				ConfigAccessor.LineParametersFileName))
				{
				RDInterface.MessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					string.Format (RDLocale.GetDefaultText (RDLDefaultTexts.Message_SaveFailure_Fmt),
					ConfigAccessor.LineParametersFileName));
				}
			}

		// Редактирование данных диаграммы
		private void MRedactData_Click (object sender, EventArgs e)
			{
			// Сохранение стилей
			List<DiagramStyle> linesStyles = [];
			List<DiagramStyle> objectsStyles = [];
			List<DiagramAdditionalObjects> objectsTypes = [];

			for (int i = 0; i < dd.LinesCount; i++)
				linesStyles.Add (new DiagramStyle (dd.GetStyle (i)));
				
			for (int i = 0; i < dd.AdditionalObjectsCount; i++)
				{
				objectsTypes.Add (dd.GetObjectType ((uint)i));
				objectsStyles.Add (new DiagramStyle (dd.GetStyle (i + (int)dd.LinesCount)));
				}

			// Запуск
			DiagramDataEditor dde = new DiagramDataEditor (dd);

			// Обработка
			if (!dde.Cancelled)
				{
				// Сброс списка кривых
				/*LineNamesList.Items.Clear ();*/
				ClearObjects ();

				// Загрузка данных
				dd = new DiagramData (dde.ResultTable);

				// Добавление столбцов и восстановление стилей
				for (int i = 0; i < linesStyles.Count; i++)
					{
					dd.AddDiagram (linesStyles[i].XColumnNumber, linesStyles[i].YColumnNumber);
					dd.LoadStyle (i, linesStyles[i]);

					/*LineNamesList.Items.Add (dd.GetDataColumnName (linesStyles[i].YColumnNumber) + " @ " +
						dd.GetDataColumnName (linesStyles[i].XColumnNumber));*/
					AddObject (dd.GetDataColumnName (linesStyles[i].YColumnNumber) + " @ " +
						dd.GetDataColumnName (linesStyles[i].XColumnNumber));
					}
				for (int i = 0; i < objectsStyles.Count; i++)
					{
					dd.AddObject (objectsTypes[i]);
					dd.LoadStyle (i + (int)dd.LinesCount, objectsStyles[i]);

					/*LineNamesList.Items.Add (objectsStyles[i].LineName);*/
					AddObject (objectsStyles[i].LineName);
					}

				// Обновление состояния
				/*if (LineNamesList.Items.Count != 0)*/
				if (MList.DropDownItems.Count > objectsOffset)
					{
					/*LineNamesList.SelectedItems.Clear ();
					LineNamesList.SelectedIndex = LineNamesList.Items.Count - 1;*/
					SelectIndex (MList.DropDownItems.Count - 1 - objectsOffset);

					// Перерисовка
					Redraw ();
					}
				}

			// Обнуление списков
			linesStyles.Clear ();
			objectsTypes.Clear ();
			objectsStyles.Clear ();
			}

		#endregion

		#region Меню программы «Дополнительно»

		// Склеивание таблиц данных
		private void MMergeTables_Click (object sender, EventArgs e)
			{
			// Вызов диалога
			_ = new TablesMergerForm ();
			}

		// Генерация векторного изображения
		private void MGenerateVI_Click (object sender, EventArgs e)
			{
			// Вызов диалога
			_ = new SVGGeneratorForm ();
			}

		#endregion

		#region Управление диаграммой

		// Метод добавляет кривую на диаграмму
		private void AddColumn_Click (object sender, EventArgs e)
			{
			// Запуск выбора
			ColumnsAdder cad = new ColumnsAdder (dd);

			if (!cad.Cancelled)
				{
				int res;
				if (cad.IsNewObjectADiagram)
					{
					res = dd.AddDiagram (cad.XColumnNumber, cad.YColumnNumber);
					}
				else
					{
					res = dd.AddObject (cad.AdditionalObjectType);
					}

				if ((res < 0) && (res != -3))
					{
					throw new Exception (RDLocale.GetText ("ExceptionMessage") + " (9)");
					}
				else if (res == -3)
					{
					if (cad.IsNewObjectADiagram)
						{
						RDInterface.MessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
							string.Format (RDLocale.GetText ("LinesOverloadError"),
							DiagramData.MaxLines));
						}
					else
						{
						RDInterface.MessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
							string.Format (RDLocale.GetText ("ObjectsOverloadError"),
							DiagramData.MaxAdditionalObjects));
						}
					return;
					}

				// Обновление списка названий кривых
				if (cad.IsNewObjectADiagram)
					{
					/*LineNamesList.Items.Insert ((int)dd.LinesCount - 1, dd.GetDataColumnName (cad.YColumnNumber) +
						" @ " + dd.GetDataColumnName (cad.XColumnNumber));*/
					InsertObject ((int)dd.LinesCount - 1, dd.GetDataColumnName (cad.YColumnNumber) +
						" @ " + dd.GetDataColumnName (cad.XColumnNumber));
					/*LineNamesList.SelectedItems.Clear ();*/
					ClearSelectedIndices ();
					/*LineNamesList.SelectedIndex = (int)dd.LinesCount - 1;*/
					SelectIndex ((int)dd.LinesCount - 1);
					}
				else
					{
					/*LineNamesList.Items.Add (dd.GetStyle ((int)dd.LinesCount +
						(int)dd.AdditionalObjectsCount - 1).LineName);*/
					AddObject (dd.GetStyle ((int)dd.LinesCount + (int)dd.AdditionalObjectsCount - 1).LineName);
					/*LineNamesList.SelectedItems.Clear ();*/
					ClearSelectedIndices ();
					/*LineNamesList.SelectedIndex = LineNamesList.Items.Count - 1;*/
					SelectIndex (MList.DropDownItems.Count - 1 - objectsOffset);
					}

				// Перерисовка
				cad.Dispose ();
				Redraw ();
				}
			}

		// Метод удаляет кривую с диаграммы
		private void DeleteColumn_Click (object sender, EventArgs e)
			{
			// Контроль
			/*if (LineNamesList.SelectedIndices.Count == 0)*/
			if (selectedIndices.Count < 1)
				{
				RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					"LinesForDeleteNSError");
				return;
				}

			if (RDInterface.LocalizedMessageBox (RDMessageFlags.Question | RDMessageFlags.CenterText,
				"LinesDelete", RDLDefaultTexts.Button_YesNoFocus, RDLDefaultTexts.Button_No) ==
				RDMessageButtons.ButtonTwo)
				{
				return;
				}

			// Формирование массива удаляемых индексов
			List<int> lineIndices = [];
			List<int> objectIndices = [];
			for (int i = 0; i < selectedIndices.Count; i++)
				{
				if (selectedIndices[i] < dd.LinesCount)
					lineIndices.Add (selectedIndices[i]);
				else
					objectIndices.Add (selectedIndices[i] - (int)dd.LinesCount);
				}

			// Удаление
			int offset = 0;
			for (int i = 0; i < lineIndices.Count; i++)
				{
				// Удаление диаграммы
				dd.DeleteDiagram ((uint)(lineIndices[i] - offset));

				// Удаление имени
				MList.DropDownItems.RemoveAt (objectsOffset + lineIndices[i] - offset);

				// Увеличение смещения по списку
				offset++;
				}

			offset = 0;
			for (int i = 0; i < objectIndices.Count; i++)
				{
				// Удаление диаграммы
				dd.DeleteObject ((uint)(objectIndices[i] - offset));

				// Удаление имени
				MList.DropDownItems.RemoveAt (objectsOffset + objectIndices[i] - offset + (int)dd.LinesCount);

				// Увеличение смещения по списку
				offset++;
				}

			// Перерисовка
			GetSelectedIndices ();
			Redraw ();

			/*// Блокировка кнопки удаления, если необходимо
			if (LineNamesList.Items.Count != 0)
				{
				LineNamesList.SelectedItems.Clear ();
				LineNamesList.SelectedIndex = 0;
				}*/
			}

		// Метод добавляет кривую на диаграмму
		private void MShowHide_Click (object sender, EventArgs e)
			{
			DrawLine.Checked = !DrawLine.Checked;
			}

		// Метод добавляет кривую на диаграмму с помощью строки параметров
		private void AddColumnCmd_Click (object sender, EventArgs e)
			{
			// Запуск выбора
			ColumnsAdderCmd cac = new ColumnsAdderCmd (dd.DataColumnsCount, false);

			if (!cac.Cancelled)
				{
				// Добавление
				for (int i = 0; i < cac.AutoNameOffset.Count; i++)
					{
					int res = dd.AddDiagram (cac.XColumnNumber[i], cac.YColumnNumber[i]);
					if ((res < 0) && (res != -3))
						{
						throw new Exception (RDLocale.GetText ("ExceptionMessage") + " (10)");
						}
					else if (res == -3)
						{
						RDInterface.MessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
							string.Format (RDLocale.GetText ("LinesOverloadError"),
							DiagramData.MaxLines));
						break;
						}

					// Обновление списка названий кривых
					/*LineNamesList.Items.Insert ((int)dd.LinesCount - 1,
						dd.GetDataColumnName (cac.YColumnNumber[i]) + " @ " +
						dd.GetDataColumnName (cac.XColumnNumber[i]));*/
					InsertObject ((int)dd.LinesCount - 1,
						dd.GetDataColumnName (cac.YColumnNumber[i]) + " @ " +
						dd.GetDataColumnName (cac.XColumnNumber[i]));
					/*LineNamesList.SelectedItems.Clear ();*/
					ClearSelectedIndices ();
					/*LineNamesList.SelectedIndex = (int)dd.LinesCount - 1;*/
					SelectIndex ((int)dd.LinesCount - 1);

					// Установка параметров
					ImageWidth.Value = cac.ImageWidth[i];
					ImageHeight.Value = cac.ImageHeight[i];
					LeftOffset.Value = cac.ImageLeft[i];
					TopOffset.Value = cac.ImageTop[i];

					if (!cac.AutoNameOffset[i])
						{
						AutoTextOffset.Checked = cac.AutoNameOffset[i];
						LineName.Text = cac.LineName[i];
						LineNameLeftOffset.Value = cac.LineNameLeftOffset[i];
						LineNameTopOffset.Value = cac.LineNameTopOffset[i];
						}
					}

				// Перерисовка
				Redraw ();
				}

			cac.Dispose ();
			}

		// Загрузка значений в остальные поля
		private void LineNames_SelectedIndexChanged (object sender, EventArgs e)
			{
			// Принудительное повторное отображение
			if (sender != null)
				MList.ShowDropDown ();

			// Отмена операции, если поле для настройки не выбрано
			/*if (LineNamesList.SelectedIndices.Count == 0)*/
			GetSelectedIndices ();
			if (selectedIndex < 0)
				{
				Redraw ();
				return;
				}

			// Блокировка обработчиков событий _ChangeValue для всех контролов
			loading = true;

			// Защита от незапланированных границ
			try
				{
				// Установка ограничений
				MaxX.DecimalPlaces = MinX.DecimalPlaces = (dd.XRoundPos ((uint)selectedIndex) < 2) ?
					(-dd.XRoundPos ((uint)selectedIndex) + 2) : 0;
				MaxX.Increment = MinX.Increment = (decimal)Math.Pow (10.0, -(double)MaxX.DecimalPlaces);
				MaxY.DecimalPlaces = MinY.DecimalPlaces = (dd.YRoundPos ((uint)selectedIndex) < 2) ?
					(-dd.YRoundPos ((uint)selectedIndex) + 2) : 0;
				MaxY.Increment = MinY.Increment = (decimal)Math.Pow (10.0, -(double)MaxY.DecimalPlaces);

				// Загрузка значений в контролы
				DiagramStyle ds = dd.GetStyle (selectedIndex);

				DrawLine.Checked = ds.AllowDrawing;

				try
					{
					MinX.Value = (decimal)ds.MinX;
					MaxX.Value = (decimal)ds.MaxX;
					MinY.Value = (decimal)ds.MinY;
					MaxY.Value = (decimal)ds.MaxY;
					}
				catch { }

				LeftOffset.Value = ds.DiagramImageLeftOffset;
				TopOffset.Value = ds.DiagramImageTopOffset;

				ImageWidth.Value = ds.DiagramImageWidth;
				ImageHeight.Value = ds.DiagramImageHeight;

				SwitchXY.Checked = ds.SwitchXY;

				AutoTextOffset.Checked = ds.AutoTextOffset;
				OxTextOffset.Value = ds.OxTextOffset;
				OyTextOffset.Value = ds.OyTextOffset;
				LineName.Text = ds.LineName;
				LineNameLeftOffset.Value = ds.LineNameLeftOffset;
				LineNameTopOffset.Value = ds.LineNameTopOffset;

				// Оси
				AutoDivisions.Checked = ds.AutoPrimaryDivisions;
				OxPrimaryDiv.Value = ds.XPrimaryDivisions;
				OyPrimaryDiv.Value = ds.YPrimaryDivisions;
				OxSecondaryDiv.Value = ds.XSecondaryDivisions;
				OySecondaryDiv.Value = ds.YSecondaryDivisions;
				AxesWidth.Value = ds.AxesLinesWidth;
				AxesColor.BackColor = ds.AxesColor;
				OxPlacementCombo.SelectedIndex = (int)ds.OxPlacement;
				OyPlacementCombo.SelectedIndex = (int)ds.OyPlacement;
				OxFormatCombo.SelectedIndex = (int)ds.OxFormat;
				OyFormatCombo.SelectedIndex = (int)ds.OyFormat;

				// Шрифты
				AxesFont.Font = ds.AxesFont;
				AxesFontColor.BackColor = ds.AxesFontColor;
				TextFont.Font = ds.TextFont;
				TextFontColor.BackColor = ds.TextFontColor;

				// Сетка
				GridWidth.Value = ds.GridLinesWidth;
				GridPrimaryColor.BackColor = ds.PrimaryGridColor;
				GridSecondaryColor.BackColor = ds.SecondaryGridColor;

				// Линия
				LineWidth.Value = ds.LineWidth;
				LineColor.BackColor = ds.LineColor;
				LineStyleCombo.SelectedIndex = (int)ds.LineDrawingFormat;
				LineMarker.Value = ((ds.LineMarkerNumber > ml.MarkersCount) ?
					1 : ds.LineMarkerNumber);
				}
			catch
				{
				throw new Exception (RDLocale.GetText ("ExceptionMessage") + " (12)");
				}

			// Обновление ограничений
			ImageWidth_ValueChanged (ImageWidth, null);
			ImageHeight_ValueChanged (ImageHeight, null);

			// Обновление состояния контролов
			ChangeControlsState (true);

			// Возврат состояния
			loading = false;

			/*if (!selecting)
				{
				// Сохранение индексов выбранных кривых
				selectedInidces.Clear ();
				for (int i = 0; i < LineNamesList. SelectedIndices.Count; i++)
					selectedInidces.Add (LineNamesList.SelectedIndices[i]);
				GetSelectedIndices ();*/

			// Перерисовка (необходима для отображения выделения кривых)
			Redraw ();
			/*}*/
			}

		// Совмещение кривых
		private void MergeLines_Click (object sender, EventArgs e)
			{
			// Контроль
			if (selectedIndices.Count != 2)
				{
				RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					"MergingError");
				return;
				}

			// Запрос параметров совмещения
			string name1 = MList.DropDownItems[objectsOffset + selectedIndices[0]].Text;
			string name2 = MList.DropDownItems[objectsOffset + selectedIndices[1]].Text;
			DiagramMerger dm = new DiagramMerger (name1, name2);
			if (dm.Cancelled)
				return;

			// Обработка варианта совмещения
			int fromIdx, toIdx;
			if (dm.MergeVariant == MergeVariants.FirstLine)
				{
				fromIdx = selectedIndices[0];
				toIdx = selectedIndices[1];
				}
			else
				{
				fromIdx = selectedIndices[1];
				toIdx = selectedIndices[0];
				}

			DiagramStyle toStyle = dd.GetStyle (toIdx);
			DiagramStyle fromStyle = dd.GetStyle (fromIdx);

			toStyle.DiagramImageLeftOffset = fromStyle.DiagramImageLeftOffset;
			toStyle.DiagramImageTopOffset = fromStyle.DiagramImageTopOffset;
			toStyle.DiagramImageWidth = fromStyle.DiagramImageWidth;
			toStyle.DiagramImageHeight = fromStyle.DiagramImageHeight;

			// Приведение к общему виду
			toStyle.SwitchXY = fromStyle.SwitchXY;
			toStyle.AutoTextOffset = fromStyle.AutoTextOffset = true;
			toStyle.AutoPrimaryDivisions = fromStyle.AutoPrimaryDivisions = false;
			toStyle.AxesColor = fromStyle.AxesColor;
			toStyle.AxesFont = fromStyle.AxesFont;
			toStyle.AxesLinesWidth = fromStyle.AxesLinesWidth;

			// Обработка осей
			bool ox = (dm.MergeAxe == MergeAxes.Ox);
			bool oy = (dm.MergeAxe == MergeAxes.Oy);
			if (dm.MergeAxe == MergeAxes.Both)
				ox = oy = true;

			if (ox)
				{
				toStyle.OxFormat = fromStyle.OxFormat;
				toStyle.OxPlacement = fromStyle.OxPlacement = AxesPlacements.RightBottom;
				toStyle.XPrimaryDivisions = fromStyle.XPrimaryDivisions;
				toStyle.XSecondaryDivisions = fromStyle.XSecondaryDivisions;

				toStyle.MinX = fromStyle.MinX = Math.Min (toStyle.MinX, fromStyle.MinX);
				toStyle.MaxX = fromStyle.MaxX = Math.Max (toStyle.MaxX, fromStyle.MaxX);
				}
			else
				{
				toStyle.OxPlacement = AxesPlacements.LeftTop;
				fromStyle.OxPlacement = AxesPlacements.RightBottom;
				}

			if (oy)
				{
				toStyle.OyFormat = fromStyle.OyFormat;
				toStyle.OyPlacement = fromStyle.OyPlacement = AxesPlacements.LeftTop;
				toStyle.YPrimaryDivisions = fromStyle.YPrimaryDivisions;
				toStyle.YSecondaryDivisions = fromStyle.YSecondaryDivisions;

				toStyle.MinY = fromStyle.MinY =
					Math.Min (toStyle.MinY, fromStyle.MinY);
				toStyle.MaxY = fromStyle.MaxY =
					Math.Max (toStyle.MaxY, fromStyle.MaxY);
				}
			else
				{
				toStyle.OyPlacement = AxesPlacements.LeftTop;
				fromStyle.OyPlacement = AxesPlacements.RightBottom;
				}

			// Обработка окраски
			if (dm.PaintAxes)
				{
				toStyle.AxesFontColor = toStyle.TextFontColor = toStyle.LineColor;
				fromStyle.AxesFontColor = fromStyle.TextFontColor = fromStyle.LineColor;
				}

			// Перерисовка
			LineNames_SelectedIndexChanged (/*LineNamesList*/null, null);
			}

		// Замена данных кривой
		private void ReplaceColumn_Click (object sender, EventArgs e)
			{
			// Контроль
			/*if ((LineNamesList.SelectedIndices.Count == 0) || (LineNamesList.SelectedIndex >= dd.LinesCount))*/
			if ((selectedIndices.Count == 0) || (selectedIndex >= dd.LinesCount))
				{
				RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					"LineForReplaceNSError");
				return;
				}

			// Запуск выбора
			DiagramStyle ds = dd.GetStyle (selectedIndex);
			ColumnsAdder cad = new ColumnsAdder (dd, (int)ds.XColumnNumber, (int)ds.YColumnNumber);

			if (!cad.Cancelled)
				{
				int res = dd.ReplaceDiagram ((uint)selectedIndex, cad.XColumnNumber, cad.YColumnNumber);
				if (res < 0)
					{
					throw new Exception (RDLocale.GetText ("ExceptionMessage") + " (14)");
					}

				// Обновление списка названий кривых
				/*LineNamesList.Items[LineNamesList.SelectedIndex]*/
				MList.DropDownItems[objectsOffset + selectedIndex].Text = dd.GetDataColumnName (cad.YColumnNumber) +
					" @ " + dd.GetDataColumnName (cad.XColumnNumber);

				// Перерисовка
				Redraw ();
				}
			}

		#endregion

		#region Настройка диаграммы

		// Общий метод обновления значений параметров
		private void UpdateDiagramParameters (Control Sender)
			{
			// Блокировка обработки в случае, если программа находится в состоянии выбора кривой для настройки
			if (loading)
				return;

			// Обновление значений
			try
				{
				for (int i = 0; i < selectedIndices.Count; i++)
					{
					DiagramStyle ds = dd.GetStyle (selectedIndices[i]);

					// Объединяемые параметры
					switch (Sender.Name)
						{
						// Общие параметры
						case "MinX":
							ds.MinX = (double)MinX.Value;
							break;

						case "MaxX":
							ds.MaxX = (double)MaxX.Value;
							break;

						case "MinY":
							ds.MinY = (double)MinY.Value;
							break;

						case "MaxY":
							ds.MaxY = (double)MaxY.Value;
							break;

						case "LeftOffset":
							ds.DiagramImageLeftOffset = (uint)LeftOffset.Value;
							break;

						case "TopOffset":
							ds.DiagramImageTopOffset = (uint)TopOffset.Value;
							break;

						case "ImageWidth":
							ds.DiagramImageWidth = (uint)ImageWidth.Value;
							break;

						case "ImageHeight":
							ds.DiagramImageHeight = (uint)ImageHeight.Value;
							break;

						case "SwitchXY":
							ds.SwitchXY = SwitchXY.Checked;
							break;

						case "DrawLine":
							ds.AllowDrawing = DrawLine.Checked;
							break;

						// Оси
						case "AutoDivisions":
							ds.AutoPrimaryDivisions = AutoDivisions.Checked;
							break;

						case "OxPrimaryDiv":
							ds.XPrimaryDivisions = (uint)OxPrimaryDiv.Value;
							break;

						case "OyPrimaryDiv":
							ds.YPrimaryDivisions = (uint)OyPrimaryDiv.Value;
							break;

						case "OxSecondaryDiv":
							ds.XSecondaryDivisions = (uint)OxSecondaryDiv.Value;
							break;

						case "OySecondaryDiv":
							ds.YSecondaryDivisions = (uint)OySecondaryDiv.Value;
							break;

						case "AxesWidth":
							ds.AxesLinesWidth = (uint)AxesWidth.Value;
							break;

						case "AxesColor":
							ds.AxesColor = AxesColor.BackColor;
							break;

						case "OxPlacementCombo":
							ds.OxPlacement = (AxesPlacements)OxPlacementCombo.SelectedIndex;
							break;

						case "OyPlacementCombo":
							ds.OyPlacement = (AxesPlacements)OyPlacementCombo.SelectedIndex;
							break;

						case "OxFormatCombo":
							ds.OxFormat = (NumbersFormat)OxFormatCombo.SelectedIndex;
							break;

						case "OyFormatCombo":
							ds.OyFormat = (NumbersFormat)OyFormatCombo.SelectedIndex;
							break;

						// Шрифты
						case "AxesFont":
							ds.AxesFont = AxesFont.Font;
							break;

						case "AxesFontColor":
							ds.AxesFontColor = AxesFontColor.BackColor;
							break;

						case "TextFont":
							ds.TextFont = TextFont.Font;
							break;

						case "TextFontColor":
							ds.TextFontColor = TextFontColor.BackColor;
							break;

						// Сетка
						case "GridWidth":
							ds.GridLinesWidth = (uint)GridWidth.Value;
							break;

						case "GridPrimaryColor":
							ds.PrimaryGridColor = GridPrimaryColor.BackColor;
							break;

						case "GridSecondaryColor":
							ds.SecondaryGridColor = GridSecondaryColor.BackColor;
							break;

						// Линия
						case "LineWidth":
							ds.LineWidth = (uint)LineWidth.Value;
							break;

						case "LineColor":
							ds.LineColor = LineColor.BackColor;
							break;

						case "LineStyleCombo":
							ds.LineDrawingFormat = (DrawingLinesFormats)LineStyleCombo.SelectedIndex;
							break;

						case "LineMarker":
							ds.LineMarkerNumber = (uint)LineMarker.Value;
							break;

						// Параметры, перенесённые из необъединяемых
						case "AutoTextOffset":
							ds.AutoTextOffset = AutoTextOffset.Checked;
							break;

						case "OxTextOffset":
							ds.OxTextOffset = (uint)OxTextOffset.Value;
							break;

						case "OyTextOffset":
							ds.OyTextOffset = (uint)OyTextOffset.Value;
							break;

						case "LineNameLeftOffset":
							ds.LineNameLeftOffset = (uint)LineNameLeftOffset.Value;
							break;

						case "LineNameTopOffset":
							ds.LineNameTopOffset = (uint)LineNameTopOffset.Value;
							break;

						// Необъединяемые параметры
						case "LineName":
							dd.GetStyle (selectedIndex).LineName = LineName.Text;
							break;

						// Любое нештатное значение
						default:
							throw new Exception (RDLocale.GetText ("ExceptionMessage") + " (13)");
						}
					}
				}
			// Вряд ли, но на всякий случай
			catch
				{
				RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					"LinesForSettingNSError");
				}

			// Перерисовка
			Redraw ();
			}

		// Изменение основных настроек диаграммы
		private void UniSettings_ValueChanged (object sender, EventArgs e)
			{
			UpdateDiagramParameters ((Control)sender);
			}

		private void ImageWidth_ValueChanged (object sender, EventArgs e)
			{
			// Установка новых ограничений
			OyTextOffset.Maximum = ImageWidth.Value * (decimal)(DiagramStyle.MaxOyTextMargin);
			LineNameLeftOffset.Maximum = ImageWidth.Value * (decimal)(DiagramStyle.MaxLineNameLeftMargin);

			UpdateDiagramParameters ((Control)sender);
			}

		private void ImageHeight_ValueChanged (object sender, EventArgs e)
			{
			// Установка новых ограничений
			OxTextOffset.Maximum = ImageHeight.Value * (decimal)(DiagramStyle.MaxOxTextMargin);
			LineNameTopOffset.Maximum = ImageHeight.Value * (decimal)(DiagramStyle.MaxLineNameTopMargin);

			UpdateDiagramParameters ((Control)sender);
			}

		private void MergeRanges_Click (object sender, EventArgs e)
			{
			// Сохранение списка обрабатываемых индексов
			/*List<int> ind ices = [];
			for (int i = 0; i < LineNamesList. SelectedIndices.Count; i++)
				ind ices.Add (LineNamesList.SelectedIndices[i]);*/
			int[] indices = GetSelectedIndices ();

			if (indices.Length < 2)
				return;

			// Поиск границ
			DiagramStyle ds = dd.GetStyle (indices[0]);
			double[] ranges = [ds.MinX, ds.MaxX, ds.MinY, ds.MaxY];

			for (int i = 1; i < indices.Length; i++)
				{
				ds = dd.GetStyle (indices[i]);
				if (ranges[0] > ds.MinX)
					ranges[0] = ds.MinX;
				if (ranges[1] < ds.MaxX)
					ranges[1] = ds.MaxX;
				if (ranges[2] > ds.MinY)
					ranges[2] = ds.MinY;
				if (ranges[3] < ds.MaxY)
					ranges[3] = ds.MaxY;
				}

			// Назначение
			for (int i = 0; i < indices.Length; i++)
				{
				ds = dd.GetStyle (indices[i]);
				if ((ds.XColumnNumber == 0) && (ds.YColumnNumber == 0))
					continue;

				// Выбор кривой
				/*LineNamesList.SelectedIndices.Clear ();*/
				ClearSelectedIndices ();
				/*LineNamesList.SelectedIndices.Add (indices[i]);*/
				SelectIndex (indices[i]);

				MinX.Value = (decimal)ranges[0];
				MaxX.Value = (decimal)ranges[1];
				MinY.Value = (decimal)ranges[2];
				MaxY.Value = (decimal)ranges[3];
				}

			/*// Завершено
			indi ces.Clear ();*/
			}

		private void AlignAsRow_Click (object sender, EventArgs e)
			{
			AlignLines (0);
			}

		private void AlignAsColumn_Click (object sender, EventArgs e)
			{
			AlignLines (1);
			}

		private void AlignLeftTop_Click (object sender, EventArgs e)
			{
			AlignLines (2);
			}

		private void AlignField_Click (object sender, EventArgs e)
			{
			AlignLines (3);
			}

		/*private void AlignLines (bool InRow)*/
		private void AlignLines (byte Mode)
			{
			// Сохранение списка обрабатываемых индексов
			/*List<int> ind ices = [];
			for (int i = 0; i < LineNamesList. SelectedIndices.Count; i++)
				indi ces.Add (LineNamesList.SelectedIndices[i]);*/
			int[] indices = GetSelectedIndices ();

			// Обработка
			if (Mode > 1)
				goto align23;

			for (int i = 1; i < indices.Length; i++)
				{
				// Выбор кривой
				/*LineNamesList.SelectedIndices.Clear ();*/
				ClearSelectedIndices ();
				/*LineNamesList.SelectedIndices.Add (indices[i]);*/
				SelectIndex (indices[i]);

				// Установка параметров
				/*if (InRow)*/
				switch (Mode)
					{
					case 0:
						LeftOffset.Value = dd.GetStyle (indices[i - 1]).DiagramImageLeftOffset +
							dd.GetStyle (indices[i - 1]).DiagramImageWidth;
						TopOffset.Value = dd.GetStyle (indices[i - 1]).DiagramImageTopOffset;
						break;

					case 1:
						LeftOffset.Value = dd.GetStyle (indices[i - 1]).DiagramImageLeftOffset;
						TopOffset.Value = dd.GetStyle (indices[i - 1]).DiagramImageTopOffset +
							dd.GetStyle (indices[i - 1]).DiagramImageHeight;
						break;
					}
				}

			/*goto alignEnd;*/

		align23:
			for (int i = 0; i < indices.Length; i++)
				{
				// Выбор кривой
				/*LineNamesList.SelectedIndices.Clear ();
				LineNamesList.SelectedIndices.Add (indices[i]);*/
				ClearSelectedIndices ();
				SelectIndex (indices[i]);

				// Установка параметров
				switch (Mode)
					{
					case 2:
						LeftOffset.Value = 0;
						TopOffset.Value = 0;
						break;

					case 3:
						LeftOffset.Value = 0;
						TopOffset.Value = 0;
						ImageWidth.Value = DiagramBox.Width;
						ImageHeight.Value = DiagramBox.Height;
						break;
					}
				}

			/*// Завершено
			alignEnd:
			indi ces.Clear ();*/
			}

		private void AutoTextOffset_CheckedChanged (object sender, EventArgs e)
			{
			UpdateDiagramParameters ((Control)sender);

			Label07.Enabled = Label08.Enabled = Label09.Enabled = Label10.Enabled =
			OxTextOffset.Enabled = OyTextOffset.Enabled = LineName.Enabled =
			LineNameLeftOffset.Enabled = LineNameTopOffset.Enabled = !AutoTextOffset.Checked;
			}

		// Моментальный разворот диапазонов
		private void SwapX_Click (object sender, EventArgs e)
			{
			decimal x = MaxX.Value;
			MaxX.Value = MinX.Value;
			MinX.Value = x;
			}

		private void SwapY_Click (object sender, EventArgs e)
			{
			decimal y = MaxY.Value;
			MaxY.Value = MinY.Value;
			MinY.Value = y;
			}

		// Настройки осей
		private void AutoDivisions_CheckedChanged (object sender, EventArgs e)
			{
			UpdateDiagramParameters ((Control)sender);
			OxPrimaryDiv.Enabled = OyPrimaryDiv.Enabled = !AutoDivisions.Checked;
			UniSettings_ValueChanged (OxPrimaryDiv, null);
			UniSettings_ValueChanged (OyPrimaryDiv, null);
			}

		// Изменение цветов
		private void AxesColorTurnOff_Click (object sender, EventArgs e)
			{
			AxesColor.BackColor = DiagramStyle.ImageBackColor;
			}

		private void AxesColor_Click (object sender, EventArgs e)
			{
			ColorSelectDialog.Color = AxesColor.BackColor;
			ColorSelectDialog.ShowDialog ();
			AxesColor.BackColor = ColorSelectDialog.Color;
			}

		private void TextColorFromCurve_Click (object sender, EventArgs e)
			{
			ColorFromCurve (0);
			}

		private void AxeColorFromCurve_Click (object sender, EventArgs e)
			{
			ColorFromCurve (1);
			}

		// Управление списком выбранных индексов объектов
		private int[] GetSelectedIndices ()
			{
			selectedIndices.Clear ();
			/*for (int i = 0; i < LineNamesList.SelectedIndices.Count; i++)
				selectedIndices.Add (LineNamesList.SelectedIndices[i]);*/
			for (int i = objectsOffset; i < MList.DropDownItems.Count; i++)
				if (((ToolStripMenuItem)MList.DropDownItems[i]).Checked)
					selectedIndices.Add (i - objectsOffset);

			if (selectedIndices.Count > 0)
				selectedIndex = selectedIndices[0];
			else
				selectedIndex = -1;

			return selectedIndices.ToArray ();
			}

		private void ClearSelectedIndices ()
			{
			selectedIndices.Clear ();
			selectedIndex = -1;
			for (int i = objectsOffset; i < MList.DropDownItems.Count; i++)
				((ToolStripMenuItem)MList.DropDownItems[i]).Checked = false;
			}

		// Index здесь считается с нуля, аналогично списку объектов
		private void SelectIndex (int Index)
			{
			if ((Index < 0) || (Index >= MList.DropDownItems.Count - objectsOffset))
				return;

			ToolStripMenuItem item = (ToolStripMenuItem)MList.DropDownItems[Index + objectsOffset];
			if (item.Checked)
				return;

			selectedIndices.Add (Index);
			selectedIndices.Sort ();
			selectedIndex = Index;
			item.Checked = true;

			LineNames_SelectedIndexChanged (null, null);
			}

		// Управление списком объектов
		private void ClearObjects ()
			{
			int count = MList.DropDownItems.Count;
			for (int i = objectsOffset; i < count; i++)
				MList.DropDownItems.RemoveAt (objectsOffset);
			}

		private void AddObject (string Name)
			{
			ToolStripMenuItem item = (ToolStripMenuItem)MList.DropDownItems.Add (Name, null,
				LineNames_SelectedIndexChanged);
			item.CheckOnClick = true;
			}

		private void InsertObject (int Offset, string Name)
			{
			ToolStripMenuItem item = new ToolStripMenuItem (Name, null, LineNames_SelectedIndexChanged);
			MList.DropDownItems.Insert (objectsOffset + Offset, item);
			item.CheckOnClick = true;
			}

		private void MSelectAll_Click (object sender, EventArgs e)
			{
			for (int i = 0; i < MList.DropDownItems.Count - objectsOffset; i++)
				SelectIndex (i);
			MList.ShowDropDown ();
			}

		private void MDeselectAll_Click (object sender, EventArgs e)
			{
			ClearSelectedIndices ();
			Redraw ();
			MList.ShowDropDown ();
			}

		// Получение цвета шрифта от линии кривой
		private void ColorFromCurve (byte Mode)
			{
			/*// Сохранение списка обрабатываемых индексов
			List<int> ind ices = [];
			for (int i = 0; i < LineNamesList. SelectedIndices.Count; i++)
				indi ces.Add (LineNamesList.SelectedIndices[i]);*/
			int[] indices = GetSelectedIndices ();

			// Обработка
			for (int i = 0; i < indices.Length; i++)
				{
				// Выбор кривой
				/*LineNamesList.SelectedIndices.Clear ();
				LineNamesList.SelectedIndices.Add (indices[i]);*/
				ClearSelectedIndices ();
				SelectIndex (indices[i]);

				// Установка параметров
				switch (Mode)
					{
					case 0:
						TextFontColor.BackColor = LineColor.BackColor;
						break;

					case 1:
						AxesFontColor.BackColor = LineColor.BackColor;
						break;
					}
				}
			}

		// Изменение шрифтов подписей
		private void RunFontSelectCycle ()
			{
			// Вызов окна шрифтов с контролем успешности выбора
			bool success = false;
			while (!success)
				{
				success = true;

				try
					{
					if (StyleFontDialog.ShowDialog () == DialogResult.Cancel)
						{
						return;
						}
					}
				catch
					{
					RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
						"FontSelectError");
					success = false;
					}
				}
			}

		private void TextFont_Click (object sender, EventArgs e)
			{
			StyleFontDialog.Font = TextFont.Font;
			RunFontSelectCycle ();
			TextFont.Font = StyleFontDialog.Font;
			}

		private void AxesFont_Click (object sender, EventArgs e)
			{
			StyleFontDialog.Font = AxesFont.Font;
			RunFontSelectCycle ();
			AxesFont.Font = StyleFontDialog.Font;
			}

		// Изменение цветов подписей
		private void TextFontColor_Click (object sender, EventArgs e)
			{
			ColorSelectDialog.Color = TextFontColor.BackColor;
			ColorSelectDialog.ShowDialog ();
			TextFontColor.BackColor = ColorSelectDialog.Color;
			}

		private void AxesFontColor_Click (object sender, EventArgs e)
			{
			ColorSelectDialog.Color = AxesFontColor.BackColor;
			ColorSelectDialog.ShowDialog ();
			AxesFontColor.BackColor = ColorSelectDialog.Color;
			}

		// Изменение цветов линий сетки
		private void GridPrimaryColor_Click (object sender, EventArgs e)
			{
			ColorSelectDialog.Color = GridPrimaryColor.BackColor;
			ColorSelectDialog.ShowDialog ();
			GridPrimaryColor.BackColor = ColorSelectDialog.Color;
			}

		private void GridSecondaryColor_Click (object sender, EventArgs e)
			{
			ColorSelectDialog.Color = GridSecondaryColor.BackColor;
			ColorSelectDialog.ShowDialog ();
			GridSecondaryColor.BackColor = ColorSelectDialog.Color;
			}

		private void GridColorTurnOff_Click (object sender, EventArgs e)
			{
			GridPrimaryColor.BackColor = DiagramStyle.ImageBackColor;
			GridSecondaryColor.BackColor = DiagramStyle.ImageBackColor;
			}

		// Изменение цвета линии
		private void LineColor_Click (object sender, EventArgs e)
			{
			ColorSelectDialog.Color = LineColor.BackColor;
			ColorSelectDialog.ShowDialog ();
			LineColor.BackColor = ColorSelectDialog.Color;
			}

		private void LineColor_BackColorChanged (object sender, EventArgs e)
			{
			if (LineMarkerImage.BackgroundImage != null)
				LineMarkerImage.BackgroundImage.Dispose ();

			LineMarkerImage.BackgroundImage = ml.GetMarker ((uint)LineMarker.Value - 1, LineColor.BackColor);
			UpdateDiagramParameters ((Control)sender);
			}

		// Изменение формата линии
		private void LineStyleCombo_SelectedIndexChanged (object sender, EventArgs e)
			{
			LineMarker.Enabled = (LineStyleCombo.SelectedIndex > 0);
			UpdateDiagramParameters ((Control)sender);
			}

		// Изменение маркера
		private void LineMarker_ValueChanged (object sender, EventArgs e)
			{
			if (LineMarkerImage.BackgroundImage != null)
				LineMarkerImage.BackgroundImage.Dispose ();

			LineMarkerImage.BackgroundImage = ml.GetMarker ((uint)LineMarker.Value - 1, LineColor.BackColor);
			UpdateDiagramParameters ((Control)sender);
			}

		#endregion

		#region Обработка эффектов панели настроек и списка кривых

		// Панели настроек
		private void MainTabControl_MouseHover (object sender, EventArgs e)
			{
			MainTabControl_MouseClick (null, null);
			}

		private void MainTabControl_MouseClick (object sender, MouseEventArgs e)
			{
			MainTabControl.Height = (int)ConfigAccessor.MinHeight - 375;
			}

		private void MainTabControl_Leave (object sender, EventArgs e)
			{
			MainTabControl.Height = DiagramBox.Top - MainTabControl.Top;
			}

		/*// Список кривых
		private void LineNamesList_MouseHover (object sender, EventArgs e)
			{
			LineNamesList_MouseClick (null, null);
			}*/

		/*private void LineNamesList_MouseClick (object sender, MouseEventArgs e)
			{
			if (LineNamesList.Height != LineNamesList.ItemHeight * 10 + 4)
				{
				LineNamesList.Height = LineNamesList.ItemHeight * 10 + 4;

				// Следующий код позволяет избегать сброса списка выделенных кривых в поле LineNamesList при
				// щелчке мышью
				selecting = true;

				if (LineNamesList.Items.Count >= selectedIndices.Count)		// По неизвестной причине последний элемент в selectedIndices
																			// не удаляется при опустошении LineNamesList.Items
					{
					LineNamesList.SelectedIndices.Clear ();
					for (int i = 0; i < selectedIndices.Count; i++)
						LineNamesList.SelectedIndices.Add (selectedIndices[i]);
					}

				selecting = false;
				}
			}*/

		/*private void LineNamesList_Leave (object sender, EventArgs e)
			{
			LineNamesList.Height = LineNamesList.ItemHeight * 1 + 4;
			}*/

		#endregion
		}
	}
