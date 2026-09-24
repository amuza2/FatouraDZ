#!/usr/bin/env bash
#
# Vérifie que les icônes empaquetées correspondent bien à l'icône maîtresse
# src/Assets/invoice.png.
#
# Appelé par build-release.sh (avant toute publication) et par
# .github/workflows/build.yml (à chaque PR) : une icône oubliée doit être
# détectée au moment où on modifie la source, pas après avoir publié un tag. Une
# icône fausse est publique — elle est visible chez tous les utilisateurs — et ne
# se corrige qu'avec une nouvelle version.
#
#   scripts/verifier-icones.sh
#
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/.." && pwd)"

MASTER_RELATIVE="src/Assets/invoice.png"
MASTER="$REPO_ROOT/$MASTER_RELATIVE"
ICONS_DIR="$REPO_ROOT/packaging/icons"
HASH_FILE="$ICONS_DIR/icone-source.sha256"
ICO="$REPO_ROOT/src/Assets/fatouradz.ico"

SIZES="48 64 128 256 512"

# Windows n'embarque pas moins que la barre des tâches (16) et l'aperçu de
# l'Explorateur (256) : un ICO réduit à une seule taille passerait inaperçu à
# l'exécution, pas ici.
ICO_TAILLES_MIN=5

erreurs=0
erreur() { printf 'error: %s\n' "$*" >&2; erreurs=$((erreurs + 1)); }

[ -f "$MASTER" ] || { erreur "icône maîtresse introuvable : $MASTER_RELATIVE"; exit 1; }

# --------------------------------------------------------------------------
# Présence
# --------------------------------------------------------------------------

for taille in $SIZES; do
    [ -f "$ICONS_DIR/fatouradz-$taille.png" ] \
        || erreur "icône manquante : packaging/icons/fatouradz-$taille.png"
done
[ -f "$ICONS_DIR/fatouradz.png" ] || erreur "icône manquante : packaging/icons/fatouradz.png"
[ -f "$ICO" ] || erreur "icône Windows manquante : src/Assets/fatouradz.ico"

if [ "$erreurs" -gt 0 ]; then
    printf '\nLancez scripts/gen-icons.sh, puis validez les fichiers produits.\n' >&2
    exit 1
fi

# --------------------------------------------------------------------------
# Dimensions et format
# --------------------------------------------------------------------------

# Lecture directe de l'en-tête, pour ne pas dépendre d'ImageMagick qui n'est pas
# installé sur les runners GitHub : cela permet à cette vérification de tourner
# partout, y compris là où les icônes ont été générées ailleurs.
if command -v python3 >/dev/null 2>&1; then
    dimension_png() {
        python3 - "$1" <<'PY'
import struct
import sys

try:
    with open(sys.argv[1], "rb") as fichier:
        entete = fichier.read(24)
except OSError:
    print("illisible")
    raise SystemExit(0)

if entete[:8] != b"\x89PNG\r\n\x1a\n":
    print("pas-un-png")
    raise SystemExit(0)

largeur, hauteur = struct.unpack(">II", entete[16:24])
print(f"{largeur}x{hauteur}")
PY
    }

    # Un fichier renommé ou tronqué passe la vérification de présence, pas
    # celle-ci : c'est exactement l'erreur qu'on veut attraper avant la release.
    for taille in $SIZES; do
        fichier="$ICONS_DIR/fatouradz-$taille.png"
        lue="$(dimension_png "$fichier")"
        [ "$lue" = "${taille}x${taille}" ] \
            || erreur "packaging/icons/fatouradz-$taille.png mesure $lue au lieu de ${taille}x${taille}"
    done

    lue="$(dimension_png "$ICONS_DIR/fatouradz.png")"
    [ "$lue" = "256x256" ] \
        || erreur "packaging/icons/fatouradz.png mesure $lue au lieu de 256x256 (icône attendue par appimagetool)"

    # Vérification structurelle de l'ICO : nombre d'images, tailles présentes et
    # présence effective de chaque image dans le fichier. Un fichier tronqué
    # garde un en-tête valide — c'est précisément ce qu'une lecture superficielle
    # laisserait passer, pour se voir ensuite dans la barre des tâches Windows.
    verdict_ico="$(python3 - "$ICO" "$ICO_TAILLES_MIN" <<'PY'
import os
import struct
import sys

chemin = sys.argv[1]
minimum = int(sys.argv[2])

try:
    taille_fichier = os.path.getsize(chemin)

    with open(chemin, "rb") as fichier:
        entete = fichier.read(6)
        if len(entete) < 6:
            print("ERREUR en-tête incomplet")
            raise SystemExit(0)

        # 2 octets réservés, 2 octets de type (1 = icône), nombre d'images.
        reserve, type_fichier, nombre = struct.unpack("<HHH", entete)
        if reserve != 0 or type_fichier != 1:
            print("ERREUR ce n'est pas un fichier d'icônes Windows")
            raise SystemExit(0)

        plus_grande = 0
        for _ in range(nombre):
            # Entrée d'annuaire : 1 octet largeur, 1 hauteur, 1 couleurs,
            # 1 réservé, 2 plans, 2 bits, 4 taille, 4 décalage.
            entree = fichier.read(16)
            if len(entree) < 16:
                print("ERREUR annuaire incomplet")
                raise SystemExit(0)

            # 256 ne tient pas sur un octet : la convention ICO l'encode en 0.
            largeur = entree[0] or 256
            hauteur = entree[1] or 256
            taille_image, decalage = struct.unpack("<II", entree[8:16])

            if decalage + taille_image > taille_fichier:
                print(f"ERREUR image {largeur}x{hauteur} tronquée (fichier incomplet)")
                raise SystemExit(0)

            plus_grande = max(plus_grande, largeur)
except Exception as exception:  # noqa: BLE001 - le verdict doit rester sur une ligne
    print(f"ERREUR lecture impossible : {exception}")
    raise SystemExit(0)

if nombre < minimum:
    print(f"ERREUR {nombre} image(s) seulement, au moins {minimum} attendues")
elif plus_grande < 256:
    print(f"ERREUR pas d'image 256 px (plus grande : {plus_grande}) : l'aperçu de l'Explorateur serait flou")
else:
    print(f"OK {nombre} images, jusqu'à {plus_grande} px")
PY
)"

    case "$verdict_ico" in
        OK*) : ;;
        ERREUR*) erreur "src/Assets/fatouradz.ico : ${verdict_ico#ERREUR }" ;;
        *) erreur "src/Assets/fatouradz.ico : vérification impossible (${verdict_ico:-aucune sortie})" ;;
    esac
else
    printf 'note: python3 absent — tailles des icônes non vérifiées\n'
fi

# --------------------------------------------------------------------------
# Correspondance avec la source
# --------------------------------------------------------------------------

# L'empreinte de la source est enregistrée à la génération : la comparer à
# l'icône maîtresse détecte une source modifiée sans régénération, y compris
# après un clone (les dates de modification, elles, ne survivent pas à un
# checkout, et un contrôle par date donnerait une alerte à chaque exécution CI).
if [ -f "$HASH_FILE" ]; then
    SHA256SUM=""
    if command -v sha256sum >/dev/null 2>&1; then SHA256SUM="sha256sum"
    elif command -v shasum >/dev/null 2>&1; then SHA256SUM="shasum -a 256"
    fi

    if [ -n "$SHA256SUM" ]; then
        attendue="$(cut -d' ' -f1 < "$HASH_FILE")"
        actuelle="$(cd "$REPO_ROOT" && $SHA256SUM "$MASTER_RELATIVE" | cut -d' ' -f1)"

        if [ -n "$attendue" ] && [ "$attendue" != "$actuelle" ]; then
            erreur "l'icône maîtresse a changé depuis la dernière génération d'icônes
       empreinte enregistrée : $attendue
       empreinte actuelle    : $actuelle"
        fi
    fi
else
    erreur "packaging/icons/icone-source.sha256 absent : impossible de savoir de quelle source viennent les icônes"
fi

# --------------------------------------------------------------------------

if [ "$erreurs" -gt 0 ]; then
    printf '\n%d problème(s) d'"'"'icônes. Lancez scripts/gen-icons.sh, puis validez packaging/icons/ et src/Assets/fatouradz.ico.\n' "$erreurs" >&2
    exit 1
fi

printf 'Icônes : jeu complet, aux bonnes tailles et à jour (source %s)\n' "$MASTER_RELATIVE"
