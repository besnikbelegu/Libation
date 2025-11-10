# Libation: Liberate your Library

## [Download Libation](https://github.com/rmcrackan/Libation/releases/latest)

### If you found this useful, tell a friend. If you found this REALLY useful, you can click here to [PayPal.me](https://paypal.me/mcrackan?locale.x=en_us)
...or just tell more friends. As long as I'm maintaining this software, it will remain **free** and **open source**.



# Table of Contents

- [Audible audiobook manager](#audible-audiobook-manager)
    - [The good](#the-good)
    - [The bad](#the-bad)
    - [The ugly](#the-ugly)
- [Getting started](Documentation/GettingStarted.md)
    - [Download Libation](Documentation/GettingStarted.md#download-libation-1)
    - [Installation](Documentation/GettingStarted.md#installation)
    - [Create Accounts](Documentation/GettingStarted.md#create-accounts)
    - [Import your library](Documentation/GettingStarted.md#import-your-library)
    - [Download your books -- DRM-free!](Documentation/GettingStarted.md#download-your-books----drm-free)
    - [Download PDF attachments](Documentation/GettingStarted.md#download-pdf-attachments)
    - [Details of downloaded files](Documentation/GettingStarted.md#details-of-downloaded-files)
    - [Export your library](Documentation/GettingStarted.md#export-your-library)
    - If you still need help, [you can open an issue here](https://github.com/rmcrackan/Libation/issues) for bug reports, feature requests, or specialized help.
- [Searching and filtering](Documentation/SearchingAndFiltering.md)
    - [Tags](Documentation/SearchingAndFiltering.md#tags)
    - [Searches](Documentation/SearchingAndFiltering.md#searches)
    - [Search examples](Documentation/SearchingAndFiltering.md#search-examples)
    - [Filters](Documentation/SearchingAndFiltering.md#filters)
- [Advanced](Documentation/Advanced.md)
    - [Files and folders](Documentation/Advanced.md#files-and-folders)
    - [Settings](Documentation/Advanced.md#settings)
    - [Custom File Naming](Documentation/NamingTemplates.md)
    - [Command Line Interface](Documentation/Advanced.md#command-line-interface)
    - [Custom Theme Colors](Documentation/Advanced.md#custom-theme-colors) (Chardonnay Only)
    - [Audio Formats (Dolby Atmos, Widevine, Spacial Audio)](Documentation/AudioFileFormats.md)
- [Docker](Documentation/Docker.md)
- [Frequently Asked Questions](Documentation/FrequentlyAskedQuestions.md)
- [For Developers](#for-developers)
    - [Prerequisites](#prerequisites)
    - [Running Locally](#running-locally)
    - [Debugging](#debugging)
    - [Building for Different Platforms](#building-for-different-platforms)
    - [Database Management](#database-management)
    - [Running Tests](#running-tests)
    - [Project Architecture](#project-architecture)
    - [Useful Resources for Developers](#useful-resources-for-developers)

## Getting started

* [Download](https://github.com/rmcrackan/Libation/releases/latest)
* [Step-by-step walk-through](Documentation/GettingStarted.md)

## Audible audiobook manager

### The good

* Import library from audible, including cover art
* Download and remove DRM from all books
* Download accompanying PDFs
* Add tags to books for better organization
* Powerful advanced search built on the Lucene search engine
* Customizable saved filters for common searches
* Open source
* Supports most regions: US, UK, Canada, Germany, France, Australia, Japan, India, and Spain
* Fully supported in Windows, Mac, and Linux

<a name="theBad"/>

### The bad

* Large file size
* Made by a programmer, not a designer so the goals are function rather than beauty. And it shows

### The ugly

* Documentation? Yer lookin' at it
* This is a single-developer personal passion project. Support, response, updates, enhancements, bug fixes etc are as my free time allows
* I have a full-time job, a life, and a finite attention span. Therefore a lot of time can potentially go by with no improvements of any kind

Disclaimer: I've made every good-faith effort to include nothing insecure, malicious, anti-privacy, or destructive. That said: use at your own risk.

I made this for myself and I want to share it with the great programming and audible/audiobook communities which have been so generous with their time and help.

---

## For Developers

### Prerequisites

- **.NET 9.0 SDK** - [Download from dot.net](https://dot.net)
- **Git** - For version control
- Optional: **Visual Studio 2022** or **Visual Studio Code** with C# extensions

### Running Locally

#### Quick Start

```bash
# Clone the repository
git clone https://github.com/rmcrackan/Libation.git
cd Libation

# Build and run the Avalonia UI (modern UI - recommended)
dotnet run --project Source/LibationAvalonia/LibationAvalonia.csproj
```

#### Build Only (without running)

```bash
# Build the Avalonia UI
dotnet build Source/LibationAvalonia/LibationAvalonia.csproj

# Build the entire solution (includes WinForms legacy UI)
dotnet build Source/Libation.sln
```

#### Run from Built Binary

```bash
# Debug build
./Source/bin/Avalonia/Debug/Libation

# Release build
dotnet build Source/LibationAvalonia/LibationAvalonia.csproj -c Release
./Source/bin/Avalonia/Release/Libation
```

#### Using VS Code Tasks

If using VS Code, you can run pre-configured build tasks:

```bash
# Via command palette: Ctrl+Shift+P / Cmd+Shift+P > Tasks: Run Task
# Available tasks:
- build           # Build both Avalonia UI and LinuxConfigApp
- build_libation  # Build Avalonia UI only
- build_linux     # Build for Linux target (x64)
```

### Debugging

#### Enable EF Core Query Logging

To debug database queries, enable Entity Framework Core logging in your code:

```csharp
using var context = new LibationContext();
context.ConfigureLogging(s => System.Diagnostics.Debug.WriteLine(s));
// Output will appear in Visual Studio Output tab or debug console
```

See `Dinah.EntityFrameworkCore/DbContextLoggingExtensions.cs` for implementation details.

#### UI Data Binding Issues (Avalonia)

- Verify `GridEntry.NotifyPropertyChanged()` property names are spelled correctly (Avalonia is case-sensitive)
- Use Avalonia DevTools for tracing bindings (Debug build includes `Avalonia.Diagnostics`)
- Ensure `DataContext` is set before showing windows/dialogs

#### Logging

The project uses **Serilog** for structured logging. Logs are written to:
- Console (Debug builds)
- Log files in the application data directory

Enable verbose logging in the UI settings for detailed debugging output.

### Building for Different Platforms

#### Windows (Default)

```bash
dotnet build Source/LibationAvalonia/LibationAvalonia.csproj
```

#### macOS

```bash
dotnet build Source/LibationAvalonia/LibationAvalonia.csproj \
  -p:RuntimeIdentifier=osx-x64
```

#### Linux (x64)

```bash
dotnet build Source/LibationAvalonia/LibationAvalonia.csproj \
  -p:TargetFramework=net9.0 \
  -p:RuntimeIdentifier=linux-x64
```

See [InstallOnLinux.md](Documentation/InstallOnLinux.md) for additional Linux build details.

### Database Management

#### View Entity Framework Migrations

```bash
dotnet ef migrations list --project Source/DataLayer/DataLayer.csproj
```

#### Add a New Migration

```bash
# Using Package Manager Console (Visual Studio)
# Set Default Project to: DataLayer
# Set Startup Project to: DataLayer (or your test project)
Add-Migration MyDescriptiveName -context LibationContext

# Or via CLI
dotnet ef migrations add MyDescriptiveName --project Source/DataLayer/DataLayer.csproj -c LibationContext
```

#### Update Database to Latest Migration

```bash
# Using Package Manager Console
Update-Database -context LibationContext

# Or via CLI
dotnet ef database update --project Source/DataLayer/DataLayer.csproj -c LibationContext
```

#### Troubleshooting Migrations

- **"Add-Migration not recognized"** → Install NuGet package: `Microsoft.EntityFrameworkCore.Tools`
- **Unhelpful error messages** → Add `-verbose` flag to see detailed output
- **Multiple contexts** → Always specify `-context LibationContext` to avoid ambiguity
- **SQLite path issues** → Use forward slashes for paths: `Data Source=C:/foo/bar/db.sqlite`

### Running Tests

```bash
# Run all tests
dotnet test Source/_Tests/

# Run specific test project
dotnet test Source/_Tests/AudibleUtilities.Tests/

# Run with coverage
dotnet test Source/_Tests/ --collect:"XPlat Code Coverage"
```

Test projects:
- `AudibleUtilities.Tests` - Audible API integration tests
- `FileLiberator.Tests` - Audio file decryption tests
- `FileManager.Tests` - File management tests
- `LibationFileManager.Tests` - File manager service tests
- `LibationSearchEngine.Tests` - Search engine tests

### Project Architecture

The solution enforces layered dependencies (numbered 1-6):

1. **Core Libraries** - Universal utilities
2. **Utilities (domain ignorant)** - Encryption, network, file handling
3. **Domain Internal Utilities (db ignorant)** - Libation-specific logic
4. **Domain (db)** - EF Core entities and configurations (`DataLayer`)
5. **Domain Utilities (db aware)** - Application services (`ApplicationServices`)
6. **Application** - UIs and CLIs (`LibationAvalonia`, `LibationCli`, `LibationWinForms`)

For detailed architecture information, see:
- `Source/_ARCHITECTURE NOTES.txt`
- `Source/__README - COLLABORATORS.txt`
- `.github/copilot-instructions.md`

### Useful Resources for Developers

- **Avalonia UI Patterns:** `Source/_AvaloniaUI Primer.txt`
- **Database Operations:** `Source/_DB_NOTES.txt`
- **Contributing Guidelines:** `Source/__README - COLLABORATORS.txt`

