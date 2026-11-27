// Određuje prikaz retkosti oružja u korisničkom interfejsu.
using UnityEngine;

namespace AWizardsFight
{
    public static class WeaponRarityPresentation
    {
        public static Color ColorFor(WeaponRarity rarity)
        {
            switch (rarity)
            {
                case WeaponRarity.Rare:
                    return new Color(.35f, .65f, 1f);
                case WeaponRarity.Epic:
                    return new Color(.78f, .45f, 1f);
                case WeaponRarity.Legendary:
                    return new Color(1f, .68f, .2f);
                default:
                    return new Color(.82f, .84f, .88f);
            }
        }

        public static string Label(WeaponRarity rarity) => "<color=#" + ColorUtility.ToHtmlStringRGB(ColorFor(rarity)) + ">" + rarity + "</color>";
    }
}
