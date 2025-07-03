using OddScout.Application.Common.Interfaces;
using OddScout.Application.DTOs.Scraping;

namespace OddScout.Application.Scraping.Queries.GetValueBets;

public sealed record GetValueBetsQuery(
    int? Take = null,
    decimal? MinimumEV = null
) : IQuery<List<ValueBetDto>>;