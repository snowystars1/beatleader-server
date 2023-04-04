﻿using BeatLeader_Server.Models;

namespace BeatLeader_Server.Utils;

public enum Operation
{
    any = 0,
    all = 1,
    not = 2,
}

public static partial class MapListUtils
{
    public static IQueryable<Leaderboard> Filter(this IQueryable<Leaderboard> source,
                                                 ReadAppContext context,
                                                 string? sortBy = null,
                                                 string? order = null,
                                                 string? search = null,
                                                 string? type = null,
                                                 string? mode = null,
                                                 int? mapType = null,
                                                 Operation allTypes = 0,
                                                 Requirements? mapRequirements = null,
                                                 Operation allRequirements = 0,
                                                 string? mytype = null,
                                                 float? stars_from = null,
                                                 float? stars_to = null,
                                                 int? date_from = null,
                                                 int? date_to = null,
                                                 string? currentID = null) =>
        source.FilterBySearch(search, ref type, ref mode, ref mapType, ref allTypes, ref mapRequirements, ref allRequirements, ref stars_from, ref stars_to, ref mytype, ref date_from, ref date_to)
              .Sort(sortBy, order!, type, mytype, date_from, date_to, currentID)
              .WithType(type)
              .WithMapType(mapType, allTypes)
              .WithMode(mode)
              .WithMapRequirements(mapRequirements, allRequirements)
              .FilterByMyType(context, mytype, currentID)
              .WithStarsFrom(stars_from)
              .WithStarsTo(stars_to);
}