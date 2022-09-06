using System;
using System.Collections.Generic;
using System.Numerics;

namespace Polyline2DCSharp
{
    public static class Polyline2D
    {
        private struct PolySegment
        {
            public PolySegment(LineSegment center, float thickness)
            {
                this.center = center;
                // calculate the segment's outer edges by offsetting
                // the central line by the normal vector
                // multiplied with the thickness

                // center + center.normal() * thickness 
                edge1 = center + Vec2.multiply(center.normal(), thickness);
                edge2 = center - Vec2.multiply(center.normal(), thickness);
            }

            public LineSegment center;
            public LineSegment edge1;
            public LineSegment edge2;
        };

        public enum JointStyle
        {
            /**
			 * Corners are drawn with sharp joints.
			 * If the joint's outer angle is too large,
			 * the joint is drawn as beveled instead,
			 * to avoid the miter extending too far out.
			 */
            MITER,
            /**
             * Corners are flattened.
             */
            BEVEL,
            /**
             * Corners are rounded off.
             */
            ROUND
        };

        public enum EndCapStyle
        {
            /**
			 * Path ends are drawn flat,
			 * and don't exceed the actual end point.
			 */
            BUTT, // lol
            /**
             * Path ends are drawn flat,
             * but extended beyond the end point
             * by half the line thickness.
             */
            SQUARE,
            /**
             * Path ends are rounded off.
             */
            ROUND,
            /**
             * Path ends are connected according to the JointStyle.
             * When using this EndCapStyle, don't specify the common start/end point twice,
             * as Polyline2D connects the first and last input point itself.
             */
            JOINT
        };

        /**
         * The threshold for mitered joints.
         * If the joint's angle is smaller than this angle,
         * the joint will be drawn beveled instead.
         */
        const float miterMinAngle = 0.349066f; // ~20 degrees

        /**
         * The minimum angle of a round joint's triangles.
         */
        const float roundMinAngle = 0.174533f; // ~10 degrees

        public static List<Vec2> Create(List<Vec2> vertices, List<Vec2> points, float thickness,
                                 JointStyle jointStyle = JointStyle.MITER,
                                 EndCapStyle endCapStyle = EndCapStyle.BUTT,
                                 bool allowOverlap = false)
        {
            // operate on half the thickness to make our lives easier
            thickness /= 2;

            List<PolySegment> segments = new List<PolySegment>();
            for (var i = 0; i + 1 < points.Count; i++)
            {
                var point1 = points[i];
                var point2 = points[i + 1];

                // to avoid division-by-zero errors,
                // only create a line segment for non-identical points
                if (!Vec2.equal(point1, point2))
                {
                    segments.Add(new PolySegment(new LineSegment(point1, point2), thickness));
                }
            }

            if (endCapStyle == EndCapStyle.JOINT)
            {
                // create a connecting segment from the last to the first point

                var point1 = points[points.Count - 1];
                var point2 = points[0];

                // to avoid division-by-zero errors,
                // only create a line segment for non-identical points
                if (!Vec2.equal(point1, point2))
                {
                    segments.Add(new(new LineSegment(point1, point2), thickness));
                }
            }

            if (segments.Count == 0)
            {
                // handle the case of insufficient input points
                return vertices;
            }

            var nextStart1 = new Vec2(0, 0);
            var nextStart2 = new Vec2(0, 0);
            var start1 = new Vec2(0, 0);
            var start2 = new Vec2(0, 0);
            var end1 = new Vec2(0, 0);
            var end2 = new Vec2(0, 0);

            var firstSegment = segments[0];
            var lastSegment = segments[segments.Count - 1];

            var pathStart1 = firstSegment.edge1.a;
            var pathStart2 = firstSegment.edge2.a;
            var pathEnd1 = lastSegment.edge1.b;
            var pathEnd2 = lastSegment.edge2.b;

            // handle different end cap styles
            if (endCapStyle == EndCapStyle.SQUARE)
            {
                // extend the start/end points by half the thickness
                pathStart1 = Vec2.subtract(pathStart1, Vec2.multiply(firstSegment.edge1.direction(), thickness));
                pathStart2 = Vec2.subtract(pathStart2, Vec2.multiply(firstSegment.edge2.direction(), thickness));
                pathEnd1 = Vec2.add(pathEnd1, Vec2.multiply(lastSegment.edge1.direction(), thickness));
                pathEnd2 = Vec2.add(pathEnd2, Vec2.multiply(lastSegment.edge2.direction(), thickness));

            }
            else if (endCapStyle == EndCapStyle.ROUND)
            {
                // draw half circle end caps
                CreateTriangleFan(vertices, firstSegment.center.a, firstSegment.center.a,
                                  firstSegment.edge1.a, firstSegment.edge2.a, false);
                CreateTriangleFan(vertices, lastSegment.center.b, lastSegment.center.b,
                                  lastSegment.edge1.b, lastSegment.edge2.b, true);

            }
            else if (endCapStyle == EndCapStyle.JOINT)
            {
                // join the last (connecting) segment and the first segment
                CreateJoint(vertices, lastSegment, firstSegment, jointStyle,
                            ref pathEnd1, ref pathEnd2, ref pathStart1, ref pathStart2, allowOverlap);
            }

            // generate mesh data for path segments
            for (var i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];

                // calculate start
                if (i == 0)
                {
                    // this is the first segment
                    start1 = pathStart1;
                    start2 = pathStart2;
                }

                if (i + 1 == segments.Count)
                {
                    // this is the last segment
                    end1 = pathEnd1;
                    end2 = pathEnd2;

                }
                else
                {
                    CreateJoint(vertices, segment, segments[i + 1], jointStyle,
                                ref end1, ref end2, ref nextStart1, ref nextStart2, allowOverlap);
                }

                // emit vertices

                vertices.Add(start1);
                vertices.Add(start2);
                vertices.Add(end1);

                vertices.Add(end1);
                vertices.Add(start2);
                vertices.Add(end2);

                start1 = nextStart1;
                start2 = nextStart2;
            }

            return vertices;
        }


        private static List<Vec2> CreateJoint(List<Vec2> vertices,
                                      PolySegment segment1, PolySegment segment2,
                                      JointStyle jointStyle, ref Vec2 end1, ref Vec2 end2,
                                      ref Vec2 nextStart1, ref Vec2 nextStart2,
                                      bool allowOverlap)

        {
            // calculate the angle between the two line segments
            var dir1 = segment1.center.direction();
            var dir2 = segment2.center.direction();

            var angle = Vec2.angle(dir1, dir2);

            // wrap the angle around the 180° mark if it exceeds 90°
            // for minimum angle detection
            var wrappedAngle = angle;
            if (wrappedAngle > MathF.PI / 2)
            {
                wrappedAngle = MathF.PI - wrappedAngle;
            }

            if (jointStyle == JointStyle.MITER && wrappedAngle < miterMinAngle)
            {
                // the minimum angle for mitered joints wasn't exceeded.
                // to avoid the intersection point being extremely far out,
                // thus producing an enormous joint like a rasta on 4/20,
                // we render the joint beveled instead.
                jointStyle = JointStyle.BEVEL;
            }

            if (jointStyle == JointStyle.MITER)
            {
                // calculate each edge's intersection point
                // with the next segment's central line
                var sec1 = LineSegment.intersection(segment1.edge1, segment2.edge1, true);
                var sec2 = LineSegment.intersection(segment1.edge2, segment2.edge2, true);

                end1 = sec1 ?? segment1.edge1.b;
                end2 = sec2 ?? segment1.edge2.b;

                nextStart1 = end1;
                nextStart2 = end2;

            }
            else
            {
                // joint style is either BEVEL or ROUND

                // find out which are the inner edges for this joint
                var x1 = dir1.x;
                var x2 = dir2.x;
                var y1 = dir1.y;
                var y2 = dir2.y;

                var clockwise = x1 * y2 - x2 * y1 < 0;

                LineSegment inner1; LineSegment inner2; LineSegment outer1; LineSegment outer2;

                // as the normal vector is rotated counter-clockwise,
                // the first edge lies to the left
                // from the central line's perspective,
                // and the second one to the right.
                if (clockwise)
                {
                    outer1 = segment1.edge1;
                    outer2 = segment2.edge1;
                    inner1 = segment1.edge2;
                    inner2 = segment2.edge2;
                }
                else
                {
                    outer1 = segment1.edge2;
                    outer2 = segment2.edge2;
                    inner1 = segment1.edge1;
                    inner2 = segment2.edge1;
                }

                // calculate the intersection point of the inner edges
                var innerSecOpt = LineSegment.intersection(inner1, inner2, allowOverlap);

                var innerSec = innerSecOpt ??
                                 // for parallel lines, simply connect them directly
                                 inner1.b;

                // if there's no inner intersection, flip
                // the next start position for near-180° turns
                Vec2 innerStart;
                if (innerSecOpt is not null)
                {
                    innerStart = innerSec;
                }
                else if (angle > MathF.PI / 2)
                {
                    innerStart = outer1.b;
                }
                else
                {
                    innerStart = inner1.b;
                }

                if (clockwise)
                {
                    end1 = outer1.b;
                    end2 = innerSec;

                    nextStart1 = outer2.a;
                    nextStart2 = innerStart;

                }
                else
                {
                    end1 = innerSec;
                    end2 = outer1.b;

                    nextStart1 = innerStart;
                    nextStart2 = outer2.a;
                }

                // connect the intersection points according to the joint style

                if (jointStyle == JointStyle.BEVEL)
                {
                    // simply connect the intersection points
                    vertices.Add(outer1.b);
                    vertices.Add(outer2.a);
                    vertices.Add(innerSec);

                }
                else if (jointStyle == JointStyle.ROUND)
                {
                    // draw a circle between the ends of the outer edges,
                    // centered at the actual point
                    // with half the line thickness as the radius
                    CreateTriangleFan(vertices, innerSec, segment1.center.b, outer1.b, outer2.a, clockwise);
                }
                else
                {
                    throw new Exception("Generate failed : jointStyle is invaild");
                }
            }

            return vertices;
        }

        private static List<Vec2> CreateTriangleFan(List<Vec2> vertices, Vec2 connectTo, Vec2 origin,
                                            Vec2 start, Vec2 end, bool clockwise)
        {
            var point1 = Vec2.subtract(start, origin);
            var point2 = Vec2.subtract(end, origin);

            // calculate the angle between the two points
            var angle1 = MathF.Atan2(point1.y, point1.x);
            var angle2 = MathF.Atan2(point2.y, point2.x);

            // ensure the outer angle is calculated
            if (clockwise)
            {
                if (angle2 > angle1)
                {
                    angle2 = angle2 - 2 * MathF.PI;
                }
            }
            else
            {
                if (angle1 > angle2)
                {
                    angle1 = angle1 - 2 * MathF.PI;
                }
            }

            var jointAngle = angle2 - angle1;

            // calculate the amount of triangles to use for the joint
            var numTriangles = MathF.Max(1, (int)MathF.Floor(MathF.Abs(jointAngle) / roundMinAngle));

            // calculate the angle of each triangle
            var triAngle = jointAngle / numTriangles;

            Vec2 startPoint = start;
            Vec2 endPoint;
            for (int t = 0; t < numTriangles; t++)
            {
                if (t + 1 == numTriangles)
                {
                    // it's the last triangle - ensure it perfectly
                    // connects to the next line
                    endPoint = end;
                }
                else
                {
                    var rot = (t + 1) * triAngle;

                    // rotate the original point around the origin
                    endPoint.x = MathF.Cos(rot) * point1.x - MathF.Sin(rot) * point1.y;
                    endPoint.y = MathF.Sin(rot) * point1.x + MathF.Cos(rot) * point1.y;

                    // re-add the rotation origin to the target point
                    endPoint = Vec2.add(endPoint, origin);
                }

                // emit the triangle
                vertices.Add(startPoint);
                vertices.Add(endPoint);
                vertices.Add(connectTo);

                startPoint = endPoint;
            }

            return vertices;
        }
    }
}
