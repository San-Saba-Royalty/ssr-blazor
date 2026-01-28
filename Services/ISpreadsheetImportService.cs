using SSRBusiness.Entities;

namespace SSRBlazor.Services
{
    public class ImportResult
    {
        public bool Success { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> ChangeLog { get; set; } = new();
    }

    public interface ISpreadsheetImportService
    {
        Task<ImportResult> ImportDataAsync(string clipboardData, int acquisitionId, string userId);
    }
}
