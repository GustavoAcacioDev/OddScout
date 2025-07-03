using OddScout.Application.Common.Interfaces;

namespace OddScout.Application.Scraping.Commands.RunBetbyScraping;

public sealed record RunBetbyScrapingCommand : ICommand<int>;