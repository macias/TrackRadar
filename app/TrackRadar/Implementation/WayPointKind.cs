namespace TrackRadar.Implementation
{
    public enum WayPointKind
    {
        Regular, // alarms when leaving, when moving towards (with direction)
        Endpoint // alarm only when moving towards and without (!) direction
    }
}