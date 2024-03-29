﻿namespace TrackRadar.Implementation
{
    public partial class GpxLoader
    {
        // todo: private
        internal enum CrossroadKind
        {
            None, // regular point, not a crossroad

            Intersection, // true intersection, X
            Extension, // -- * -- (it is connection between ends of the tracks)
            PassingBy, // distant "intersection", > * <
            Endpoint // simply the end of the track, not crossroad per se
        }
        
    }
}
