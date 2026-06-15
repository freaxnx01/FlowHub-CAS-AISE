using FlowHub.Core.Captures;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace FlowHub.Web.Components.Pages;

public partial class Search : ComponentBase
{
    [Inject] private IEmbeddingService Embeddings { get; set; } = default!;

    [Inject] private ICaptureRepository CaptureRepository { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private string? _query;
    private bool _searching;
    private bool _notEnabled;
    private string? _error;
    private IReadOnlyList<Capture>? _results;

    private async Task OnKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await RunSearchAsync();
        }
    }

    private async Task RunSearchAsync()
    {
        if (_searching || string.IsNullOrWhiteSpace(_query))
        {
            return;
        }

        _searching = true;
        _error = null;
        _notEnabled = false;
        _results = null;
        StateHasChanged();

        try
        {
            var vector = await Embeddings.GenerateAsync(_query);
            if (vector is null)
            {
                // No embedding provider wired (NullEmbeddingService) — the same posture the
                // REST endpoint surfaces as 503. Tell the user rather than show an empty list.
                _notEnabled = true;
                return;
            }

            _results = await CaptureRepository.SearchByEmbeddingAsync(vector, 10);
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _searching = false;
        }
    }

    private static string Truncate(string text, int max)
        => text.Length <= max ? text : text[..max] + "…";
}
