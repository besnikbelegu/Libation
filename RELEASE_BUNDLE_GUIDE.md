# Libation Release Bundle Creation Guide

This guide explains how to create release bundles for all supported platforms using the automated release script.

## Quick Start

```bash
cd /path/to/Libation
./Scripts/create-release-bundles.sh 12.8.0
```

This will create **self-contained bundles** for:
- macOS x64 (.tgz) - includes .NET runtime
- macOS ARM64 (.tgz) - includes .NET runtime
- Windows x64 (.zip) - includes .NET runtime
- Linux x64 (.deb) - includes .NET runtime
- Linux x64 (.rpm) - includes .NET runtime

**Users can extract and run immediately - no .NET installation needed!**

For detailed information about runtime distribution, see [RUNTIME_DISTRIBUTION_GUIDE.md](RUNTIME_DISTRIBUTION_GUIDE.md)

## Prerequisites

Before running the release script, ensure you have:

1. **dotnet CLI** - for building and publishing
   ```bash
   dotnet --version  # Should be 9.0 or later
   ```

2. **Standard Unix tools:**
   - `tar` - for creating .tgz files
   - `zip`/`unzip` - for creating .zip files
   - `bash` - for running scripts

3. **Build scripts in place:**
   - `Scripts/Bundle_MacOS.sh` - for macOS bundles
   - `Scripts/Bundle_Debian.sh` - for Debian bundles
   - `Scripts/Bundle_Redhat.sh` - for RedHat bundles

## Usage

### Basic Usage

```bash
./Scripts/create-release-bundles.sh VERSION
```

**Example:**
```bash
./Scripts/create-release-bundles.sh 12.8.0
```

### Advanced Usage

Specify a custom output directory:

```bash
./Scripts/create-release-bundles.sh VERSION /path/to/output
```

**Example:**
```bash
./Scripts/create-release-bundles.sh 12.8.0 ~/releases/v12.8.0
```

## What the Script Does

### 1. Validation
- Checks that version number is provided
- Verifies project structure
- Confirms all required tools are available

### 2. Build Setup
- Creates temporary build directories
- Creates final output directory

### 3. Publishing
Publishes platform-specific builds for:
- macOS x64 (Intel)
- macOS ARM64 (Apple Silicon)
- Windows x64
- Linux x64
- Linux ARM64

### 4. Bundle Creation
Generates platform-specific packages:

**macOS:** Libation.12.8.0-macOS-chardonnay-osx-x64.tgz
- Contains Libation.app bundle with all dependencies
- Properly signed and code-signed
- Ready for distribution

**Windows:** Libation.12.8.0-Windows-chardonnay-win-x64.zip
- Contains all binaries and dependencies
- Ready to extract and run

**Linux Debian:** libation_12.8.0_amd64.deb
- Debian package format
- Can be installed with `apt install`
- Includes systemd integration (if configured)

**Linux RedHat:** libation-12.8.0-1.x86_64.rpm
- RPM package format
- Can be installed with `rpm -i`
- Includes systemd integration (if configured)

### 5. Verification
- Confirms all bundles were created
- Reports bundle sizes
- Calculates total release size

### 6. Reporting
Generates `RELEASE_BUNDLES_v{VERSION}.txt` with:
- List of all created bundles
- File sizes
- MD5 checksums
- Installation instructions
- Deployment checklist

### 7. Cleanup
Removes temporary build directories (keeps final bundles)

## Output

All bundles are created in the output directory with this naming convention:

```
Libation.{VERSION}-{OS}-{VARIANT}-{RID}.{EXT}
libation_{VERSION}_{ARCH}.deb
libation-{VERSION}-{RELEASE}.{ARCH}.rpm
```

**Examples:**
```
Libation.12.8.0-macOS-chardonnay-osx-x64.tgz
Libation.12.8.0-macOS-chardonnay-osx-arm64.tgz
Libation.12.8.0-Windows-chardonnay-win-x64.zip
libation_12.8.0_amd64.deb
libation-12.8.0-1.x86_64.rpm
RELEASE_BUNDLES_v12.8.0.txt
```

## Release Workflow

### Step 1: Prepare Version
```bash
# Update version in AppScaffolding.csproj
# Create commit: git commit -m "Bump version to 12.8.0"
# Create tag: git tag -a v12.8.0 -m "Release v12.8.0"
# Push: git push origin master && git push origin v12.8.0
```

### Step 2: Build Bundles
```bash
cd /path/to/Libation
./Scripts/create-release-bundles.sh 12.8.0
```

### Step 3: Verify Bundles
```bash
cd output_directory
ls -lh Libation.12.8.0-*
cat RELEASE_BUNDLES_v12.8.0.txt
```

### Step 4: Create GitHub Release
1. Go to: https://github.com/rmcrackan/Libation/releases/new
2. Use tag: `v12.8.0`
3. Add release notes
4. Upload all bundles
5. Publish release

### Step 5: Announce
- Update website
- Post on community channels
- Send announcements to users

## Troubleshooting

### "dotnet command not found"
```bash
# Install .NET SDK from https://dotnet.microsoft.com/download
# Or verify dotnet is in PATH
which dotnet
```

### Bundle script not found
```bash
# Ensure all Bundle_*.sh scripts are in Scripts/ directory
ls -la Scripts/Bundle_*.sh
```

### Build fails for specific platform
```bash
# Try manual publish to debug
dotnet publish Source/LibationAvalonia/LibationAvalonia.csproj \
  -c Release \
  -p:RuntimeIdentifier=osx-x64 \
  -o /tmp/test-build
```

### Insufficient disk space
The script requires about 2-3 GB temporary space:
- Each platform build: ~200-300 MB
- With 5 platforms: ~1-1.5 GB
- Final bundles: ~200-250 MB total

Set custom temp directory:
```bash
TEMP_BUILD_DIR=/path/to/larger/disk ./Scripts/create-release-bundles.sh 12.8.0
```

## Customization

### Change output location
```bash
./Scripts/create-release-bundles.sh 12.8.0 /home/user/releases
```

### Change temp build directory
```bash
TEMP_BUILD_DIR=/mnt/fast-ssd ./Scripts/create-release-bundles.sh 12.8.0
```

### View detailed progress
Edit script and remove `> /dev/null 2>&1` from bundle creation commands.

## Platform-Specific Notes

### macOS
- Requires proper code signing setup
- Uses system native libraries
- Creates .app bundle structure
- Recommended: Notarize for distribution

### Windows
- Outputs as zip for easy distribution
- Requires .NET runtime on target system
- Can be made portable with appropriate settings

### Linux - Debian
- Uses dpkg format
- Installs to standard locations
- Creates systemd service (if configured)

### Linux - RedHat
- Uses RPM format
- Compatible with RHEL, CentOS, Fedora
- Creates systemd service (if configured)

## Best Practices

1. **Always test bundles** on target platforms before release
2. **Verify bundle sizes** are reasonable (watch for bloat)
3. **Check MD5 checksums** if manually distributing
4. **Document any platform-specific issues** in release notes
5. **Keep old bundles** for at least one major version as fallback
6. **Monitor download statistics** to detect issues

## Automation Tips

### Continuous Integration
Integrate into CI/CD pipeline:
```yaml
# Example GitHub Actions
- name: Create Release Bundles
  run: ./Scripts/create-release-bundles.sh ${{ github.ref_name }}
```

### Scheduled Builds
Create cron job for nightly builds:
```bash
0 2 * * * cd /path/to/Libation && ./Scripts/create-release-bundles.sh nightly-$(date +%Y%m%d) /var/releases/nightly
```

## Additional Resources

- [Libation GitHub Repository](https://github.com/rmcrackan/Libation)
- [Releases Page](https://github.com/rmcrackan/Libation/releases)
- [Documentation](https://github.com/rmcrackan/Libation/Documentation)
- [Build Scripts](https://github.com/rmcrackan/Libation/Scripts)

## Support

For issues with the release script:
1. Check troubleshooting section above
2. Run with verbose output (remove `> /dev/null` redirects)
3. Check .NET version compatibility
4. Verify all prerequisites are installed
5. Open an issue on GitHub with error details

---

**Last Updated:** November 10, 2025  
**Script Version:** 1.0  
**Libation Version:** 12.8.0+
