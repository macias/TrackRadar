using System;

namespace TrackRadar.Implementation
{
    public static class Formatter
    {
        public static string ZuluFormat(DateTimeOffset dto)
        {
            return dto.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'FFFFFFF'Z'");
        }

        public static string FormatShortDateTime(DateTimeOffset dto)
        {
            return DateTimeOffset.Now.ToString("dd_HH:mm:ss.fff");
        }

    }
}