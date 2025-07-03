using OddScout.Application.Common.Interfaces;

namespace OddScout.Application.Scraping.Commands.RunPinnacleScraping;

public sealed record RunPinnacleScrapingCommand : ICommand<int>;