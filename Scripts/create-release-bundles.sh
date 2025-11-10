#!/bin/bash

##############################################################################
# Libation Release Bundle Generator
# 
# This script automates the creation of platform-specific bundles for all
# supported platforms (macOS, Windows, Linux) for a given version.
#
# Usage: ./create-release-bundles.sh [VERSION] [OUTPUT_DIR]
#
# Examples:
#   ./create-release-bundles.sh 12.8.0
#   ./create-release-bundles.sh 12.8.0 /path/to/output
#
# The script will:
#   1. Validate input parameters
#   2. Create platform-specific published builds
#   3. Generate platform bundles (macOS .tgz, Windows .zip, Linux .deb/.rpm)
#   4. Verify bundle integrity
#   5. Generate a summary report
#
##############################################################################

set -e

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Script configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
TEMP_BUILD_DIR="${TEMP_BUILD_DIR:-/tmp/libation-builds}"
FINAL_OUTPUT_DIR="${2:-.}"
VERSION="${1:-}"

# Supported platforms (array of "name:rid" pairs)
PLATFORM_NAMES=("macOS_x64" "macOS_ARM64" "Windows_x64" "Linux_x64" "Linux_ARM64")
PLATFORM_RIDS=("osx-x64" "osx-arm64" "win-x64" "linux-x64" "linux-arm64")

##############################################################################
# Helper Functions
##############################################################################

print_header() {
    echo -e "${BLUE}╔════════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${BLUE}║${NC} $1"
    echo -e "${BLUE}╚════════════════════════════════════════════════════════════════╝${NC}"
}

print_section() {
    echo -e "\n${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${BLUE}▶${NC} $1"
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
}

print_success() {
    echo -e "${GREEN}✓${NC} $1"
}

print_error() {
    echo -e "${RED}✗${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}⚠${NC} $1"
}

print_info() {
    echo -e "${BLUE}ℹ${NC} $1"
}

##############################################################################
# Validation Functions
##############################################################################

validate_inputs() {
    if [ -z "$VERSION" ]; then
        print_error "Version number not provided"
        echo ""
        echo "Usage: $0 [VERSION] [OUTPUT_DIR]"
        echo ""
        echo "Examples:"
        echo "  $0 12.8.0"
        echo "  $0 12.8.0 /path/to/output"
        exit 1
    fi

    if [ ! -d "$PROJECT_ROOT" ]; then
        print_error "Project root not found: $PROJECT_ROOT"
        exit 1
    fi

    if [ ! -f "$PROJECT_ROOT/Source/Libation.sln" ]; then
        print_error "Libation.sln not found in $PROJECT_ROOT/Source/"
        exit 1
    fi

    print_success "Version: $VERSION"
    print_success "Project root: $PROJECT_ROOT"
    print_success "Output directory: $FINAL_OUTPUT_DIR"
}

check_prerequisites() {
    print_section "Checking Prerequisites"

    local missing_tools=()

    if ! command -v dotnet &> /dev/null; then
        missing_tools+=("dotnet")
    else
        print_success "dotnet found: $(dotnet --version)"
    fi

    if ! command -v tar &> /dev/null; then
        missing_tools+=("tar")
    else
        print_success "tar found"
    fi

    if ! command -v zip &> /dev/null; then
        missing_tools+=("zip")
    else
        print_success "zip found"
    fi

    if ! command -v unzip &> /dev/null; then
        missing_tools+=("unzip")
    else
        print_success "unzip found"
    fi

    # Check for bundle scripts
    if [ ! -f "$SCRIPT_DIR/Bundle_MacOS.sh" ]; then
        print_warning "Bundle_MacOS.sh not found (macOS bundles will be skipped)"
    else
        print_success "Bundle_MacOS.sh found"
    fi

    if [ ! -f "$SCRIPT_DIR/Bundle_Debian.sh" ]; then
        print_warning "Bundle_Debian.sh not found (Debian bundles will be skipped)"
    else
        print_success "Bundle_Debian.sh found"
    fi

    if [ ! -f "$SCRIPT_DIR/Bundle_Redhat.sh" ]; then
        print_warning "Bundle_Redhat.sh not found (RedHat bundles will be skipped)"
    else
        print_success "Bundle_Redhat.sh found"
    fi

    if [ ${#missing_tools[@]} -gt 0 ]; then
        print_error "Missing required tools: ${missing_tools[*]}"
        exit 1
    fi
}

##############################################################################
# Build Functions
##############################################################################

setup_build_directories() {
    print_section "Setting Up Build Directories"

    rm -rf "$TEMP_BUILD_DIR"
    mkdir -p "$TEMP_BUILD_DIR"
    print_success "Created temp build directory: $TEMP_BUILD_DIR"

    mkdir -p "$FINAL_OUTPUT_DIR"
    print_success "Created output directory: $FINAL_OUTPUT_DIR"
}

publish_platform_builds() {
    print_section "Publishing Platform-Specific Builds"

    local total_platforms=${#PLATFORM_RIDS[@]}
    local current=0

    for ((i = 0; i < total_platforms; i++)); do
        ((current++))
        local platform_name="${PLATFORM_NAMES[$i]}"
        local rid="${PLATFORM_RIDS[$i]}"
        local output_dir="$TEMP_BUILD_DIR/$rid"

        echo -e "\n${BLUE}[${current}/${total_platforms}]${NC} Publishing for $platform_name (RID: $rid)..."

        dotnet publish \
            "$PROJECT_ROOT/Source/LibationAvalonia/LibationAvalonia.csproj" \
            -c Release \
            -p:RuntimeIdentifier="$rid" \
            -o "$output_dir" \
            --no-self-contained \
            2>&1 | tail -5

        if [ -d "$output_dir" ] && [ "$(ls -A "$output_dir")" ]; then
            print_success "Published for $platform_name to $output_dir"
        else
            print_error "Failed to publish for $platform_name"
            exit 1
        fi
    done

    print_success "All platform builds published successfully"
}

##############################################################################
# Bundle Creation Functions
##############################################################################

create_macos_bundle() {
    print_info "Creating macOS bundle..."

    local rid="$1"
    local build_dir="$TEMP_BUILD_DIR/$rid"

    if [ ! -d "$build_dir" ]; then
        print_warning "Build directory not found for $rid, skipping macOS bundle"
        return
    fi

    if [ ! -f "$SCRIPT_DIR/Bundle_MacOS.sh" ]; then
        print_warning "Bundle_MacOS.sh not found, skipping macOS bundle"
        return
    fi

    # Make script executable
    chmod +x "$SCRIPT_DIR/Bundle_MacOS.sh"

    # Run the bundle script
    pushd "$FINAL_OUTPUT_DIR" > /dev/null
    bash "$SCRIPT_DIR/Bundle_MacOS.sh" "$build_dir" "$VERSION" "$rid" > /dev/null 2>&1
    popd > /dev/null

    # Find and verify the created bundle
    local bundle_file=$(ls -t "$FINAL_OUTPUT_DIR"/Libation.${VERSION}-macOS-*.tgz 2>/dev/null | head -1)
    if [ -f "$bundle_file" ]; then
        local size=$(du -h "$bundle_file" | cut -f1)
        print_success "macOS bundle created: $(basename "$bundle_file") ($size)"
        echo "$bundle_file"
    else
        print_warning "macOS bundle creation may have failed"
    fi
}

create_windows_bundle() {
    print_info "Creating Windows bundle..."

    local rid="$1"
    local build_dir="$TEMP_BUILD_DIR/$rid"

    if [ ! -d "$build_dir" ]; then
        print_warning "Build directory not found for $rid, skipping Windows bundle"
        return
    fi

    local bundle_name="Libation.${VERSION}-Windows-chardonnay-${rid}.zip"
    local bundle_path="$FINAL_OUTPUT_DIR/$bundle_name"

    # Create zip file
    pushd "$TEMP_BUILD_DIR" > /dev/null
    zip -r -q "$bundle_path" "$rid/"
    popd > /dev/null

    if [ -f "$bundle_path" ]; then
        local size=$(du -h "$bundle_path" | cut -f1)
        print_success "Windows bundle created: $bundle_name ($size)"
        echo "$bundle_path"
    else
        print_error "Failed to create Windows bundle"
    fi
}

create_linux_deb_bundle() {
    print_info "Creating Linux Debian bundle..."

    local rid="$1"
    local build_dir="$TEMP_BUILD_DIR/$rid"

    if [ ! -d "$build_dir" ]; then
        print_warning "Build directory not found for $rid, skipping Debian bundle"
        return
    fi

    if [ ! -f "$SCRIPT_DIR/Bundle_Debian.sh" ]; then
        print_warning "Bundle_Debian.sh not found, skipping Debian bundle"
        return
    fi

    # Make script executable
    chmod +x "$SCRIPT_DIR/Bundle_Debian.sh"

    # Run the bundle script
    pushd "$FINAL_OUTPUT_DIR" > /dev/null
    bash "$SCRIPT_DIR/Bundle_Debian.sh" "$build_dir" "$VERSION" "$rid" > /dev/null 2>&1
    popd > /dev/null

    # Find and verify the created bundle
    local bundle_file=$(ls -t "$FINAL_OUTPUT_DIR"/libation_${VERSION}*.deb 2>/dev/null | head -1)
    if [ -f "$bundle_file" ]; then
        local size=$(du -h "$bundle_file" | cut -f1)
        print_success "Debian bundle created: $(basename "$bundle_file") ($size)"
        echo "$bundle_file"
    else
        print_warning "Debian bundle creation may have failed"
    fi
}

create_linux_rpm_bundle() {
    print_info "Creating Linux RedHat bundle..."

    local rid="$1"
    local build_dir="$TEMP_BUILD_DIR/$rid"

    if [ ! -d "$build_dir" ]; then
        print_warning "Build directory not found for $rid, skipping RedHat bundle"
        return
    fi

    if [ ! -f "$SCRIPT_DIR/Bundle_Redhat.sh" ]; then
        print_warning "Bundle_Redhat.sh not found, skipping RedHat bundle"
        return
    fi

    # Make script executable
    chmod +x "$SCRIPT_DIR/Bundle_Redhat.sh"

    # Run the bundle script
    pushd "$FINAL_OUTPUT_DIR" > /dev/null
    bash "$SCRIPT_DIR/Bundle_Redhat.sh" "$build_dir" "$VERSION" "$rid" > /dev/null 2>&1
    popd > /dev/null

    # Find and verify the created bundle
    local bundle_file=$(ls -t "$FINAL_OUTPUT_DIR"/libation-${VERSION}*.rpm 2>/dev/null | head -1)
    if [ -f "$bundle_file" ]; then
        local size=$(du -h "$bundle_file" | cut -f1)
        print_success "RedHat bundle created: $(basename "$bundle_file") ($size)"
        echo "$bundle_file"
    else
        print_warning "RedHat bundle creation may have failed"
    fi
}

create_all_bundles() {
    print_section "Creating Release Bundles"

    declare -a bundle_files

    # Create macOS bundles
    if create_macos_bundle "osx-x64"; then
        bundle_files+=("$?")
    fi
    if create_macos_bundle "osx-arm64"; then
        bundle_files+=("$?")
    fi

    # Create Windows bundle
    if create_windows_bundle "win-x64"; then
        bundle_files+=("$?")
    fi

    # Create Linux Debian bundle
    if create_linux_deb_bundle "linux-x64"; then
        bundle_files+=("$?")
    fi

    # Create Linux RedHat bundle
    if create_linux_rpm_bundle "linux-x64"; then
        bundle_files+=("$?")
    fi
}

##############################################################################
# Verification and Reporting Functions
##############################################################################

verify_bundles() {
    print_section "Verifying Bundles"

    local bundle_count=0
    local total_size=0

    echo ""
    for file in "$FINAL_OUTPUT_DIR"/Libation.${VERSION}* "$FINAL_OUTPUT_DIR"/libation*; do
        if [ -f "$file" ] 2>/dev/null; then
            bundle_count=$((bundle_count + 1))
            local size=$(du -h "$file" | cut -f1)
            local basename=$(basename "$file")
            echo -e "${GREEN}✓${NC} $basename ($size)"

            # Add to total
            local bytes=$(du -b "$file" | cut -f1)
            total_size=$((total_size + bytes))
        fi
    done

    if [ $bundle_count -eq 0 ]; then
        print_warning "No bundles found in output directory"
    else
        print_success "Found $bundle_count bundle(s)"
        print_info "Total size: $(numfmt --to=iec-i --suffix=B $total_size 2>/dev/null || echo "$total_size bytes")"
    fi
}

generate_summary_report() {
    print_section "Release Bundle Summary"

    local report_file="$FINAL_OUTPUT_DIR/RELEASE_BUNDLES_v${VERSION}.txt"

    cat > "$report_file" << EOF
╔════════════════════════════════════════════════════════════════════════════╗
║                Libation v${VERSION} Release Bundles Report                        ║
╚════════════════════════════════════════════════════════════════════════════╝

Generated: $(date)
Version: ${VERSION}
Output Directory: ${FINAL_OUTPUT_DIR}

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
AVAILABLE BUNDLES
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

EOF

    for file in "$FINAL_OUTPUT_DIR"/Libation.${VERSION}* "$FINAL_OUTPUT_DIR"/libation*; do
        if [ -f "$file" ] 2>/dev/null; then
            local basename=$(basename "$file")
            local size=$(du -h "$file" | cut -f1)
            local checksum=$(md5sum "$file" 2>/dev/null | cut -d' ' -f1 || echo "N/A")
            echo "✓ $basename" >> "$report_file"
            echo "  Size: $size" >> "$report_file"
            echo "  MD5: $checksum" >> "$report_file"
            echo "" >> "$report_file"
        fi
    done

    cat >> "$report_file" << 'EOF'

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
INSTALLATION INSTRUCTIONS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

macOS:
  1. Extract the .tgz file: tar -xzf Libation.*.tgz
  2. Move Libation.app to /Applications/
  3. Run: /Applications/Libation.app/Contents/MacOS/Libation

Windows:
  1. Extract the .zip file
  2. Run: Libation.exe

Linux (Debian):
  1. Install: sudo apt install ./libation_*.deb
  2. Run: libation

Linux (RedHat):
  1. Install: sudo rpm -i libation-*.rpm
  2. Run: libation

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
DEPLOYMENT CHECKLIST
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

□ Verify all bundles are present and have correct sizes
□ Test bundle integrity on target platforms
□ Create GitHub release at https://github.com/rmcrackan/Libation/releases
□ Upload bundles to GitHub release
□ Update website with new version
□ Announce release on community channels
□ Monitor for bug reports

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

For more information, visit: https://github.com/rmcrackan/Libation
EOF

    print_success "Summary report created: RELEASE_BUNDLES_v${VERSION}.txt"
    cat "$report_file"
}

##############################################################################
# Cleanup Functions
##############################################################################

cleanup() {
    print_section "Cleanup"

    # Optional: Remove temp build directory
    if [ -d "$TEMP_BUILD_DIR" ]; then
        print_info "Cleaning up temporary build directory..."
        rm -rf "$TEMP_BUILD_DIR"
        print_success "Temporary files cleaned up"
    fi
}

##############################################################################
# Main Execution
##############################################################################

main() {
    print_header "Libation v${VERSION} Release Bundle Generator"

    validate_inputs
    check_prerequisites
    setup_build_directories
    publish_platform_builds
    create_all_bundles
    verify_bundles
    generate_summary_report
    cleanup

    echo ""
    print_success "Release bundle creation completed successfully!"
    echo ""
    print_info "Output directory: $FINAL_OUTPUT_DIR"
    print_info "For deployment, upload the bundles to GitHub releases:"
    echo "   https://github.com/rmcrackan/Libation/releases"
    echo ""
}

# Run main function
main "$@"
