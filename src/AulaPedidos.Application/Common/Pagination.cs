namespace AulaPedidos.Application.Common;

public sealed record PageRequest(int PageNumber = 1, int PageSize = 20)
{
    public bool IsValid => PageNumber is >= 1 and <= 1_000_000 && PageSize is >= 1 and <= 100;
    public int Skip => (PageNumber - 1) * PageSize;
}

public sealed record Page<T>(IReadOnlyList<T> Items, int TotalCount, int PageNumber, int PageSize);
