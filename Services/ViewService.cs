using Microsoft.EntityFrameworkCore;
using System.Text.Json;
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
    public async Task<(bool Success, string? Error)> ApplyViewToUserAsync(string userId, int? viewId, string? pageName = null)
    {
        try
        {
            if (!int.TryParse(userId, out int userIdInt))
            {
                return (false, "Invalid User ID format.");
            }

            if (!string.IsNullOrEmpty(pageName) && viewId.HasValue)
            {
                var pref = new UserPagePreference
                {
                    UserID = userIdInt,
                    PageName = pageName,
                    ViewID = viewId.Value
                };
                await _repository.SaveUserPagePreferenceAsync(pref);
                _logger.LogInformation("Applied view {ViewId} to user {UserId} for page {PageName}", viewId, userId, pageName);
                return (true, null);
            }

            var user = await _userRepository.LoadUserByUserIdAsync(userIdInt);
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
    /// Save user's column ordering preference for a view
    /// </summary>
    public async Task<(bool Success, string? Error)> SaveUserColumnPreferenceAsync(int userId, string viewName, string module, List<string> columnOrder)
    {
        try
        {
            var jsonOrder = JsonSerializer.Serialize(columnOrder);
            var pref = new UserViewPreference
            {
                UserID = userId,
                ViewName = viewName,
                TableName = module, // Using Module as TableName/Context
                ColumnOrder = jsonOrder,
                IsDefault = false
            };

            await _repository.SaveUserViewPreferenceAsync(pref);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving column preference for user {UserId}, view {ViewName}", userId, viewName);
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Set the default view for a user (persisted in DB)
    /// </summary>
    public async Task<(bool Success, string? Error)> SetUserDefaultViewAsync(string userId, int viewId, string? pageName = null)
    {
        // This is essentially an alias for ApplyViewToUserAsync but explicit in intent for setting default
        return await ApplyViewToUserAsync(userId, viewId, pageName);
    }

    /// <summary>
    /// Get user's persisted view on login or page load
    /// </summary>
    public async Task<int?> GetUserDefaultViewAsync(string userId, string? pageName = null)
    {
        if (!int.TryParse(userId, out int userIdInt))
        {
            return null;
        }

        if (!string.IsNullOrEmpty(pageName))
        {
            var pref = await _repository.GetUserPagePreferenceAsync(userIdInt, pageName);
            if (pref != null) return pref.ViewID;
        }

        var user = await _userRepository.LoadUserByUserIdAsync(userIdInt);
        return user?.LastViewID;
    }

    /// <summary>
    /// Get all views for dropdown for a specific module
    /// </summary>
    public async Task<List<View>> GetAllViewsAsync(string module = "Acquisition")
    {
        return await _repository.GetViewsAsync(module)
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
    /// Get view configuration with all field selections, optionally applying user preferences
    /// </summary>
    public async Task<ViewConfiguration?> GetViewConfigurationAsync(int viewId, int? userId = null)
    {
        var view = await _repository.GetByIdNoTrackingAsync(viewId);
        if (view == null) return null;

        var allFields = await _fieldRepository.GetDisplayFieldsAsync(view.Module);
        var selectedFields = await _repository.GetViewFieldsAsync(viewId);

        // Check for user preference override
        List<string>? userColumnOrder = null;
        if (userId.HasValue)
        {
            var pref = await _repository.GetUserViewPreferenceAsync(userId.Value, view.ViewName ?? "", view.Module);
            if (pref != null && !string.IsNullOrEmpty(pref.ColumnOrder))
            {
                try
                {
                    userColumnOrder = JsonSerializer.Deserialize<List<string>>(pref.ColumnOrder);
                }
                catch
                {
                    // Ignore empty or invalid JSON
                }
            }
        }

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

        // If user has a preference, re-order and update selection status
        if (userColumnOrder != null && userColumnOrder.Any())
        {
            // Create a dictionary for O(1) lookups of the user's order index
            var orderMap = userColumnOrder
                .Select((name, index) => new { Name = name, Index = index })
                .ToDictionary(x => x.Name, x => x.Index);

            // Update fields:
            // 1. If it's in the user's list, it is Selected and has a specific order.
            // 2. If it's NOT in the user's list, it is Not Selected (hidden), and we push it to the end.
            foreach (var field in config.Fields)
            {
                if (orderMap.TryGetValue(field.FieldName, out var newIndex))
                {
                    field.IsSelected = true;
                    field.DisplayOrder = newIndex + 1;
                }
                else
                {
                    field.IsSelected = false;
                    field.DisplayOrder = 9999; // Move to end
                }
            }

            // Sort by the new DisplayOrder
            config.Fields = config.Fields.OrderBy(f => f.DisplayOrder).ToList();
        }

        return config;
    }

    /// <summary>
    /// Get blank view configuration for new view
    /// </summary>
    public async Task<ViewConfiguration> GetNewViewConfigurationAsync(string module = "Acquisition")
    {
        var allFields = await _fieldRepository.GetDisplayFieldsAsync(module);
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
            }).ToList(),
            Module = module
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
            if (await _repository.ViewNameExistsAsync(config.ViewName, config.Module))
            {
                return (false, null, "A view with this name already exists");
            }

            // Create view
            var view = new View
            {
                ViewName = config.ViewName,
                Module = config.Module
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
            if (await _repository.ViewNameExistsAsync(config.ViewName, config.Module, config.ViewID))
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