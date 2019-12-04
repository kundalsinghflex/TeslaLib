using System.Runtime.Serialization;

namespace TeslaLib.Models
{
    public enum RoofType
    {
        [EnumMember(Value = "Colored")]
        Colored,

        [EnumMember(Value = "None")]
        None,

        [EnumMember(Value = "Black")]
        Black,

        [EnumMember(Value = "Glass")]
        Glass,

        [EnumMember(Value = "AllGlassPanoramic")]
        AllGlassPanoramic,

        [EnumMember(Value = "ModelX")]
        ModelX,

        [EnumMember(Value = "Sunroof")]
        Sunroof,
    }
}
