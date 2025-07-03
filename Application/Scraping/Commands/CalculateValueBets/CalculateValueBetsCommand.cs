using OddScout.Application.Common.Interfaces;
using OddScout.Application.DTOs.Scraping;

namespace OddScout.Application.Scraping.Commands.CalculateValueBets;

public sealed record CalculateValueBetsCommand : ICommand<List<ValueBetDto>>;