#!/usr/bin/env bash
#
# Construit un paquet Debian (.deb) à partir d'une release FatouraDZ déjà publiée.
#
# Appelé normalement par scripts/build-release.sh ; utilisable seul :
#
#   scripts/build-deb.sh --binary artifacts/FatouraDZ-0.1.0-linux-x64
#
# dpkg-deb n'est pas utilisé : le paquet est assemblé avec ar et tar, qui sont
# présents partout (y compris sur un runner CI minimal), et les membres produits
# sont ceux attendus par dpkg (debian-binary, control.tar.gz, data.tar.gz).
#
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/.." && pwd)"

VERSION=""
ARCH=""
BINARY_DIR=""
OUTPUT_DIR="$REPO_ROOT/artifacts"

MAINTAINER="amuza2 <info@dzdevelopers.com>"

die() { printf 'error: %s\n' "$*" >&2; exit 1; }
info() { printf '\n==> %s\n' "$*"; }

usage() {
    sed -n '2,14p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'
    cat <<'EOF'

Options:
  --binary DIR       Dossier publié à empaqueter (obligatoire).
  -v, --version VER  Version du paquet (défaut : tag Git le plus proche, sinon le csproj).
  -a, --arch ARCH    amd64 ou arm64 (défaut : déduit du nom du dossier --binary).
  -o, --output DIR   Où écrire le .deb (défaut : artifacts/).
  -h, --help         Affiche cette aide.
EOF
    exit 0
}

while [ $# -gt 0 ]; do
    case "$1" in
        --binary)     BINARY_DIR="${2:-}"; shift 2 ;;
        -v|--version) VERSION="${2:-}"; shift 2 ;;
        -a|--arch)    ARCH="${2:-}"; shift 2 ;;
        -o|--output)  OUTPUT_DIR="${2:-}"; shift 2 ;;
        -h|--help)    usage ;;
        *)            die "option inconnue : $1 (essayez --help)" ;;
    esac
done

[ -n "$BINARY_DIR" ] || die "--binary DIR est obligatoire (essayez --help)"
[ -d "$BINARY_DIR" ] || die "dossier inexistant : $BINARY_DIR"
[ -f "$BINARY_DIR/fatouradz" ] || die "aucun binaire « fatouradz » dans $BINARY_DIR"

csproj_version() {
    sed -n 's:.*<Version>\([^<]*\)</Version>.*:\1:p' "$REPO_ROOT/src/FatouraDZ.csproj" | head -n1
}

if [ -z "$VERSION" ]; then
    tag="$(git -C "$REPO_ROOT" describe --tags --abbrev=0 2>/dev/null || true)"
    if [ -n "$tag" ]; then VERSION="${tag#v}"; else VERSION="$(csproj_version)"; fi
fi
[ -n "$VERSION" ] || die "version introuvable ; passez --version"

# Architecture Debian : déduite du RID quand --arch n'est pas fourni.
if [ -z "$ARCH" ]; then
    case "$(basename "$BINARY_DIR")" in
        *linux-arm64*) ARCH="arm64" ;;
        *)             ARCH="amd64" ;;
    esac
fi
case "$ARCH" in
    amd64|arm64) ;;
    *) die "architecture non gérée : $ARCH (attendu : amd64 ou arm64)" ;;
esac

command -v ar >/dev/null 2>&1 || die "ar est requis (paquet binutils)"

# --------------------------------------------------------------------------
# Arborescence du paquet
# --------------------------------------------------------------------------

WORK_DIR="$(mktemp -d)"
trap 'rm -rf "$WORK_DIR"' EXIT

PKG="$WORK_DIR/fatouradz"
mkdir -p "$PKG/DEBIAN" \
    "$PKG/usr/bin" \
    "$PKG/usr/share/applications" \
    "$PKG/usr/share/metainfo" \
    "$PKG/usr/share/doc/fatouradz"

info "Préparation du paquet $VERSION ($ARCH)"

install -m 755 "$BINARY_DIR/fatouradz" "$PKG/usr/bin/fatouradz"

if [ -d "$BINARY_DIR/share" ]; then
    cp -a "$BINARY_DIR/share/." "$PKG/usr/share/"
else
    install -Dm644 "$REPO_ROOT/packaging/fatouradz.desktop" \
        "$PKG/usr/share/applications/fatouradz.desktop"
    install -Dm644 "$REPO_ROOT/packaging/io.github.amuza2.fatouradz.metainfo.xml" \
        "$PKG/usr/share/metainfo/io.github.amuza2.fatouradz.metainfo.xml"
    for size in 48 64 128 256 512; do
        install -Dm644 "$REPO_ROOT/packaging/icons/fatouradz-$size.png" \
            "$PKG/usr/share/icons/hicolor/${size}x${size}/apps/fatouradz.png"
    done
    install -Dm644 "$REPO_ROOT/src/Assets/invoice.png" \
        "$PKG/usr/share/icons/fatouradz-source.png"
fi

# Les dépendances graphiques ne sont pas embarquées : elles viennent du système.
# ICU est en Recommends et non en Depends : son numéro de sonde varie selon la
# version de Debian (libicu72, libicu74, libicu76…), et une dépendance exacte
# rendrait le paquet ininstallable ailleurs. Il est présent sur toute
# installation de bureau ; sans lui, .NET se rabat sur une culture invariante et
# les formats français perdent leurs séparateurs.
DEPENDS="libc6, libx11-6, libice6, libsm6, libfontconfig1, libgl1"
RECOMMENDS="libicu72 | libicu74 | libicu76"

INSTALLED_SIZE="$(du -k -s "$PKG/usr" | cut -f1)"

cat > "$PKG/DEBIAN/control" <<EOF
Package: fatouradz
Version: $VERSION
Section: office
Priority: optional
Architecture: $ARCH
Maintainer: $MAINTAINER
Installed-Size: $INSTALLED_SIZE
Depends: $DEPENDS
Recommends: $RECOMMENDS
Homepage: https://github.com/amuza2/FatouraDZ
Description: Facturation pour les entreprises algériennes
 FatouraDZ permet de creer, gerer et exporter des factures conformes a la
 reglementation algerienne : plusieurs entreprises, clients, factures, avoirs
 et proformas, TVA a 19 % et 9 %, droit de timbre selon le bareme progressif en
 vigueur, retenue a la source, export PDF et Excel, et journal de comptabilite.
 .
 Les donnees restent sur le poste, dans une base SQLite locale.
EOF

if command -v md5sum >/dev/null 2>&1; then
    ( cd "$PKG" && find usr -type f -exec md5sum {} + | sed 's|  usr/|  usr/|' > DEBIAN/md5sums )
fi

install -m 644 "$REPO_ROOT/LICENSE" "$PKG/usr/share/doc/fatouradz/copyright"

# --------------------------------------------------------------------------
# Assemblage
# --------------------------------------------------------------------------

# dpkg attend exactement ces trois membres, dans cet ordre.
printf '2.0\n' > "$WORK_DIR/debian-binary"
( cd "$PKG/DEBIAN" && tar -czf "$WORK_DIR/control.tar.gz" --owner=0 --group=0 . )
( cd "$PKG" && tar -czf "$WORK_DIR/data.tar.gz" --owner=0 --group=0 --exclude=./DEBIAN . )

mkdir -p "$OUTPUT_DIR"
OUT="$OUTPUT_DIR/FatouraDZ-${VERSION}-${ARCH}.deb"
rm -f "$OUT"

( cd "$WORK_DIR" && ar rc "$OUT" debian-binary control.tar.gz data.tar.gz )

if command -v sha256sum >/dev/null 2>&1; then
    ( cd "$(dirname "$OUT")" && sha256sum "$(basename "$OUT")" > "$(basename "$OUT").sha256" )
fi

printf '\n'
ls -lh "$OUT"
