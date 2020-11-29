using System.Runtime.CompilerServices;
using TrackRadar.Implementation;

[assembly: InternalsVisibleTo("TrackRadar.Tests")]
[assembly: InternalsVisibleTo("TrackRadar.Tests2")]

namespace TrackRadar
{
    public enum AlarmSound
    {
        GoAhead = TurnKind.GoAhead,

        LeftEasy = TurnKind.LeftEasy,
        LeftCross = TurnKind.LeftCross,
        LeftSharp = TurnKind.LeftSharp,

        RightEasy = TurnKind.RightEasy,
        RightCross = TurnKind.RightCross,
        RightSharp = TurnKind.RightSharp,  // WATCH OUT -- need to be the last of TurnKinds


        OffTrack,
        GpsLost,
        BackOnTrack,
        DoubleTurn,
        Crossroad,

        Disengage,
    }
}
