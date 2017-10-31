using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace Vampire
{
    public class EmbraceWorker
    {

        public BloodlineDef def;
        
        public virtual bool CanEmbrace(CompVampire sire, CompVampire childe)
        {
            if (sire == null || childe == null) return false;
            if (childe.AbilityUser.RaceProps.Humanlike &&
                !childe.IsVampire &&
                childe.Sire == null &&
                sire.IsVampire)
                return true;
            return false;
        }

        public virtual void DoEffect(CompVampire sireComp, CompVampire childeComp)
        {
            if (childeComp.AbilityUser is Pawn p)
            {
                Pawn temp = p;
                IntVec3 tempLoc = p.PositionHeld;
                Faction f = p.Faction;
                Map tempMap = p.MapHeld;
                if (childeComp?.BloodPool is Need_Blood b && sireComp?.BloodPool is Need_Blood tempB)
                {
                    b.TransferBloodTo(9999, tempB);
                }
                Pawn newPawn = ResurrectedPawnUtility.DoGeneratePawnFromSource(temp, false);
                if (!p.Dead) p.Kill(null);

                GenSpawn.Spawn(newPawn, tempLoc, tempMap);
                newPawn.workSettings.EnableAndInitialize();

                if (p.Corpse != null) p.Corpse.DeSpawn();

                if (sireComp?.BloodPool is Need_Blood sB && newPawn?.BloodNeed() is Need_Blood cB)
                {
                    cB.CurBloodPoints = 0;
                    sB.TransferBloodTo(1, cB);
                    cB.CurBloodPoints = 1;
                }
                newPawn.VampComp().Notify_Embraced(sireComp);
            }
        }

        public virtual bool TryEmbrace(Pawn sire, Pawn childe)
        {
            if (CanEmbrace(sire.GetComp<CompVampire>(), childe.GetComp<CompVampire>()))
            {
                DoEffect(sire.GetComp<CompVampire>(), childe.GetComp<CompVampire>());
                return true;
            }
            return false;
        }
    }
}
