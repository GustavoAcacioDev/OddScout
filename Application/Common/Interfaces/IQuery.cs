using MediatR;

namespace OddScout.Application.Common.Interfaces;

public interface IQuery<out TResponse> : IRequest<TResponse> { }