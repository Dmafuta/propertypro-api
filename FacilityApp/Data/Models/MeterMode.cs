namespace FacilityApp.Data.Models;

public enum MeterMode
{
    Analogue = 0,   // Postpaid — staff reads monthly, billed after consumption
    Prepaid  = 1,   // Token/credit based — resident loads credit
    Smart    = 2,   // IoT / remote automated reads
}
