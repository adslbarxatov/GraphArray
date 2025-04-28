using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает форму ввода параметрической строки для построения новой кривой
	/// </summary>
	public partial class ColumnsAdderCmd: Form
		{
		// Переменные
		private uint dataColumnsCount = 0;  // Число столбцов в исходном массиве данных
		private string correctLine;

		/// <summary>
		/// Возвращает номер столбца данных, интерпретируемого как столбец абсцисс
		/// </summary>
		public List<uint> XColumnNumber
			{
			get
				{
				return xColumnNumber;
				}
			}
		private List<uint> xColumnNumber = [];

		/// <summary>
		/// Возвращает номер столбца данных, интерпретируемого как столбец ординат
		/// </summary>
		public List<uint> YColumnNumber
			{
			get
				{
				return yColumnNumber;
				}
			}
		private List<uint> yColumnNumber = [];

		/// <summary>
		/// Возвращает ширину изображения новой диаграммы
		/// </summary>
		public List<uint> ImageWidth
			{
			get
				{
				return imageWidth;
				}
			}
		private List<uint> imageWidth = [];

		/// <summary>
		/// Возвращает высоту изображения новой диаграммы
		/// </summary>
		public List<uint> ImageHeight
			{
			get
				{
				return imageHeight;
				}
			}
		private List<uint> imageHeight = [];

		/// <summary>
		/// Возвращает смещение изображения новой диаграммы относительно левой границы экрана
		/// </summary>
		public List<uint> ImageLeft
			{
			get
				{
				return imageLeft;
				}
			}
		private List<uint> imageLeft = [];

		/// <summary>
		/// Возвращает смещение изображения новой диаграммы относительно верхней границы экрана
		/// </summary>
		public List<uint> ImageTop
			{
			get
				{
				return imageTop;
				}
			}
		private List<uint> imageTop = [];

		/// <summary>
		/// Возвращает подпись диаграммы
		/// </summary>
		public List<string> LineName
			{
			get
				{
				return lineName;
				}
			}
		private List<string> lineName = [];

		/// <summary>
		/// Возвращает смещение подписи диаграммы относительно левой границы экрана
		/// </summary>
		public List<uint> LineNameLeftOffset
			{
			get
				{
				return lineNameLeftOffset;
				}
			}
		private List<uint> lineNameLeftOffset = [];

		/// <summary>
		/// Возвращает смещение подписи диаграммы относительно верхней границы экрана
		/// </summary>
		public List<uint> LineNameTopOffset
			{
			get
				{
				return lineNameTopOffset;
				}
			}
		private List<uint> lineNameTopOffset = [];

		/// <summary>
		/// Возвращает флаг, указывающий, следует ли определять автоматически положение подписи диаграммы
		/// </summary>
		public List<bool> AutoNameOffset
			{
			get
				{
				return autoNameOffset;
				}
			}
		private List<bool> autoNameOffset = [];

		/// <summary>
		/// Возвращает true, если выбор данных был отменён
		/// </summary>
		public bool Cancelled
			{
			get
				{
				return cancelled;
				}
			}
		private bool cancelled = true;

		/// <summary>
		/// Конструктор. Инициализирует интерфейс ввода параметров
		/// </summary>
		/// <param name="DataColumnsCount">Число доступных столбцов исходного массива данных</param>
		/// <param name="Silent">Инициализация в тихом режиме</param>
		public ColumnsAdderCmd (uint DataColumnsCount, bool Silent)
			{
			// Инициализация и локализация формы
			InitializeComponent ();
			RDLocale.SetControlsText (this);
			ApplyButton.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK);
			AbortButton.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel);
			this.Text = RDLocale.GetText (this.Name + "_T");

			// Настройка контролов
			OFDialog.Filter = SFDialog.Filter = RDLocale.GetText (this.Name + "_OFDialog_F");
			OFDialog.Title = RDLocale.GetText (this.Name + "_OFDialog");
			SFDialog.Title = RDLocale.GetText (this.Name + "_SFDialog");

			correctLine = RDLocale.GetText (this.Name + "_Correct");

			// Загрузка параметров
			dataColumnsCount = DataColumnsCount;

			// Запуск
			if (!Silent)
				this.ShowDialog ();
			}

		// Отмена
		private void AbortButton_Click (object sender, EventArgs e)
			{
			this.Close ();
			}

		// ОК
		private void ApplyButton_Click (object sender, EventArgs e)
			{
			cancelled = false;

			this.Close ();
			}

		// Обработка строки параметров
		private void CommandLine_TextChanged (object sender, EventArgs e)
			{
			// Блокировка кнопки ОК
			ApplyButton.Enabled = false;
			ProcessingMsg.ForeColor = Color.FromArgb (192, 0, 0);

			// Сброс состояния
			autoNameOffset.Clear ();
			xColumnNumber.Clear ();
			yColumnNumber.Clear ();
			imageWidth.Clear ();
			imageHeight.Clear ();
			imageLeft.Clear ();
			imageTop.Clear ();
			lineName.Clear ();
			lineNameLeftOffset.Clear ();
			lineNameTopOffset.Clear ();

			// Обработка
			ProcessingMsg.Text = ProcessCommandLine (CommandLine.Text);
			if (ProcessingMsg.Text == correctLine)
				{
				// Успешно
				ApplyButton.Enabled = true;
				ProcessingMsg.ForeColor = Color.FromArgb (0, 192, 0);
				}
			}

		// Метод обрабатывает строку и в случае успеха добавляет её в список готовых строк
		private string ProcessCommandLine (string Line)
			{
			// Разделение параметров
			string[] values = Line.Split (parFileSplitter, StringSplitOptions.RemoveEmptyEntries);

			// Предварительные значения
			bool autoNameOffsetT = true;
			uint xColumnNumberT;
			uint yColumnNumberT;
			uint imageWidthT;
			uint imageHeightT;
			uint imageLeftT;
			uint imageTopT;
			string lineNameT = "";
			uint lineNameLeftOffsetT = 0;
			uint lineNameTopOffsetT = 0;

			if ((values.Length == 6) || (values.Length == 10))
				{
				// Короткая строка
				try
					{
					xColumnNumberT = uint.Parse (values[0]);
					yColumnNumberT = uint.Parse (values[1]);
					xColumnNumberT--;
					yColumnNumberT--;
					}
				catch
					{
					return RDLocale.GetText (this.Name + "_ICNError");
					}

				try
					{
					imageWidthT = uint.Parse (values[2]);
					imageHeightT = uint.Parse (values[3]);
					}
				catch
					{
					return RDLocale.GetText (this.Name + "_IDSError");
					}

				try
					{
					imageLeftT = uint.Parse (values[4]);
					imageTopT = uint.Parse (values[5]);
					}
				catch
					{
					return RDLocale.GetText (this.Name + "_IDOError");
					}

				// Длинная строка
				if (values.Length == 10)
					{
					autoNameOffsetT = (values[9] == "1");

					lineNameT = values[6];

					try
						{
						lineNameLeftOffsetT = uint.Parse (values[7]);
						lineNameTopOffsetT = uint.Parse (values[8]);
						}
					catch
						{
						return RDLocale.GetText (this.Name + "_ITOError");
						}
					}
				}
			else
				{
				return RDLocale.GetText (this.Name + "_IPCError");
				}

			// Контроль диапазонов
			if ((xColumnNumberT >= dataColumnsCount) || (yColumnNumberT >= dataColumnsCount))
				{
				return string.Format (RDLocale.GetText (this.Name + "_OCNError"), dataColumnsCount);
				}

			if ((imageWidthT > DiagramStyle.MaxImageWidth) || (imageWidthT < DiagramStyle.MinImageWidth) ||
				(imageHeightT > DiagramStyle.MaxImageHeight) || (imageHeightT < DiagramStyle.MinImageHeight))
				{
				return string.Format (RDLocale.GetText (this.Name + "_ODSError"),
					DiagramStyle.MinImageWidth, DiagramStyle.MaxImageWidth, DiagramStyle.MinImageHeight,
					DiagramStyle.MaxImageHeight);
				}

			if ((imageLeftT > DiagramStyle.MaxImageWidth) || (imageTopT > DiagramStyle.MaxImageHeight))
				{
				return string.Format (RDLocale.GetText (this.Name + "_ODOError"),
					DiagramStyle.MaxImageWidth, DiagramStyle.MaxImageHeight);
				}

			if ((lineNameLeftOffsetT > imageWidthT) || (lineNameTopOffsetT > imageHeightT))
				{
				return string.Format (RDLocale.GetText (this.Name + "_OTOError"),
					imageWidthT, imageHeightT);
				}

			// Успешно
			autoNameOffset.Add (autoNameOffsetT);
			xColumnNumber.Add (xColumnNumberT);
			yColumnNumber.Add (yColumnNumberT);
			imageWidth.Add (imageWidthT);
			imageHeight.Add (imageHeightT);
			imageLeft.Add (imageLeftT);
			imageTop.Add (imageTopT);
			lineName.Add (lineNameT);
			lineNameLeftOffset.Add (lineNameLeftOffsetT);
			lineNameTopOffset.Add (lineNameTopOffsetT);

			return correctLine;
			}

		// Выбор варианта загрузки
		private void MultiCmd_CheckedChanged (object sender, EventArgs e)
			{
			if (MultiCmd.Checked)
				{
				CommandLine.Enabled = false;
				SelectFile.Enabled = true;

				ApplyButton.Enabled = false;
				}
			}

		private void SingleCmd_CheckedChanged (object sender, EventArgs e)
			{
			if (SingleCmd.Checked)
				{
				CommandLine.Enabled = true;
				SelectFile.Enabled = false;

				ApplyButton.Enabled = false;

				// Принудительная повторная обработка строки
				CommandLine_TextChanged (null, null);
				}
			}

		// Выбор файла
		private void SelectFile_Click (object sender, EventArgs e)
			{
			OFDialog.ShowDialog ();
			}

		private void OFDialog_FileOk (object sender, CancelEventArgs e)
			{
			// Блокировка кнопки ОК
			ApplyButton.Enabled = false;
			ProcessingResults.ForeColor = Color.FromArgb (192, 0, 0);

			// Загрузка параметров
			if (!LoadParametersFile (OFDialog.FileName))
				return;

			// Конечная обработка
			if (autoNameOffset.Count != 0)
				ApplyButton.Enabled = true;

			if (ProcessingResults.Text == "")
				{
				ProcessingResults.Text = correctLine;
				ProcessingResults.ForeColor = Color.FromArgb (0, 192, 0);
				}
			}

		/// <summary>
		/// Метод извлекает параметры добавляемых кривых из указанного файла
		/// </summary>
		/// <param name="FileName">Имя загружаемого файла</param>
		/// <returns>Возвращает true, если загрузка выполнена успешно</returns>
		public bool LoadParametersFile (string FileName)
			{
			// Сброс состояния
			autoNameOffset.Clear ();
			xColumnNumber.Clear ();
			yColumnNumber.Clear ();
			imageWidth.Clear ();
			imageHeight.Clear ();
			imageLeft.Clear ();
			imageTop.Clear ();
			lineName.Clear ();
			lineNameLeftOffset.Clear ();
			lineNameTopOffset.Clear ();
			ProcessingResults.Text = "";

			// Попытка открытия файла
			TextReader TR;
			string s;
			FileStream FS = null;

			if (RDGenerics.StartedFromMSStore)
				{
				s = RDGenerics.GetEncoding (RDEncodings.UTF8).
					GetString (GraphArrayResources.LineParameters);
				TR = new StringReader (s);
				}
			else
				{
				try
					{
					FS = new FileStream (FileName, FileMode.Open);
					}
				catch
					{
					ProcessingResults.Text =
						string.Format (RDLocale.GetDefaultText (RDLDefaultTexts.Message_LoadFailure_Fmt), FileName);
					return false;
					}

				// Файл открыт
				TR = new StreamReader (FS, RDGenerics.GetEncoding (RDEncodings.UTF8));
				}

			// Чтение и обработка
			uint line = 0;
			while (!string.IsNullOrWhiteSpace (s = TR.ReadLine ()))
				{
				string res = ProcessCommandLine (s);
				line++;

				if (res != correctLine)
					{
					ProcessingResults.Text += ("#" + line.ToString () + ": " + res + RDLocale.RN);
					}

				if (line >= DiagramData.MaxLines)
					{
					ProcessingResults.Text += (string.Format (RDLocale.GetText ("LinesOverloadError"),
						DiagramData.MaxLines) + RDLocale.RN);
					break;
					}
				}

			// Завершено
			TR.Close ();
			if (FS != null)
				FS.Close ();
			return true;
			}

		/// <summary>
		/// Метод создаёт образец файла параметров добавляемых кривых
		/// </summary>
		/// <param name="FileName">Имя создаваемого файла</param>
		/// <returns>Возвращает true в случае успеха</returns>
		public bool CreateParametersFile (string FileName)
			{
			if (!CreateDefaultParametersFile (FileName))
				{
				ProcessingResults.Text =
					string.Format (RDLocale.GetDefaultText (RDLDefaultTexts.Message_LoadFailure_Fmt),
					FileName);
				return false;
				}

			return true;
			}

		/// <summary>
		/// Метод создаёт образец файла параметров добавляемых кривых
		/// </summary>
		/// <param name="FileName">Имя создаваемого файла</param>
		/// <returns>Возвращает true в случае успеха</returns>
		public static bool CreateDefaultParametersFile (string FileName)
			{
			// Попытка открытия файла
			FileStream FS;
			try
				{
				FS = new FileStream (FileName, FileMode.Create);
				}
			catch
				{
				return false;
				}

			// Запись и завершение
			FS.Write (GraphArrayResources.LineParameters, 0, GraphArrayResources.LineParameters.Length);
			FS.Close ();
			return true;
			}

		/// <summary>
		/// Метод сохраняет параметры стиля указанного объекта DiagramData в файл, запрашивая его имя
		/// </summary>
		/// <param name="Data">Объект DiagramData, стиль которого следует использовать для создания 
		/// файла параметров</param>
		/// <returns>Возвращает 0 в случае успеха, -1, если входные параметры некорректны, -2, 
		/// если не удаётся создать файл,
		/// 1, если файл не был выбран</returns>
		public int SaveParametersFile (DiagramData Data)
			{
			// Контроль параметров
			if ((Data == null) || (Data.InitResult != DiagramDataInitResults.Ok))
				return -1;

			// Запрос имени файла
			if (SFDialog.ShowDialog () != DialogResult.OK)
				return 1;

			// Запись
			if (!WriteParametersFile (Data, SFDialog.FileName))
				return -2;

			// Успешно
			return 0;
			}

		/// <summary>
		/// Метод сохраняет параметры стилей в файл шаблона
		/// </summary>
		/// <param name="Data">Данные диаграммы для извлечения шаблона</param>
		/// <param name="FileName">Файл шаблона</param>
		/// <returns>Возвращает true в случае успеха</returns>
		public static bool WriteParametersFile (DiagramData Data, string FileName)
			{
			// Попытка создания файла файла
			FileStream FS;
			try
				{
				FS = new FileStream (FileName, FileMode.Create);
				}
			catch
				{
				return false;
				}
			StreamWriter SW = new StreamWriter (FS, RDGenerics.GetEncoding (RDEncodings.UTF8));

			// Запись
			string sp = parFileSplitter[0].ToString ();
			for (int i = 0; i < Data.LinesCount; i++)
				{
				SW.Write ((Data.GetStyle (i).XColumnNumber + 1).ToString () + sp);
				SW.Write ((Data.GetStyle (i).YColumnNumber + 1).ToString () + sp);
				SW.Write (Data.GetStyle (i).DiagramImageWidth.ToString () + sp);
				SW.Write (Data.GetStyle (i).DiagramImageHeight.ToString () + sp);
				SW.Write (Data.GetStyle (i).DiagramImageLeftOffset.ToString () + sp);
				SW.Write (Data.GetStyle (i).DiagramImageTopOffset.ToString () + sp);
				SW.Write (Data.GetStyle (i).LineName + sp);
				SW.Write (Data.GetStyle (i).LineNameLeftOffset.ToString () + sp);
				SW.Write (Data.GetStyle (i).LineNameTopOffset.ToString () + sp);
				SW.WriteLine (Data.GetStyle (i).AutoTextOffset ? "1" : "0");
				}

			// Завершение
			SW.Close ();
			FS.Close ();
			return true;
			}
		private static char[] parFileSplitter = [';'];
		}
	}
