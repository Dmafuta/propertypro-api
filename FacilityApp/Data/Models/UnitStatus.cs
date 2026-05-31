namespace FacilityApp.Data.Models;

public enum UnitStatus
{
    Available       = 0,
    Occupied        = 1,
    Vacant          = 2,   // Owner-occupied but temporarily unoccupied
    UnderMaintenance = 3,
    Reserved        = 4,
}
