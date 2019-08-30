namespace WowOnlineKeeper
{
	using PInvoke;
	using System;
	using Tuple = System.ValueTuple<int, int>;

	public readonly struct Point
	{
		public readonly int X;
		public readonly int Y;
		public Point(in Tuple t) => (X, Y) = t;

		public int MaxComponent => Math.Max(X, Y);
		public int MinComponent => Math.Min(X, Y);
		public int Sum => X + Y;
		public Point Abs() => (Math.Abs(X), Math.Abs(Y));
		public IntPtr ToLParam() => (IntPtr) ((X & 0xFFFF) | (Y << 16));
		public Point Scale(in Point scale) => (X * scale.X, Y * scale.Y);
		public override string ToString() => $"({X.ToString()}, {Y.ToString()})";

		public static Point operator -(in Point p) => (-p.X, -p.Y);
		public static Point operator +(in Point p, in Point q) => (p.X + q.X, p.Y + q.Y);
		public static Point operator -(in Point p, in Point q) => (p.X - q.X, p.Y - q.Y);
		public static Point operator *(in Point p, int num) => (p.X * num, p.Y * num);
		public static Point operator *(int num, in Point p) => (p.X * num, p.Y * num);
		public static Point operator /(in Point p, int num) => (p.X / num, p.Y / num);

		public static implicit operator Point(in Tuple t) => new Point(t);
		public static implicit operator Point(in POINT p) => (p.x, p.y);
		public static implicit operator POINT(in Point p) => new POINT {x = p.X, y = p.Y};
	}
}