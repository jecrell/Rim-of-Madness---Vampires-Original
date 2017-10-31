using System;
using Verse;
using Verse.AI;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace Vampire
{
    public class JobGiver_SeekShelterFromSunlight : ThinkNode_JobGiver
    {

        protected IntRange ticksBetweenWandersRange = new IntRange(20, 100);

        protected LocomotionUrgency locomotionUrgency = LocomotionUrgency.Walk;

        protected Danger maxDanger = Danger.None;

        protected float wanderRadius = 7.5f;


        public bool IsZero(IntVec3 loc)
        {
            return loc.x == 0 && loc.y == 0 && loc.z == 0;
        }

        protected virtual IntVec3 GetExactWanderDest(Pawn pawn)
        {
            IntVec3 wanderRoot = pawn.PositionHeld;
            return RCellFinder.RandomWanderDestFor(pawn, wanderRoot, this.wanderRadius, delegate(Pawn p, IntVec3 v) { if (v.Roofed(p.MapHeld)) return true; return false; }, PawnUtility.ResolveMaxDanger(pawn, this.maxDanger));
        }

        public override float GetPriority(Pawn pawn)
        {
            if (pawn.VampComp() is CompVampire v && v.IsVampire &&
            GenLocalDate.HourInteger(pawn) >= 6 && GenLocalDate.HourInteger(pawn) <= 17 &&
            !pawn.PositionHeld.Roofed(pawn.MapHeld))
            {
                return 9.5f;
            }
            return 0f;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            Room room = pawn.GetRoom(RegionType.Set_Passable);
            if (room != null)
            {
                if (room.PsychologicallyOutdoors)
                {
                    Area area = pawn.MapHeld.areaManager.Home;
                    if (area != null)
                    {
                        if (area.ActiveCells.FirstOrDefault(x => x.Roofed(pawn.Map) && x.Walkable(pawn.Map)) is IntVec3 safePlace && !IsZero(safePlace) && safePlace.IsValid)
                        {
                            Log.Message("Safe Place");
                            return new Job(JobDefOf.Goto, safePlace) { locomotionUrgency = LocomotionUrgency.Sprint };
                        }

                    }


                    Thing thing = GenClosest.ClosestThingReachable(pawn.PositionHeld, pawn.Map, ThingRequest.ForDef(ThingDefOf.Fire), PathEndMode.Touch, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), 23, null, null, 0, -1, false, RegionType.Set_Passable, false);
                    if (thing != null)
                    {
                        Log.Message("Flee Place");

                        IntVec3 fleeLoc = CellFinderLoose.GetFleeDest(pawn, new List<Thing>() { thing }, 23);
                        return new Job(JobDefOf.FleeAndCower, thing);
                    }

                    Region region;
                    CellFinder.TryFindClosestRegionWith(pawn.GetRegion(RegionType.Set_Passable), TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn), (x => !x.Room.PsychologicallyOutdoors), 9999, out region, RegionType.Set_All);   //.ClosestRegionIndoors(pawn.Position, pawn.Map, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), RegionType.Set_Passable);
                    if (region != null)
                    {
                        IntVec3 result;
                        if (region.TryFindRandomCellInRegion(x => !IsZero(x) && x.IsValid && x.InBounds(pawn.MapHeld) && x.GetDoor(pawn.MapHeld) == null, out result))
                        {
                            Log.Message("Region Place");

                            return new Job(JobDefOf.Goto, result) { locomotionUrgency = LocomotionUrgency.Sprint };
                        }
                    }
                    IntVec3? cellResult = null;
                    cellResult = CellFinderLoose.RandomCellWith(x => !IsZero(x) && x.IsValid && x.InBounds(pawn.MapHeld) && x.Roofed(pawn.MapHeld) && x.Walkable(pawn.MapHeld)
                    && pawn.Map.reachability.CanReach(pawn.PositionHeld, x, PathEndMode.OnCell, TraverseMode.ByPawn, Danger.Deadly), pawn.MapHeld, 1000);
                    if (cellResult != null && cellResult.Value.IsValid && !IsZero(cellResult.Value))
                    {
                        Log.Message("Random Place");

                        return new Job(JobDefOf.Goto, cellResult.Value) { locomotionUrgency = LocomotionUrgency.Sprint };
                    }

                    IntVec3? hideyHoleResult = null;
                    hideyHoleResult = VampireUtility.FindHideyHoleSpot(VampDefOf.ROMV_HideyHole, Rot4.Random, pawn.PositionHeld, pawn.MapHeld);
                    if (hideyHoleResult != null && hideyHoleResult.Value.IsValid)
                    {
                        Log.Message("Hidey Place");

                        return new Job(VampDefOf.ROMV_DigAndHide, hideyHoleResult.Value) { locomotionUrgency = LocomotionUrgency.Sprint };

                    }

                }
                //bool nextMoveOrderIsWait = pawn.mindState.nextMoveOrderIsWait;
                //pawn.mindState.nextMoveOrderIsWait = !pawn.mindState.nextMoveOrderIsWait;
                //if (nextMoveOrderIsWait)
                //{
                //    return new Job(JobDefOf.WaitWander)
                //    {
                //        expiryInterval = this.ticksBetweenWandersRange.RandomInRange
                //    };
                //}
                //IntVec3 exactWanderDest = this.GetExactWanderDest(pawn);
                //if (!exactWanderDest.IsValid)
                //{
                //    pawn.mindState.nextMoveOrderIsWait = false;
                //    return null;
                //}
                //pawn.Map.pawnDestinationManager.ReserveDestinationFor(pawn, exactWanderDest);
                //return new Job(JobDefOf.GotoWander, exactWanderDest)
                //{
                //    locomotionUrgency = this.locomotionUrgency
                //};
            }
            return null;
        }

        //private static Region ClosestRegionIndoors(IntVec3 root, Map map, TraverseParms traverseParms, RegionType traversableRegionTypes = RegionType.Set_Passable)
        //{
        //    Region region = root.GetRegion(map, traversableRegionTypes);
        //    if (region == null)
        //    {
        //        return null;
        //    }
        //    RegionEntryPredicate entryCondition = (Region from, Region r) => r.Allows(traverseParms, false);
        //    Region foundReg = null;
        //    RegionProcessor regionProcessor = delegate (Region r)
        //    {
        //        if (r.portal != null)
        //        {
        //            return false;
        //        }
        //        if (!r.Room.PsychologicallyOutdoors)
        //        {
        //            foundReg = r;
        //            return true;
        //        }
        //        return false;
        //    };
        //    RegionTraverser.BreadthFirstTraverse(region, entryCondition, regionProcessor, 9999, traversableRegionTypes);
        //    return foundReg;
        //}
    }
}
