using Dinah.Core;
using FileManager;
using FileLiberator;
using LibationFileManager;
using LibationUiBase;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

#nullable enable
namespace LibationWinForms.Dialogs
{
	public partial class SettingsDialog
	{
		private void logsBtn_Click(object sender, EventArgs e)
		{
			if (File.Exists(LogFileFilter.LogFilePath))
				Go.To.File(LogFileFilter.LogFilePath);
			else
				Go.To.Folder(((LongPath)Configuration.Instance.LibationFiles).ShortPathName);
		}

		private void Load_Important(Configuration config)
		{
			{
				loggingLevelCb.Items.Clear();
				foreach (var level in Enum<Serilog.Events.LogEventLevel>.GetValues())
					loggingLevelCb.Items.Add(level);
				loggingLevelCb.SelectedItem = config.LogLevel;
			}

			booksLocationDescLbl.Text = desc(nameof(config.Books));
			pdfsLocationDescLbl.Text = desc(nameof(config.PDFs));
			saveEpisodesToSeriesFolderCbox.Text = desc(nameof(config.SavePodcastsToParentFolder));
			overwriteExistingCbox.Text = desc(nameof(config.OverwriteExisting));
			creationTimeLbl.Text = desc(nameof(config.CreationTime));
			lastWriteTimeLbl.Text = desc(nameof(config.LastWriteTime));
			gridScaleFactorLbl.Text = desc(nameof(config.GridScaleFactor));
			gridFontScaleFactorLbl.Text = desc(nameof(config.GridFontScaleFactor));

			var dateTimeSources = Enum.GetValues<Configuration.DateTimeSource>().Select(v => new EnumDisplay<Configuration.DateTimeSource>(v)).ToArray();
			creationTimeCb.Items.AddRange(dateTimeSources);
			lastWriteTimeCb.Items.AddRange(dateTimeSources);

			creationTimeCb.SelectedItem = dateTimeSources.SingleOrDefault(v => v.Value == config.CreationTime) ?? dateTimeSources[0];
			lastWriteTimeCb.SelectedItem = dateTimeSources.SingleOrDefault(v => v.Value == config.LastWriteTime) ?? dateTimeSources[0];


			booksSelectControl.SetSearchTitle("books location");
			booksSelectControl.SetDirectoryItems(
				new()
				{
					Configuration.KnownDirectories.UserProfile,
					Configuration.KnownDirectories.AppDir,
					Configuration.KnownDirectories.MyDocs,
					Configuration.KnownDirectories.MyMusic,
				},
				Configuration.KnownDirectories.UserProfile,
				"Books");
			booksSelectControl.SelectDirectory(config.Books?.PathWithoutPrefix ?? "");

			pdfsSelectControl.SetSearchTitle("PDFs location");
			pdfsSelectControl.SetDirectoryItems(
				new()
				{
					Configuration.KnownDirectories.UserProfile,
					Configuration.KnownDirectories.AppDir,
					Configuration.KnownDirectories.MyDocs,
					Configuration.KnownDirectories.MyMusic,
				},
				Configuration.KnownDirectories.UserProfile,
				"PDFs");
			pdfsSelectControl.SelectDirectory(config.PDFs?.PathWithoutPrefix ?? "");

			saveEpisodesToSeriesFolderCbox.Checked = config.SavePodcastsToParentFolder;
			overwriteExistingCbox.Checked = config.OverwriteExisting;
			gridScaleFactorTbar.Value = scaleFactorToLinearRange(config.GridScaleFactor);
			gridFontScaleFactorTbar.Value = scaleFactorToLinearRange(config.GridFontScaleFactor);
		}

		private bool Save_Important(Configuration config)
		{
			var newBooks = booksSelectControl.SelectedDirectory;

			#region validation
			static void validationError(string text, string caption)
				=> MessageBox.Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
			if (string.IsNullOrWhiteSpace(newBooks))
			{
				validationError("Cannot set Books Location to blank", "Location is blank");
				return false;
			}
			LongPath lonNewBooks = newBooks;
			if (!Directory.Exists(lonNewBooks))
			{
				try
				{
					Directory.CreateDirectory(lonNewBooks);
				}
				catch (Exception ex)
				{
					validationError($"Error creating Books Location:\r\n{ex.Message}", "Error creating directory");
					return false;
				}
			}
			#endregion


			config.Books = newBooks;

			var newPDFs = pdfsSelectControl.SelectedDirectory;

			// PDFs directory is optional; empty = use legacy behavior
			if (!string.IsNullOrWhiteSpace(newPDFs))
			{
				LongPath lonNewPDFs = newPDFs;
				if (!Directory.Exists(lonNewPDFs))
				{
					try
					{
						Directory.CreateDirectory(lonNewPDFs);
					}
					catch (Exception ex)
					{
						validationError($"Error creating PDFs Location:\r\n{ex.Message}", "Error creating directory");
						return false;
					}
				}
			}

			config.PDFs = string.IsNullOrWhiteSpace(newPDFs) ? null : newPDFs;

			{
				var logLevelOld = config.LogLevel;
				var logLevelNew = (loggingLevelCb.SelectedItem as Serilog.Events.LogEventLevel?) ?? Serilog.Events.LogEventLevel.Information;

				config.LogLevel = logLevelNew;

				// only warn if changed during this time. don't want to warn every time user happens to change settings while level is verbose
				if (logLevelOld != logLevelNew)
					MessageBoxLib.VerboseLoggingWarning_ShowIfTrue();
			}

			config.SavePodcastsToParentFolder = saveEpisodesToSeriesFolderCbox.Checked;
			config.OverwriteExisting = overwriteExistingCbox.Checked;

			config.CreationTime = (creationTimeCb.SelectedItem as EnumDisplay<Configuration.DateTimeSource>)?.Value ?? Configuration.DateTimeSource.File;
			config.LastWriteTime = (lastWriteTimeCb.SelectedItem as EnumDisplay<Configuration.DateTimeSource>)?.Value ?? Configuration.DateTimeSource.File;
			return true;
		}

		private static int scaleFactorToLinearRange(float scaleFactor)
			=> (int)float.Round(100 * MathF.Log2(scaleFactor));
		private static float linearRangeToScaleFactor(int value)
			=> MathF.Pow(2, value / 100f);

		private void applyDisplaySettingsBtn_Click(object sender, EventArgs e)
		{
			config.GridFontScaleFactor = linearRangeToScaleFactor(gridFontScaleFactorTbar.Value);
			config.GridScaleFactor = linearRangeToScaleFactor(gridScaleFactorTbar.Value);
		}

		private async void migratePdfsBtn_Click(object sender, EventArgs e)
		{
			// Check if PDFs directory is configured
			var pdfsDirectory = pdfsSelectControl.SelectedDirectory;
			if (string.IsNullOrWhiteSpace(pdfsDirectory))
			{
				MessageBox.Show(
					"PDFs directory is not configured. Please configure a PDFs location above before migrating.",
					"PDFs Directory Not Configured",
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
				return;
			}

			// Confirm with user
			var result = MessageBox.Show(
				"This will move all existing PDFs from their current location (alongside audio files) to the configured PDFs directory.\n\n" +
				"The folder structure will be preserved, and empty directories will be cleaned up.\n\n" +
				"Do you want to proceed?",
				"Migrate PDFs",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Question);

			if (result != DialogResult.Yes)
				return;

			// Create progress form
			using var progressForm = new Form
			{
				Text = "Migrating PDFs",
				Width = 500,
				Height = 200,
				FormBorderStyle = FormBorderStyle.FixedDialog,
				StartPosition = FormStartPosition.CenterParent,
				ControlBox = false,
				MaximizeBox = false,
				MinimizeBox = false
			};

			var progressLabel = new Label
			{
				Text = "Preparing migration...",
				AutoSize = false,
				Width = 460,
				Height = 100,
				Left = 20,
				Top = 20
			};

			progressForm.Controls.Add(progressLabel);
			progressForm.Show(this);

			try
			{
				var progress = new Progress<PdfMigration.MigrationProgress>(p =>
				{
					if (progressForm.InvokeRequired)
					{
						progressForm.Invoke(new Action(() =>
						{
							progressLabel.Text = $"Processing: {p.CurrentBook}\n\nProgress: {p.ProcessedPdfs} / {p.TotalPdfs}";
							if (p.Errors.Any())
							{
								progressLabel.Text += $"\n\nErrors: {p.Errors.Count}";
							}
						}));
					}
					else
					{
						progressLabel.Text = $"Processing: {p.CurrentBook}\n\nProgress: {p.ProcessedPdfs} / {p.TotalPdfs}";
						if (p.Errors.Any())
						{
							progressLabel.Text += $"\n\nErrors: {p.Errors.Count}";
						}
					}
				});

				var migrationResult = await PdfMigration.MigratePdfsToNewLocationAsync(progress);

				progressForm.Close();

				// Show result
				var message = $"Migration completed!\n\n" +
							  $"Total PDFs: {migrationResult.TotalPdfs}\n" +
							  $"Successfully migrated: {migrationResult.SuccessfulMigrations}\n" +
							  $"Skipped (already in correct location): {migrationResult.SkippedPdfs}\n" +
							  $"Failed: {migrationResult.FailedMigrations}";

				if (migrationResult.Errors.Any())
				{
					message += $"\n\nErrors:\n{string.Join("\n", migrationResult.Errors.Take(5))}";
					if (migrationResult.Errors.Count > 5)
						message += $"\n... and {migrationResult.Errors.Count - 5} more errors";
				}

				MessageBox.Show(
					message,
					"Migration Complete",
					MessageBoxButtons.OK,
					migrationResult.FailedMigrations > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				progressForm.Close();

				Serilog.Log.Logger.Error(ex, "Error during PDF migration");
				MessageBox.Show(
					$"Error during migration: {ex.Message}\n\nSee log for details.",
					"Migration Error",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			}
		}
	}
}
