using MyBot.Engine;
using System.Linq;

namespace MyBot.Geometry
{
    public class Path
    {
        public readonly Point[] Points;
        public Line[] Lines;

        public Path(params Point[] points)
        {
            Points = points;
            Lines = new Line[points.Length - 1];
            for (int i = 0; i < Lines.Length; i++)
            {
                Lines[i] = new Line(points[i], points[i + 1]);
            }
        }

        public Point GetNextPoint(Point p, int maxDistance)
        {
            if (p.Distance(Lines.Last().SecondPoint) < maxDistance)
                return Lines.Last().SecondPoint;
            Line l = Lines.Last(x =>
                (x.IsProjectionOnLine(p) && x.Distance(p) < maxDistance) || (p.Distance(x.FirstPoint) < maxDistance));
            if (l == null)
                return Lines.Last().SecondPoint;
            else
            {
                Point proj = l.Project(p);
                Point direction = l.UnitVector;
                return proj + direction * System.Math.Sqrt(maxDistance * maxDistance - System.Math.Pow(p.Distance(proj), 2));
            }
        }
        public bool GetLine(Point p, out Line line)
        {
            line = Lines.LastOrDefault(x => x.IsOnLine(p));
            if (line == null)
                return false;
            return true;
        }

        public override string ToString()
        {
            return "<" + Points.Select(x => x.ToString()).ToArray().Join(" -> ") + ">";
        }
    }
}
