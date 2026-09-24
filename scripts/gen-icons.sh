#!/usr/bin/env bash
#
# Régénère les icônes empaquetées depuis l'icône maîtresse src/Assets/invoice.png.
#
# Les fichiers générés (packaging/icons/*.png et src/Assets/fatouradz.ico) sont
# versionnés : construire une release n'exige donc ni ImageMagick ni cette
# commande. Ne relancer ce script que lorsque la source change, puis valider les
# fichiers produits.
#
#   scripts/gen-icons.sh
#
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/.." && pwd)"

# Source unique de l'icône : remplacer ce fichier suffit à changer l'icône de
# l'application (fenêtre, exécutable Windows, entrée de bureau, AppImage, .deb).
MASTER="$REPO_ROOT/src/Assets/invoice.png"

OUT_ICONS="$REPO_ROOT/packaging/icons"
OUT_ICO="$REPO_ROOT/src/Assets/fatouradz.ico"

# 48 et 64 sont les tailles réellement demandées par les environnements de
# bureau ; 128/256/512 servent aux vues en grand, au .deb et à l'AppImage.
SIZES="48 64 128 256 512"

# Windows choisit la taille la plus proche selon le contexte : 16 pour la barre
# des tâches, 256 pour l'aperçu de l'Explorateur.
ICO_SIZES="16 24 32 48 64 128 256"

die() { printf 'error: %s\n' "$*" >&2; exit 1; }
info() { printf '\n==> %s\n' "$*"; }
warn() { printf 'warning: %s\n' "$*" >&2; }

[ -f "$MASTER" ] || die "icône maîtresse introuvable : $MASTER"

# ImageMagick 7 fournit « magick » ; « convert » reste accepté mais est déprécié
# et affiche un avertissement à chaque appel.
if command -v magick >/dev/null 2>&1; then
    IM="magick"
elif command -v convert >/dev/null 2>&1; then
    IM="convert"
else
    die "ImageMagick est requis (paquet imagemagick)"
fi

LARGEUR_SOURCE="$($IM identify -format '%w' "$MASTER")"
HAUTEUR_SOURCE="$($IM identify -format '%h' "$MASTER")"

info "Source : ${MASTER#"$REPO_ROOT"/} ($LARGEUR_SOURCE x $HAUTEUR_SOURCE)"

if [ "$LARGEUR_SOURCE" -ne "$HAUTEUR_SOURCE" ]; then
    warn "la source n'est pas carrée : elle sera étirée dans un cadre carré"
fi

# Un agrandissement est visiblement plus doux qu'un rendu vectoriel : mieux vaut
# le dire ici que le découvrir dans une barre des tâches.
if [ "$LARGEUR_SOURCE" -lt 256 ]; then
    warn "la source fait moins de 256 px : les grandes tailles seront agrandies"
    warn "fournissez une icône d'au moins 512 px pour un rendu net"
fi

rendre() {
    local taille="$1" sortie="$2"

    if [ "$taille" -gt "$LARGEUR_SOURCE" ]; then
        # Agrandissement : Lanczos puis léger renforcement, sinon les bords et
        # les traits fins paraissent mous.
        $IM "$MASTER" -background none -filter Lanczos \
            -resize "${taille}x${taille}" -unsharp 0x1+0.6+0.02 "$sortie"
    else
        $IM "$MASTER" -background none -filter Lanczos \
            -resize "${taille}x${taille}" "$sortie"
    fi
}

mkdir -p "$OUT_ICONS"

info "Génération des PNG"
for taille in $SIZES; do
    rendre "$taille" "$OUT_ICONS/fatouradz-$taille.png"
    printf '  %s (%sx%s)\n' "$OUT_ICONS/fatouradz-$taille.png" "$taille" "$taille"
done

# Icône attendue par appimagetool à la racine de l'AppDir et par la clé Icon= du
# fichier .desktop.
cp "$OUT_ICONS/fatouradz-256.png" "$OUT_ICONS/fatouradz.png"

info "Génération de l'ICO Windows"
TMP_DIR="$(mktemp -d)"
trap 'rm -rf "$TMP_DIR"' EXIT

ico_args=()
for taille in $ICO_SIZES; do
    rendre "$taille" "$TMP_DIR/icon-$taille.png"
    ico_args+=("$TMP_DIR/icon-$taille.png")
done

$IM "${ico_args[@]}" -type TrueColorAlpha "$OUT_ICO"
printf '  %s (%s tailles)\n' "$OUT_ICO" "$(printf '%s' "$ICO_SIZES" | wc -w)"

# Empreinte de la source : build-release.sh la compare à l'icône maîtresse pour
# refuser une release dont les icônes auraient été oubliées. Le fichier est
# versionné, donc la vérification fonctionne aussi après un clone (les dates de
# modification, elles, ne survivent pas à un checkout).
if command -v sha256sum >/dev/null 2>&1; then
    ( cd "$REPO_ROOT" && sha256sum "${MASTER#"$REPO_ROOT"/}" ) > "$OUT_ICONS/icone-source.sha256"
elif command -v shasum >/dev/null 2>&1; then
    ( cd "$REPO_ROOT" && shasum -a 256 "${MASTER#"$REPO_ROOT"/}" ) > "$OUT_ICONS/icone-source.sha256"
else
    warn "ni sha256sum ni shasum : $OUT_ICONS/icone-source.sha256 non mis à jour"
fi

info "Terminé"
printf 'Validez les fichiers produits avant de committer (git status).\n'
