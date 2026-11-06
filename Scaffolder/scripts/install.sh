#!/bin/bash
# Universal install script for Scaffolder
# Usage: curl -fsSL https://github.com/akaletekoffilevis/Scaffolder-CLI/releases/latest/download/install.sh | bash

set -euo pipefail

REPO="akaletekoffilevis/Scaffolder-CLI"
INSTALL_DIR="${INSTALL_DIR:-/usr/local/bin}"
BINARY_NAME="scaffold"
GITHUB_API="https://api.github.com/repos/${REPO}"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

log_info() { echo -e "${BLUE}[INFO]${NC} $*"; }
log_success() { echo -e "${GREEN}[OK]${NC} $*"; }
log_warn() { echo -e "${YELLOW}[WARN]${NC} $*"; }
log_error() { echo -e "${RED}[ERROR]${NC} $*"; }

detect_platform() {
    local os arch rid
    os=$(uname -s | tr '[:upper:]' '[:lower:]')
    arch=$(uname -m)

    case "$os" in
        linux)
            if [[ "$arch" == "x86_64" ]]; then
                if ldd --version 2>&1 | head -1 | grep -q musl; then
                    rid="linux-musl-x64"
                else
                    rid="linux-x64"
                fi
            elif [[ "$arch" == "aarch64" || "$arch" == "arm64" ]]; then
                rid="linux-arm64"
            else
                log_error "Architecture non supportée: $arch"
                exit 1
            fi
            ext="tar.gz"
            ;;
        darwin)
            if [[ "$arch" == "arm64" ]]; then
                rid="osx-arm64"
            else
                rid="osx-x64"
            fi
            ext="tar.gz"
            ;;
        *)
            log_error "OS non supporté: $os"
            exit 1
            ;;
    esac

    echo "$rid $ext"
}

get_latest_release() {
    curl -fsSL "${GITHUB_API}/releases/latest" | grep -o '"tag_name": "[^"]*' | cut -d'"' -f4
}

download_and_install() {
    local rid="$1" ext="$2" version="$3"
    local asset="scaffold-${rid}.${ext}"
    local url="https://github.com/${REPO}/releases/download/${version}/${asset}"
    local checksum_url="${url}.sha256"
    local tmpdir=$(mktemp -d)

    log_info "Téléchargement de ${asset}..."
    if ! curl -fsSL -o "${tmpdir}/${asset}" "$url"; then
        log_error "Échec du téléchargement depuis $url"
        exit 1
    fi

    log_info "Vérification SHA256..."
    if curl -fsSL -o "${tmpdir}/${asset}.sha256" "$checksum_url"; then
        if (cd "$tmpdir" && sha256sum -c "${asset}.sha256" 2>/dev/null); then
            log_success "Checksum OK"
        else
            log_warn "Checksum non vérifié (fichier corrompu ou absent)"
        fi
    else
        log_warn "Fichier checksum non trouvé, on continue..."
    fi

    log_info "Extraction..."
    if [[ "$ext" == "tar.gz" ]]; then
        tar -xzf "${tmpdir}/${asset}" -C "$tmpdir"
    else
        unzip -q "${tmpdir}/${asset}" -d "$tmpdir"
    fi

    local binary_path="${tmpdir}/scaffold"
    [[ -f "${tmpdir}/scaffold.exe" ]] && binary_path="${tmpdir}/scaffold.exe"

    if [[ ! -f "$binary_path" ]]; then
        log_error "Binaire non trouvé dans l'archive"
        exit 1
    fi

    log_info "Installation dans ${INSTALL_DIR}..."
    if [[ -w "$INSTALL_DIR" ]]; then
        mv "$binary_path" "${INSTALL_DIR}/${BINARY_NAME}"
        chmod +x "${INSTALL_DIR}/${BINARY_NAME}"
    else
        sudo mv "$binary_path" "${INSTALL_DIR}/${BINARY_NAME}"
        sudo chmod +x "${INSTALL_DIR}/${BINARY_NAME}"
    fi

    rm -rf "$tmpdir"
    log_success "Installé : ${INSTALL_DIR}/${BINARY_NAME}"
}

main() {
    echo "╔═══════════════════════════════════════╗"
    echo "║   Scaffolder - Universal Installer   ║"
    echo "╚═══════════════════════════════════════╝"
    echo

    local platform_info
    platform_info=$(detect_platform)
    local rid=$(echo "$platform_info" | cut -d' ' -f1)
    local ext=$(echo "$platform_info" | cut -d' ' -f2)

    log_info "Plateforme détectée: ${rid}"

    local version
    version=$(get_latest_release)
    log_info "Dernière version: ${version}"

    download_and_install "$rid" "$ext" "$version"

    echo
    log_success "Scaffolder installé avec succès !"
    echo
    echo "Utilisation :"
    echo "  scaffold new          # Créer un nouveau projet"
    echo "  scaffold add controller User  # Ajouter un composant"
    echo "  scaffold data status  # Voir la base chiffrée"
    echo "  scaffold ui           # Interface web"
    echo "  scaffold --help       # Aide complète"
    echo
    ${INSTALL_DIR}/${BINARY_NAME} --version
}

main "$@"