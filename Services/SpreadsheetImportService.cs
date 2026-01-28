using Microsoft.EntityFrameworkCore;
using SSRBusiness.BusinessClasses;
using SSRBusiness.Data;
using SSRBusiness.Entities;

namespace SSRBlazor.Services
{
    public class SpreadsheetImportService : ISpreadsheetImportService
    {
        private readonly SsrDbContext _context;
        private readonly AcquisitionRepository _acquisitionRepo;

        public SpreadsheetImportService(SsrDbContext context, AcquisitionRepository acquisitionRepo)
        {
            _context = context;
            _acquisitionRepo = acquisitionRepo;
        }

        public async Task<ImportResult> ImportDataAsync(string clipboardData, int acquisitionId, string userId)
        {
            var result = new ImportResult();
            var errorList = new List<string>();

            if (string.IsNullOrWhiteSpace(clipboardData))
            {
                result.Errors.Add("Clipboard data is empty.");
                return result;
            }

            var importValues = clipboardData.Split('\t').Select(s => s.Trim()).ToList();
            if (importValues.Count != 21)
            {
                result.Errors.Add("Incorrect number of columns (based on Tab spacing) in import line. Expected 21.");
                return result;
            }

            // Parse Columns
            try
            {
                string leaseName = importValues[0];
                string leaseCounties = importValues[1];
                string leaseStateCode = importValues[2];
                string acresStr = importValues[3];
                string survey = importValues[4];
                string abstractStr = importValues[5];
                string operatorName = importValues[6];
                string unitType = importValues[7];
                string unitInterestStr = importValues[8];
                string owner = importValues[9];
                string add1 = importValues[10];
                string add2 = importValues[11];
                // Swap address logic
                if (string.IsNullOrEmpty(add1) && !string.IsNullOrEmpty(add2))
                {
                    add1 = add2;
                    add2 = string.Empty;
                }
                string city = importValues[12];
                string stateCode = importValues[13];
                string zip = importValues[14];
                // string offer = importValues[15]; // Ignored in legacy
                // string offerDate = importValues[16]; // Ignored in legacy snippet visual
                string effectiveDateStr = importValues[17];
                // string letterOffer = importValues[18]; // Ignored
                string moneyOfferStr = importValues[19].Replace("$", "").Replace(",", "");
                // string monthlyRevenue = importValues[20]; // Ignored

                // Load Acquisition
                var acquisition = await _context.Acquisitions
                    .Include(a => a.AcquisitionSellers)
                    .FirstOrDefaultAsync(a => a.AcquisitionID == acquisitionId);

                if (acquisition == null)
                {
                    result.Errors.Add($"Acquisition ID {acquisitionId} not found.");
                    return result;
                }

                // Track Changes for Acquisition
                var acqEntry = _context.Entry(acquisition);
                // Snapshot values? We stick to direct modification.

                // 1. Process Seller
                // Legacy logic: Load AcquisitionSeller by AcquisitionID. If null, new.
                // In Blazor/EF Core, we check the collection.
                // Assuming "Primary" seller is the first one or we add one.
                // Legacy: `acqSeller.LoadAcquisitionSellerByAcquisitionID`.

                AcquisitionSeller acqSeller = acquisition.AcquisitionSellers.FirstOrDefault();
                bool isNewSeller = false;

                if (acqSeller == null)
                {
                    isNewSeller = true;
                    acqSeller = new AcquisitionSeller
                    {
                        AcquisitionID = acquisitionId,
                        CreatedOn = DateTime.Now,
                    };
                    // Since AcquisitionSeller links to Seller, we might need a Seller entity.
                    // But legacy sets SellerName heavily on AcqSeller (denormalized?) or on the Seller object?
                    // Legacy: `acqSeller.Entity.SellerName = ...`.
                    // In current Entity model, let's check AcquisitionSeller definition.
                    // Usually it links to a Seller entity.
                    // If AcquisitionSeller has direct fields, update them.
                    _context.AcquisitionSellers.Add(acqSeller); // Add to context
                }

                acqSeller.LastUpdatedOn = DateTime.Now;

                // Update fields if empty and provided
                if (string.IsNullOrEmpty(acqSeller.SellerName) && !string.IsNullOrEmpty(owner))
                    acqSeller.SellerName = owner;

                if (string.IsNullOrEmpty(acqSeller.SellerLastName) && !string.IsNullOrEmpty(owner))
                {
                    var names = owner.Split(' ').ToList();
                    acqSeller.SellerLastName = names.LastOrDefault();
                }

                if (string.IsNullOrEmpty(acqSeller.AddressLine1) && !string.IsNullOrEmpty(add1))
                    acqSeller.AddressLine1 = add1;

                if (string.IsNullOrEmpty(acqSeller.AddressLine2) && !string.IsNullOrEmpty(add2))
                    acqSeller.AddressLine2 = add2;

                if (string.IsNullOrEmpty(acqSeller.City) && !string.IsNullOrEmpty(city))
                    acqSeller.City = city;

                if (string.IsNullOrEmpty(acqSeller.StateCode) && !string.IsNullOrEmpty(stateCode))
                    acqSeller.StateCode = stateCode;

                if (string.IsNullOrEmpty(acqSeller.ZipCode) && !string.IsNullOrEmpty(zip))
                    acqSeller.ZipCode = zip;

                // 2. Process Acquisition Fields
                DateTime effDate;
                if (!acquisition.EffectiveDate.HasValue && !string.IsNullOrEmpty(effectiveDateStr) && DateTime.TryParse(effectiveDateStr, out effDate))
                {
                    acquisition.EffectiveDate = effDate;
                }

                if (!string.IsNullOrEmpty(moneyOfferStr) && decimal.TryParse(moneyOfferStr, out var moneyOffer))
                {
                    acquisition.TotalBonus = (acquisition.TotalBonus ?? 0) + moneyOffer;
                }

                // Recalc TotalBonusAndFee
                decimal totalBonus = acquisition.TotalBonus ?? 0;
                decimal taxAmountPaid = acquisition.TaxAmountPaid ?? 0;
                decimal considerationFee = acquisition.ConsiderationFee ?? 0;
                acquisition.TotalBonusAndFee = totalBonus - considerationFee - taxAmountPaid;

                // 3. Process Unit
                if (!string.IsNullOrEmpty(leaseName))
                {
                    // Check duplicate
                    bool unitExists = await _context.AcquisitionUnits
                        .AnyAsync(u => u.AcquisitionID == acquisitionId && u.UnitName == leaseName);

                    if (unitExists)
                    {
                        result.Errors.Add($"Unit '{leaseName}' already exists. Please enter a different unit name.");
                        // Standard legacy behavior seemed to be "Error list" but halt there? 
                        // The legacy continues check.
                    }
                    else
                    {
                        var newUnit = new AcquisitionUnit
                        {
                            AcquisitionID = acquisitionId,
                            UnitName = leaseName.Length > 200 ? leaseName.Substring(0, 200) : leaseName,
                            SsrInPay = "N"
                        };

                        if (!string.IsNullOrEmpty(unitInterestStr) && decimal.TryParse(unitInterestStr, out var interest))
                            newUnit.UnitInterest = interest;

                        if (unitType == "RI" || unitType == "OR")
                            newUnit.UnitTypeCode = unitType; // Assuming mapping exists or string matches

                        if (!string.IsNullOrEmpty(acresStr) && decimal.TryParse(acresStr, out var acres))
                        {
                            newUnit.GrossAcres = acres;
                            newUnit.NetAcres = acres;
                        }

                        string surveyAndAbstract = $"{survey} {abstractStr}".Trim();
                        newUnit.Surveys = surveyAndAbstract.Length > 500 ? surveyAndAbstract.Substring(0, 500) : surveyAndAbstract;

                        // Add Unit
                        _context.AcquisitionUnits.Add(newUnit);
                        // Save to generate ID for child relations
                        await _context.SaveChangesAsync();

                        // Log Creation (Placeholder for ActionService or specialized logging)
                        // LogAcquisitionChange(userId, "C", ...);

                        // 4. Process Counties (Linked to Unit)
                        if (!string.IsNullOrEmpty(leaseCounties) && !string.IsNullOrEmpty(leaseStateCode))
                        {
                            var countyList = leaseCounties.Split('&').Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c)).ToList();
                            foreach (var countyName in countyList)
                            {
                                var county = await _context.Counties.FirstOrDefaultAsync(c => c.StateCode == leaseStateCode && c.CountyName == countyName);
                                if (county != null)
                                {
                                    bool acqUnitCountyExists = await _context.AcqUnitCounties.AnyAsync(auc => auc.AcquisitionUnitID == newUnit.AcquisitionUnitID && auc.CountyID == county.CountyID);

                                    if (acqUnitCountyExists)
                                    {
                                        result.Errors.Add($"This county ({countyName}) exists for this unit already.");
                                    }
                                    else
                                    {
                                        var newAcqUnitCounty = new AcqUnitCounty
                                        {
                                            AcquisitionID = acquisitionId,
                                            AcquisitionUnitID = newUnit.AcquisitionUnitID,
                                            CountyID = county.CountyID
                                        };
                                        _context.AcqUnitCounties.Add(newAcqUnitCounty);
                                        await _context.SaveChangesAsync();

                                        // Ensure AcquisitionCounty exists
                                        bool acqCountyExists = await _context.AcquisitionCounties.AnyAsync(ac => ac.AcquisitionID == acquisitionId && ac.CountyID == county.CountyID);
                                        if (!acqCountyExists)
                                        {
                                            _context.AcquisitionCounties.Add(new AcquisitionCounty
                                            {
                                                AcquisitionID = acquisitionId,
                                                CountyID = county.CountyID
                                            });
                                            await _context.SaveChangesAsync();
                                        }

                                        // 5. Process Operator (Linked to UnitCounty)
                                        if (!string.IsNullOrEmpty(operatorName))
                                        {
                                            var op = await _context.Operators.FirstOrDefaultAsync(o => o.OperatorName == operatorName);
                                            if (op != null)
                                            {
                                                bool opExists = await _context.AcqUnitCountyOperators.AnyAsync(auco => auco.AcqUnitCountyID == newAcqUnitCounty.AcqUnitCountyID && auco.OperatorID == op.OperatorID);
                                                if (opExists)
                                                {
                                                    result.Errors.Add("This operator exists for this unit and county already.");
                                                }
                                                else
                                                {
                                                    var newOpLink = new AcqUnitCountyOperator
                                                    {
                                                        AcquisitionID = acquisitionId,
                                                        AcqUnitCountyID = newAcqUnitCounty.AcqUnitCountyID,
                                                        OperatorID = op.OperatorID
                                                    };
                                                    _context.AcqUnitCountyOperators.Add(newOpLink);
                                                    await _context.SaveChangesAsync();

                                                    // Ensure AcquisitionOperator
                                                    bool acqOpExists = await _context.AcquisitionOperators.AnyAsync(ao => ao.AcquisitionID == acquisitionId && ao.OperatorID == op.OperatorID);
                                                    if (!acqOpExists)
                                                    {
                                                        _context.AcquisitionOperators.Add(new AcquisitionOperator
                                                        {
                                                            AcquisitionID = acquisitionId,
                                                            OperatorID = op.OperatorID
                                                        });
                                                        await _context.SaveChangesAsync();
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                result.Errors.Add($"Unable to locate Operator '{operatorName}'.");
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    result.Errors.Add($"Unable to locate County '{countyName}' in State '{leaseStateCode}'.");
                                }
                            }
                        }
                    }
                }
                else
                {
                    result.Errors.Add("Unit name is required.");
                }

                // Final save for any pending changes (e.g. Acquisition fields)
                await _context.SaveChangesAsync();
                result.Success = result.Errors.Count == 0;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error processing import: {ex.Message}");
                result.Success = false;
            }

            return result;
        }
    }
}
