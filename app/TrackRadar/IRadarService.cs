using MathUnit;
using System;

namespace TrackRadar
{   
    public interface IRadarService : ILogger
    {
        TimeSpan OffTrackAlarmInterval { get; }
        Length OffTrackAlarmDistance { get; }
        TimeSpan TurnAheadAlarmDistance { get; }
        TimeSpan TurnAheadAlarmInterval { get; }
        Speed RestSpeedThreshold { get; }
        Speed RidingSpeedThreshold { get; }

        void WriteCrossroad(double latitudeDegrees, double longitudeDegrees);
        void WriteOffTrack(double latitudeDegrees, double longitudeDegrees, string name = null);
        //bool TryAlarm(Alarm alarm,out string reason);
        //bool TryGetLatestTurnAheadAlarmAt(out long timeStamp);
    }
}