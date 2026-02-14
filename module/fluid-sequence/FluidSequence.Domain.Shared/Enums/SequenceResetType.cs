using System.ComponentModel;

namespace FluidSequence.Domain.Shared.Enums
{
    public enum SequenceResetType
    {
        [Description("从不重置")] None = 0,
        [Description("按日重置")] Daily = 1,
        [Description("按月重置")] Monthly = 2,
        [Description("按年重置")] Yearly = 3,
        [Description("按周重置")] Weekly = 4,
        [Description("按季度重置")] Quarterly = 5,
        [Description("按财年重置")] FiscalYearly = 6
    }
}
