namespace DogruFatura.Core.Models;

public enum WaterPaymentRecipient
{
    // Historical records must retain their original VAT and calculation.
    Unspecified = 0,
    Municipality = 1,
    Company = 2
}
