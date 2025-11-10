# Libation Runtime Distribution Guide

## Problem

Users may encounter this error when trying to run Libation:
```
You must install .NET Desktop Runtime to run this application
```

This happens when the application bundles require the .NET runtime to be installed separately on the user's system.

## Solution: Self-Contained Bundles

The updated release script now creates **self-contained bundles** that include the .NET runtime. This means:

✅ **Users don't need to install .NET separately**
✅ **Bundles are larger but completely standalone**
✅ **Better user experience for non-developers**
⚠️ **Trade-off: Bundle size increases (includes 100-150 MB runtime per platform)**

## Distribution Options

### Option 1: Self-Contained Bundles (Recommended for Users)

**Includes .NET runtime - No installation needed**

```bash
./Scripts/create-release-bundles.sh 12.8.0
```

This creates self-contained bundles:
- `Libation.12.8.0-macOS-chardonnay-osx-x64.tgz` (~150-200 MB)
- `Libation.12.8.0-macOS-chardonnay-osx-arm64.tgz` (~150-200 MB)
- `Libation.12.8.0-Windows-chardonnay-win-x64.zip` (~150-200 MB)

Users simply extract and run - no prerequisites.

### Option 2: Framework-Dependent Bundles (Smaller, Requires .NET)

For users who already have .NET 9.0 Desktop Runtime installed:

```bash
./Scripts/create-release-bundles-framework-dependent.sh 12.8.0
```

This creates smaller bundles (~45-50 MB each) but requires users to install:
- [.NET 9.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)

## How to Fix the Runtime Error

### For End Users

#### Option A: Install .NET Runtime (Quick Fix)

1. Download [.NET 9.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
2. Install for your platform:
   - **Windows:** Run installer, restart
   - **macOS:** Run installer, restart
   - **Linux:** `sudo apt install dotnet-runtime-9.0` (Debian/Ubuntu)

Then run Libation again.

#### Option B: Use Self-Contained Bundle (Preferred)

Use the pre-built self-contained bundles which include the runtime:
- Download from GitHub releases
- Extract
- Run immediately (no installation needed)

### For Developers/Maintainers

#### Create Self-Contained Bundles

The release script has been updated to create self-contained bundles by default:

```bash
./Scripts/create-release-bundles.sh 12.8.0
```

This uses `--self-contained` flag when publishing.

#### Verify Self-Contained Status

Check if a build includes the runtime:

```bash
# Self-contained will have many runtime DLLs
ls -la output/Libation.*.tgz
tar -tzf Libation.12.8.0-macOS-chardonnay-osx-x64.tgz | grep -i "libc\|runtime" | head -20

# Framework-dependent will be much smaller
# and NOT include runtime libraries
```

## Technical Details

### Self-Contained Publishing

```bash
dotnet publish \
  -c Release \
  -p:RuntimeIdentifier=osx-x64 \
  --self-contained \
  -o ./output
```

**What gets included:**
- Application assemblies
- .NET runtime for the specific platform
- Native libraries (.dylib, .so, .dll)
- Configuration files

**Advantages:**
- Works immediately after extraction
- No dependency on system .NET installation
- Guaranteed runtime compatibility
- Better for non-technical users

**Disadvantages:**
- Larger bundle size (~150-200 MB per platform)
- Increased storage and bandwidth
- Multiple copies of runtime (one per platform)

### Framework-Dependent Publishing

```bash
dotnet publish \
  -c Release \
  -p:RuntimeIdentifier=osx-x64 \
  --no-self-contained \
  -o ./output
```

**Advantages:**
- Much smaller bundle size (~45-50 MB)
- Users can share single runtime installation
- Updates apply to all apps using that runtime

**Disadvantages:**
- Requires .NET runtime pre-installed
- More complex for non-technical users
- Dependency on specific .NET version

## Release Strategy

### Recommended Approach

**Publish both versions:**

1. **Self-Contained (Primary)**
   - Default for general users
   - Listed first on GitHub releases
   - Recommended download

2. **Framework-Dependent (Alternative)**
   - Listed as "Small/Portable" version
   - For users with .NET already installed
   - For bandwidth-constrained environments

## GitHub Release Instructions

When creating a release on GitHub:

1. **Primary Downloads** (Self-Contained)
   ```
   Libation.12.8.0-macOS-chardonnay-osx-x64.tgz
   Libation.12.8.0-Windows-chardonnay-win-x64.zip
   libation_12.8.0_amd64.deb  (already self-contained)
   libation-12.8.0-1.x86_64.rpm (already self-contained)
   ```

2. **Installation Notes**
   ```
   Extract and run immediately - no installation needed!
   
   For macOS:
   $ tar -xzf Libation.12.8.0-macOS-chardonnay-osx-x64.tgz
   $ open Libation.app
   
   For Windows:
   - Extract the .zip file
   - Run Libation.exe
   
   For Linux:
   $ sudo apt install ./libation_12.8.0_amd64.deb
   $ libation
   ```

## Troubleshooting

### Bundle Size Seems Wrong

Self-contained bundles are **intentionally larger** (~150-200 MB):

```bash
# Check bundle contents
tar -tzf Libation.12.8.0-macOS-chardonnay-osx-x64.tgz | wc -l

# Should include many runtime files
tar -tzf Libation.12.8.0-macOS-chardonnay-osx-x64.tgz | grep -i "system\|runtime\|clr" | head -10
```

### Still Getting Runtime Error

1. **Check .NET version:**
   ```bash
   dotnet --version  # Should show 9.0.x
   ```

2. **Verify bundle is self-contained:**
   ```bash
   # Extract and look for runtime files
   tar -xzf Libation.12.8.0-macOS-chardonnay-osx-x64.tgz
   ls -la Libation.app/Contents/MacOS/ | grep -i "runtime\|clr"
   
   # Should see many runtime-related files
   ```

3. **Check architecture match:**
   ```bash
   uname -m  # Should match bundle architecture
   ```

## Performance Considerations

### Runtime Overhead

Self-contained bundles have minimal performance overhead:
- Same .NET runtime included
- Identical execution performance
- Slightly longer startup for disk I/O

### Disk Space

On macOS/Windows/Linux, the overhead is:
- **Per application:** ~150-200 MB
- **If multiple .NET apps installed:** Each gets its own copy (unavoidable with self-contained)

## Future Considerations

### Trimming
Could reduce bundle size ~30% with:
```bash
-p:PublishTrimmed=true \
-p:TrimMode=link
```

Requires careful testing to ensure all features work.

### Single-File Bundles
Could create single executable:
```bash
-p:PublishSingleFile=true \
-p:IncludeNativeLibrariesForSelfExtract=true
```

Trade-off: Extraction overhead on first run.

## Verification Checklist

- [ ] Bundle extracts without errors
- [ ] Application runs immediately after extraction
- [ ] No "missing runtime" errors
- [ ] File permissions correct (executable bits set)
- [ ] Configuration files accessible
- [ ] Database operations work
- [ ] Network operations work
- [ ] No "missing .NET" warnings in logs

## Resources

- [.NET Publishing Overview](https://learn.microsoft.com/en-us/dotnet/core/deploying/)
- [Self-Contained Deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/overview#self-contained-deployment)
- [Framework-Dependent Apps](https://learn.microsoft.com/en-us/dotnet/core/deploying/overview#framework-dependent-apps)
- [Runtime Download](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)

---

**Updated:** November 10, 2025  
**Applies to:** Libation v12.8.0 and later  
**Status:** Self-contained bundles enabled by default
