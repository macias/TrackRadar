namespace TrackRadar
{
    public partial class GpxLoader
    {
        // todo: private
        internal enum CrossroadKind
        {
            None, // regular point, not a crossroad

            Intersection, // true intersection, X
            Extension, // -- * --
            PassingBy, // distant "intersection", > * <
        }
        
    }
}
