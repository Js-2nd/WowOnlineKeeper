namespace WowOnlineKeeper
{
	using System;

	public readonly struct Vector
	{
		public readonly double X;
		public readonly double Y;

		public Vector(double x, double y) => (X, Y) = (x, y);
		public Vector(in ValueTuple<double, double> tuple) => (X, Y) = tuple;

		public Vector Scale(double scale) => (X * scale, Y * scale);
		public Vector Scale(in Vector scale) => (X * scale.X, Y * scale.Y);

		public static Vector operator +(in Vector v, in Vector w) => (v.X + w.X, v.Y + w.Y);
		public static Vector operator -(in Vector v, in Vector w) => (v.X - w.X, v.Y - w.Y);
		public static Vector operator -(in Vector v) => (-v.X, -v.Y);
		public static Vector operator *(in Vector v, double num) => (v.X * num, v.Y * num);
		public static Vector operator *(double num, in Vector v) => (v.X * num, v.Y * num);
		public static Vector operator /(in Vector v, double num) => (v.X / num, v.Y / num);

		public static implicit operator Vector(in ValueTuple<double, double> tuple) => new Vector(tuple);
	}
}