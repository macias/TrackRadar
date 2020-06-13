using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TrackRadar.Tests")]

namespace TrackRadar
{    
    public enum Alarm
    {
        OffTrack,
        GpsLost,
        PositiveAcknowledgement,
        Crossroad,
    }
}