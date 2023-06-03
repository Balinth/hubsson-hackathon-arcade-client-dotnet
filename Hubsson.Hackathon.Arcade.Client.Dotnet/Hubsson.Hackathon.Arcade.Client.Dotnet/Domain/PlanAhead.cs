using System.Collections.Immutable;

namespace Hubsson.Hackathon.Arcade.Client.Dotnet.Domain;

public record OtherPlayer(Coords HeadPos, Direction LastDirection); 

public record PlanAheadNode
{
    public ImmutableHashSet<Coords> Obstacles { get; init; }
    public PlanAheadNode Parent { get; init; }
    public ImmutableList<OtherPlayer> OtherPlayers { get; init; }
    public Coords CurrentPos { get; init; }

    public List<PlanAheadNode> ExpandPlan(
        string currentPlayerId,
        int mapWidth,
        int mapHeight)
    {
        var possibleDirections = PathFind.GetPossibleDirections(Obstacles, mapWidth, mapHeight, CurrentPos).Select(d => PathFind.CoordsToDirection(CurrentPos, d)).ToList();
        return possibleDirections.Select(d =>
            this with
            {
                Obstacles = Obstacles,
                Parent = this
            }
        ).ToList();
    }

    public ImmutableHashSet<Coords> ExpandObstaclesWithAssumedPaths()
    {
        return null;
    }
}