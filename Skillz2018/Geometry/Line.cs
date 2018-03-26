using System.Collections.Generic;

namespace MyBot.Geometry
{
    public class Line
    {
        public const int DEFAULT_ACCEPTABLE_DISTANCE = 25;

        public readonly Point FirstPoint;
        public readonly Point SecondPoint;
        public readonly Point UnitVector;
        public readonly Point Tangent;

        public Line(Point FirstPoint, Point SecondPoint)
        {
            this.FirstPoint = FirstPoint;
            this.SecondPoint = SecondPoint;
            UnitVector = (SecondPoint - FirstPoint).Normalized;
            Tangent = UnitVector.Adjacent;
        }

        public bool IsOnLine(Point v, int maxDistance = DEFAULT_ACCEPTABLE_DISTANCE)
        {
            return (Project(v).IsBetween(FirstPoint, SecondPoint) || v.Distance(FirstPoint) <= maxDistance || v.Distance(SecondPoint) <= maxDistance) && Project(v).Distance(v) <= maxDistance;
        }
        public bool IsProjectionOnLine(Point v, int maxDistance = DEFAULT_ACCEPTABLE_DISTANCE)
        {
            return IsOnLine(Project(v), maxDistance);
        }
        public Point Project(Point v)
        {
            // LINE = s + k*SecondPoint
            Point s = FirstPoint - SecondPoint;

            return (((v - SecondPoint) * s) / (s * s)) * s + SecondPoint;
        }
        public double Distance(Point v)
        {
            return ~(v - Project(v));
        }

        public Path SkewTriangle(Point center, Point Skew, double FirstDistance, double SecondDistance)
        {
            if (!IsOnLine(center))
                return new Path(FirstPoint, SecondPoint);

            Point Unit = UnitVector;
            List<Point> pts = new List<Point>();
            pts.Add(FirstPoint);
            if (center.Distance(FirstPoint) > FirstDistance)
                pts.Add(center - FirstDistance * Unit);
            pts.Add(center + Skew);
            if (center.Distance(SecondPoint) > SecondDistance)
                pts.Add(center + SecondDistance * Unit);
            pts.Add(SecondPoint);
            return new Path(pts.ToArray());
        }
        public Path SkewRectangle(Point center, Point Skew, double FirstDistance, double SecondDistance)
        {
            if (!IsOnLine(center))
                return new Path(FirstPoint, SecondPoint);

            Point Unit = UnitVector;
            Point FirstSide = center - FirstDistance * Unit;
            Point SecondSide = center + SecondDistance * Unit;
            return new Path(FirstPoint, FirstSide, FirstSide + Skew, SecondSide + Skew, SecondSide, SecondPoint);
        }
        /// <summary>
        /// UNSAFE - MIGHT CRASH BOT
        /// </summary>
        /// <param name="dentor"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public Path IndentTriangleUnsafe(Point dentor, double distance)
        {
            if (Distance(dentor) > distance)
                return (Path)this;
            double d = Distance(dentor);
            double r = distance;
            double SideDistances = System.Math.Sqrt(r * r - d * d);
            double Inside = r*r/d - d;
            Point Projection = Project(dentor);
            Point Adjacent = Projection == dentor ? Tangent : (Projection - dentor).Normalized;

            return SkewTriangle(Projection, Adjacent * Inside, SideDistances, SideDistances);
        }
        public Path[] IndentTriangle(Point dentor, double distance)
        {
            if (Distance(dentor) > distance)
                return new Path[] { (Path)this };
            double d = Distance(dentor);
            double SideDistances = distance;
            Point Projection = Project(dentor);
            double SkewDistance = dentor.Distance(Projection);
            Point Adjacent = SkewDistance <= 0 ? Tangent : (Projection - dentor).Normalized;

            return new Path[] { SkewTriangle(Projection, Adjacent * (distance - SkewDistance), SideDistances, SideDistances), SkewTriangle(Projection, Adjacent * (-1 * (distance + SkewDistance)), SideDistances, SideDistances) };
        }
        /// <summary>
        /// Indents the path by a rectangle avoiding the dentor for a certain radius
        /// </summary>
        /// <param name="dentor"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public Path[] IndentRectangle(Point dentor, double distance)
        {
            if (Distance(dentor) > distance)
                return new Path[] { (Path)this };
            double d = Distance(dentor);
            double SideDistances = System.Math.Sqrt(distance * distance - d * d);
            double Inside = distance - d;
            Point Projection = Project(dentor);
            Point Adjacent = Projection == dentor ? Tangent : (Projection - dentor).Normalized;

            return new Path[] { SkewRectangle(Projection, Adjacent * Inside, SideDistances, SideDistances), SkewRectangle(Projection, ((-1) * Adjacent) * Inside, SideDistances, SideDistances) };
        }

        public static explicit operator Path(Line l)
        {
            return new Path(l.FirstPoint, l.SecondPoint);
        }

        public override string ToString()
        {
            return "<" + FirstPoint + " -> " + SecondPoint + ">";
        }
    }
}
