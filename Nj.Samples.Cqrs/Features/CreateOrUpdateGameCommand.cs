﻿/* This file is copyright © 2022 Dnj.Colab repository authors.

Dnj.Colab content is distributed as free software: you can redistribute it and/or modify it under the terms of the General Public License version 3 as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

Dnj.Colab content is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the General Public License version 3 for more details.

You should have received a copy of the General Public License version 3 along with this repository. If not, see <https://github.com/smaicas-org/Dnj.Colab/blob/dev/LICENSE>. */

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Nj.Samples.Cqrs.Data;
using Nj.Samples.Cqrs.Data.Entities;
using Nj.Samples.Cqrs.RCL.Models;

namespace Nj.Samples.Cqrs.Features;

/// <summary>
///     COMMAND
/// </summary>
public class CreateOrUpdateGameCommand : IRequest<GameDto>
{
    public GameDto Game { get; set; }
}

/// <summary>
///     HANDLER
/// </summary>
public class CreateOrUpdateGameCommandHandler : IRequestHandler<CreateOrUpdateGameCommand, GameDto>
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public CreateOrUpdateGameCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory) => _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));

    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    /// <exception cref="DbUpdateConcurrencyException">
    ///     A concurrency violation is encountered while saving to the database.
    ///     A concurrency violation occurs when an unexpected number of rows are affected during save.
    ///     This is usually because the data in the database has been modified since it was loaded into memory.
    /// </exception>
    /// <exception cref="DbUpdateException">An error is encountered while saving to the database.</exception>
    public async Task<GameDto> Handle(CreateOrUpdateGameCommand request, CancellationToken cancellationToken)
    {
        await using AppDbContext context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        GameEntity entity = new()
        {
            Id = request.Game.Id,
            Title = request.Game.Title,
            Genre = request.Game.Genre,
            Platform = request.Game.Platform,
            ReleaseDate = request.Game.ReleaseDate
        };
        if (Guid.Empty.Equals(entity.Id))
        {
            EntityEntry<GameEntity> res = await context.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            entity = res.Entity;
        }
        else
        {
            entity = context.Games.Update(entity).Entity;
        }

        await context.SaveChangesAsync(cancellationToken);
        request.Game.Id = entity.Id;
        return request.Game;
    }
}

/// <summary>
///     VALIDATOR
/// </summary>
public class CreateOrUpdateGameCommandValidator : AbstractValidator<CreateOrUpdateGameCommand>
{
    public CreateOrUpdateGameCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(dto => dto.Game).NotNull().WithMessage("Game is null");

        RuleLevelCascadeMode = CascadeMode.Continue;

        RuleFor(dto => dto.Game.Title).NotEmpty().WithMessage("Title is required");
        RuleFor(dto => dto.Game.Genre).NotEmpty().WithMessage("Genre is required");
        RuleFor(dto => dto.Game.Platform).NotEmpty().WithMessage("Platform is required");
        RuleFor(dto => dto.Game.ReleaseDate).GreaterThan(default(DateTime))
            .WithMessage("Release Date cannot be default");
    }
}