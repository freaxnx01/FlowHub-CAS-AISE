using FlowHub.Core.Captures;
using FlowHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowHub.Persistence;

internal static class CaptureQueryBuilder
{
    // Caller must order by (CreatedAt DESC, Id DESC) before calling Apply — the cursor clause is semantically
    // dependent on that ordering to produce correct keyset pagination results.
    internal static IQueryable<CaptureEntity> Apply(IQueryable<CaptureEntity> query, CaptureFilter filter)
    {
        if (filter.Stages is { Count: > 0 } stages)
        {
            var stageStrings = stages.Select(s => s.ToString()).ToHashSet();
            query = query.Where(c => stageStrings.Contains(c.Stage));
        }

        if (filter.Source is ChannelKind src)
        {
            var sourceString = src.ToString();
            query = query.Where(c => c.Source == sourceString);
        }

        if (filter.Tag is { } tag)
        {
            query = query.Where(c => c.Tags.Any(t => t.Value == tag));
        }

        if (filter.SearchTerm is { } term)
        {
            query = query.Where(c =>
                EF.Functions.ILike(c.Content, $"%{term}%")
                || (c.Title != null && EF.Functions.ILike(c.Title, $"%{term}%")));
        }

        if (filter.Cursor is CaptureCursor cursor)
        {
            query = query.Where(c =>
                c.CreatedAt < cursor.CreatedAt
                || (c.CreatedAt == cursor.CreatedAt && c.Id.CompareTo(cursor.Id) < 0));
        }

        return query;
    }
}
