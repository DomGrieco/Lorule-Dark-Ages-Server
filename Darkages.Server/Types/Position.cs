﻿using System;
using System.Collections.Generic;

namespace Darkages.Types
{
    public class Position
    {
        public ushort X, Y;

        public Position(ushort x, ushort y)
        {
            X = x;
            Y = y;
        }

        public Position(byte x, byte y) : this(x, (ushort) y)
        {
        }

        public Position(int x, int y) : this((ushort) x, (ushort) y)
        {
        }

        public Position() : this(0, 0)
        {
        }

        public int DistanceFrom(Position other)
        {
            return DistanceFrom(other.X, other.Y);
        }

        public int DistanceFrom(ushort X, ushort Y)
        {
            double XDiff = Math.Abs(X - this.X);
            double YDiff = Math.Abs(Y - this.Y);
            return (int) (XDiff > YDiff ? XDiff : YDiff);
        }

        public bool IsNearby(Position pos)
        {
            return pos.DistanceFrom(X, Y) <= ServerContext.Config.VeryNearByProximity;
        }

        public bool WithinSquare(Position loc, int num)
        {
            return Math.Abs(X - loc.X) <= num && Math.Abs(Y - loc.Y) <= num;
        }

        public static Position operator +(Position a, Direction b)
        {
            var location = new Position(a.X, a.Y);
            switch (b)
            {
                case Direction.North:
                    location.Y--;
                    return location;
                case Direction.East:
                    location.X++;
                    return location;
                case Direction.South:
                    location.Y++;
                    return location;
                case Direction.West:
                    location.X--;
                    return location;
            }

            return location;
        }

        public static Direction operator -(Position a, Position b)
        {
            if (a.X == b.X && a.Y == b.Y + 1)
                return Direction.North;
            if (a.X == b.X && a.Y == b.Y - 1)
                return Direction.South;
            if (a.X == b.X + 1 && a.Y == b.Y)
                return Direction.West;
            if (a.X == b.X - 1 && a.Y == b.Y)
                return Direction.East;

            return Direction.UnDefined;
        }

        public TileContentPosition[] SurroundingContent(Area map)
        {
            var list = new List<TileContentPosition>();

            if (X > 0)
                list.Add(new TileContentPosition(
                    new Position(X - 1, Y),
                    map[X - 1, Y]));
            if (Y > 0)
                list.Add(new TileContentPosition(
                    new Position(X, Y - 1),
                    map[X, Y - 1]));
            if (X < map.Rows - 1)
                list.Add(new TileContentPosition(
                    new Position(X + 1, Y),
                    map[X + 1, Y])
                );
            if (Y < map.Cols - 1)
                list.Add(new TileContentPosition(
                    new Position(X, Y + 1),
                    map[X, Y + 1])
                );

            return list.ToArray();
        }


        public bool IsNextTo(Position pos)
        {
            if (X == pos.X && Y + 1 == pos.Y) return true;
            if (X == pos.X && Y - 1 == pos.Y) return true;
            if (X == pos.X + 1 && Y == pos.Y) return true;
            if (X == pos.X - 1 && Y == pos.Y) return true;

            return false;
        }

        public class TileContentPosition
        {
            public TileContentPosition(Position pos, TileContent content)
            {
                Position = pos;
                Content = content;
            }

            public Position Position { get; set; }
            public TileContent Content { get; set; }
        }
    }
}