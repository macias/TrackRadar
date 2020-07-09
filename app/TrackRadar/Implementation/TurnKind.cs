namespace TrackRadar.Implementation
{
    public enum TurnKind
    {
        GoAhead,

        LeftEasy,
        LeftCross, // means L-turn, I didn't find any good name for it
        LeftSharp,

        RightEasy,
        RightCross,
        RightSharp,
    }
}