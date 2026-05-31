namespace FacilityApp.Data.Models;

public enum MeterReadingType
{
    Opening   = 0,   // First reading when meter is commissioned
    ManualRead = 1,  // Staff physically reads the meter
    AutoRead  = 2,   // Smart meter automated push
    Estimated = 3,   // Estimated reading (actual not available)
    Closing   = 4,   // Final reading when meter is retired/replaced
}
