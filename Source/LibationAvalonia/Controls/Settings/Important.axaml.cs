using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Dinah.Core;
using FileManager;
using FileLiberator;
using LibationAvalonia.Dialogs;
using LibationAvalonia.ViewModels.Settings;
using LibationFileManager;
using LibationUiBase.Forms;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable enable
namespace LibationAvalonia.Controls.Settings
{
	public partial class Important : UserControl
	{
		private ImportantSettingsVM? ViewModel => DataContext as ImportantSettingsVM;
		public Important()
		{
			InitializeComponent();
			if (Design.IsDesignMode)
			{
				DataContext = new ImportantSettingsVM(Configuration.CreateMockInstance());
			}

			ThemeComboBox.SelectionChanged += ThemeComboBox_SelectionChanged;
		}

		private void EditThemeColors_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
		{
			if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
			{
				//Only allow a single instance of the theme picker
				//Show it as a window, not a dialog, so users can preview
				//their changes throughout the entire app.
				if (lifetime.Windows.OfType<ThemePickerDialog>().FirstOrDefault() is ThemePickerDialog dialog)
				{
					dialog.BringIntoView();
				}
				else
				{
					var themePicker = new ThemePickerDialog();
					themePicker.Show();
				}
			}
		}

		private void ThemeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
		{
			//Remove the combo box before changing the theme, then re-add it.
			//This is a workaround to a crash that will happen if the theme
			//is changed while the combo box is open
			ThemeComboBox.SelectionChanged -= ThemeComboBox_SelectionChanged;
			var parent = ThemeComboBox.Parent as Panel;
			if (parent?.Children.Remove(ThemeComboBox) ?? false)
			{
				Configuration.Instance.SetString(ViewModel?.ThemeVariant, nameof(ViewModel.ThemeVariant));
				parent.Children.Add(ThemeComboBox);
			}
			ThemeComboBox.SelectionChanged += ThemeComboBox_SelectionChanged;
		}

		private async void MigratePdfs_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
		{
			// Check if PDFs directory is configured
			if (string.IsNullOrWhiteSpace(ViewModel?.PDFsDirectory))
			{
				await MessageBox.Show(
					"PDFs directory is not configured. Please configure a PDFs location above before migrating.",
					"PDFs Directory Not Configured",
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
				return;
			}

			// Confirm with user
			var result = await MessageBox.Show(
				"This will move all existing PDFs from their current location (alongside audio files) to the configured PDFs directory.\n\n" +
				"The folder structure will be preserved, and empty directories will be cleaned up.\n\n" +
				"Do you want to proceed?",
				"Migrate PDFs",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Question);

			if (result != DialogResult.Yes)
				return;

			try
			{
				// Run migration (this will show progress in logs)
				var migrationResult = await PdfMigration.MigratePdfsToNewLocationAsync();

				// Show result
				var sb = new StringBuilder();
				sb.AppendLine("Migration completed!");
				sb.AppendLine();
				sb.AppendLine($"Total PDFs: {migrationResult.TotalPdfs}");
				sb.AppendLine($"Successfully migrated: {migrationResult.SuccessfulMigrations}");
				sb.AppendLine($"Skipped (already in correct location): {migrationResult.SkippedPdfs}");
				sb.AppendLine($"Failed: {migrationResult.FailedMigrations}");

				if (migrationResult.Errors.Any())
				{
					sb.AppendLine();
					sb.AppendLine("Errors:");
					foreach (var error in migrationResult.Errors.Take(5))
						sb.AppendLine(error);
					if (migrationResult.Errors.Count > 5)
						sb.AppendLine($"... and {migrationResult.Errors.Count - 5} more errors");
				}

				await MessageBox.Show(
					sb.ToString(),
					"Migration Complete",
					MessageBoxButtons.OK,
					migrationResult.FailedMigrations > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				Serilog.Log.Logger.Error(ex, "Error during PDF migration");
				await MessageBox.Show(
					$"Error during migration: {ex.Message}\n\nSee log for details.",
					"Migration Error",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			}
		}
	}
}
