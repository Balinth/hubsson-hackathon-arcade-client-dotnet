using System.Collections.Immutable;

namespace Hubsson.Hackathon.Arcade.Client.Dotnet.Domain;

public record OtherPlayer(Coords HeadPos, Direction LastDirection); 

public record PlanAheadNode
{
    public ImmutableHashSet<Coords> Obstacles { get; init; }
    public PlanAheadNode? Parent { get; init; }
    public ImmutableList<OtherPlayer> OtherPlayers { get; init; }
    public Coords CurrentPos { get; init; }

    public static Direction Strategize(ImmutableHashSet<Coords> startingObstacles, int width, int height, Coords startingPos,
        ImmutableList<OtherPlayer> startingOtherPlayers, CancellationToken cancellationToken)
    {
        PriorityQueue<PlanAheadNode, float> nodes = new PriorityQueue<PlanAheadNode, float>(){};

        var rootNode = new PlanAheadNode()
        {
            Obstacles = startingObstacles,
            Parent = null,
            OtherPlayers = startingOtherPlayers,
            CurrentPos = startingPos
        };
        
        nodes.Enqueue(rootNode, 0);
        
        while (cancellationToken.IsCancellationRequested is false)
        {
            var currentNode = nodes.Dequeue();
            var newNodes = currentNode.ExpandPlan(width, height);
            foreach (var newNode in newNodes)
            {
                nodes.Enqueue(newNode, newNode.CalcFreedomHeuristicScore(width, height));
            }
        }

        var nextStep = nodes.Dequeue().GetFirstMove();

        Console.WriteLine($"Iterations planned ahead: {nextStep.CalcIteration()}");

        var newDir = PathFind.CoordsToDirection(startingPos, nextStep.CurrentPos);
        if (newDir == Direction.Down)
        {
            int breakHere;
        }

        return newDir;
    }
    
    public List<PlanAheadNode> ExpandPlan(
        int mapWidth,
        int mapHeight)
    {
        var possibleDirections = PathFind.GetPossibleDirections(Obstacles, mapWidth, mapHeight, CurrentPos).Select(d => PathFind.CoordsToDirection(CurrentPos, d)).ToList();
        return possibleDirections.Select(d =>
        {
            var newOtherPlayers = ImmutableList<OtherPlayer>.Empty;
            var newObstacles = Obstacles;
            if (Parent is not null)
                newObstacles = newObstacles.Add(Parent.CurrentPos);
            newObstacles = newObstacles.Add(PathFind.DirectionToCoords(d, CurrentPos));
            foreach (var otherPlayer in OtherPlayers)
            {
                var (obstacles, newOtherPlayer) =
                    ExpandObstaclesWithAssumedPath(newObstacles, mapWidth, mapHeight, otherPlayer);
                newObstacles = obstacles;
                newOtherPlayers = newOtherPlayers.Add(newOtherPlayer);
            }

            var newCurrentPos = PathFind.DirectionToCoords(d, CurrentPos);
            
            return new PlanAheadNode
            {
                OtherPlayers = newOtherPlayers,
                Obstacles = newObstacles,
                Parent = this,
                CurrentPos = newCurrentPos,
            };
        }).ToList();
    }

    public float CalcFreedomHeuristicScore(int width, int height)
    {
        return (
            CalcIteration()
            + PathFind.GetPossibleDirections(Obstacles, width, height, CurrentPos).Count()
            ) * -1;
    }

    public int CalcIteration()
    {
        var result = 1;
        var currentParent = Parent;
        while (currentParent is not null)
        {
            result++;
            currentParent = currentParent.Parent;
        }

        return result;
    }
    public PlanAheadNode GetFirstMove()
    {
        var currentNode = this;
        while (true)
        {
            if (currentNode?.Parent?.Parent is null)
                return currentNode;
            currentNode = currentNode.Parent;
        }
    }

    public static (ImmutableHashSet<Coords>, OtherPlayer) ExpandObstaclesWithAssumedPath(ImmutableHashSet<Coords> obstacles, int width, int height, OtherPlayer otherPlayer)
    {
        var possibleDirections = PathFind.GetPossibleDirections(obstacles, width, height, otherPlayer.HeadPos).Select(d => PathFind.CoordsToDirection(otherPlayer.HeadPos, d)).ToList();;
        
        var direction =
            (possibleDirections.Cast<Direction?>().Any(d => d == otherPlayer.LastDirection)
                ? (Direction?)otherPlayer.LastDirection
                : possibleDirections.Cast<Direction?>().FirstOrDefault()) ?? Direction.Down;
        
        var newPos = PathFind.DirectionToCoords(direction, otherPlayer.HeadPos);
        return (
            obstacles.Add(newPos),
            new OtherPlayer(HeadPos: newPos, LastDirection: direction)
        );
    }
}