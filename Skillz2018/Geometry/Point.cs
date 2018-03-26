using Pirates;
using MyBot.Engine;

namespace MyBot.Geometry
{
    public class Point
    {
        public readonly double Row;
        public readonly double Col;
        public Point Adjacent
        {
            get
            {
                return new Point(1, -Row / Col).Normalized;
            }
        }

        public Point(double Row, double Col)
        {
            this.Row = Row;
            this.Col = Col;
        }

        public bool IsBetween(Point p1, Point p2)
        {
            return Row.IsBetween(p1.Row, p2.Row) && Col.IsBetween(p1.Col, p2.Col);
        }
        public double Distance(Point p2)
        {
            return ~(this - p2);
        }
        public Point Normalized
        {
            get
            {
                if (~this == 0)
                    return new Point(0, 0);
                return this / ~this;
            }
        }
        public Point InDirection(Point p, double Distance)
        {
            return this + (p - this).Normalized * Distance;
        }
        public static double operator ~(Point p1)
        {
            return System.Math.Sqrt(p1 * p1);
        }
        public static double operator *(Point p1, Point p2)
        {
            return p1.Row * p2.Row + p1.Col * p2.Col;
        }
        public static Point operator +(Point p1, Point p2)
        {
            return new Point(p1.Row + p2.Row, p1.Col + p2.Col);
        }
        public static Point operator -(Point p1, Point p2)
        {
            return p1 + (-1) * p2;
        }
        public static Point operator *(Point p1, double m)
        {
            return new Point(p1.Row * m, p1.Col * m);
        }
        public static Point operator *(double m, Point p1)
        {
            return p1 * m;
        }
        public static Point operator /(Point p1, double m)
        {
            return new Point(p1.Row / m, p1.Col / m);
        }

        public static bool operator ==(Point p1, Point p2)
        {
            return (ReferenceEquals(p1, null) && ReferenceEquals(p2, null)) || (!ReferenceEquals(p1, null) && !ReferenceEquals(p2, null) && p1.Row == p2.Row && p1.Col == p2.Col);
        }
        public static bool operator !=(Point p1, Point p2)
        {
            return !(p1 == p2);
        }
        public override bool Equals(object obj)
        {
            return obj is Point && (Point)obj == this;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static explicit operator Location(Point c)
        {
            return new Location((int)c.Row, (int)c.Col);
        }
        public static implicit operator Point(MapObject mo)
        {
            Location l = mo.GetLocation();
            return new Point(l.Row, l.Col);
        }

        public override string ToString()
        {
            return "<" + Row + "," + Col + ">";
        }
    }
}
