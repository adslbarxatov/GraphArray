using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс обеспечивает загрузку стандартных и дополнительных маркеров для построения диаграмм
	/// </summary>
	public class MarkersLoader: IDisposable
		{
		// Массив маркеров
		private List<Bitmap> markers = [];

		// Размеры стандартных маркеров
		private const int standartMarkersSize = 7;

		// Директория с маркерами
		private const string markersDirectory = "Markers";

		/// <summary>
		/// Максимальное количество доступных маркеров
		/// </summary>
		public const uint MaxMarkers = 100;

		/// <summary>
		/// Возвращает изображение маркера, окрашенное указанным цветом
		/// </summary>
		/// <param name="MarkerColor">Требуемый цвет маркера</param>
		/// <param name="MarkerNumber">Номер требуемого маркера, начиная с нуля</param>
		/// <returns>Изображение маркера или изображение первого стандартного маркера, 
		/// если номер маркера указан некорректно
		/// </returns>
		public Bitmap GetMarker (uint MarkerNumber, Color MarkerColor)
			{
			// Контроль
			int markerNumber = (int)MarkerNumber;
			if (MarkerNumber >= markers.Count)
				markerNumber = 0;

			// Создание копии маркера
			Bitmap b = (Bitmap)markers[markerNumber].Clone ();
			for (int h = 0; h < b.Height; h++)
				{
				for (int w = 0; w < b.Width; w++)
					{
					if ((uint)(b.GetPixel (w, h).ToArgb ()) == 0xFF000000)
						b.SetPixel (w, h, MarkerColor);
					}
				}

			// Возврат
			return b;
			}

		/// <summary>
		/// Возвращает количество доступных маркеров
		/// </summary>
		public uint MarkersCount
			{
			get
				{
				return (uint)markers.Count;
				}
			}

		/// <summary>
		/// Конструктор. Выполняет загрузку изображений маркеров
		/// </summary>
		public MarkersLoader ()
			{
			#region Загрузка дополнительных маркеров из файлов

			// Получение списка файлов
			string markersPath = RDGenerics.GetStoragePath (true, markersDirectory);
			string[] markersImages;
			try
				{
				markersImages = Directory.GetFiles (markersPath, "*.png");
				}
			catch
				{
				return;
				}

			// Загрузка изображений
			Bitmap b;
			for (int i = 0; (i < markersImages.Length) && (i < MaxMarkers); i++)
				{
				// Попытка открытия
				try
					{
					b = (Bitmap)Image.FromFile (markersImages[i]);
					}
				catch
					{
					continue;
					}

				// Применение обработок, если они не были применены ранее
				bool needsUpdate = !Path.GetFileName (markersImages[i]).StartsWith ('@');
				if (needsUpdate)
					{
					if (b.Width != b.Height)
						{
						b.Dispose ();
						continue;
						}

					if ((b.Width < 3) || (b.Width > 17))
						{
						b.Dispose ();
						continue;
						}

					// Замещение цветов
					for (int y = 0; y < b.Height; y++)
						{
						for (int x = 0; x < b.Width; x++)
							{
							Color c = b.GetPixel (x, y);
							if (c.R + c.G + c.B > 128 * 3)
								b.SetPixel (x, y, Color.FromArgb (255, 255, 255));
							else
								b.SetPixel (x, y, Color.FromArgb (0, 0, 0));
							}
						}

					// Установка белого как прозрачного
					b.MakeTransparent (Color.FromArgb (255, 255, 255));
					}

				markers.Add ((Bitmap)b.Clone ());
				b.Dispose ();

				if (needsUpdate)
					{
					// Перезапись и добавление
					try
						{
						markers[markers.Count - 1].Save (markersPath + "@" + Path.GetFileName (markersImages[i]), ImageFormat.Png);
						File.Move (markersImages[i], markersImages[i] + ".bak");
						}
					catch { }
					}
				}

			// Завершение
			#endregion

			#region Добавление стандартных маркеров

			// Стандартные маркеры уже созданы
			if (File.Exists (markersPath + "@0.png"))
				return;

			b = new Bitmap (standartMarkersSize, standartMarkersSize);
			Brush backBrush = new SolidBrush (Color.FromArgb (0, 255, 255, 255)),
				foreBrush = new SolidBrush (Color.FromArgb (0, 0, 0));

			// Квадратик
			Graphics g = Graphics.FromImage (b);
			g.FillRectangle (backBrush, 0, 0, b.Width, b.Height);
			g.FillRectangle (foreBrush, 1, 1, b.Width - 2, b.Height - 2);

			markers.Insert (0, (Bitmap)b.Clone ());	// Нужно отвязать картинку от объекта b, иначе правка сохранится в ней
			try
				{
				b.Save (markersPath + "@0.png", ImageFormat.Png);
				}
			catch { }

			b.Dispose ();
			g.Dispose ();

			// Кружочек
			b = new Bitmap (standartMarkersSize, standartMarkersSize);
			g = Graphics.FromImage (b);
			g.FillRectangle (backBrush, 0, 0, b.Width, b.Height);
			g.FillEllipse (foreBrush, 0, 0, b.Width, b.Height);

			markers.Insert (1, (Bitmap)b.Clone ());
			try
				{
				b.Save (markersPath + "@1.png", ImageFormat.Png);
				}
			catch { }

			b.Dispose ();
			g.Dispose ();

			// Треугольник
			b = new Bitmap (standartMarkersSize, standartMarkersSize);
			g = Graphics.FromImage (b);
			g.FillRectangle (backBrush, 0, 0, b.Width, b.Height);
			Point[] pts = [new Point (0, b.Height), new Point (b.Width / 2, 0), new Point (b.Width, b.Height)];
			g.FillPolygon (foreBrush, pts);

			markers.Insert (2, (Bitmap)b.Clone ());
			try
				{
				b.Save (markersPath + "@2.png", ImageFormat.Png);
				}
			catch { }

			b.Dispose ();
			g.Dispose ();

			// Прямоугольник
			b = new Bitmap (standartMarkersSize, standartMarkersSize);
			g = Graphics.FromImage (b);
			g.FillRectangle (backBrush, 0, 0, b.Width, b.Height);
			g.DrawRectangle (new Pen (foreBrush), 1, 1, b.Width - 2, b.Height - 2);
			
			markers.Insert (3, (Bitmap)b.Clone ());
			try
				{
				b.Save (markersPath + "@3.png", ImageFormat.Png);
				}
			catch { }

			b.Dispose ();
			g.Dispose ();

			// Колечко
			b = new Bitmap (standartMarkersSize, standartMarkersSize);
			g = Graphics.FromImage (b);
			g.FillRectangle (backBrush, 0, 0, b.Width, b.Height);
			g.DrawEllipse (new Pen (foreBrush), 1, 1, b.Width - 3, b.Height - 3);
			
			markers.Insert (4, (Bitmap)b.Clone ());
			try
				{
				b.Save (markersPath + "@4.png", ImageFormat.Png);
				}
			catch { }

			b.Dispose ();
			g.Dispose ();

			// Крестик
			b = new Bitmap (standartMarkersSize, standartMarkersSize);
			g = Graphics.FromImage (b);
			g.FillRectangle (backBrush, 0, 0, b.Width, b.Height);
			g.DrawLine (new Pen (foreBrush), 1, 1, b.Width - 2, b.Height - 2);
			g.DrawLine (new Pen (foreBrush), 1, b.Height - 2, b.Width - 2, 1);
			
			markers.Insert (5, (Bitmap)b.Clone ());
			try
				{
				b.Save (markersPath + "@5.png", ImageFormat.Png);
				}
			catch { }

			b.Dispose ();
			g.Dispose ();

			// Завершение
			foreBrush.Dispose ();
			backBrush.Dispose ();

			#endregion
			}

		/// <summary>
		/// Метод освобождает все используемые ресурсы
		/// </summary>
		public void Dispose ()
			{
			for (int i = 0; i < markers.Count; i++)
				markers[i].Dispose ();

			markers.Clear ();
			}
		}
	}
