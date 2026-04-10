using FlowHub.Core.Captures;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FlowHub.Web.Components.Pages;

public partial class Captures : ComponentBase
{
    [Inject] private ICaptureService CaptureService { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "lc")]
    public string? LifecycleQueryParam { get; set; }

    private IReadOnlyList<Capture>? _allCaptures;
    private IReadOnlyList<Capture>? _filtered;
    private LifecycleStage? _selectedLifecycle;
    private ChannelKind? _selectedChannel;
    private string? _searchText;
    private string? _loadError;

    protected override async Task OnInitializedAsync()
    {
        if (Enum.TryParse<LifecycleStage>(LifecycleQueryParam, ignoreCase: true, out var lc))
        {
            _selectedLifecycle = lc;
        }

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loadError = null;
        _allCaptures = null;
        _filtered = null;
        StateHasChanged();

        try
        {
            _allCaptures = await CaptureService.GetAllAsync();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            _loadError = ex.Message;
        }
    }

    private void OnFilterChanged(LifecycleStage? _) => ApplyFilters();

    private void OnFilterChanged(ChannelKind? _) => ApplyFilters();

    private void ApplyFilters()
    {
        if (_allCaptures is null)
        {
            _filtered = null;
            return;
        }

        IEnumerable<Capture> query = _allCaptures;

        if (_selectedLifecycle is { } lc)
        {
            query = query.Where(c => c.Stage == lc);
        }

        if (_selectedChannel is { } ch)
        {
            query = query.Where(c => c.Source == ch);
        }

        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            query = query.Where(c => c.Content.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
        }

        _filtered = query.ToList();
    }

    private void ClearFilters()
    {
        _selectedLifecycle = null;
        _selectedChannel = null;
        _searchText = null;
        ApplyFilters();
    }

    private void OnRowClick(DataGridRowClickEventArgs<Capture> args)
        => Navigation.NavigateTo($"/captures/{args.Item.Id}");

    private static string FormatRelative(DateTimeOffset when)
    {
        var delta = DateTimeOffset.UtcNow - when;
        if (delta.TotalMinutes < 1) return "now";
        if (delta.TotalMinutes < 60) return $"{(int)delta.TotalMinutes} m";
        if (delta.TotalHours < 24) return $"{(int)delta.TotalHours} h";
        return $"{(int)delta.TotalDays} d";
    }
}
