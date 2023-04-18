﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

using Vec3 = System.Numerics.Vector3;
using Vec4 = System.Numerics.Vector4;

namespace Epsylon.TextureSquish {
    static class MathUtils {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SwapElements<T>(this T[] array, int index1, int index2) {
            T tmp = array[index1];
            array[index1] = array[index2];
            array[index2] = tmp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToPackedInt565(this Vec3 colour) {
            // get the components in the correct range
            int r = FloatToInt(31.0f * colour.X, 31);
            int g = FloatToInt(63.0f * colour.Y, 63);
            int b = FloatToInt(31.0f * colour.Z, 31);

            // pack into a single value
            return (r << 11) | (g << 5) | b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FloatToInt(this float a, int limit) {
            // use ANSI round-to-zero behaviour to get round-to-nearest
            int i = (int) (a + 0.5f);

            // clamp to the limit
            if (i < 0) i = 0;
            else if (i > limit) i = limit;

            // done
            return i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec3 Clamp(this Vec3 value, Vec3 min, Vec3 max) {
            return Vec3.Max(min, Vec3.Min(max, value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 Clamp(this Vec4 value, Vec4 min, Vec4 max) {
            return Vec4.Max(min, Vec4.Min(max, value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float LengthManhattan(this Vec3 v) {
            return Math.Abs(v.X) + Math.Abs(v.Y) + Math.Abs(v.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec3 Truncate(this Vec3 v) {
            return new Vec3(
                v.X > 0.0f ? (float) Math.Floor(v.X) : (float) Math.Ceiling(v.X),
                v.Y > 0.0f ? (float) Math.Floor(v.Y) : (float) Math.Ceiling(v.Y),
                v.Z > 0.0f ? (float) Math.Floor(v.Z) : (float) Math.Ceiling(v.Z)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec3 GetVec3(this Vec4 v) { return new Vec3(v.X, v.Y, v.Z); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 SplatX(this Vec4 v) { return new Vec4(v.X); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 SplatY(this Vec4 v) { return new Vec4(v.Y); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 SplatZ(this Vec4 v) { return new Vec4(v.Z); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 SplatW(this Vec4 v) { return new Vec4(v.W); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 GetVec4(this byte[] array, int startIndex) {
            var x = array[startIndex + 0];
            var y = array[startIndex + 1];
            var z = array[startIndex + 2];
            var w = array[startIndex + 3];

            return new Vec4((float) x / 255.0f, (float) y / 255.0f, (float) z / 255.0f, (float) w / 255.0f);
        }

        /// <summary>
        /// a * b + c
        /// </summary>        
        /// <returns>a * b + c</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 MultiplyAdd(this Vec4 a, Vec4 b, Vec4 c) {
            return a * b + c;
        }

        /// <summary>
        /// a * b - c
        /// </summary>        
        /// <returns>a * b - c</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 NegativeMultiplySubtract(this Vec4 a, Vec4 b, Vec4 c) {
            return c - a * b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 Reciprocal(this Vec4 v) {
            return new Vec4
            (
                1.0f / v.X,
                1.0f / v.Y,
                1.0f / v.Z,
                1.0f / v.W
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 Truncate(this Vec4 v) {
            return new Vec4
            (
                (float) (v.X > 0 ? Math.Floor(v.X) : Math.Ceiling(v.X)),
                (float) (v.Y > 0 ? Math.Floor(v.Y) : Math.Ceiling(v.Y)),
                (float) (v.Z > 0 ? Math.Floor(v.Z) : Math.Ceiling(v.Z)),
                (float) (v.W > 0 ? Math.Floor(v.W) : Math.Ceiling(v.W))
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CompareAnyLessThan(this float left, float right) {
            return left < right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CompareAnyLessThan(this Vec4 left, Vec4 right) {
            return left.X < right.X
                || left.Y < right.Y
                || left.Z < right.Z
                || left.W < right.W;
        }

    }
}
