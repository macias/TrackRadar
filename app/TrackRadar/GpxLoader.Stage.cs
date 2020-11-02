using Geo;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrackRadar
{
    public partial class GpxLoader
    {
        private static readonly int StageCount = Enum.GetValues(typeof(Stage)).Length;

        internal enum Stage
        {
            Loading,
            ComputingCrossroads,
            SplitByWaypoints,
            AssigningTurns,
            SectionId,
            AlternateTurns,
            //TurnsToTurnsGraph,
        }
    }
}
