#!/bin/bash
# Build DEB/RPM packages for Scaffolder
# Usage: ./scripts/build-packages.sh [version]

set -euo pipefail

VERSION="${1:-2.1.0}"
BUILD_DIR="$(mktemp -d)"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

echo "Building packages for v${VERSION} in ${BUILD_DIR}"

# First, build the binary
cd "${ROOT_DIR}"
dotnet publish Scaffolder/Scaffolder.csproj \
  -c Release -r linux-x64 --self-contained true \
  -p:PublishAot=true -p:StripSymbols=true -p:InvariantGlobalization=true \
  -o "${BUILD_DIR}/usr/local/bin"

cd "${BUILD_DIR}"

# --- DEB Package ---
echo "Creating DEB package..."
mkdir -p DEBIAN
cat > DEBIAN/control <<EOF
Package: scaffold
Version: ${VERSION}
Section: devel
Priority: optional
Architecture: amd64
Maintainer: Scaffolder Team <team@scaffolder.dev>
Description: CLI universel pour générer des projets
 C#, Python, JavaScript, TypeScript, Rust, Go, Ruby, PHP, Dart,
 Swift, Kotlin, Java, Zig, Scala, Elixir, Crystal, Haskell
Homepage: https://github.com/akaletekoffilevis/Scaffolder-CLI
EOF

dpkg-deb --build . "scaffold_${VERSION}_amd64.deb"
echo "DEB created: scaffold_${VERSION}_amd64.deb"

# --- RPM Package (using fpm if available) ---
if command -v fpm &> /dev/null; then
    echo "Creating RPM package..."
    fpm -s dir -t rpm \
      -n scaffold -v "${VERSION}" \
      --description "CLI universel pour générer des projets" \
      --url "https://github.com/akaletekoffilevis/Scaffolder-CLI" \
      --license "MIT" \
      --maintainer "Scaffolder Team <team@scaffolder.dev>" \
      usr/local/bin/scaffold=/usr/local/bin/scaffold
    echo "RPM created"
else
    echo "fpm not found, skipping RPM"
fi

# Copy artifacts to root
cp "${BUILD_DIR}"/*.deb "${ROOT_DIR}/" 2>/dev/null || true
cp "${BUILD_DIR}"/*.rpm "${ROOT_DIR}/" 2>/dev/null || true

echo "Done. Artifacts in ${ROOT_DIR}"
ls -la "${ROOT_DIR}"/scaffold* 2>/dev/null || true