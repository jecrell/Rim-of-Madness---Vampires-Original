using AbilityUser;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace Vampire
{
    public class Projectile_BloodStealer : Projectile_AbilityBase
    {
        public override void Impact_Override(Thing hitThing)
        {
            base.Impact_Override(hitThing);

            if (hitThing is Pawn p && p.BloodNeed() is Need_Blood bn)
            {
                MoteMaker.ThrowText(p.DrawPos, p.Map, "-1", -1f);
                bn.AdjustBlood(-2);
            }
            Projectile_BloodReturner projectile =
                (Projectile_BloodReturner)GenSpawn.Spawn(ThingDef.Named("ROMV_BloodProjectile_Returner"), hitThing.PositionHeld, hitThing.Map);
            projectile.Launch(hitThing, this.origin.ToIntVec3(), null);
        }
        
    }
}
