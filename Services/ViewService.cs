using Microsoft.EntityFrameworkCore;
using SSRBlazor.Models;
using SSRBusiness.BusinessClasses;
using SSRBusiness.Entities;

namespace SSRBlazor.Services;

/// <summary>
/// Service for View business logic
/// </summary>
public class ViewService
{
    private readonly ViewRepository _repository;
    private readonly DisplayFieldRepository _fieldRepository;
    private readonly UserRepository _userRepository;
    private readonly ILogger<ViewService> _logger;

    public ViewService(
        ViewRepository repository,
        DisplayFieldRepository fieldRepository,
        UserRepository userRepository,
        ILogger<ViewService> logger)
    {
        _repository = repository;
        _fieldRepository = fieldRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    #region View Operations

    /// <summary>
    /// Apply a view to a user's session/profile
    /// </summary>
    public async Task<(bool Success, string? Error)> ApplyViewToUserAsync(string userId, int? viewId)
    {
        try
        {
            var user = await _userRepository.LoadUserByUserIdAsync(userId);
            if (user == null)
            {
                return (false, "User not found");
            }

            user.LastViewID = viewId;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("Applied view {ViewId} to user {UserId}", viewId, userId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying view to user");
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Get all views for dropdown
    /// </summary>
    public async Task<List<View>> GetAllViewsAsync()
    {
        return await _repository.GetViewsAsync()
            .OrderBy(v => v.ViewName)
            .ToListAsync();
    }

    /// <summary>
    /// Get view by ID
    /// </summary>
    public async Task<View?> GetByIdAsync(int viewId)
    {
        return await _repository.GetByIdNoTrackingAsync(viewId);
    }

    /// <summary>
    /// Get view configuration with all field selections
    /// </summary>
    public async Task<ViewConfiguration?> GetViewConfigurationAsync(int viewId)
    {
        var view = await _repository.GetByIdNoTrackingAsync(viewId);
        if (view == null) return null;

        var allFields = await _fieldRepository.GetDisplayFieldsAsync();
        var selectedFields = await _repository.GetViewFieldsAsync(viewId);

        var config = new ViewConfiguration
        {
            ViewID = view.ViewID,
            ViewName = view.ViewName ?? string.Empty,
            Fields = allFields.Select(f =>
            {
                var selection = selectedFields.FirstOrDefault(sf => sf.FieldID == f.FieldID);
                return new ViewFieldSelection
                {
                    FieldID = f.FieldID,
                    FieldName = f.FieldName,
                    DisplayName = f.FieldName,
                    IsSelected = selection != null,
                    DisplayOrder = selection?.DisplayOrder ?? f.DisplayOrder,
                    ViewFieldID = selection?.ViewFieldID
                };
            }).ToList()
        };

        return config;
    }

    /// <summary>
    /// Get blank view configuration for new view
    /// </summary>
    public async Task<ViewConfiguration> GetNewViewConfigurationAsync()
    {
        var allFields = await _fieldRepository.GetDisplayFieldsAsync();
        return new ViewConfiguration
        {
            ViewID = 0,
            ViewName = string.Empty,
            Fields = allFields.Select(f => new ViewFieldSelection
            {
                FieldID = f.FieldID,
                FieldName = f.FieldName,
                DisplayName = f.FieldName,
                IsSelected = false,
                DisplayOrder = f.DisplayOrder
            }).ToList()
        };
    }

    /// <summary>
    /// Create new view
    /// </summary>
    public async Task<(bool Success, View? View, string? Error)> CreateAsync(ViewConfiguration config)
    {
        try
        {
            // Validate name
            if (string.IsNullOrWhiteSpace(config.ViewName))
            {
                return (false, null, "View name is required");
            }

            // Check for duplicate name
            if (await _repository.ViewNameExistsAsync(config.ViewName))
            {
                return (false, null, "A view with this name already exists");
            }

            // Create view
            var view = new View
            {
                ViewName = config.ViewName
            };

            var created = await _repository.AddAsync(view);

            // Add selected fields
            var selectedFields = config.Fields
                .Where(f => f.IsSelected)
                .Select((f, index) => new ViewField
                {
                    ViewID = created.ViewID,
                    FieldID = f.FieldID,
                    DisplayOrder = index + 1
                })
                .ToList();

            if (selectedFields.Any())
            {
                await _repository.AddViewFieldsAsync(selectedFields);
            }

            _logger.LogInformation("Created view {ViewId}: {ViewName}", created.ViewID, created.ViewName);
            return (true, created, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating view");
            return (false, null, ex.Message);
        }
    }

    /// <summary>
    /// Update existing view
    /// </summary>
    public async Task<(bool Success, View? View, string? Error)> UpdateAsync(ViewConfiguration config)
    {
        try
        {
            // Validate
            if (config.ViewID <= 0)
            {
                return (false, null, "Invalid view ID");
            }

            if (string.IsNullOrWhiteSpace(config.ViewName))
            {
                return (false, null, "View name is required");
            }

            // Check for duplicate name
            if (await _repository.ViewNameExistsAsync(config.ViewName, config.ViewID))
            {
                return (false, null, "A view with this name already exists");
            }

            // Get existing view
            var view = await _repository.GetByIdAsync(config.ViewID);
            if (view == null)
            {
                return (false, null, "View not found");
            }

            // Update view properties
            view.ViewName = config.ViewName;

            await _repository.UpdateAsync(view);

            // Update fields - remove all and re-add selected
            var selectedFields = config.Fields
                .Where(f => f.IsSelected)
                .Select((f, index) => new ViewField
                {
                    ViewID = view.ViewID,
                    FieldID = f.FieldID,
                    DisplayOrder = index + 1
                })
                .ToList();

            await _repository.UpdateViewFieldsAsync(view.ViewID, selectedFields);

            _logger.LogInformation("Updated view {ViewId}: {ViewName}", view.ViewID, view.ViewName);
            return (true, view, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating view {ViewId}", config.ViewID);
            return (false, null, ex.Message);
        }
    }

    /// <summary>
    /// Delete view
    /// </summary>
    public async Task<(bool Success, string? Error)> DeleteAsync(int viewId)
    {
        try
        {
            var view = await _repository.GetByIdAsync(viewId);
            if (view == null)
            {
                return (false, "View not found");
            }

            await _repository.HardDeleteAsync(view);

            _logger.LogInformation("Deleted view {ViewId}", viewId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting view {ViewId}", viewId);
            return (false, ex.Message);
        }
    }

    #endregion
}