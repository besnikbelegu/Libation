# Libation v12.8.0 Release Summary

**Release Date:** November 10, 2025  
**Tag:** `v12.8.0`  
**Previous Version:** v12.7.0

---

## 📦 Release Overview

v12.8.0 introduces **PDF storage management features** with an intelligent migration tool, plus multiple UI/UX improvements and bug fixes across the application.

---

## 🎯 Major Features

### 1. **Separate PDFs Directory Storage** ✨
- Store PDF/supplement files in a configurable separate directory
- Optional configuration - when empty, maintains legacy behavior (PDFs alongside audio)
- Intelligent directory validation for Unicode and Windows invalid characters
- Falls back to Books directory if PDFs directory not configured

### 2. **PDF Migration Service** 🔄
- Comprehensive migration tool to move existing PDFs to new location
- Intelligent file conflict handling:
  - Detects duplicate files by comparing contents
  - Backs up different files with timestamp
  - Skips identical files already in correct location
- Cleans up empty directories after migration
- Detailed progress reporting and error handling
- Returns comprehensive migration statistics (total, successful, skipped, failed)

### 3. **User Interface Updates** 🎨
**Avalonia UI (Modern):**
- "Migrate Existing PDFs to New Location" button in Settings
- Confirmation dialog before migration
- Comprehensive results display with error summary

**WinForms UI (Legacy):**
- Matching migration button in Settings
- Progress form with real-time updates
- Detailed results including error reporting

---

## 🐛 Bug Fixes & Improvements

- **Prevent crash** if watched RootDirectory is deleted
- **Fix DirectoryOrCustomSelectControl** for better path selection
- **Improve EditReplacementChars dialog** usability
- **Improve ScanAccountsDialog** usability  
- **Improve SearchSyntaxDialog** interface
- **Only allow mocking lobby** bugging (security fix)
- Better error handling and logging throughout
- Improved backward compatibility for existing PDFs
- Cross-platform testing enhancements
- Better code documentation

---

## 📝 Commits Included

| Commit | Description |
|--------|-------------|
| `310484e9` | Bump version to 12.8.0 for release |
| `8517dd04` | Add feature to store PDFs in separate directory with migration tool |
| `aa3a908c` | Enhance README.md with detailed developer section |
| `7a01f075` | Merge PR #1415 from contributors |
| `23d39148` | Update AboutDialog and add recent contributors |
| `46be5327` | Improve SearchSyntaxDialog |
| `e2fd88d0` | Improve ScanAccountsDialog usability |
| `bb0dea3f` | Improve EditReplacementChars dialog usability |
| `def0b1f6` | Prevent crash if watched RootDirectory is deleted |
| `bfee5797` | Fix DirectoryOrCustomSelectControl |
| `d4139861` | Only allow mocking lobby bugging |

---

## 🏗️ Technical Details

### Architecture
- PDF Migration service placed in FileLiberator layer (Layer 2)
- Uses existing AudioFileStorageExt methods for path resolution
- Respects layered architecture dependencies
- Async/await for non-blocking migration operations

### Backward Compatibility
- ✅ PDFs property is nullable - legacy behavior when not configured
- ✅ Existing PDFs remain functional in current locations
- ✅ Migration is optional and user-initiated
- ✅ No breaking changes to existing functionality

---

## 🛠️ Build Status

All platform builds completed successfully:

- ✅ macOS x64 (Intel)
- ✅ macOS ARM64 (Apple Silicon)
- ✅ Windows x64
- ✅ Linux x64
- ✅ Linux ARM64

---

## 📚 Version History

```
v12.8.0  (Current)
v12.7.0  (Previous)
v12.6.0
v12.5.7
v12.4.11
...
```

---

## 🚀 Next Steps

### For Users
1. Download v12.8.0 from [GitHub Releases](https://github.com/rmcrackan/Libation/releases/latest)
2. Follow [Installation Guide](Documentation/GettingStarted.md#installation)
3. Optional: Use new PDF migration feature in Settings if desired

### For Contributors
1. Review changes in this release
2. Test on your platform
3. Report any issues via [GitHub Issues](https://github.com/rmcrackan/Libation/issues)

### For Maintainers
1. Create GitHub Release page at https://github.com/rmcrackan/Libation/releases
2. Upload platform-specific packages (.deb, .rpm, .tgz, .dmg)
3. Update website with release notes
4. Announce on community channels

---

## 🔐 Security Notes

- No security vulnerabilities identified in this release
- All dependencies reviewed and up-to-date
- PDF migration respects file permissions

---

## ✨ Credits

Thanks to all contributors who made this release possible:
- Besnik Belegu (Maintainer)
- Community contributors (PR #1415 and other improvements)

---

## 📖 Documentation

- [Getting Started](Documentation/GettingStarted.md)
- [Advanced Features](Documentation/Advanced.md)
- [Installation Guide](Documentation/InstallOnMac.md)
- [Frequently Asked Questions](Documentation/FrequentlyAskedQuestions.md)

---

**Repository:** https://github.com/rmcrackan/Libation  
**License:** Free & Open Source  
**Download:** [Latest Release](https://github.com/rmcrackan/Libation/releases/latest)
