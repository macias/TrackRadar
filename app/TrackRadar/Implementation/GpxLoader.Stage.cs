﻿using Geo;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrackRadar.Implementation
{
    public partial class GpxLoader
    {
        private static readonly int StageCount = Enum.GetValues(typeof(Stage)).Length;

        public enum Stage
        {
            Loading,
            ComputingCrossroads,
            RemovePassingBy,
            AddingEndpoints,
            SplitByWaypoints,
            AssigningTurns,
            SectionId,
            AlternateTurns,
            //TurnsToTurnsGraph,
        }
    }
}
