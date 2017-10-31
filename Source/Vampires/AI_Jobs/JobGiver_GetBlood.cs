using RimWorld;
using System;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Vampire
{
    public class JobGiver_GetBlood : ThinkNode_JobGiver
    {
        //private HungerCategory minCategory;

        //public override ThinkNode DeepCopy(bool resolve = true)
        //{
        //    JobGiver_GetBlood jobGiver_GetBlood = (JobGiver_GetBlood)base.DeepCopy(resolve);
        //    jobGiver_GetBlood.minCategory = this.minCategory;
        //    return jobGiver_GetBlood;
        //}

        public override float GetPriority(Pawn pawn)
        {

            if (pawn.VampComp() == null)
            {
                return 0f;
            }
            if (!pawn.VampComp().IsVampire)
            {
                return 0f;
            }
            Need_Blood blood = pawn.needs.TryGetNeed<Need_Blood>();
            if (blood == null)
            {
                return 0f;
            }
            if (blood.preferredFeedMode == PreferredFeedMode.None)
            {
                return 0f;
            }
             //HungerCategory.Starving && FoodUtility.ShouldBeFedBySomeone(pawn))
            //{
            //    return 0f;
            //}
            //if (blood.CurCategory < this.minCategory)
            //{
            //    return 0f;
            //}
            if (blood.CurLevelPercentage < blood.ShouldFeedPerc)
            {
                return 9.5f;
            }
            return 0f;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            Need_Blood blood = pawn.needs.TryGetNeed<Need_Blood>();
            if (blood == null)
            {
                return null;
            }
            bool desperate = blood.CurCategory == HungerCategory.Starving;
            bool isHuntHuman = blood.preferredFeedMode == PreferredFeedMode.HumanoidLethal || blood.preferredFeedMode == PreferredFeedMode.HumanoidNonLethal;
            bool isHuntLethal = blood.preferredFeedMode == PreferredFeedMode.HumanoidLethal || blood.preferredFeedMode == PreferredFeedMode.AnimalLethal;
            Thing thing;
            ThingDef def;
            if (!BloodUtility.TryFindBestBloodSourceFor(pawn, pawn, desperate, out thing, out def))
            {
                return null;
            }
            if (thing != null)
            {
                Pawn pawn2 = thing as Pawn;
                if (pawn2 != null)
                {
                    return new Job(VampDefOf.ROMV_Feed, pawn2)
                    {
                        killIncappedTarget = isHuntLethal
                    };
                }
                return new Job(VampDefOf.ROMV_ConsumeBlood, thing)
                {
                    count = BloodUtility.WillConsumeStackCountOf(pawn, def)
                };
            }
            return null;
        }



    }
}
