namespace WowOnlineKeeper
{
	using System;

	public struct Picker
	{
		public Vector Pivot;
		public Vector Anchor;
		public Vector Offset;

		public Picker(in Vector pivot, in Vector anchor = default, in Vector offset = default) =>
			(Pivot, Anchor, Offset) = (pivot, anchor, offset);
	}

	public static class PickerExtensions
	{
		public static Vector Pick(this in Picker picker, in Vector size, double aspectRatio) =>
			size.Scale(picker.Pivot) + Fit(size, aspectRatio).Scale(picker.Anchor) + picker.Offset;

		static Vector Fit(in Vector size, double aspectRatio)
		{
			double x = Math.Min(size.X, size.Y * aspectRatio);
			return (x, x / aspectRatio);
		}
	}
}