#!/usr/bin/env bash
#
# Construit une AppImage à partir d'une release FatouraDZ déjà publiée.
#
# Appelé normalement par scripts/build-release.sh ; utilisable seul :
#
#   scripts/build-appimage.sh --binary artifacts/FatouraDZ-0.1.0-linux-x64
#
# appimagetool n'est pas fourni par les distributions : il est téléchargé à la
# demande (voir fetch_appimagetool) sauf si --appimagetool pointe sur une copie
# locale.
#
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/.." && pwd)"

VERSION=""
ARCH=""
BINARY_DIR=""
OUTPUT_DIR="$REPO_ROOT/artifacts"
APPIMAGETOOL=""
TOOL_CACHE="${XDG_CACHE_HOME:-$HOME/.cache}/fatouradz"

die() { printf 'error: %s\n' "$*" >&2; exit 1; }
info() { printf '\n==> %s\n' "$*"; }
warn() { printf 'warning: %s\n' "$*" >&2; }

usage() {
    sed -n '2,13p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'
    cat <<'EOF'

Options:
  --binary DIR          Dossier publié à empaqueter (obligatoire).
  -v, --version VER     Version à inscrire dans le nom (défaut : tag Git le plus proche, sinon le csproj).
  -a, --arch ARCH       Architecture : x86_64 ou aarch64 (défaut : celle de la machine).
  -o, --output DIR      Où écrire le .AppImage (défaut : artifacts/).
      --appimagetool P  Utiliser cet appimagetool au lieu d'en télécharger un.
      --tool-cache DIR  Cache du téléchargement de appimagetool.
  -h, --help            Affiche cette aide.
EOF
    exit 0
}

while [ $# -gt 0 ]; do
    case "$1" in
        --binary)        BINARY_DIR="${2:-}"; shift 2 ;;
        -v|--version)    VERSION="${2:-}"; shift 2 ;;
        -a|--arch)       ARCH="${2:-}"; shift 2 ;;
        -o|--output)     OUTPUT_DIR="${2:-}"; shift 2 ;;
        --appimagetool)  APPIMAGETOOL="${2:-}"; shift 2 ;;
        --tool-cache)    TOOL_CACHE="${2:-}"; shift 2 ;;
        -h|--help)       usage ;;
        *)               die "option inconnue : $1 (essayez --help)" ;;
    esac
done

[ -n "$BINARY_DIR" ] || die "--binary DIR est obligatoire (essayez --help)"
[ -d "$BINARY_DIR" ] || die "dossier inexistant : $BINARY_DIR"
[ -f "$BINARY_DIR/fatouradz" ] || die "aucun binaire « fatouradz » dans $BINARY_DIR"

host_arch() {
    case "$(uname -m)" in
        x86_64|amd64)  echo x86_64 ;;
        aarch64|arm64) echo aarch64 ;;
        *)             echo unknown ;;
    esac
}

# L'architecture de l'AppImage se déduit du RID du dossier publié : appimagetool
# ne sait pas recompiler une application, les deux doivent correspondre.
if [ -z "$ARCH" ]; then
    case "$(basename "$BINARY_DIR")" in
        *linux-arm64*) ARCH="aarch64" ;;
        *linux-x64*)   ARCH="x86_64" ;;
        *)             ARCH="$(host_arch)" ;;
    esac
fi
case "$ARCH" in
    x86_64|aarch64) ;;
    *) die "architecture non gérée : $ARCH — appimagetool ne fournit que x86_64/aarch64/i686/armhf" ;;
esac

csproj_version() {
    sed -n 's:.*<Version>\([^<]*\)</Version>.*:\1:p' "$REPO_ROOT/src/FatouraDZ.csproj" | head -n1
}

if [ -z "$VERSION" ]; then
    tag="$(git -C "$REPO_ROOT" describe --tags --abbrev=0 2>/dev/null || true)"
    if [ -n "$tag" ]; then VERSION="${tag#v}"; else VERSION="$(csproj_version)"; fi
fi
[ -n "$VERSION" ] || die "version introuvable ; passez --version"

# --------------------------------------------------------------------------
# appimagetool
# --------------------------------------------------------------------------

# appimagetool n'a pas de releases versionnées : « continuous » est le seul tag,
# et ses fichiers sont reconstruits sur place. Épingler une empreinte casserait
# donc chaque release dès qu'en amont on reconstruit : à la place, l'empreinte
# publiée par l'API GitHub est récupérée et le téléchargement est vérifié contre
# elle. Cela détecte un téléchargement tronqué ou substitué ; APPIMAGETOOL_SHA256
# permet d'épingler exactement, et --appimagetool d'utiliser un binaire de confiance.
appimagetool_digest() {
    local asset="$1"
    command -v python3 >/dev/null 2>&1 || return 0
    curl -fsSL -m 30 \
        "https://api.github.com/repos/AppImage/appimagetool/releases/tags/continuous" 2>/dev/null \
        | python3 -c '
import json, sys
try:
    data = json.load(sys.stdin)
except Exception:
    sys.exit(0)
want = sys.argv[1]
for asset in data.get("assets", []):
    if asset.get("name") == want:
        digest = asset.get("digest") or ""
        print(digest.split(":", 1)[1] if ":" in digest else "")
        break
' "$asset" 2>/dev/null || true
}

sha256_of() {
    if command -v sha256sum >/dev/null 2>&1; then sha256sum "$1" | cut -d' ' -f1
    elif command -v shasum >/dev/null 2>&1; then shasum -a 256 "$1" | cut -d' ' -f1
    else openssl dgst -sha256 "$1" | awk '{print $NF}'
    fi
}

fetch_appimagetool() {
    local asset="appimagetool-${ARCH}.AppImage"
    local dest="$TOOL_CACHE/$asset"
    local url="https://github.com/AppImage/appimagetool/releases/download/continuous/$asset"

    if [ ! -x "$dest" ]; then
        # La progression va sur stderr : la sortie de cette fonction est sa valeur
        # de retour.
        printf '\n==> Téléchargement de %s\n' "$asset" >&2
        mkdir -p "$TOOL_CACHE"
        # Téléchargement à côté de la cible : une exécution échouée ne laisse pas
        # un binaire à moitié écrit que la suivante tenterait d'exécuter.
        curl -fL --retry 3 --connect-timeout 20 -o "$dest.part" "$url" \
            || die "téléchargement impossible : $url"
        [ -s "$dest.part" ] || { rm -f "$dest.part"; die "$asset téléchargé est vide"; }
        mv "$dest.part" "$dest"
        chmod +x "$dest"
    fi

    local expected="${APPIMAGETOOL_SHA256:-}"
    [ -n "$expected" ] || expected="$(appimagetool_digest "$asset")"
    if [ -n "$expected" ]; then
        local actual
        actual="$(sha256_of "$dest")"
        [ "$actual" = "$expected" ] \
            || die "empreinte de $asset incorrecte
  attendu $expected
  obtenu  $actual
Supprimez $dest et réessayez, ou passez --appimagetool avec votre propre copie."
    else
        warn "empreinte publiée de $asset introuvable ; utilisation sans vérification"
        warn "sha256($asset) = $(sha256_of "$dest")"
    fi

    printf '%s' "$dest"
}

if [ -z "$APPIMAGETOOL" ]; then
    APPIMAGETOOL="$(fetch_appimagetool)"
fi
[ -x "$APPIMAGETOOL" ] || die "appimagetool n'est pas exécutable : $APPIMAGETOOL"

# appimagetool est lui-même une AppImage : il a besoin de FUSE pour se monter.
# Les conteneurs et les runners CI en sont souvent dépourvus, d'où le repli sur le
# mode « extract-and-run » intégré plutôt qu'une erreur de montage incompréhensible.
appimagetool_cmd() {
    if "$APPIMAGETOOL" --version >/dev/null 2>&1; then
        "$APPIMAGETOOL" "$@"
    else
        "$APPIMAGETOOL" --appimage-extract-and-run "$@"
    fi
}

# --------------------------------------------------------------------------
# AppDir
# --------------------------------------------------------------------------

WORK_DIR="$(mktemp -d)"
trap 'rm -rf "$WORK_DIR"' EXIT

APPDIR="$WORK_DIR/FatouraDZ.AppDir"
info "Construction de l'AppDir pour $ARCH"

mkdir -p "$APPDIR/usr/bin" "$APPDIR/usr/share"

install -m 755 "$BINARY_DIR/fatouradz" "$APPDIR/usr/bin/fatouradz"

# Fichiers d'intégration au bureau : pris dans le dossier de release quand ils y
# sont (build-release.sh les y dépose), sinon dans le dépôt.
if [ -d "$BINARY_DIR/share" ]; then
    cp -a "$BINARY_DIR/share/." "$APPDIR/usr/share/"
else
    install -Dm644 "$REPO_ROOT/packaging/fatouradz.desktop" \
        "$APPDIR/usr/share/applications/fatouradz.desktop"
    install -Dm644 "$REPO_ROOT/packaging/io.github.amuza2.fatouradz.metainfo.xml" \
        "$APPDIR/usr/share/metainfo/io.github.amuza2.fatouradz.metainfo.xml"
    for size in 48 64 128 256 512; do
        install -Dm644 "$REPO_ROOT/packaging/icons/fatouradz-$size.png" \
            "$APPDIR/usr/share/icons/hicolor/${size}x${size}/apps/fatouradz.png"
    done
    install -Dm644 "$REPO_ROOT/src/Assets/invoice.png" \
        "$APPDIR/usr/share/icons/fatouradz-source.png"
fi

# appimagetool attend le .desktop et une icône portant son nom à la racine de
# l'AppDir, et déduit l'identifiant AppStream et les informations de mise à jour
# du .desktop racine. Le nom doit correspondre exactement à Icon= (fatouradz).
install -Dm644 "$APPDIR/usr/share/applications/fatouradz.desktop" \
    "$APPDIR/fatouradz.desktop"
install -Dm755 "$REPO_ROOT/packaging/AppRun" "$APPDIR/AppRun"
install -Dm644 "$REPO_ROOT/packaging/icons/fatouradz-256.png" "$APPDIR/fatouradz.png"

# .DirIcon : c'est l'icône affichée par les gestionnaires de fichiers.
cp "$APPDIR/fatouradz.png" "$APPDIR/.DirIcon"

# --------------------------------------------------------------------------
# Assemblage
# --------------------------------------------------------------------------

mkdir -p "$OUTPUT_DIR"
OUT="$OUTPUT_DIR/FatouraDZ-${VERSION}-${ARCH}.AppImage"
rm -f "$OUT"

info "Exécution de appimagetool"
ARCH="$ARCH" NO_STRIP="${NO_STRIP:-1}" \
    appimagetool_cmd "$APPDIR" "$OUT" \
    || die "appimagetool a échoué"

chmod 755 "$OUT"

printf '%s  %s\n' "$(sha256_of "$OUT")" "$(basename "$OUT")" > "$OUT.sha256"

printf '\n'
info "Construit : $(basename "$OUT")"
ls -lh "$OUT"
