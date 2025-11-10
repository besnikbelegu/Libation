using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ApplicationServices;
using DataLayer;
using FileManager;
using LibationFileManager;
using Serilog;

#nullable enable
namespace FileLiberator
{
	public class PdfMigration
	{
		public record MigrationResult(int TotalPdfs, int SuccessfulMigrations, int FailedMigrations, int SkippedPdfs, List<string> Errors);

		public class MigrationProgress
		{
			public int TotalPdfs { get; set; }
			public int ProcessedPdfs { get; set; }
			public string? CurrentBook { get; set; }
			public List<string> Errors { get; } = new();
		}

		/// <summary>
		/// Migrates PDFs from their current location (alongside audio files) to the configured PDFs directory.
		/// Only migrates PDFs that are marked as Liberated in the database.
		/// </summary>
		/// <param name="progressCallback">Optional callback for progress updates</param>
		/// <returns>Migration result with statistics</returns>
		public static async Task<MigrationResult> MigratePdfsToNewLocationAsync(IProgress<MigrationProgress>? progressCallback = null)
		{
			// Check if PDFs directory is configured
			var pdfsDirectory = Configuration.Instance.PDFs;
			if (string.IsNullOrWhiteSpace(pdfsDirectory))
			{
				var error = "PDFs directory is not configured. Please configure it in Settings before migrating.";
				Log.Logger.Warning(error);
				return new MigrationResult(0, 0, 0, 0, new List<string> { error });
			}

			Log.Logger.Information("Starting PDF migration to {PDFsDirectory}", pdfsDirectory);

			var progress = new MigrationProgress();
			var errors = new List<string>();
			int successCount = 0;
			int failedCount = 0;
			int skippedCount = 0;

			try
			{
				// Get all library books with PDFs that have been downloaded
				var libraryBooks = await Task.Run(() =>
				{
					using var context = DbContexts.GetContext();
					return context.GetLibrary_Flat_NoTracking()
						.Where(lb => lb.Book.HasPdf() && lb.Book.PDF_Exists())
						.ToList();
				});

				progress.TotalPdfs = libraryBooks.Count;
				Log.Logger.Information("Found {Count} PDFs to potentially migrate", progress.TotalPdfs);

				foreach (var libraryBook in libraryBooks)
				{
					progress.CurrentBook = libraryBook.Book.TitleWithSubtitle;
					progressCallback?.Report(progress);

					try
					{
						var migrated = await MigrateSinglePdfAsync(libraryBook);
						if (migrated)
							successCount++;
						else
							skippedCount++;
					}
					catch (Exception ex)
					{
						failedCount++;
						var error = $"Failed to migrate PDF for '{libraryBook.Book.TitleWithSubtitle}': {ex.Message}";
						Log.Logger.Error(ex, "Error migrating PDF for {BookTitle}", libraryBook.Book.TitleWithSubtitle);
						errors.Add(error);
						progress.Errors.Add(error);
					}

					progress.ProcessedPdfs++;
					progressCallback?.Report(progress);
				}

				Log.Logger.Information("PDF migration complete. Success: {Success}, Failed: {Failed}, Skipped: {Skipped}",
					successCount, failedCount, skippedCount);

				return new MigrationResult(progress.TotalPdfs, successCount, failedCount, skippedCount, errors);
			}
			catch (Exception ex)
			{
				Log.Logger.Error(ex, "Error during PDF migration");
				errors.Add($"Migration failed: {ex.Message}");
				return new MigrationResult(progress.TotalPdfs, successCount, failedCount, skippedCount, errors);
			}
		}

		/// <summary>
		/// Migrates a single PDF file to the new PDFs directory location.
		/// Returns true if migrated, false if skipped (already in correct location or doesn't exist).
		/// Throws exception on error.
		/// </summary>
		private static async Task<bool> MigrateSinglePdfAsync(LibraryBook libraryBook)
		{
			// Get the current (legacy) PDF path - check multiple possible extensions
			var possibleExtensions = new[] { ".pdf", ".PDF" };
			string? currentPath = null;

			// First, try to find the PDF alongside the audio file
			var audioPath = AudibleFileStorage.Audio.GetPath(libraryBook.Book.AudibleProductId);
			var audioDir = Path.GetDirectoryName(audioPath);

			if (!string.IsNullOrEmpty(audioDir) && Directory.Exists(audioDir))
			{
				foreach (var ext in possibleExtensions)
				{
					var testPath = AudibleFileStorage.Audio.GetCustomDirFilename(libraryBook, audioDir, ext, returnFirstExisting: true);
					if (File.Exists(testPath))
					{
						currentPath = testPath;
						break;
					}
				}
			}

			// If not found alongside audio, try Books directory
			if (currentPath == null)
			{
				foreach (var ext in possibleExtensions)
				{
					var testPath = AudibleFileStorage.Audio.GetBooksDirectoryFilename(libraryBook, ext, returnFirstExisting: true);
					if (File.Exists(testPath))
					{
						currentPath = testPath;
						break;
					}
				}
			}

			if (currentPath == null)
			{
				Log.Logger.Debug("PDF not found on disk for {BookTitle}, skipping", libraryBook.Book.TitleWithSubtitle);
				return false;
			}

			// Get the new (target) PDF path
			var extension = Path.GetExtension(currentPath);
			var newPath = AudibleFileStorage.Audio.GetPDFsDirectoryFilename(libraryBook, extension);

			// Check if already in the correct location
			if (string.Equals(Path.GetFullPath(currentPath), Path.GetFullPath(newPath), StringComparison.OrdinalIgnoreCase))
			{
				Log.Logger.Debug("PDF already in correct location for {BookTitle}, skipping", libraryBook.Book.TitleWithSubtitle);
				return false;
			}

			// Ensure target directory exists
			var targetDir = Path.GetDirectoryName(newPath);
			if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
			{
				Log.Logger.Debug("Creating directory: {Directory}", targetDir);
				Directory.CreateDirectory(targetDir);
			}

			// Handle potential file conflicts
			if (File.Exists(newPath))
			{
				// Check if files are identical
				if (await FilesAreIdenticalAsync(currentPath, newPath))
				{
					Log.Logger.Information("Identical PDF already exists at destination for {BookTitle}, deleting source", libraryBook.Book.TitleWithSubtitle);
					File.Delete(currentPath);
					CleanupEmptyDirectories(currentPath);
					return true;
				}

				// Files are different - backup the existing file
				var backupPath = GetBackupPath(newPath);
				Log.Logger.Warning("Different PDF exists at destination for {BookTitle}, backing up existing to {BackupPath}",
					libraryBook.Book.TitleWithSubtitle, backupPath);
				File.Move(newPath, backupPath);
			}

			// Move the file
			Log.Logger.Information("Moving PDF for {BookTitle} from {OldPath} to {NewPath}",
				libraryBook.Book.TitleWithSubtitle, currentPath, newPath);

			await Task.Run(() => File.Move(currentPath, newPath));

			// Clean up empty directories left behind
			CleanupEmptyDirectories(currentPath);

			return true;
		}

		/// <summary>
		/// Checks if two files are identical by comparing their content.
		/// </summary>
		private static async Task<bool> FilesAreIdenticalAsync(string path1, string path2)
		{
			var file1Info = new FileInfo(path1);
			var file2Info = new FileInfo(path2);

			// Quick check: if sizes differ, files are different
			if (file1Info.Length != file2Info.Length)
				return false;

			// Compare file contents
			return await Task.Run(() =>
			{
				const int bufferSize = 8192;
				using var stream1 = File.OpenRead(path1);
				using var stream2 = File.OpenRead(path2);

				byte[] buffer1 = new byte[bufferSize];
				byte[] buffer2 = new byte[bufferSize];

				int bytesRead1, bytesRead2;
				do
				{
					bytesRead1 = stream1.Read(buffer1, 0, bufferSize);
					bytesRead2 = stream2.Read(buffer2, 0, bufferSize);

					if (bytesRead1 != bytesRead2)
						return false;

					for (int i = 0; i < bytesRead1; i++)
					{
						if (buffer1[i] != buffer2[i])
							return false;
					}
				}
				while (bytesRead1 > 0);

				return true;
			});
		}

		/// <summary>
		/// Generates a backup file path by appending a timestamp.
		/// </summary>
		private static string GetBackupPath(string filePath)
		{
			var directory = Path.GetDirectoryName(filePath);
			var fileName = Path.GetFileNameWithoutExtension(filePath);
			var extension = Path.GetExtension(filePath);
			var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

			return Path.Combine(directory ?? "", $"{fileName}_backup_{timestamp}{extension}");
		}

		/// <summary>
		/// Cleans up empty directories left behind after moving a file.
		/// Walks up the directory tree and removes empty directories until hitting the Books directory.
		/// </summary>
		private static void CleanupEmptyDirectories(string filePath)
		{
			try
			{
				var directory = Path.GetDirectoryName(filePath);
				if (string.IsNullOrEmpty(directory))
					return;

				var booksDir = AudibleFileStorage.BooksDirectory?.ToString();
				if (string.IsNullOrEmpty(booksDir))
					return;

				// Walk up the directory tree
				while (!string.IsNullOrEmpty(directory) &&
					   !string.Equals(directory, booksDir, StringComparison.OrdinalIgnoreCase) &&
					   directory.StartsWith(booksDir, StringComparison.OrdinalIgnoreCase))
				{
					// Check if directory is empty
					if (!Directory.EnumerateFileSystemEntries(directory).Any())
					{
						Log.Logger.Debug("Removing empty directory: {Directory}", directory);
						Directory.Delete(directory);
						directory = Path.GetDirectoryName(directory);
					}
					else
					{
						// Directory is not empty, stop walking up
						break;
					}
				}
			}
			catch (Exception ex)
			{
				// Don't fail the migration if cleanup fails
				Log.Logger.Warning(ex, "Failed to cleanup empty directories for {FilePath}", filePath);
			}
		}
	}
}
