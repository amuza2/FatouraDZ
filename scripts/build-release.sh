#!/usr/bin/env bash
#
# Construction des livrables FatouraDZ.
#
# Produit, dans artifacts/ par défaut :
#
#   FatouraDZ-<version>-<rid>/                dossier de release (Linux)
#   FatouraDZ-<version>-<rid>.tar.gz          le même, archivé
#   FatouraDZ-<version>-<rid>.tar.gz.sha256
#   FatouraDZ-<version>-<arch>.AppImage       (sautée avec --no-appimage)
#   FatouraDZ-<version>-<arch>.AppImage.sha256
#   FatouraDZ-<version>-<arch>.deb            (sautée avec --no-deb)
#   FatouraDZ-<version>-<arch>.deb.sha256
#   FatouraDZ-<version>-<rid>.zip             portable Windows (voir --no-windows)
#   FatouraDZ-<version>-<rid>.zip.sha256
#
# Le runtime .NET est embarqué : aucun artefact n'a besoin de « dotnet-runtime »
# installé. Les bibliothèques graphiques (X11, OpenGL, fontconfig) et ICU ne sont
# PAS embarquées, volontairement : l'application utilise celles du système, comme
# toute application de bureau. Un -p:InvariantGlobalization serait nécessaire pour
# s'en passer, au prix des formats français.
#
# L'installateur Windows (.exe) est construit par le job Windows de
# .github/workflows/release.yml : le compilateur Inno Setup n'existe que sous
# Windows. Cette machine croisée produit en revanche le dossier publié et le ZIP
# portables, le SDK .NET sachant publier pour win-x64/win-arm64 depuis Linux.
#
# Usage : scripts/build-release.sh [options]
#
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/.." && pwd)"
PROJECT="$REPO_ROOT/src/FatouraDZ.csproj"

OUTPUT_DIR="$REPO_ROOT/artifacts"
LINUX_ARCHS="linux-x64"
WINDOWS_ARCHS="win-x64"
VERSION=""
SINGLE_FILE=1
COMPRESS=1
BUILD_LINUX=1
BUILD_WINDOWS=1
BUILD_APPIMAGE=1
BUILD_DEB=1

die() { printf 'error: %s\n' "$*" >&2; exit 1; }
info() { printf '\n==> %s\n' "$*"; }
warn() { printf 'warning: %s\n' "$*" >&2; }

usage() {
    sed -n '2,28p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'
    cat <<'EOF'

Options:
  -v, --version VER        Version à construire (défaut : tag Git le plus proche,
                           sinon <Version> du csproj). Transmise au compilateur,
                           pour que l'écran « À propos » corresponde au nom de
                           l'artefact.
  -a, --archs "RID ..."    Identifiants de runtime Linux (défaut : linux-x64).
                           Ex. --archs "linux-x64 linux-arm64"
  -w, --windows-archs "RID ..."
                           Identifiants de runtime Windows (défaut : win-x64).
      --no-linux           Ne construire que les artefacts Windows.
      --no-windows         Ne construire que les artefacts Linux.
      --no-appimage        Sauter l'AppImage (construite pour l'architecture de
                           la machine, et nécessite le réseau une fois).
      --no-deb             Sauter le paquet .deb.
  -o, --output DIR         Dossier de sortie (défaut : artifacts/).
      --no-single-file     Publier un dossier de fichiers au lieu d'un binaire :
                           démarrage plus rapide, mais beaucoup de fichiers.
      --no-compress        Désactiver la compression du binaire unique.
  -h, --help               Affiche cette aide.
EOF
    exit 0
}

while [ $# -gt 0 ]; do
    case "$1" in
        -v|--version)      VERSION="${2:-}"; shift 2 ;;
        -a|--archs)        LINUX_ARCHS="${2:-}"; shift 2 ;;
        -w|--windows-archs) WINDOWS_ARCHS="${2:-}"; shift 2 ;;
        -o|--output)       OUTPUT_DIR="${2:-}"; shift 2 ;;
        --no-linux)        BUILD_LINUX=0; shift ;;
        --no-windows)      BUILD_WINDOWS=0; shift ;;
        --no-appimage)     BUILD_APPIMAGE=0; shift ;;
        --no-deb)          BUILD_DEB=0; shift ;;
        --no-single-file)  SINGLE_FILE=0; shift ;;
        --no-compress)     COMPRESS=0; shift ;;
        -h|--help)         usage ;;
        *)                 die "option inconnue : $1 (essayez --help)" ;;
    esac
done

command -v dotnet >/dev/null 2>&1 || die "dotnet n'est pas dans le PATH"

# Icône maîtresse unique : l'icône de la fenêtre, celle de l'exécutable Windows,
# l'entrée de bureau, l'AppImage et le .deb en dérivent (scripts/gen-icons.sh).
MASTER_ICON_RELATIVE="src/Assets/invoice.png"
MASTER_ICON="$REPO_ROOT/$MASTER_ICON_RELATIVE"
# L'icône Windows est un fichier distinct, généré depuis la source ci-dessus.
ICO_RELATIVE="src/Assets/fatouradz.ico"
ICO="$REPO_ROOT/$ICO_RELATIVE"

csproj_version() {
    sed -n 's:.*<Version>\([^<]*\)</Version>.*:\1:p' "$PROJECT" | head -n1
}

SHA256SUM=""
if command -v sha256sum >/dev/null 2>&1; then SHA256SUM="sha256sum"
elif command -v shasum >/dev/null 2>&1; then SHA256SUM="shasum -a 256"
fi

# Écrit depuis le dossier de sortie, pour que le fichier référence le nom nu de
# l'artefact et que `sha256sum -c` fonctionne là où il a été téléchargé.
write_checksum() {
    [ -n "$SHA256SUM" ] || return 0
    local dir name
    dir="$(dirname "$1")"
    name="$(basename "$1")"
    ( cd "$dir" && $SHA256SUM "$name" > "$name.sha256" )
}

# zip n'est pas installé partout ; le module zipfile de Python produit une archive
# équivalente, sans dépendance supplémentaire.
creer_zip() {
    local archive="$1" contenu="$2"

    if command -v zip >/dev/null 2>&1; then
        rm -f "$archive"
        ( cd "$(dirname "$contenu")" && zip -qr "$archive" "$(basename "$contenu")" )
        return 0
    fi

    command -v python3 >/dev/null 2>&1 \
        || die "ni zip ni python3 pour créer $archive"
    rm -f "$archive"
    ( cd "$(dirname "$contenu")" && python3 -m zipfile -c "$archive" "$(basename "$contenu")" )
}

# Les icônes empaquetées sont générées depuis l'icône maîtresse et versionnées :
# une release doit refuser de partir avec un jeu d'icônes périmé, parce que
# l'erreur est publique (elle touche tous les utilisateurs) et coûte une nouvelle
# version à corriger. La vérification est partagée avec la CI, qui l'exécute à
# chaque PR : elle est donc dans son propre script.
verifier_icones() {
    local script="$SCRIPT_DIR/verifier-icones.sh"

    [ -x "$script" ] || die "vérification des icônes introuvable : $script"
    "$script" || die "jeu d'icônes non conforme (voir les messages ci-dessus)"
}

CSPROJ_VERSION="$(csproj_version)"

if [ -z "$VERSION" ]; then
    tag="$(git -C "$REPO_ROOT" describe --tags --abbrev=0 2>/dev/null || true)"
    if [ -n "$tag" ]; then
        VERSION="${tag#v}"
    else
        VERSION="$CSPROJ_VERSION"
        warn "aucun tag Git trouvé ; version du csproj utilisée : $VERSION"
    fi
fi

[ -n "$VERSION" ] || die "version indéterminée ; passez --version"

verifier_icones

info "Construction de FatouraDZ $VERSION"

# Le <Version> du csproj est ce que rapporte un simple `dotnet build`, tandis
# qu'une release correspond au tag téléchargé par l'utilisateur. -p:Version écrase
# la valeur pour cette construction afin que les deux concordent.
if [ "$VERSION" != "$CSPROJ_VERSION" ]; then
    warn "version $VERSION différente de <Version>$CSPROJ_VERSION</Version> dans le csproj"
fi

# Une release se construit normalement sur un tag. Sinon le nom de l'artefact
# annonce quand même la version du tag le plus proche : autant le dire plutôt que
# de livrer un binaire mal étiqueté.
if command -v git >/dev/null 2>&1 && git -C "$REPO_ROOT" rev-parse --git-dir >/dev/null 2>&1; then
    if ! git -C "$REPO_ROOT" describe --tags --exact-match >/dev/null 2>&1; then
        warn "HEAD n'est pas exactement sur un tag — révision non publiée"
    fi
    if [ -n "$(git -C "$REPO_ROOT" status --porcelain)" ]; then
        warn "l'arbre de travail contient des modifications non validées"
    fi
fi

# --------------------------------------------------------------------------
# Publication
# --------------------------------------------------------------------------

publish() {
    local rid="$1" stage="$2"

    local publish_args=(
        --nologo -v minimal
        -c Release
        -r "$rid"
        --self-contained true
        -o "$stage"
        # L'écran « À propos » lit la version dans l'assembly : elle doit être
        # celle de la release, pas celle par défaut du csproj.
        -p:Version="$VERSION"
        # Pas de .pdb dans un artefact destiné à l'utilisateur ; les sources et le
        # tag sont sur GitHub.
        -p:DebugType=none
        # Le trimming doit rester désactivé : Avalonia résout les vues et les
        # styles par réflexion, et une publication trimmée les perd silencieusement
        # (fenêtres vides, styles manquants) au lieu d'échouer.
        -p:PublishTrimmed=false
        # ReadyToRun est refusé en compilation croisée pour certaines cibles, et
        # n'apporte que peu ici par rapport à un bundle compressé.
        -p:PublishReadyToRun=false
    )

    if [ "$SINGLE_FILE" -eq 1 ]; then
        publish_args+=(
            -p:PublishSingleFile=true
            # Obligatoire sous Linux : sans cela, les bibliothèques natives Skia
            # restent en fichiers séparés, ce qui annule l'intérêt du binaire unique.
            -p:IncludeNativeLibrariesForSelfExtract=true
        )
        if [ "$COMPRESS" -eq 1 ]; then
            # Divise à peu près par deux le téléchargement. Le bundle est décompressé
            # une seule fois dans DOTNET_BUNDLE_EXTRACT_BASE_DIR (voir packaging/AppRun),
            # donc seul le premier lancement en paie le prix.
            publish_args+=(-p:EnableCompressionInSingleFile=true)
        fi
    fi

    dotnet publish "$PROJECT" "${publish_args[@]}"
}

# Les fichiers d'intégration au bureau, communs au tarball, au .deb et à l'AppImage.
stage_share() {
    local stage="$1"

    install -Dm644 "$REPO_ROOT/packaging/fatouradz.desktop" \
        "$stage/share/applications/fatouradz.desktop"

    install -Dm644 "$REPO_ROOT/packaging/io.github.amuza2.fatouradz.metainfo.xml" \
        "$stage/share/metainfo/io.github.amuza2.fatouradz.metainfo.xml"

    # Icône : les PNG sont pré-rendus depuis src/Assets/invoice.png par
    # scripts/gen-icons.sh, pour qu'une release n'ait besoin ni d'ImageMagick ni
    # de rsvg-convert. L'icône maîtresse est embarquée en plus, pour qu'un
    # reconditionneur (paquet de distribution) reparte de la source.
    for size in 48 64 128 256 512; do
        install -Dm644 "$REPO_ROOT/packaging/icons/fatouradz-$size.png" \
            "$stage/share/icons/hicolor/${size}x${size}/apps/fatouradz.png"
    done
    install -Dm644 "$MASTER_ICON" "$stage/share/icons/fatouradz-source.png"

    # AppStream lit la liste des versions publiées : on y ajoute la version
    # construite dans la copie livrée, sans modifier le fichier du dépôt.
    local metainfo="$stage/share/metainfo/io.github.amuza2.fatouradz.metainfo.xml"
    if ! grep -q "version=\"$VERSION\"" "$metainfo"; then
        sed -i "s|</releases>|    <release version=\"$VERSION\" date=\"$(date +%Y-%m-%d)\"/>\n  </releases>|" "$metainfo"
    fi
}

stage_common_docs() {
    local stage="$1"
    install -Dm644 "$REPO_ROOT/LICENSE" "$stage/LICENSE"
    install -Dm644 "$REPO_ROOT/README.md" "$stage/README.md"
}

# --------------------------------------------------------------------------
# Linux
# --------------------------------------------------------------------------

build_linux() {
    local rid="$1"
    local arch_appimage

    info "Publication Linux $rid"
    local stage="$OUTPUT_DIR/FatouraDZ-$VERSION-$rid"
    rm -rf "$stage"
    mkdir -p "$stage"

    publish "$rid" "$stage"

    [ -f "$stage/FatouraDZ" ] || die "la publication n'a pas produit $stage/FatouraDZ"

    # Convention Unix : un binaire en minuscules. Le nom du fichier n'est utilisé
    # nulle part par l'application.
    mv "$stage/FatouraDZ" "$stage/fatouradz"

    stage_common_docs "$stage"
    install -Dm755 "$SCRIPT_DIR/install.sh" "$stage/install.sh"
    stage_share "$stage"

    local tarball="$stage.tar.gz"
    rm -f "$tarball"
    tar -czf "$tarball" -C "$OUTPUT_DIR" "$(basename "$stage")"
    write_checksum "$tarball"

    printf '\n'
    ls -lh "$tarball"

    if [ "$BUILD_DEB" -eq 1 ]; then
        info "Paquet .deb ($rid)"
        "$SCRIPT_DIR/build-deb.sh" --binary "$stage" --version "$VERSION" --output "$OUTPUT_DIR"
    fi

    # L'AppImage ne se construit que pour l'architecture de la machine : appimagetool
    # ne sait pas recompiler une application.
    if [ "$BUILD_APPIMAGE" -eq 1 ]; then
        case "$rid" in
            linux-x64)   arch_appimage="x86_64" ;;
            linux-arm64) arch_appimage="aarch64" ;;
            *)           arch_appimage="" ;;
        esac

        local host_current
        host_current="$(uname -m)"
        case "$arch_appimage:$host_current" in
            x86_64:x86_64|x86_64:amd64|aarch64:aarch64|aarch64:arm64)
                "$SCRIPT_DIR/build-appimage.sh" --binary "$stage" --version "$VERSION" \
                    --arch "$arch_appimage" --output "$OUTPUT_DIR"
                ;;
            *)
                warn "pas d'AppImage pour $rid : la machine est $host_current"
                ;;
        esac
    fi
}

# --------------------------------------------------------------------------
# Windows (portable ; l'installateur .exe est construit sous Windows)
# --------------------------------------------------------------------------

build_windows() {
    local rid="$1"

    info "Publication Windows $rid"
    local stage="$OUTPUT_DIR/FatouraDZ-$VERSION-$rid"
    rm -rf "$stage"
    mkdir -p "$stage"

    publish "$rid" "$stage"

    [ -f "$stage/FatouraDZ.exe" ] || die "la publication n'a pas produit $stage/FatouraDZ.exe"

    # L'icône du .exe est posée par le SDK ; rien d'autre ici ne peut le vérifier,
    # et un exe sans icône est visible chez tous les utilisateurs Windows.
    if command -v python3 >/dev/null 2>&1; then
        python3 "$SCRIPT_DIR/verifier-icone-exe.py" "$ICO" "$stage/FatouraDZ.exe" \
            || die "l'exécutable Windows n'embarque pas l'icône de l'application"
    else
        warn "python3 absent : icône de l'exécutable Windows non vérifiée"
    fi

    stage_common_docs "$stage"
    install -Dm644 "$REPO_ROOT/packaging/windows/LISEZ-MOI.txt" "$stage/LISEZ-MOI.txt"

    local archive="$stage.zip"
    creer_zip "$archive" "$stage"
    write_checksum "$archive"

    printf '\n'
    ls -lh "$archive"
}

mkdir -p "$OUTPUT_DIR"

if [ "$BUILD_LINUX" -eq 1 ]; then
    for rid in $LINUX_ARCHS; do
        build_linux "$rid"
    done
fi

if [ "$BUILD_WINDOWS" -eq 1 ]; then
    for rid in $WINDOWS_ARCHS; do
        build_windows "$rid"
    done
fi

info "Terminé"
# Récapitulatif : du plutôt que « ls | grep », qui casse sur les noms exotiques.
for fichier in "$OUTPUT_DIR"/*; do
    [ -e "$fichier" ] || continue
    printf '  %-52s %s\n' "$(basename "$fichier")" "$(du -sh "$fichier" | cut -f1)"
done
