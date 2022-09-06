using System;
using System.Collections.Generic;
using System.Text;

namespace Polyline2DCSharp
{
    public struct Vec2
    {
        public Vec2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public float x;
        public float y;


        public static bool equal(Vec2 a, Vec2 b)
        {
            return a.x == b.x && a.y == b.y;
        }


        public static Vec2 multiply(Vec2 a, Vec2 b)
        {
            return new(a.x * b.x, a.y * b.y);
        }


        public static Vec2 multiply(Vec2 vec, float factor)
        {
            return new(vec.x * factor, vec.y * factor);
        }


        public static Vec2 divide(Vec2 vec, float factor)
        {
            return new(vec.x / factor, vec.y / factor);
        }


        public static Vec2 add(Vec2 a, Vec2 b)
        {
            return new(a.x + b.x, a.y + b.y);
        }


        public static Vec2 subtract(Vec2 a, Vec2 b)
        {
            return new(a.x - b.x, a.y - b.y);
        }


        public static float magnitude(Vec2 vec)
        {
            return MathF.Sqrt(vec.x * vec.x + vec.y * vec.y);
        }


        public static Vec2 withLength(Vec2 vec, float len)
        {
            var mag = magnitude(vec);
            var factor = mag / len;
            return divide(vec, factor);
        }


        public static Vec2 normalized(Vec2 vec)
        {
            return withLength(vec, 1);
        }

        /**
         * Calculates the dot product of two vectors.
         */

        public static float dot(Vec2 a, Vec2 b)
        {
            return a.x * b.x + a.y * b.y;
        }

        /**
         * Calculates the cross product of two vectors.
         */

        public static float cross(Vec2 a, Vec2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        /**
         * Calculates the angle between two vectors.
         */

        public static float angle(Vec2 a, Vec2 b)
        {
            return MathF.Acos(dot(a, b) / (magnitude(a) * magnitude(b)));
        }
    }
}
