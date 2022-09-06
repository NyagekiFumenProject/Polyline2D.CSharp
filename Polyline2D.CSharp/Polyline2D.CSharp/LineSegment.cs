using System;
using System.Collections.Generic;
using System.Text;

namespace Polyline2DCSharp
{
    public struct LineSegment
    {
        public Vec2 a;
        public Vec2 b;

        public LineSegment(Vec2 a, Vec2 b)
        {
            this.a = a;
            this.b = b;
        }

        /**
	 * @return A copy of the line segment, offset by the given vector.
	 */
        public static LineSegment operator +(LineSegment s, Vec2 toAdd)
        {
            return new LineSegment(Vec2.add(s.a, toAdd), Vec2.add(s.b, toAdd));
        }

        /**
         * @return A copy of the line segment, offset by the given vector.
         */
        public static LineSegment operator -(LineSegment s, Vec2 toRemove)
        {
            return new LineSegment(Vec2.subtract(s.a, toRemove), Vec2.subtract(s.b, toRemove));
        }

        /**
         * @return The line segment's normal vector.
         */
        public Vec2 normal()
        {
            var dir = direction();

            // return the direction vector
            // rotated by 90 degrees counter-clockwise
            return new(-dir.y, dir.x);
        }

        /**
         * @return The line segment's direction vector.
         */
        public Vec2 direction(bool normalized = true)
        {
            var vec = Vec2.subtract(b, a);

            return normalized
                   ? Vec2.normalized(vec)
                   : vec;
        }

        public static Vec2? intersection(LineSegment a, LineSegment b, bool infiniteLines)
        {
            // calculate un-normalized direction vectors
            var r = a.direction(false);
            var s = b.direction(false);

            var originDist = Vec2.subtract(b.a, a.a);

            var uNumerator = Vec2.cross(originDist, r);
            var denominator = Vec2.cross(r, s);

            if (MathF.Abs(denominator) < 0.0001f)
            {
                // The lines are parallel
                return default;
            }

            // solve the intersection positions
            var u = uNumerator / denominator;
            var t = Vec2.cross(originDist, s) / denominator;

            if (!infiniteLines && (t < 0 || t > 1 || u < 0 || u > 1))
            {
                // the intersection lies outside of the line segments
                return default;
            }

            // calculate the intersection point
            // a.a + r * t;
            return Vec2.add(a.a, Vec2.multiply(r, t));
        }
    }
}
