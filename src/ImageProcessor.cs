using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает форму извлечения данных из изображения
	/// </summary>
	public partial class ImageProcessor: Form
		{
		// Переменные
		private Bitmap img;
		private Bitmap scaledImg;
		private double scaleCoeff;

		private Point leftPoint, rightPoint;
		private bool leftInited = false;
		private bool rightInited = false;

		private Pen pLine, pCross;

		/// <summary>
		/// Возвращает true, если ввод был отменён
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
		/// Возвращает массив абсцисс сгенерированной кривой
		/// </summary>
		public List<double> X
			{
			get
				{
				return x;
				}
			}
		private List<double> x = [];

		/// <summary>
		/// Возвращает массив ординат сгенерированной кривой
		/// </summary>
		public List<List<double>> Y
			{
			get
				{
				return y;
				}
			}
		private List<List<double>> y = [];

		/// <summary>
		/// Возвращает список названий столбцов данных
		/// </summary>
		public List<string> ColumnsNames
			{
			get
				{
				return columnsNames;
				}
			}
		private List<string> columnsNames = [];

		/// <summary>
		/// Конструктор. Запускает извлечение данных из изображения
		/// </summary>
		/// <param name="LoadedImage">Загруженное изображение для обработки</param>
		public ImageProcessor (Bitmap LoadedImage)
			{
			// Инициализация и локализация формы
			InitializeComponent ();
			RDGenerics.LoadWindowDimensions (this);
			img = LoadedImage;

			RDLocale.SetControlsText (this);
			BExtract.Text = RDLocale.GetText ("ExtractButton");
			BAbort.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel);
			BHelp.Text = RDLocale.GetText ("GraphArrayFormMenuStrip_MUpperHelp");
			this.Text = RDLocale.GetText (this.Name + "_T");

			// Подготовка изображения
			if (img.Width > img.Height)
				scaleCoeff = (double)img.Width / MainImage.Width;
			else
				scaleCoeff = (double)img.Height / MainImage.Height;

			// Пересчёт размеров
			scaledImg = new Bitmap (img, (int)(img.Width / scaleCoeff), (int)(img.Height / scaleCoeff));

			MainImage.Image = (Image)scaledImg.Clone ();
			MainImage.Width = MainImage.Image.Width;
			MainImage.Height = MainImage.Image.Height;
			MainImage.Left = (this.ClientSize.Width - MainImage.Width) / 2;

			// Прочее
			pLine = new Pen (Color.FromArgb (255, 0, 255), 1.0f);
			pCross = new Pen (Color.FromArgb (255, 0, 0), 1.0f);

			// Запуск
			this.ShowDialog ();
			}

		// Отмена
		private void Cancel_Click (object sender, EventArgs e)
			{
			this.Close ();
			}

		// Сохранение
		private void Extract_Click (object sender, EventArgs e)
			{
			// Расчёт реальных координат
			int x0 = (int)(leftPoint.X * scaleCoeff);
			int y0 = (int)(leftPoint.Y * scaleCoeff);
			int x1 = (int)(rightPoint.X * scaleCoeff);
			int y1 = (int)(rightPoint.Y * scaleCoeff);

			// Определение расположения
			bool vOriented = Math.Abs (x1 - x0) < Math.Abs (y1 - y0);
			bool vAxeOriented = (x0 == x1);
			bool hAxeOriented = (y0 == y1);

			// Инициализация
			columnsNames.Add ("R");
			columnsNames.Add ("G");
			columnsNames.Add ("B");

			y.Add ([]);
			y.Add ([]);
			y.Add ([]);

			int idx0, idx1;
			if (vOriented)
				{
				idx0 = Math.Min (y0, y1);
				idx1 = Math.Max (y0, y1);
				}
			else
				{
				idx0 = Math.Min (x0, x1);
				idx1 = Math.Max (x0, x1);
				}

			// Сбор данных
			double size = idx1 - idx0;
			for (int i = idx0; i <= idx1; i++)
				{
				// Расчёт второй координаты
				int coord1 = i;

				int coord2;
				if (vAxeOriented)
					coord2 = x0;
				else if (hAxeOriented)
					coord2 = y0;
				else if (vOriented)
					coord2 = (int)((coord1 - y1) * (x0 - x1) / (double)(y0 - y1) + x1);
				else
					coord2 = (int)((coord1 - x1) * (y0 - y1) / (double)(x0 - x1) + y1);

				// Извлечение цвета
				if (vOriented)
					{
					int swap = Math.Min (coord2, img.Width - 1);
					coord2 = Math.Min (coord1, img.Height - 1);
					coord1 = swap;
					}
				else
					{
					coord1 = Math.Min (coord1, img.Width - 1);
					coord2 = Math.Min (coord2, img.Height - 1);
					}

				Color c = img.GetPixel (coord1, coord2);

				// Добавление нормализованных значений
				x.Add ((i - idx0) / size);
				y[0].Add (c.R / 255.0);
				y[1].Add (c.G / 255.0);
				y[2].Add (c.B / 255.0);
				}

			// Готово
			cancelled = false;
			this.Close ();
			}

		// Закрытие окна
		private void ImageProcessor_FormClosing (object sender, FormClosingEventArgs e)
			{
			// Очистка ресурсов
			scaledImg.Dispose ();
			pLine.Dispose ();
			pCross.Dispose ();

			// Сохранение
			RDGenerics.SaveWindowDimensions (this);
			}

		// Быстрая справка
		private void BHelp_Click (object sender, EventArgs e)
			{
			RDInterface.LocalizedMessageBox (RDMessageFlags.Information | RDMessageFlags.NoSound,
				"ImageExtractionGuide");
			}

		// Указание отрезка
		private void MainImage_MouseDown (object sender, MouseEventArgs e)
			{
			// Защита
			if ((e.Button != MouseButtons.Left) && (e.Button != MouseButtons.Right) || (e.Clicks > 1))
				return;

			// Запись точки
			if (e.Button == MouseButtons.Left)
				{
				leftPoint = e.Location;
				leftInited = true;
				}
			else
				{
				rightPoint = e.Location;
				rightInited = true;
				}

			// Отрисовка
			MainImage.Image.Dispose ();
			MainImage.Image = (Image)scaledImg.Clone ();
			Graphics g = Graphics.FromImage (MainImage.Image);

			if (leftInited && rightInited)
				g.DrawLine (pLine, leftPoint, rightPoint);

			if (leftInited)
				{
				g.DrawLine (pCross, leftPoint.X, leftPoint.Y - 5, leftPoint.X, leftPoint.Y + 5);
				g.DrawLine (pCross, leftPoint.X - 5, leftPoint.Y, leftPoint.X + 5, leftPoint.Y);
				}

			if (rightInited)
				{
				g.DrawLine (pCross, rightPoint.X, rightPoint.Y - 5, rightPoint.X, rightPoint.Y + 5);
				g.DrawLine (pCross, rightPoint.X - 5, rightPoint.Y, rightPoint.X + 5, rightPoint.Y);
				}

			// Завершение
			g.Dispose ();
			BExtract.Enabled = leftInited && rightInited;
			}
		}
	}
