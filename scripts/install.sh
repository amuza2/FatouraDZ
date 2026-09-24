#!/usr/bin/env bash
#
# Installe FatouraDZ pour l'utilisateur courant.
#
# À lancer depuis un dossier de release décompressé (l'archive .tar.gz d'une
# release GitHub), là où se trouvent le binaire et l'arborescence share/ :
#
#   tar -xzf FatouraDZ-0.1.0-linux-x64.tar.gz
#   cd FatouraDZ-0.1.0-linux-x64
#   ./install.sh
#
# Relancer le script met à jour l'installation en place. `./install.sh
# --uninstall` retire tout ce qui a été installé et laisse les données
# (factures, paramètres) intactes.
#
set -euo pipefail

SRC_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
PREFIX="${XDG_DATA_HOME:-$HOME/.local}"   # modifié par --prefix
UNINSTALL=0

BINARY="fatouradz"
DESKTOP_ID="fatouradz.desktop"
METAINFO_ID="io.github.amuza2.fatouradz.metainfo.xml"
ICON_SIZES="48 64 128 256 512"

die() { printf 'error: %s\n' "$*" >&2; exit 1; }
info() { printf '\n%s\n' "$*"; }

usage() {
    sed -n '2,14p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'
    cat <<'EOF'

Options:
  --prefix DIR    Installe sous DIR (défaut : ~/.local, soit ~/.local/bin et
                  ~/.local/share). Utiliser --prefix /usr/local pour une
                  installation système (droits d'écriture requis).
  --uninstall     Retire une installation précédente.
  -h, --help      Affiche cette aide.
EOF
    exit 0
}

while [ $# -gt 0 ]; do
    case "$1" in
        --prefix)    PREFIX="${2:-}"; shift 2 ;;
        --uninstall) UNINSTALL=1; shift ;;
        -h|--help)   usage ;;
        *)           die "option inconnue : $1 (essayez --help)" ;;
    esac
done

[ -n "$PREFIX" ] || die "--prefix attend un dossier"
PREFIX="${PREFIX%/}"

BIN_DIR="$PREFIX/bin"
SHARE_DIR="$PREFIX/share"

# --------------------------------------------------------------------------
# Outils
# --------------------------------------------------------------------------

refresh_caches() {
    # « A && B || C » est volontaire : C ne s'exécute que si A échoue, et le
    # « || true » évite qu'un cache absent fasse échouer l'installation.
    # shellcheck disable=SC2015
    command -v update-desktop-database >/dev/null 2>&1 &&
        update-desktop-database "$SHARE_DIR/applications" 2>/dev/null || true
    # shellcheck disable=SC2015
    command -v gtk-update-icon-cache >/dev/null 2>&1 &&
        gtk-update-icon-cache -f -t "$SHARE_DIR/icons/hicolor" 2>/dev/null || true
}

# Les bibliothèques natives nécessaires au rendu (X11, OpenGL, fontconfig) ne sont
# pas embarquées : elles viennent du système, comme pour n'importe quelle
# application graphique. Un `ldd` en dit plus long qu'un « l'application ne
# démarre pas » découvert après coup.
libs_manquantes() {
    local binaire="$1"
    command -v ldd >/dev/null 2>&1 || return 0
    ldd "$binaire" 2>/dev/null | awk '/not found/ {print $1}' | sort -u
}

paquets_suggerees() {
    local id_like=""
    # shellcheck disable=SC1091  # /etc/os-release n'est pas fourni à shellcheck : il est lu à l'exécution
    id_like="$(. /etc/os-release 2>/dev/null && printf '%s %s' "${ID:-}" "${ID_LIKE:-}")"

    case "$id_like" in
        *debian*|*ubuntu*) echo "libx11-6 libice6 libsm6 libfontconfig1 libgl1 libicu72 (ou libicu76)" ;;
        *fedora*|*rhel*)   echo "libX11 libICE libSM fontconfig mesa-libGL libicu" ;;
        *arch*)            echo "libx11 libice libsm fontconfig mesa libicu" ;;
        *suse*)            echo "libX11-6 libICE6 libSM6 fontconfig libGL1 libicu" ;;
        *)                 echo "les bibliothèques X11 de base, fontconfig, OpenGL et ICU" ;;
    esac
}

# --------------------------------------------------------------------------
# Désinstallation
# --------------------------------------------------------------------------

if [ "$UNINSTALL" -eq 1 ]; then
    info "Suppression de FatouraDZ de $PREFIX"
    removed=0

    paths=(
        "$BIN_DIR/$BINARY"
        "$SHARE_DIR/applications/$DESKTOP_ID"
        "$SHARE_DIR/metainfo/$METAINFO_ID"
        "$SHARE_DIR/icons/fatouradz-source.png"
    )
    for size in $ICON_SIZES; do
        paths+=("$SHARE_DIR/icons/hicolor/${size}x${size}/apps/fatouradz.png")
    done

    for path in "${paths[@]}"; do
        if [ -e "$path" ]; then
            rm -f "$path"
            printf '  supprimé %s\n' "$path"
            removed=$((removed + 1))
        fi
    done

    refresh_caches
    printf '\n%d fichier(s) supprimé(s).\n' "$removed"
    printf 'Vos factures et paramètres (%s) sont intacts.\n' \
        "${XDG_DATA_HOME:-$HOME/.local/share}/FatouraDZ"
    exit 0
fi

# --------------------------------------------------------------------------
# Contrôles préalables
# --------------------------------------------------------------------------

[ -f "$SRC_DIR/$BINARY" ] || die "« $BINARY » est introuvable à côté de ce script — lancez-le depuis un dossier de release décompressé"
[ -d "$SRC_DIR/share" ] || die "le dossier « share » est introuvable à côté de ce script — lancez-le depuis un dossier de release décompressé"

# --------------------------------------------------------------------------
# Installation
# --------------------------------------------------------------------------

info "Installation de FatouraDZ dans $PREFIX"

mkdir -p "$BIN_DIR" \
    "$SHARE_DIR/applications" \
    "$SHARE_DIR/metainfo" \
    "$SHARE_DIR/icons/hicolor/scalable/apps"

install -m 755 "$SRC_DIR/$BINARY" "$BIN_DIR/$BINARY"
printf '  %s\n' "$BIN_DIR/$BINARY"

install -m 644 "$SRC_DIR/share/applications/$DESKTOP_ID" \
    "$SHARE_DIR/applications/$DESKTOP_ID"
install -m 644 "$SRC_DIR/share/metainfo/$METAINFO_ID" \
    "$SHARE_DIR/metainfo/$METAINFO_ID"

# L'icône maîtresse est installée à part, sous un nom explicite : elle sert de
# référence pour un reconditionnement, et les tailles hicolor ci-dessus suffisent
# aux environnements de bureau. Elle n'est volontairement pas dans l'arborescence
# hicolor, qui est réservée aux icônes thématiques.
if [ -f "$SRC_DIR/share/icons/fatouradz-source.png" ]; then
    install -Dm644 "$SRC_DIR/share/icons/fatouradz-source.png" "$SHARE_DIR/icons/fatouradz-source.png"
fi

# Les environnements de bureau choisissent l'icône la plus proche de la taille
# demandée : on installe toute l'échelle plutôt qu'un seul fichier redimensionné
# par le thème.
for size in $ICON_SIZES; do
    src="$SRC_DIR/share/icons/hicolor/${size}x${size}/apps/fatouradz.png"
    [ -f "$src" ] || continue
    mkdir -p "$SHARE_DIR/icons/hicolor/${size}x${size}/apps"
    install -m 644 "$src" "$SHARE_DIR/icons/hicolor/${size}x${size}/apps/fatouradz.png"
done
printf '  %s\n' "$SHARE_DIR/applications/$DESKTOP_ID"
printf '  %s\n' "$SHARE_DIR/icons/hicolor/*/apps/fatouradz.*"

refresh_caches

# --------------------------------------------------------------------------
# Notes après installation
# --------------------------------------------------------------------------

printf '\nInstallé.\n'

manquantes="$(libs_manquantes "$BIN_DIR/$BINARY" || true)"
if [ -n "$manquantes" ]; then
    printf '\n!! Bibliothèques manquantes détectées :\n'
    # shellcheck disable=SC2086  # un nom de bibliothèque par ligne : l'éclatement en mots est voulu
    printf '     %s\n' $manquantes
    printf '\n   Installez-les avant de lancer l'\''application, par exemple :\n'
    printf '\n     %s\n' "$(paquets_suggerees)"
    printf '\n'
fi

case ":$PATH:" in
    *":$BIN_DIR:"*) ;;
    *)
        printf '\n%s n'\''est pas dans votre PATH. Ajoutez-le à votre profil de shell :\n' "$BIN_DIR"
        printf '\n  export PATH="%s:$PATH"\n' "$BIN_DIR"
        ;;
esac

printf '\nLancez « %s » ou choisissez FatouraDZ dans votre menu d'\''applications.\n' "$BINARY"
printf 'Vos données (factures, paramètres) sont dans %s\n' \
    "${XDG_DATA_HOME:-$HOME/.local/share}/FatouraDZ"
