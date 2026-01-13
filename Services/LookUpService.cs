using Microsoft.EntityFrameworkCore;
using SSRBusiness.BusinessClasses;
using SSRBusiness.Entities;

namespace SSRBlazor.Services;

/// <summary>
/// Service for common lookup operations across the application
/// </summary>
public class LookUpService
{
    private readonly StateRepository _stateRepository;
    private readonly ILogger<LookUpService> _logger;

    public LookUpService(
        StateRepository stateRepository,
        ILogger<LookUpService> logger)
    {
        _stateRepository = stateRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all states ordered by code
    /// </summary>
    public async Task<List<State>> GetStatesAsync()
    {
        try
        {
            return await _stateRepository.GetStateListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching states");
            return new List<State>();
        }
    }
}
