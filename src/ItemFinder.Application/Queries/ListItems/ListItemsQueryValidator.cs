using FluentValidation;

namespace ItemFinder.Application.Queries.ListItems;

/// <summary>Paging bounds and projection values for the item list.</summary>
public sealed class ListItemsQueryValidator : AbstractValidator<ListItemsQuery>
{
    public const int MaxPageSize = 200;
    public const int MaxPage = 1_000_000;

    public ListItemsQueryValidator()
    {
        RuleFor(query => query.Page).InclusiveBetween(1, MaxPage);
        RuleFor(query => query.PageSize).InclusiveBetween(1, MaxPageSize);
        RuleFor(query => query.Fields)
            .Must(fields => fields is null || fields.Equals(ListItemsQuery.NameField, StringComparison.OrdinalIgnoreCase))
            .WithMessage($"The only supported projection is '{ListItemsQuery.NameField}'.");
    }
}