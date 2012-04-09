﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Mindstep.EasterEgg.Commons
{
    public static class Extensions
    {
        public static int Clamp(int i, int min, int max)
        {
            return Math.Max(Math.Min(i, max), min);
        }

        public static float Dot(this Vector3 v, Vector3 u)
        {
            return v.X * u.X + v.Y * u.Y + v.Z * u.Z;
        }

        public static Vector3 Project(this Vector3 v, Vector3 on)
        {
            return v.Dot(on) / on.LengthSquared() * on;
        }

        // remove this method.. sometime
        public static Vector4 matrixMul(Vector4 v, Matrix m)
        {
            return new Vector4(
                v.X * m.M11 + v.Y * m.M21 + v.Z * m.M31 + v.W * m.M41,
                v.X * m.M12 + v.Y * m.M22 + v.Z * m.M32 + v.W * m.M42,
                v.X * m.M13 + v.Y * m.M23 + v.Z * m.M33 + v.W * m.M43,
                v.X * m.M14 + v.Y * m.M24 + v.Z * m.M34 + v.W * m.M44
                );
        }

        public static Point Add(this Point p, Point q)
        {
            return new Point(p.X + q.X, p.Y + q.Y);
        }

        public static Point Subtract(this Point p, Point q)
        {
            return new Point(p.X - q.X, p.Y - q.Y);
        }

        public static Point Multiply(this Point p, float f)
        {
            return new Point((int)(p.X * f), (int)(p.Y * f));
        }

        public static Point Divide(this Point p, float f)
        {
            return new Point((int)(p.X / f), (int)(p.Y / f));
        }
    }
}
