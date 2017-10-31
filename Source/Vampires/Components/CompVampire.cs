using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using AbilityUser;
using Verse.AI;

namespace Vampire
{
    public enum VampState : int
    {
        None = 0,
        Forming = 1,
        Active = 2,
        Torpor = 3,
        FinalDeath = 4
    }

    public class CompVampire : CompAbilityUser
    {
        #region Variables
        private Pawn sire = null;
        private List<Pawn> childer = null;
        private List<Pawn> ghouls = null;
        private List<Pawn> souls = new List<Pawn>();
        private SkillSheet sheet = null;
        private BloodlineDef bloodline = null;
        private int generation = -1;
        private int level = 0;
        private int xp = 0;
        private int abilityPoints = 0;
        private PawnKindDef currentForm = null;

        public int ticksToLearnXP = -1;


        #endregion Variables

        #region Access Properties
        public int Level { get => level;
            set
            {
                if (value > level)
                {
                    abilityPoints++;
                    if (XP < value * 600)
                    {
                        XP = value * 600;
                    }
                }
                level = value;
            }
        }
        public int XP { get => xp; set => xp = value; }

        public float XPLastLevel
        {
            get
            {
                float result = 0f;
                if (level > 0) result = level * 600;
                return result;
            }
        }
        public float XPTillNextLevelPercent
        {
            get
            {
                return ((float)(xp - XPLastLevel) / (float)(XPTillNextLevel - XPLastLevel));
            }
        }
        public int XPTillNextLevel
        {
            get
            {
                return (level + 1) * 600;
            }
        }

        public int AbilityPoints { get => abilityPoints; set => abilityPoints = value; }
        public bool Transformed => currentForm != null;
        public PawnKindDef CurrentForm { get => currentForm; set { currentForm = value; }}
        private Graphic curFormGraphic = null;
        public Graphic CurFormGraphic { get => curFormGraphic; set => curFormGraphic = value; }

        public SkillSheet Sheet
        {
            get
            {
                if (sheet == null)
                {
                    sheet = new SkillSheet(this.Pawn);
                }
                return sheet;
            }
        }

        public Pawn Sire { get => sire; set => sire = value; }
        public List<Pawn> Childer
        {
            get
            {
                if (childer == null)
                {
                    childer = new List<Pawn>();
                    if (this.AbilityUser?.relations?.DirectRelations is List<DirectPawnRelation> rels)
                    {
                        foreach (DirectPawnRelation rel in rels)
                        {
                            if (rel.def == VampDefOf.ROMV_Childe)
                            {
                                childer.Add(rel.otherPawn);
                            }
                        }
                    }

                }
                return childer;
            }
        }
        //public List<Pawn> Ghouls
        //{
        //    get
        //    {
        //        if (ghouls == null)
        //        {
        //            ghouls = new List<Pawn>();
        //            if (this.AbilityUser?.relations?.DirectRelations is List<DirectPawnRelation> rels)
        //            {
        //                foreach (DirectPawnRelation rel in rels)
        //                {
        //                    if (rel.def == VampDefOf.ROMV_Childe)
        //                    {
        //                        ghouls.Add(rel.otherPawn);
        //                    }
        //                }
        //            }

        //        }
        //        return ghouls;
        //    }
        //}
        public List<Pawn> Souls
        {
            get
            {
                if (souls == null)
                {
                    souls = new List<Pawn>();
                }
                return souls;
            }
        }
        public BloodlineDef Bloodline { get => bloodline; set => bloodline = value; }
        public int Generation { get => generation; set => generation = value; }
        public bool Thinblooded => this.generation > 13;
        public Need_Blood BloodPool => this.AbilityUser?.needs?.TryGetNeed<Need_Blood>() ?? null;
        public float TrueCombatPower
        {
            get
            {
                float result = 0;
                result += this.AbilityUser.kindDef.combatPower;
                result += 4000;
                for (int i = 1; i <= this.generation; i++)
                {
                    if (i == 2) { result -= 3000; continue; }
                    if (i < 7)  { result -= 100; continue; }
                    result -= 5;
                }
                return result;
            }
        }
        public bool IsVampire => AbilityUser?.health?.hediffSet?.HasHediff(VampDefOf.ROM_Vampirism) ?? false;
        public bool InSunlight
        {
            get
            {
                if (this.AbilityUser.Spawned)
                {
                    Map curMap = this.AbilityUser.Map;
                    if (curMap != null)
                    {
                        if ((GenLocalDate.HourInteger(this.AbilityUser) >= 6 && GenLocalDate.HourInteger(this.AbilityUser) <= 17)
                            && !this.AbilityUser.PositionHeld.Roofed(curMap))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }
        #endregion Access Properties

        #region Utility Props
        public Pawn Pawn => this.AbilityUser;
        public Need_Blood Blood => this?.Pawn?.needs?.TryGetNeed<Need_Blood>();
        public Need_Blood BloodOther(Pawn other) => other?.needs?.TryGetNeed<Need_Blood>();
        #endregion Utility Props

        #region Methods
        public void Notify_LevelUp(bool sendNotification)
        {
            if (XP <= 0) XP = 1;
            Level++;
        }
        public void Notify_ResetAbilities()
        {
            Sheet.ResetDisciplines();
        }

        public void Notify_UpdateAbilities()
        {
            if (Sheet?.Disciplines is List<Discipline> dd && !dd.NullOrEmpty())
            {
                foreach (Discipline d in dd)
                {
                    if (d?.AvailableAbilities is List<VitaeAbilityDef> vdd && !vdd.NullOrEmpty())
                    {
                        foreach (VitaeAbilityDef vd in vdd)
                        {
                            if (this.Powers.FirstOrDefault(x => x.Def.defName == vd.defName) == null)
                            {
                                this.AddPawnAbility(vd);
                            }

                        }
                    }
                }
            }
        }

        public void GiveFeedJob(Pawn victim)
        {
            Job feedJob = new Job(VampDefOf.ROMV_Feed, victim);
            this.AbilityUser.jobs.TryTakeOrderedJob(feedJob, JobTag.SatisfyingNeeds);
        }

        public void GiveEmbraceJob(Pawn newChilde)
        {
            Job embraceJob = new Job(VampDefOf.ROMV_Embrace, newChilde);
            this.AbilityUser.jobs.TryTakeOrderedJob(embraceJob, JobTag.Misc);
        }
        
        public void InitializeVampirism(Pawn newSire, BloodlineDef newBloodline = null, int newGeneration = -1, bool firstVampire = false)
        {
            VampireGen.TryGiveVampirismHediff(this.AbilityUser, newGeneration, newBloodline, newSire, firstVampire);
            if (!firstVampire)
            {
                this.bloodline = newBloodline;// sireComp.Bloodline;
                this.generation = newGeneration;// + 1;
                this.sire = newSire;
                VampireRelationUtility.SetSireChildeRelations(this.AbilityUser, newSire?.VampComp() ?? null, newGeneration);
            }
            else
            {
                this.generation = 1;
                this.bloodline = VampDefOf.ROMV_Caine;
                this.sire = null;
            }
            VampireGen.TryGiveVampireAdditionalHediffs(this.AbilityUser);
            if (VampireUtility.IsDaylight(this.AbilityUser)) VampireUtility.MakeSleepy(this.AbilityUser);
            VampireUtility.AdjustTimeTables(this.AbilityUser);

            this.sheet = new SkillSheet(this.AbilityUser);
            this.sheet.InitializeDisciplines();
            this.Notify_LevelUp(false);
            this.AbilityUser.needs.AddOrRemoveNeedsAsAppropriate(); //This removes "food" and adds "blood"
            if (!bloodline.allowsHair)
                AbilityUser.story.hairDef = DefDatabase<HairDef>.GetNamed("Shaved");
        }

        public void Notify_Starving(int lastNonStarvingTick)
        {

        }

        public void Notify_Embraced(CompVampire sireComp)
        {
            InitializeVampirism(sireComp.AbilityUser, sireComp.Bloodline, sireComp.Generation + 1, false);
            Messages.Message("ROMV_EmbracedSuccessfully".Translate(new object[]
            {
                this.AbilityUser.LabelShort,
                sireComp.AbilityUser.LabelShort,
                sireComp.Bloodline.LabelCap
            }), MessageSound.Benefit);
        }
        
        public void Notify_Diablerie(CompVampire victim)
        {
            Messages.Message("ROMV_DiablerieSuccessfully".Translate(new object[]
{
                this.AbilityUser.LabelShort,
                victim.AbilityUser.LabelShort
            }), MessageSound.Benefit);
            this.Generation = Math.Min(this.Generation, victim.Generation);
            this.Souls.Add(victim.AbilityUser);
            VampireThoughtUtility.GiveThoughtsForDiablerie(this.AbilityUser);
        }

        #endregion Methods

        #region Overrides
        public override bool TryTransformPawn() => Bloodline != null;

        /// <summary>
        /// Prevents natural death in most circumstances.
        /// Checks to see if the Vampire needs to enter a Torpor state or not.
        /// Reacts to stake paralysis
        /// </summary>
        //public override void PostPreApplyDamage(DamageInfo dinfo, out bool absorbed)
        //{
        //    //if should kill, and not decapitation or massive body squish, start Torpor state
        //    absorbed = true;
        //}

        public override bool AllowStackWith(Thing other)
        {
            return base.AllowStackWith(other);

        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Find.Selector.NumSelected == 1)
            {
                for (int i = 0; i < this.AllPowers.Count; i++)
                {
                    if (this.AllPowers[i] is VampAbility p && (p.AbilityDef.MainVerb.hasStandardCommand && p.AbilityDef.bloodCost != 0)) yield return p.GetGizmo();
                }
            }

        }

        public override void PostExposeData()
        {
            Scribe_Defs.Look<BloodlineDef>(ref this.bloodline, "bloodline");
            Scribe_Values.Look<int>(ref this.generation, "generation");
            Scribe_Values.Look<int>(ref this.level, "level", 0);
            Scribe_Values.Look<int>(ref this.abilityPoints, "abilityPoints", 0);
            Scribe_References.Look<Pawn>(ref this.sire, "sire");
            Scribe_Collections.Look<Pawn>(ref this.souls, "souls", LookMode.Reference);
            Scribe_Deep.Look<SkillSheet>(ref this.sheet, "sheet", new object[] { this.AbilityUser });
            base.PostExposeData();
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.Powers.Clear();
                if (this.AbilityUser.IsVampire() && (base.Powers == null || base.Powers.NullOrEmpty()))
                {
                    if (this.Sheet.Disciplines is List<Discipline> dd && !dd.NullOrEmpty())
                    {
                        foreach (Discipline d in dd)
                        {
                            if (d.AvailableAbilities is List<VitaeAbilityDef> vds && !vds.NullOrEmpty())
                            {
                                foreach (VitaeAbilityDef vd in vds)
                                {
                                    this.AddPawnAbility(vd);
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion Overrides
    }
}
