﻿// ----------------------------------------------------------------------
// These are basic usings. Always let them be here.
// ----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System.Text;

// ----------------------------------------------------------------------
// These are RimWorld-specific usings. Activate/Deactivate what you need:
// ----------------------------------------------------------------------
using UnityEngine;         // Always needed
//using VerseBase;         // Material/Graphics handling functions are found here
using Verse;               // RimWorld universal objects are here (like 'Building')
using Verse.AI;          // Needed when you do something with the AI
using Verse.AI.Group;
using Verse.Sound;       // Needed when you do something with Sound
using Verse.Noise;       // Needed when you do something with Noises
using RimWorld;            // RimWorld specific functions are found here (like 'Building_Battery')
using RimWorld.Planet;   // RimWorld specific functions for world creation
//using RimWorld.SquadAI;  // RimWorld specific functions for squad brains 

namespace Vampire
{
    class ResurrectedPawnUtility
    {

        public static Pawn DoGeneratePawnFromSource(Pawn sourcePawn, bool isBerserk = true, bool oathOfHastur = false)
        {
            PawnKindDef pawnKindDef = sourcePawn.kindDef;
            Faction factionDirect = isBerserk ? Find.FactionManager.FirstFactionOfDef(FactionDefOf.SpacerHostile) : Faction.OfPlayer;
            Pawn pawn = (Pawn)ThingMaker.MakeThing(pawnKindDef.race, null);
            try
            {
                pawn.kindDef = pawnKindDef;
                pawn.SetFactionDirect(factionDirect);
                PawnComponentsUtility.CreateInitialComponents(pawn);
                pawn.gender = sourcePawn.gender;
                pawn.ageTracker.AgeBiologicalTicks = sourcePawn.ageTracker.AgeBiologicalTicks;
                pawn.ageTracker.AgeChronologicalTicks = sourcePawn.ageTracker.AgeChronologicalTicks;
                pawn.workSettings = new Pawn_WorkSettings(pawn);
                if (pawn.workSettings != null && sourcePawn.Faction.IsPlayer)
                {
                    pawn.workSettings.EnableAndInitialize();
                }

                pawn.needs.SetInitialLevels();
                //Add hediffs?
                //Add relationships?
                if (pawn.RaceProps.Humanlike)
                {
                    pawn.story.melanin = sourcePawn.story.melanin;
                    pawn.story.crownType = sourcePawn.story.crownType;
                    pawn.story.hairColor = sourcePawn.story.hairColor;
                    pawn.story.childhood = sourcePawn.story.childhood;
                    pawn.story.adulthood = sourcePawn.story.adulthood;
                    pawn.story.bodyType = sourcePawn.story.bodyType;
                    pawn.story.hairDef = sourcePawn.story.hairDef;
                        foreach (Trait current in sourcePawn.story.traits.allTraits)
                        {
                            pawn.story.traits.GainTrait(current);
                        }
                    
                        SkillFixer(pawn, sourcePawn);
                        RelationshipFixer(pawn, sourcePawn);
                        AddedPartFixer(pawn, sourcePawn);
                    //pawn.story.GenerateSkillsFromBackstory();
                    NameTriple nameTriple = sourcePawn.Name as NameTriple;
                    //if (!oathOfHastur)
                    //{
                    //    pawn.Name = new NameTriple(nameTriple.First, string.Concat(new string[]
                    //        {
                    //        "* ",
                    //        Translator.Translate("Reanimated"),
                    //        " ",
                    //        nameTriple.Nick,
                    //        " *"
                    //        }), nameTriple.Last);
                    //}
                    pawn.Name = nameTriple;
                }
                string headGraphicPath = sourcePawn.story.HeadGraphicPath;
                typeof(Pawn_StoryTracker).GetField("headGraphicPath", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(pawn.story, headGraphicPath);
                GenerateApparelFromSource(pawn, sourcePawn);
                PawnGenerationRequest con = new PawnGenerationRequest();
                PawnInventoryGenerator.GenerateInventoryFor(pawn, con);
                if (isBerserk)
                {
                    pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk);
                }
                //Log.Message(pawn.NameStringShort);
                return pawn;
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
                //Cthulhu.Utility.DebugReport(e.ToString());
            }
            return null;
        }

        public static void AddedPartFixer(Pawn pawn, Pawn sourcePawn = null)
        {
            foreach (Hediff hediff in sourcePawn.health.hediffSet.hediffs)
            {
                if (hediff is Hediff_AddedPart || hediff is Hediff_Implant)
                {
                    pawn.health.AddHediff(hediff);
                }
            }
        }

        public static void SkillFixer(Pawn pawn, Pawn sourcePawn = null)
        {
            //Add in and fix skill levels
            foreach (SkillRecord skill in sourcePawn.skills.skills)
            {
                SkillRecord pawnSkill = pawn.skills.GetSkill(skill.def);
                if (pawnSkill == null) pawn.skills.skills.Add(skill);
                else
                {
                    pawnSkill.Level = skill.Level;
                    pawnSkill.passion = skill.passion;
                }
            }
        }

        public static void RelationshipFixer(Pawn pawn, Pawn sourcePawn = null)
        {
            //Add in and fix all blood relationships
            if (sourcePawn.relations.DirectRelations != null && sourcePawn.relations.DirectRelations.Count > 0)
            {
                foreach (DirectPawnRelation pawnRel in sourcePawn.relations.DirectRelations)
                {
                    if (pawnRel.otherPawn != null && pawnRel.def != null)
                    {
                        pawn.relations.AddDirectRelation(pawnRel.def, pawnRel.otherPawn);
                    }
                }
                sourcePawn.relations.ClearAllRelations();
            }
        }

        //public static bool Zombify(ReanimatedPawn pawn)
        //{
        //    if (pawn.Drawer == null)
        //    {
        //        return false;
        //    }
        //    if (pawn.Drawer.renderer == null)
        //    {
        //        return false;
        //    }
        //    if (pawn.Drawer.renderer.graphics == null)
        //    {
        //        return false;
        //    }
        //    if (!pawn.Drawer.renderer.graphics.AllResolved)
        //    {
        //        pawn.Drawer.renderer.graphics.ResolveAllGraphics();
        //    }
        //    if (pawn.Drawer.renderer.graphics.headGraphic == null)
        //    {
        //        return false;
        //    }
        //    if (pawn.Drawer.renderer.graphics.nakedGraphic == null)
        //    {
        //        return false;
        //    }
        //    if (pawn.Drawer.renderer.graphics.headGraphic.path == null)
        //    {
        //        return false;
        //    }
        //    if (pawn.Drawer.renderer.graphics.nakedGraphic.path == null)
        //    {
        //        return false;
        //    }
        //    GiveZombieSkinEffect(pawn);
        //    return true;
        //}

        // Credit goes to Justin C for the Zombie Apocalypse code.
        // Taken from Verse.ZombieMod_Utility
        public static Pawn GenerateClonePawnFromSource(Pawn sourcePawn)
        {
            PawnKindDef pawnKindDef = PawnKindDef.Named("ReanimatedCorpse");
            Faction factionDirect = Find.FactionManager.FirstFactionOfDef(FactionDefOf.SpacerHostile);
            Pawn pawn = (Pawn)ThingMaker.MakeThing(pawnKindDef.race, null);
            pawn.kindDef = pawnKindDef;
            pawn.SetFactionDirect(factionDirect);
            pawn.pather = new Pawn_PathFollower(pawn);
            pawn.ageTracker = new Pawn_AgeTracker(pawn);
            pawn.health = new Pawn_HealthTracker(pawn);
            pawn.jobs = new Pawn_JobTracker(pawn);
            pawn.mindState = new Pawn_MindState(pawn);
            pawn.filth = new Pawn_FilthTracker(pawn);
            pawn.needs = new Pawn_NeedsTracker(pawn);
            pawn.stances = new Pawn_StanceTracker(pawn);
            pawn.natives = new Pawn_NativeVerbs(pawn);
            PawnComponentsUtility.CreateInitialComponents(pawn);
            if (pawn.RaceProps.ToolUser)
            {
                pawn.equipment = new Pawn_EquipmentTracker(pawn);
                pawn.carryTracker = new Pawn_CarryTracker(pawn);
                pawn.apparel = new Pawn_ApparelTracker(pawn);
                pawn.inventory = new Pawn_InventoryTracker(pawn);
            }
            if (pawn.RaceProps.Humanlike)
            {
                pawn.ownership = new Pawn_Ownership(pawn);
                pawn.skills = new Pawn_SkillTracker(pawn);
                pawn.relations = new Pawn_RelationsTracker(pawn);
                pawn.story = new Pawn_StoryTracker(pawn);
                pawn.workSettings = new Pawn_WorkSettings(pawn);
            }
            if (pawn.RaceProps.intelligence <= Intelligence.ToolUser)
            {
                pawn.caller = new Pawn_CallTracker(pawn);
            }
            //pawn.gender = Gender.None;
            pawn.gender = sourcePawn.gender;
            //Cthulhu.Utility.GenerateRandomAge(pawn, pawn.Map);
            pawn.ageTracker.AgeBiologicalTicks = sourcePawn.ageTracker.AgeBiologicalTicks;
            pawn.ageTracker.BirthAbsTicks = sourcePawn.ageTracker.BirthAbsTicks;

            pawn.needs.SetInitialLevels();
            if (pawn.RaceProps.Humanlike)
            {
                string headGraphicPath = sourcePawn.story.HeadGraphicPath;
                typeof(Pawn_StoryTracker).GetField("headGraphicPath", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(pawn.story, headGraphicPath);
                pawn.story.melanin = sourcePawn.story.melanin;
                pawn.story.crownType = sourcePawn.story.crownType;
                pawn.story.hairColor = sourcePawn.story.hairColor;
                NameTriple name = sourcePawn.Name as NameTriple;
                pawn.Name = name;
                pawn.story.childhood = sourcePawn.story.childhood;
                pawn.story.adulthood = sourcePawn.story.adulthood;
                pawn.story.hairDef = sourcePawn.story.hairDef;
                foreach (Trait current in sourcePawn.story.traits.allTraits)
                {
                    pawn.story.traits.GainTrait(current);
                }
                //pawn.story.GenerateSkillsFromBackstory();
            }
            GenerateApparelFromSource(pawn, sourcePawn);
            PawnGenerationRequest con = new PawnGenerationRequest();
            PawnInventoryGenerator.GenerateInventoryFor(pawn, con);
            Graphic nakedBodyGraphic = GraphicGetter_NakedHumanlike.GetNakedBodyGraphic(sourcePawn.story.bodyType, ShaderDatabase.CutoutSkin, sourcePawn.story.SkinColor);
            Graphic headGraphic = GraphicDatabase.Get<Graphic_Multi>(sourcePawn.story.HeadGraphicPath, ShaderDatabase.CutoutSkin, Vector2.one, sourcePawn.story.SkinColor);
            Graphic hairGraphic = GraphicDatabase.Get<Graphic_Multi>(sourcePawn.story.hairDef.texPath, ShaderDatabase.Cutout, Vector2.one, sourcePawn.story.hairColor);
            pawn.Drawer.renderer.graphics.headGraphic = headGraphic;
            pawn.Drawer.renderer.graphics.nakedGraphic = nakedBodyGraphic;
            pawn.Drawer.renderer.graphics.hairGraphic = hairGraphic;
            return pawn;
        }

        //static public void GenerateRandomAge(Pawn pawn, Map map)
        //{
        //    int num = 0;
        //    int num2;
        //    do
        //    {
        //        if (pawn.RaceProps.ageGenerationCurve != null)
        //        {
        //            num2 = Mathf.RoundToInt(Rand.ByCurve(pawn.RaceProps.ageGenerationCurve, 200));
        //        }
        //        else if (pawn.RaceProps.IsMechanoid)
        //        {
        //            num2 = Rand.Range(0, 2500);
        //        }
        //        else
        //        {
        //            if (!pawn.RaceProps.Animal)
        //            {
        //                goto IL_84;
        //            }
        //            num2 = Rand.Range(1, 10);
        //        }
        //        num++;
        //        if (num > 100)
        //        {
        //            goto IL_95;
        //        }
        //    }
        //    while (num2 > pawn.kindDef.maxGenerationAge || num2 < pawn.kindDef.minGenerationAge);
        //    goto IL_A5;
        //    IL_84:
        //    Log.Warning("Didn't get age for " + pawn);
        //    return;
        //    IL_95:
        //    Log.Error("Tried 100 times to generate age for " + pawn);
        //    IL_A5:
        //    pawn.ageTracker.AgeBiologicalTicks = ((long)(num2 * 3600000f) + Rand.Range(0, 3600000));
        //    int num3;
        //    if (Rand.Value < pawn.kindDef.backstoryCryptosleepCommonality)
        //    {
        //        float value = Rand.Value;
        //        if (value < 0.7f)
        //        {
        //            num3 = Rand.Range(0, 100);
        //        }
        //        else if (value < 0.95f)
        //        {
        //            num3 = Rand.Range(100, 1000);
        //        }
        //        else
        //        {
        //            int num4 = GenLocalDate.Year(map) - 2026 - pawn.ageTracker.AgeBiologicalYears;
        //            num3 = Rand.Range(1000, num4);
        //        }
        //    }
        //    else
        //    {
        //        num3 = 0;
        //    }
        //    long num5 = GenTicks.TicksAbs - pawn.ageTracker.AgeBiologicalTicks;
        //    num5 -= num3 * 3600000L;
        //    pawn.ageTracker.BirthAbsTicks = num5;
        //    if (pawn.ageTracker.AgeBiologicalTicks > pawn.ageTracker.AgeChronologicalTicks)
        //    {
        //        pawn.ageTracker.AgeChronologicalTicks = (pawn.ageTracker.AgeBiologicalTicks);
        //    }
        //}


        // More of Justin C's work. I can't take credit for this.
        // Verse.ZombieMod_Utility
        public static void GenerateApparelFromSource(Pawn newPawn, Pawn sourcePawn)
        {
            if (sourcePawn.apparel == null || sourcePawn.apparel.WornApparelCount == 0)
            {
                return;
            }
            foreach (Apparel current in sourcePawn.apparel.WornApparel)
            {
                Apparel apparel;
                if (current.def.MadeFromStuff)
                {
                    apparel = (Apparel)ThingMaker.MakeThing(current.def, current.Stuff);
                }
                else
                {
                    apparel = (Apparel)ThingMaker.MakeThing(current.def, null);
                }
                apparel.DrawColor = new Color(current.DrawColor.r, current.DrawColor.g, current.DrawColor.b, current.DrawColor.a);
                newPawn.apparel.Wear(apparel, true);
            }
        }



    }
}
