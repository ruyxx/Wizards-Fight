// Privremeni blagoslovi (sa oltara u nivou): dodaju osobine na ograničeno vreme ili do kraja nivoa.
using System.Collections.Generic;
using UnityEngine;

namespace AWizardsFight
{
    [DisallowMultipleComponent]
    public sealed class PlayerBuffs : MonoBehaviour
    {
        public sealed class Buff
        {
            public string Name;
            public StatType Stat;
            public float Value, ExpiresAt;
        }

        private readonly List<Buff> active = new List<Buff>();
        public IReadOnlyList<Buff> Active => active;

        public void Add(string name, StatType stat, float value, float seconds)
        {
            active.Add(new Buff { Name = name, Stat = stat, Value = value, ExpiresAt = seconds > 0 ? Time.time + seconds : float.PositiveInfinity });
        }

        public float Bonus(StatType stat)
        {
            float total = 0f;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (Time.time >= active[i].ExpiresAt)
                {
                    active.RemoveAt(i);
                    continue;
                }

                if (active[i].Stat == stat)
                    total += active[i].Value;
            }
            return total;
        }
    }
}
