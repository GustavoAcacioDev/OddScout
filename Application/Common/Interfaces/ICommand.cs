﻿using MediatR;

namespace OddScout.Application.Common.Interfaces;

public interface ICommand<out TResponse> : IRequest<TResponse> { }
public interface ICommand : IRequest { }