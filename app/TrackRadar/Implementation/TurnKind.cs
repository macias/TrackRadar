namespace TrackRadar.Implementation
{
    public enum TurnKind
    {
        GoAhead = Alarm.GoAhead,

        LeftEasy = Alarm.LeftEasy,
        LeftCross = Alarm.LeftCross, // means L-turn, I didn't find any good name for it
        LeftSharp = Alarm.LeftSharp, 

        RightEasy = Alarm.RightEasy,
        RightCross = Alarm.RightCross,
        RightSharp = Alarm.RightSharp,
    }
}