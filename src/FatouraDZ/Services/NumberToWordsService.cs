using System;

namespace FatouraDZ.Services;

public class NumberToWordsService : INumberToWordsService
{
    private static readonly string[] Unites = 
    { 
        "", "un", "deux", "trois", "quatre", "cinq", "six", "sept", "huit", "neuf",
        "dix", "onze", "douze", "treize", "quatorze", "quinze", "seize", "dix-sept", "dix-huit", "dix-neuf"
    };

    private static readonly string[] Dizaines = 
    { 
        "", "", "vingt", "trente", "quarante", "cinquante", "soixante", "soixante", "quatre-vingt", "quatre-vingt"
    };

    public string ConvertirEnLettres(decimal montant)
    {
        if (montant == 0)
            return "zéro dinar algérien";

        var partieEntiere = (long)Math.Floor(montant);
        var centimes = (int)Math.Round((montant - partieEntiere) * 100);

        var resultat = ConvertirNombre(partieEntiere);
        
        if (partieEntiere == 1)
            resultat += " dinar algérien";
        else
            resultat += " dinars algériens";

        if (centimes > 0)
        {
            resultat += " et " + ConvertirNombre(centimes);
            if (centimes == 1)
                resultat += " centime";
            else
                resultat += " centimes";
        }

        // Première lettre en majuscule
        return char.ToUpper(resultat[0]) + resultat[1..];
    }

    private static string ConvertirNombre(long nombre)
    {
        if (nombre == 0)
            return "zéro";

        if (nombre < 0)
            return "moins " + ConvertirNombre(-nombre);

        var resultat = "";

        // Milliards
        if (nombre >= 1_000_000_000)
        {
            var milliards = nombre / 1_000_000_000;
            if (milliards == 1)
                resultat += "un milliard ";
            else
                resultat += ConvertirNombre(milliards) + " milliards ";
            nombre %= 1_000_000_000;
        }

        // Millions
        if (nombre >= 1_000_000)
        {
            var millions = nombre / 1_000_000;
            if (millions == 1)
                resultat += "un million ";
            else
                resultat += ConvertirNombre(millions) + " millions ";
            nombre %= 1_000_000;
        }

        // Milliers
        if (nombre >= 1000)
        {
            var milliers = nombre / 1000;
            if (milliers == 1)
                resultat += "mille ";
            else
                resultat += ConvertirNombre(milliers) + " mille ";
            nombre %= 1000;
        }

        // Centaines
        if (nombre >= 100)
        {
            var centaines = nombre / 100;
            if (centaines == 1)
                resultat += "cent ";
            else
            {
                resultat += Unites[centaines] + " cents ";
                if (nombre % 100 != 0)
                    resultat = resultat.TrimEnd('s', ' ') + " ";
            }
            nombre %= 100;
        }

        // Dizaines et unités
        if (nombre > 0)
        {
            if (nombre < 20)
            {
                resultat += Unites[nombre];
            }
            else
            {
                var dizaine = nombre / 10;
                var unite = nombre % 10;

                // Cas spéciaux pour 70-79 et 90-99
                if (dizaine == 7 || dizaine == 9)
                {
                    resultat += Dizaines[dizaine];
                    if (unite == 1 && dizaine == 7)
                        resultat += "-et-onze";
                    else
                        resultat += "-" + Unites[10 + unite];
                }
                else
                {
                    resultat += Dizaines[dizaine];
                    if (unite == 1 && dizaine != 8)
                        resultat += "-et-un";
                    else if (unite > 0)
                        resultat += "-" + Unites[unite];
                    else if (dizaine == 8)
                        resultat += "s"; // quatre-vingts
                }
            }
        }

        return resultat.Trim();
    }
}
