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
        Disengage,

        GoAhead,
        LeftEasy,
        LeftCross,
        LeftSharp,
        RightEasy,
        RightCross,
        RightSharp,

    }
}
// todo: remove me
/*
GoAhead
LeftEasy
LeftCross
LeftSharp
RightEasy
RightCross
RightSharp
*/