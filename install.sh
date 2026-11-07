#!/usr/bin/env bash
# Universal install script for Scaffolder
# Usage: curl -fsSL https://github.com/akaletekoffilevis/Scaffolder-CLI/releases/latest/download/install.sh | bash
set -euo pipefail

REPO="akaletekoffilevis/Scaffolder-CLI"
INSTALL_DIR="${INSTALL_DIR:-/usr/local/bin}"
BINARY="scaffold"
API="https://api.github.com/repos/${REPO}/releases/latest"

OS="$(uname -s | tr '[:upper:]' '[:lower:]')"
ARCH="$(uname -m)"

case "$OS" in
  linux)
    case "$ARCH" in
      x86_64)
        if ldd --version 2>&1 | head -1 | grep -qi musl; then RID="linux-musl-x64"; else RID="linux-x64"; fi ;;
      aarch64|arm64) RID="linux-arm64" ;;
      *) echo "Architecture non supportee: $ARCH"; exit 1 ;;
    esac ;;
  darwin)
    case "$ARCH" in
      x86_64) RID="osx-x64" ;;
      arm64) RID="osx-arm64" ;;
      *) echo "Architecture non supportee: $ARCH"; exit 1 ;;
    esac ;;
  *) echo "OS non supporte: $OS"; exit 1 ;;
esac

ASSET="scaffold-${RID}.tar.gz"
URL="https://github.com/${REPO}/releases/latest/download/${ASSET}"

TMPDIR="$(mktemp -d)"
trap 'rm -rf "$TMPDIR"' EXIT

echo "Telechargement de $ASSET..."
curl -fLsS "$URL" -o "$TMPDIR/$ASSET"
tar -xzf "$TMPDIR/$ASSET" -C "$TMPDIR"

if [ ! -f "$TMPDIR/scaffold" ]; then
  echo "Binaire introuvable dans l'archive."; exit 1
fi
chmod +x "$TMPDIR/scaffold"

if [ -w "$INSTALL_DIR" ]; then
  mv "$TMPDIR/scaffold" "$INSTALL_DIR/$BINARY"
else
  echo "Installation dans $INSTALL_DIR (sudo requis)..."
  sudo mv "$TMPDIR/scaffold" "$INSTALL_DIR/$BINARY"
fi

echo "Scaffolder installe : $("$INSTALL_DIR/$BINARY" --version 2>/dev/null || echo ok)"
echo "Lancez : scaffold --help"
