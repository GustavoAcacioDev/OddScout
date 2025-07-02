using Microsoft.EntityFrameworkCore;
using OddScout.Domain.Entities;
using System.Collections.Generic;

namespace OddScout.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}