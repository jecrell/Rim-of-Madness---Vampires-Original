using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace Vampire
{
    public class DisciplineEffect_VampiricHealing : Verb_UseAbilityPawnEffect
    {
        public override void Effect(Pawn target)
        {
            base.Effect(target);

            int maxInjuries = 4;
            int maxInjuriesPerBodypart;

            foreach (BodyPartRecord rec in target.health.hediffSet.GetInjuredParts())
            {
                if (maxInjuries > 0)
                {
                    maxInjuriesPerBodypart = 2;
                    foreach (Hediff_Injury current in from injury in target.health.hediffSet.GetHediffs<Hediff_Injury>() where injury.Part == rec select injury)
                    {
                        if (maxInjuriesPerBodypart > 0)
                        {
                            if (current.CanHealNaturally() && !current.IsOld()) // basically check for scars and old wounds
                            {
                                current.Heal((int)current.Severity + 1);
                                maxInjuries--;
                                maxInjuriesPerBodypart--;
                            }
                        }
                    }
                }
            }
        }
    }
}
