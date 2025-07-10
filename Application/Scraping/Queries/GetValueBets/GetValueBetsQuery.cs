using OddScout.Application.Common.Interfaces;
using OddScout.Application.DTOs;
using OddScout.Application.DTOs.Scraping;

namespace OddScout.Application.Scraping.Queries.GetValueBets;

public sealed record GetValueBetsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    decimal? MinimumEV = null
) : IQuery<PagedResult<ValueBetDto>>;