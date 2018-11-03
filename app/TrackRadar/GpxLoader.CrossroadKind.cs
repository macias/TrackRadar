namespace TrackRadar
{
    internal partial class GpxLoader
    {
        private enum CrossroadKind
        {
            None, // regular point, not a crossroad

            Intersection, // true intersection, X
            Extension, // -- * --
            PassingBy, // distant "intersection", > * <
        }
        
    }
}
