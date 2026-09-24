#!/usr/bin/env python3
"""Vérifie qu'un exécutable Windows embarque bien les images d'une icône .ico.

Pourquoi c'est utile : l'icône du .exe est posée par le SDK .NET à partir de
<ApplicationIcon>. Rien dans les tests ne peut le voir, et un .exe publié sans son
icône affiche l'icône générique de Windows chez tous les utilisateurs — un défaut
visible, gênant, et corrigible seulement par une nouvelle version.

Les images d'un .ico sont recopiées telles quelles dans la ressource RT_ICON du
PE (même bloc DIB ou PNG). Comparer la charge utile de chaque image est donc un
test direct, et bien plus simple qu'un parcours complet de l'annuaire de
ressources — lequel est déjà utilisé par scripts/verifier-icones.sh pour valider
la structure du fichier .ico lui-même.

Usage :
    scripts/verifier-icone-exe.py src/Assets/fatouradz.ico artifacts/.../FatouraDZ.exe

Code de sortie : 0 si toutes les images sont présentes, 1 sinon.
"""

from __future__ import annotations

import os
import struct
import sys
from typing import Iterator


def images_ico(chemin: str) -> Iterator[tuple[int, int, int, int]]:
    """Parcourt les images d'un .ico : (largeur, hauteur, décalage, taille)."""
    with open(chemin, "rb") as fichier:
        # En-tête : 2 octets réservés, 2 de type, 2 de nombre d'images, donc le
        # nombre se lit à l'octet 4. Les entrées d'annuaire suivent, 16 octets
        # chacune, à partir de l'octet 6.
        fichier.seek(4)
        nombre = struct.unpack("<H", fichier.read(2))[0]
        fichier.seek(6)

        for _ in range(nombre):
            entree = fichier.read(16)
            if len(entree) < 16:
                return

            # 256 ne tenant pas sur un octet, la convention ICO l'encode en 0.
            largeur = entree[0] or 256
            hauteur = entree[1] or 256
            taille, decalage = struct.unpack("<II", entree[8:16])
            yield largeur, hauteur, decalage, taille


def valider_entete_ico(chemin: str) -> str | None:
    """Retourne un message d'erreur si le fichier n'est pas un .ico exploitable.

    Sans ce contrôle, passer un PNG par erreur se traduit par un nombre d'images
    absurde lu au hasard dans l'en-tête, et par un diagnostic trompeur
    (« icône probablement uniforme ») au lieu de « ce n'est pas un .ico ».
    """
    with open(chemin, "rb") as fichier:
        entete = fichier.read(6)

    if len(entete) < 6:
        return "fichier trop court pour un .ico"

    reserve, type_fichier, nombre = struct.unpack("<HHH", entete)

    if reserve != 0 or type_fichier != 1:
        return "ce n'est pas un fichier d'icônes Windows"

    # Un .ico réel n'a jamais des centaines d'images : c'est le signe qu'un autre
    # format a été passé (le PNG maître, par exemple).
    if not 1 <= nombre <= 32:
        return f"nombre d'images invraisemblable ({nombre})"

    return None


def fenetre_discriminante(charge: bytes) -> bytes | None:
    """Retourne une tranche de la charge utile utilisable comme signature.

    Une image d'icône peut être en grande partie uniforme (zones transparentes,
    aplat de couleur). Une tranche constante se retrouve par hasard dans
    n'importe quel exécutable de plusieurs mégaoctets, et ferait donc passer une
    icône absente pour présente. On exige donc une fenêtre assez variée pour
    n'avoir aucune chance de se retrouver ailleurs.
    """
    taille_fenetre = 192

    for debut in range(0, max(1, len(charge) - taille_fenetre + 1), 32):
        fenetre = charge[debut:debut + taille_fenetre]
        if len(fenetre) == taille_fenetre and len(set(fenetre)) >= 16:
            return fenetre

    return None


def main(chemin_ico: str, chemin_exe: str) -> int:
    for chemin in (chemin_ico, chemin_exe):
        if not os.path.isfile(chemin):
            print(f"error: fichier introuvable : {chemin}", file=sys.stderr)
            return 1

    with open(chemin_ico, "rb") as fichier:
        ico = fichier.read()

    with open(chemin_exe, "rb") as fichier:
        exe = fichier.read()

    probleme = valider_entete_ico(chemin_ico)
    if probleme is not None:
        print(f"error: {chemin_ico} : {probleme}", file=sys.stderr)
        return 1

    total = 0
    trouvees = 0
    ignorees = 0

    for largeur, hauteur, decalage, taille in images_ico(chemin_ico):
        charge = ico[decalage:decalage + taille]
        signature = fenetre_discriminante(charge)

        if signature is None:
            # Trop uniforme pour servir de preuve : ne pas la compter comme
            # vérifiée, plutôt que de risquer un faux positif.
            print(f"  {largeur:>3}x{hauteur:<3} trop uniforme pour être vérifiée, ignorée")
            ignorees += 1
            continue

        total += 1
        present = signature in exe
        trouvees += 1 if present else 0

        print(f"  {largeur:>3}x{hauteur:<3} {'présente' if present else 'ABSENTE'}")

    print()
    print(
        f"{os.path.basename(chemin_exe)} : {trouvees}/{total} image(s) vérifiée(s) présente(s)"
        + (f", {ignorees} ignorée(s)" if ignorees else "")
    )

    if total == 0:
        print(
            "error: aucune image de l'icône n'était assez variée pour être vérifiée — "
            "l'icône est probablement un aplat uniforme",
            file=sys.stderr,
        )
        return 1

    if trouvees == total:
        return 0

    print(
        "\nerror: l'exécutable n'embarque pas toutes les images de l'icône — "
        "vérifiez <ApplicationIcon> dans src/FatouraDZ.csproj",
        file=sys.stderr,
    )
    return 1


if __name__ == "__main__":
    if len(sys.argv) != 3:
        print(__doc__)
        raise SystemExit(2)

    raise SystemExit(main(sys.argv[1], sys.argv[2]))
