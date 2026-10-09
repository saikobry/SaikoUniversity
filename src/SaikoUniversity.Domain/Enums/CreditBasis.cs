namespace SaikoUniversity.Domain.Enums;

/// <summary>
/// The unit in which <c>RequiredCredits</c> is expressed.
/// Kept as a separate field rather than folded into the credit count so a numeric value always has
/// one unambiguous meaning on its row.
/// </summary>
public enum CreditBasis
{
    /// <summary>Credit hours. The usual basis for degree programs.</summary>
    Hours = 1,

    /// <summary>Units. The usual basis for vocational and technical programs.</summary>
    Units = 2
}
