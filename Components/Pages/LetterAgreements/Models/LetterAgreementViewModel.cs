using System;

namespace SSRBlazor.Components.Pages.LetterAgreements.Models;

/// <summary>
/// Flattened view model for displaying letter agreement data in the grid
/// Combines data from LetterAgreement and related entities
/// </summary>
public class LetterAgreementViewModel
{
    // Primary Key
    public int LetterAgreementID { get; set; }

    // Foreign Keys
    public int? AcquisitionID { get; set; }
    public int? LandManID { get; set; }

    // Dates
    public DateTime CreatedOn { get; set; }
    public DateTime LastUpdatedOn { get; set; }
    public DateTime? EffectiveDate { get; set; }

    // Financial
    public int? BankingDays { get; set; }
    public decimal? TotalBonus { get; set; }
    public decimal? ConsiderationFee { get; set; }
    public bool TakeConsiderationFromTotal { get; set; }
    public bool Referrals { get; set; }
    public decimal? ReferralFee { get; set; }

    // Calculated/Derived fields (from related tables)
    public string? SellerLastName { get; set; }
    public string? SellerName { get; set; }
    public string? SellerEmail { get; set; }
    public string? SellerPhone { get; set; }
    public string? SellerCity { get; set; }
    public string? SellerState { get; set; }
    public string? SellerZipCode { get; set; }

    public string? LandMan { get; set; }
    public string? DealStatus { get; set; }
    public string? CountyName { get; set; }
    public string? OperatorName { get; set; }
    public string? UnitName { get; set; }
    public decimal? TotalGrossAcres { get; set; }
    public decimal? TotalNetAcres { get; set; }
}
