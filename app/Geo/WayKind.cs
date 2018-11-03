using System.Collections.Generic;
using System.Linq;

namespace Geo
{
    public sealed class WayKind
    {
        public static readonly WayKind Motorway = new WayKind("motorway", 0);
        public static readonly WayKind MotorwayLink = new WayKind("motorway_link", 0);

        public static readonly WayKind Trunk = new WayKind("trunk", 1);
        public static readonly WayKind TrunkLink = new WayKind("trunk_link", 1);

        public static readonly WayKind Primary = new WayKind("primary", 2);
        public static readonly WayKind PrimaryLink = new WayKind("primary_link", 2);

        public static readonly WayKind Secondary = new WayKind("secondary", 3);
        public static readonly WayKind SecondaryLink = new WayKind("secondary_link", 3);

        public static readonly WayKind Tertiary = new WayKind("tertiary", 4);
        public static readonly WayKind TertiaryLink = new WayKind("tertiary_link", 4);

        public static readonly WayKind Other = new WayKind("Other", 5);
        private static readonly WayKind[] values = new[] {
            Motorway,
            MotorwayLink,
            Trunk,
            TrunkLink,
            Primary,
            PrimaryLink,
            Secondary,
            SecondaryLink,
            Tertiary,
            TertiaryLink,
            Other
        };

        public static IReadOnlyList<WayKind> Values { get; } = values;

        public string Name { get; }
        private readonly int priority;

        public byte IndexOf
        {
            get
            {
                for (byte i = 0; i < values.Length; ++i)
                    if (values[i] == this)
                        return i;

                throw new KeyNotFoundException();
            }
        }

        private WayKind(string name, int priority)
        {
            this.Name = name;
            this.priority = priority;
        }

        public bool IsMoreImportant(WayKind other)
        {
            return this.priority < other.priority;
        }
    }
}