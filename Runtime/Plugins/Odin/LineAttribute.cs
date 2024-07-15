using System;

namespace Baracuda.Bedrock.Odin
{
    [Flags]
    public enum DrawTiming
    {
        Before = 1,
        After = 2
    }

    public class LineAttribute : Attribute
    {
        public DrawTiming DrawTiming { get; set; } = DrawTiming.Before;
        public float SpaceBefore { get; set; } = 4;
        public float SpaceAfter { get; set; } = 4;
    }

    public class LineAfterAttribute : LineAttribute
    {
        public LineAfterAttribute()
        {
            DrawTiming = DrawTiming.After;
        }
    }
}