using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace Vampire
{
    public class HediffVampirism_VampGiver : HediffWithComps
    {
        public virtual BloodlineDef Bloodline => VampireUtility.RandBloodline;
        public virtual int Generation
        {
            get
            {
                int result = Rand.Range(10, 13);
                switch (this.CurStageIndex)
                {
                    case 0: result = 14; break;
                    case 1: result = Rand.Range(10, 13); break;
                    case 2: result = Rand.Range(7, 9); break;
                    case 3: result = Rand.Range(5, 6); break;
                }
                return result;
            }
        }

        public bool setup = false;
        public override void PostTick()
        {
            if (!setup)
            {
                bool setup = true;
                if (this.pawn.VampComp() is CompVampire v)
                {
                    int generatonToSpawn = this.Generation;
                    //Pawn sire = VampireRelationUtility.FindSireFor(this.pawn, this.Bloodline, generatonToSpawn);
                    v.InitializeVampirism(null, this.Bloodline, generatonToSpawn);
                }
            }
            base.PostTick();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.setup, "setup", false);
        }
    }
}
