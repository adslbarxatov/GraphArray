using System;
using System.Windows.Forms;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает форму настроек программы
	/// </summary>
	public partial class ProgramSettings: Form
		{
		/// <summary>
		/// Конструктор. Запускает настройку программы
		/// </summary>
		public ProgramSettings ()
			{
			// Инициализация и локализация формы
			InitializeComponent ();
			RDGenerics.LoadWindowDimensions (this);

			RDLocale.SetControlsText (this);
			SaveButton.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Button_Save);
			AbortButton.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel);
			this.Text = RDLocale.GetText (this.Name + "_T");

			// Настройка контролов
			ConfirmExit.Checked = ConfigAccessor.ForceExitConfirmation;

			if (RDGenerics.AppHasAccessRights (false, false))
				ForceUsingBackupFile.Checked = ConfigAccessor.ForceUsingBackupDataFile;
			else
				ForceUsingBackupFile.Checked = ForceUsingBackupFile.Enabled = false;

			ForceShowDiagram.Checked = ConfigAccessor.ForceShowDiagram;
			ForceSavingColumnNames.Checked = ConfigAccessor.ForceSavingColumnNames;

			// Запуск
			this.ShowDialog ();
			}

		// Отмена
		private void SaveAbort_Click (object sender, EventArgs e)
			{
			this.Close ();
			}

		// Сохранение
		private void SaveSettings_Click (object sender, EventArgs e)
			{
			ConfigAccessor.ForceExitConfirmation = ConfirmExit.Checked;
			ConfigAccessor.ForceUsingBackupDataFile = ForceUsingBackupFile.Checked;
			ConfigAccessor.ForceShowDiagram = ForceShowDiagram.Checked;
			ConfigAccessor.ForceSavingColumnNames = ForceSavingColumnNames.Checked;

			this.Close ();
			}

		// Закрытие окна
		private void ProgramSettings_FormClosing (object sender, FormClosingEventArgs e)
			{
			RDGenerics.SaveWindowDimensions (this);
			}
		}
	}
