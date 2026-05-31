#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
BIN="$ROOT/bin/Release/net9.0"

echo "=============================================="
echo "  Scaffolder - Multi-OS AOT Build"
echo "=============================================="
echo ""

# Detect host OS
UNAME_S=$(uname -s)
UNAME_M=$(uname -m)

echo "Host: $UNAME_S / $UNAME_M"
echo ""

# ---- linux-x64 (glibc) ----
echo "[1/2] Publishing linux-x64 (glibc)..."
dotnet publish "$ROOT" -c Release -r linux-x64 --self-contained -o "$BIN/linux-x64/publish" 2>&1 | grep -E "(error|warning IL|->|Scaffolder ->)" || true
cp "$BIN/linux-x64/publish/scaffold" "$BIN/linux-x64/publish/scaffold-linux-x64"
echo "  Done: $BIN/linux-x64/publish/scaffold ($(ls -lh "$BIN/linux-x64/publish/scaffold" | awk '{print $5}'))"
echo ""

# ---- linux-musl-x64 (Alpine) ----
echo "[2/2] Publishing linux-musl-x64 (Alpine)..."
dotnet publish "$ROOT" -c Release -r linux-musl-x64 --self-contained -o "$BIN/linux-musl-x64/publish" 2>&1 | grep -E "(error|warning IL|->|Scaffolder ->)" || true
cp "$BIN/linux-musl-x64/publish/scaffold" "$BIN/linux-musl-x64/publish/scaffold-linux-musl-x64"
echo "  Done: $BIN/linux-musl-x64/publish/scaffold ($(ls -lh "$BIN/linux-musl-x64/publish/scaffold" | awk '{print $5}'))"
echo ""

# ---- win-x64 (needs mingw-w64) ----
if command -v x86_64-w64-mingw32-gcc &>/dev/null; then
  echo "[3/5] Publishing win-x64..."
  dotnet publish "$ROOT" -c Release -r win-x64 --self-contained -o "$BIN/win-x64/publish" 2>&1 | grep -E "(error|warning IL|->|Scaffolder ->)" || true
  cp "$BIN/win-x64/publish/scaffold.exe" "$BIN/win-x64/publish/scaffold-win-x64.exe"
  echo "  Done: $BIN/win-x64/publish/scaffold.exe ($(ls -lh "$BIN/win-x64/publish/scaffold.exe" | awk '{print $5}'))"
else
  echo "[3/5] win-x64 : ignored (x86_64-w64-mingw32-gcc not found)"
  echo "  Install mingw-w64 on Debian: sudo apt install mingw-w64"
  echo "  On Fedora: sudo dnf install mingw64-gcc"
fi
echo ""

# ---- osx-x64 (needs osxcross) ----
if command -v x86_64-apple-darwin-cc &>/dev/null || [ -n "${OSXCROSS_HOST+x}" ]; then
  echo "[4/5] Publishing osx-x64..."
  dotnet publish "$ROOT" -c Release -r osx-x64 --self-contained -o "$BIN/osx-x64/publish" 2>&1 | grep -E "(error|warning IL|->|Scaffolder ->)" || true
  cp "$BIN/osx-x64/publish/scaffold" "$BIN/osx-x64/publish/scaffold-osx-x64"
  echo "  Done: $BIN/osx-x64/publish/scaffold ($(ls -lh "$BIN/osx-x64/publish/scaffold" | awk '{print $5}'))"
else
  echo "[4/5] osx-x64 : ignored (osxcross toolchain not found)"
  echo "  Install osxcross: https://github.com/tpoechtrager/osxcross"
fi
echo ""

# ---- osx-arm64 (Apple Silicon, needs osxcross) ----
if command -v arm64-apple-darwin-cc &>/dev/null || [ -n "${OSXCROSS_HOST+x}" ]; then
  echo "[5/5] Publishing osx-arm64 (Apple Silicon)..."
  dotnet publish "$ROOT" -c Release -r osx-arm64 --self-contained -o "$BIN/osx-arm64/publish" 2>&1 | grep -E "(error|warning IL|->|Scaffolder ->)" || true
  cp "$BIN/osx-arm64/publish/scaffold" "$BIN/osx-arm64/publish/scaffold-osx-arm64"
  echo "  Done: $BIN/osx-arm64/publish/scaffold ($(ls -lh "$BIN/osx-arm64/publish/scaffold" | awk '{print $5}'))"
else
  echo "[5/5] osx-arm64 : ignored (osxcross toolchain not found)"
fi
echo ""

echo "=============================================="
echo "  Build complete!"
echo "=============================================="
echo ""
echo "Binaries:"
ls -lh "$BIN"/*/publish/scaffold* 2>/dev/null || true
echo ""

echo "To test locally:"
echo "  ./bin/Release/net9.0/linux-x64/publish/scaffold --help"
