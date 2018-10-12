namespace TrackRadar
{
    internal partial class GpxLoader
    {
        private enum CrossroadKind
        {
            Intersection, // true intersection, X
            Extension, // -- * --
            PassingBy, // distant "intersection", > * <
        }
        
    }
}
