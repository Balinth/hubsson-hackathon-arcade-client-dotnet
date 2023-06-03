using System.Collections.Immutable;
using Hubsson.Hackathon.Arcade.Client.Dotnet.Contracts;
#pragma warning disable CS8524

namespace Hubsson.Hackathon.Arcade.Client.Dotnet.Domain;

public readonly record struct Coords(int X, int Y);

public static class PathFind
{

    public static Coords DirectionToCoords(Direction dir, Coords current) => dir switch
    {
        Direction.Up => current with { Y = current.Y - 1 },
        Direction.Down => current with { Y = current.Y + 1 },
        Direction.Left => current with { X = current.X - 1 },
        Direction.Right => current with { X = current.X + 1 },
    };

    public static Direction CoordsToDirection(Coords start, Coords end)
    {
        if (start.X - end.X == -1)
            return Direction.Left;
        if (start.X - end.X == 1)
            return Direction.Right;
        if (start.Y - end.Y == -1)
            return Direction.Down;
        return Direction.Up;
    }
    
    public static List<Direction> AllDirections = new List<Direction>()
    {
        Direction.Up,
        Direction.Down,
        Direction.Left,
        Direction.Right,
    };

    public static List<Coords> GetPossibleDirections(ImmutableHashSet<Coords> obstacles, int width, int height, Coords startPoint)
    {

        return AllDirections
            .Select(dir => DirectionToCoords(dir, startPoint))
            .Where(dir =>
            {
                if(dir.Y < 0)
                    return false;
                if(dir.X < 0)
                    return false;
                if(dir.Y >= height)
                    return false;
                if(dir.X >= width)
                    return false;
                
                return obstacles.Contains(dir) == false;
            }).ToList();
    }

    public static ImmutableHashSet<Coords> AddOtherPlayersPossibleMovesAsObstacle(ImmutableHashSet<Coords> obstacles,
        int width, int height, IEnumerable<PlayerCoordinates> otherPlayerLocations)
    {
        var result = obstacles;
        foreach (var otherPlayer in otherPlayerLocations)
        {
            var currentPlayerPos = otherPlayer.coordinates.LastOrDefault();
            if(currentPlayerPos is null)
                continue;

            var currentPlayerCoords = new Coords(currentPlayerPos.x, currentPlayerPos.y);
            
            foreach (var possibleMove in GetPossibleDirections(obstacles, width, height, currentPlayerCoords))
            {
                result = obstacles.Add(possibleMove);
            }
        }

        return result;
    }

    public static float Distance(Coords a, Coords b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }

    public static bool FoundPath(Stack<Coords> path, Coords endPoint) =>
        path.TryPeek(out var pathEnd) && pathEnd == endPoint;

    public static ImmutableHashSet<Coords> CalcObstacles(PlayerCoordinates[] playerCoords, string currentPlayerId, int mapWidth,
        int mapHeight)
    {
        var obstacles = playerCoords
            .SelectMany(p =>
                p.coordinates.Select(pc => new Coords(pc.x, pc.y)))
            .ToImmutableHashSet();
        var otherPlayerCoords = playerCoords.Where(p => p.playerId != currentPlayerId).ToList();
        return AddOtherPlayersPossibleMovesAsObstacle(obstacles, mapWidth, mapHeight, otherPlayerCoords);
    }

    public static Direction DumbAvoider(PlayerCoordinates[] playerCoords, string currentPlayerId, int mapWidth,
        int mapHeight, Direction bias)
    {
        var currentPlayerLastPos = playerCoords.Single(p => p.playerId == currentPlayerId).coordinates.Last();
        var currentPos = new Coords(currentPlayerLastPos.x, currentPlayerLastPos.y);
        var obstacles = CalcObstacles(playerCoords, currentPlayerId, mapWidth, mapHeight);
        var possibleDirection = GetPossibleDirections(obstacles, mapWidth, mapHeight, currentPos).Select(d => CoordsToDirection(currentPos, d)).ToList();
        var direction = possibleDirection.Any(d => d == bias) ? bias : possibleDirection.First();
        return direction;
    }
    
}