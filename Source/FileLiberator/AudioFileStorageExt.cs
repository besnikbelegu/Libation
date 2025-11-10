using System;
using System.Collections.Generic;
using System.Linq;
using AaxDecrypter;
using DataLayer;
using LibationFileManager;
using LibationFileManager.Templates;

namespace FileLiberator
{
	public static class AudioFileStorageExt
	{
		/// <summary>
		/// DownloadDecryptBook:
		/// File path for where to move files into.
		/// Path: directory nested inside of Books directory
		/// File name: n/a
		/// </summary>
		public static string GetDestinationDirectory(this AudioFileStorage _, LibraryBook libraryBook)
		{
			if (libraryBook.Book.IsEpisodeChild() && Configuration.Instance.SavePodcastsToParentFolder)
			{
				var series = libraryBook.Book.SeriesLink.SingleOrDefault();
				if (series is not null)
				{
					using var context = ApplicationServices.DbContexts.GetContext();
					var seriesParent = context.GetLibraryBook_Flat_NoTracking(series.Series.AudibleSeriesId);

					if (seriesParent is not null)
					{
						return Templates.Folder.GetFilename(seriesParent.ToDto(), AudibleFileStorage.BooksDirectory, "");
					}
				}
			}
			return Templates.Folder.GetFilename(libraryBook.ToDto(), AudibleFileStorage.BooksDirectory, "");
		}

		/// <summary>
		/// PDF: audio file does not exist
		/// </summary>
		public static string GetBooksDirectoryFilename(this AudioFileStorage _, LibraryBook libraryBook, string extension, bool returnFirstExisting = false)
			=> Templates.File.GetFilename(libraryBook.ToDto(), AudibleFileStorage.BooksDirectory, extension, returnFirstExisting: returnFirstExisting);

		/// <summary>
		/// PDF: audio file already exists
		/// </summary>
		public static string GetCustomDirFilename(this AudioFileStorage _, LibraryBook libraryBook, string dirFullPath, string extension, MultiConvertFileProperties partProperties = null, bool returnFirstExisting = false)
			=> partProperties is null ? Templates.File.GetFilename(libraryBook.ToDto(), dirFullPath, extension, returnFirstExisting: returnFirstExisting)
			: Templates.ChapterFile.GetFilename(libraryBook.ToDto(), partProperties, dirFullPath, extension, returnFirstExisting: returnFirstExisting);

	/// <summary>
	/// PDF: when using separate PDFs directory
	/// </summary>
	public static string GetPDFsDirectoryFilename(this AudioFileStorage audioFileStorage, LibraryBook libraryBook, string extension, bool returnFirstExisting = false)
	{
		// GetDestinationDirectory returns a full path with BooksDirectory as base
		// We need to extract just the relative folder structure to use with PDFsDirectory
		var fullDestinationInBooks = audioFileStorage.GetDestinationDirectory(libraryBook);
		var booksDir = AudibleFileStorage.BooksDirectory?.ToString();

		// Get the relative path by removing the BooksDirectory prefix
		var relativePath = fullDestinationInBooks;
		if (!string.IsNullOrEmpty(booksDir) && fullDestinationInBooks.StartsWith(booksDir))
		{
			relativePath = fullDestinationInBooks.Substring(booksDir.Length).TrimStart(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
		}

		// Combine with PDFsDirectory to get the full destination for PDFs
		var fullDestination = System.IO.Path.Combine(AudibleFileStorage.PDFsDirectory.ToString(), relativePath);

		return Templates.File.GetFilename(
			libraryBook.ToDto(),
			fullDestination,
			extension,
			returnFirstExisting: returnFirstExisting);
	}
	}
}
