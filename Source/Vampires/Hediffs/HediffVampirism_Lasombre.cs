﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Vampire
{
    public class HediffVampirism_Lasombra : HediffVampirism_VampGiver
    {
        public override BloodlineDef Bloodline => VampDefOf.ROMV_ClanLasombra;
    }
}
