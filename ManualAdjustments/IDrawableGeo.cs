using CIFPReader;

namespace ManualAdjustments;

public interface IDrawableGeo
{
	public bool Resolve(CIFP cifp);
	public IEnumerable<ICoordinate?> Draw();
	public Coordinate[] ReferencePoints { get; }
}

public record struct PossiblyResolvedWaypoint(ICoordinate? Coordinate, UnresolvedWaypoint? FixName, UnresolvedFixRadialDistance? FixRadialDistance)
{
	public readonly ICoordinate Resolve(CIFP cifp)
	{
		if (Coordinate is ICoordinate cic) return cic;

		if (FixName is UnresolvedWaypoint fnuw) return fnuw.Resolve(cifp.Fixes);

		if (FixRadialDistance is UnresolvedFixRadialDistance frdud) return frdud.Resolve(cifp.Fixes, cifp.Navaids);

		throw new NotImplementedException();
	}
}

public abstract record GeoSymbol(PossiblyResolvedWaypoint Centerpoint, decimal Size) : IDrawableGeo
{
	protected ICoordinate _resolvedCenterpoint = new Coordinate(0, 0);
	protected decimal _magVar = 0;

	public bool Resolve(CIFP cifp)
	{
		try
		{
			_resolvedCenterpoint = Centerpoint.Resolve(cifp);
			_magVar = cifp.Navaids.GetLocalMagneticVariation(_resolvedCenterpoint.GetCoordinate()).Variation;
			return true;
		}
		catch
		{
			Console.WriteLine($"Could not find {Centerpoint.FixName}");
			return false;
		}
	}

	public abstract IEnumerable<ICoordinate?> Draw();

	public Coordinate[] ReferencePoints => [_resolvedCenterpoint.GetCoordinate()];

	public sealed record Point(PossiblyResolvedWaypoint Centerpoint) : GeoSymbol(Centerpoint, 0)
	{
		public override IEnumerable<ICoordinate?> Draw() => [_resolvedCenterpoint];
	}

	public sealed record Circle(PossiblyResolvedWaypoint Centerpoint, decimal Size) : GeoSymbol(Centerpoint, Size)
	{
		public override IEnumerable<ICoordinate?> Draw() =>
			Enumerable.Range(0, 37)
			.Select(r => new TrueCourse(r * 10))
			.Select(r => (ICoordinate?)_resolvedCenterpoint.GetCoordinate().FixRadialDistance(r, Size));
	}

	public sealed record Waypoint(PossiblyResolvedWaypoint Centerpoint, decimal Size) : GeoSymbol(Centerpoint, Size)
	{
		const decimal INNER_SCALE = 0.35m;
		const decimal FLARE_1_SCALE = 0.4m;
		const decimal FLARE_2_SCALE = 0.5m;

		private static IEnumerable<Coordinate?> RepeatFirst(IEnumerable<Coordinate?> source)
		{
			Coordinate? first = null;
			foreach (Coordinate? c in source)
			{
				first ??= c;
				yield return c;
			}

			yield return first;
		}

		public override IEnumerable<ICoordinate?> Draw() => [
			..Enumerable.Range(0, 37).Select(r => new TrueCourse(r * 10)).Select(r => _resolvedCenterpoint.GetCoordinate().FixRadialDistance(r, Size * INNER_SCALE)), null,
			..RepeatFirst(Enumerable.Range(1, 4).Select(r => new MagneticCourse(r * 90, _magVar)).SelectMany<MagneticCourse, Coordinate?>(r => [
				_resolvedCenterpoint.GetCoordinate().FixRadialDistance(r - 45, Size * INNER_SCALE),
				_resolvedCenterpoint.GetCoordinate().FixRadialDistance(r - 30, Size * FLARE_1_SCALE),
				_resolvedCenterpoint.GetCoordinate().FixRadialDistance(r - 15, Size * FLARE_2_SCALE),
				_resolvedCenterpoint.GetCoordinate().FixRadialDistance(r, Size),
				_resolvedCenterpoint.GetCoordinate().FixRadialDistance(r + 15, Size * FLARE_2_SCALE),
				_resolvedCenterpoint.GetCoordinate().FixRadialDistance(r + 30, Size * FLARE_1_SCALE),
			]))
		];
	}

	public sealed record Triangle(PossiblyResolvedWaypoint Centerpoint, decimal Size) : GeoSymbol(Centerpoint, Size)
	{
		public override IEnumerable<ICoordinate?> Draw() => [
			_resolvedCenterpoint.GetCoordinate().FixRadialDistance(new MagneticCourse(000m, _magVar), Size),
			_resolvedCenterpoint.GetCoordinate().FixRadialDistance(new MagneticCourse(120m, _magVar), Size),
			_resolvedCenterpoint.GetCoordinate().FixRadialDistance(new MagneticCourse(240m, _magVar), Size),
			_resolvedCenterpoint.GetCoordinate().FixRadialDistance(new MagneticCourse(000m, _magVar), Size),
		];
	}

	public sealed record Nuclear(PossiblyResolvedWaypoint Centerpoint, decimal Size) : GeoSymbol(Centerpoint, Size)
	{
		const decimal INNER_SCALE = 0.15m;

		public override IEnumerable<ICoordinate?> Draw()
		{
			bool far = true;

			for (uint angle = 0; angle <= 360; angle += 10)
			{
				MagneticCourse radial = new(angle, _magVar);
				yield return _resolvedCenterpoint.GetCoordinate().FixRadialDistance(radial, far ? Size : Size * INNER_SCALE);

				if ((angle + 30) % 60 == 0)
				{
					far = !far;
					yield return _resolvedCenterpoint.GetCoordinate().FixRadialDistance(radial, far ? Size : Size * INNER_SCALE);
				}
			}
		}
	}

	public sealed record Flag(PossiblyResolvedWaypoint Centerpoint, decimal Size) : GeoSymbol(Centerpoint, Size)
	{
		public override IEnumerable<ICoordinate?> Draw() => [
			_resolvedCenterpoint,
			_resolvedCenterpoint.GetCoordinate().FixRadialDistance(new MagneticCourse(00, _magVar), Size),
			_resolvedCenterpoint.GetCoordinate().FixRadialDistance(new MagneticCourse(30, _magVar), Size),
			_resolvedCenterpoint.GetCoordinate().FixRadialDistance(new MagneticCourse(00, _magVar), Size * 0.66m),
		];
	}

	public sealed record Diamond(PossiblyResolvedWaypoint Centerpoint, decimal Size) : GeoSymbol(Centerpoint, Size)
	{
		public override IEnumerable<ICoordinate?> Draw() => [
			_resolvedCenterpoint.GetCoordinate().FixRadialDistance(new MagneticCourse(000m, _magVar), Size),
			_resolvedCenterpoint.GetCoordinate().FixRadialDistance(new MagneticCourse(090m, _magVar), Size * 0.5m),
			_resolvedCenterpoint.GetCoordinate().FixRadialDistance(new MagneticCourse(180m, _magVar), Size),
			_resolvedCenterpoint.GetCoordinate().FixRadialDistance(new MagneticCourse(270m, _magVar), Size * 0.5m),
			_resolvedCenterpoint.GetCoordinate().FixRadialDistance(new MagneticCourse(000m, _magVar), Size),
		];
	}

	public sealed record Chevron(PossiblyResolvedWaypoint Centerpoint, decimal Size) : GeoSymbol(Centerpoint, Size)
	{
		public override IEnumerable<ICoordinate?> Draw() => [
			_resolvedCenterpoint.GetCoordinate().FixRadialDistance(new MagneticCourse(240m, _magVar), Size),
			_resolvedCenterpoint.GetCoordinate().FixRadialDistance(new MagneticCourse(000m, _magVar), Size),
			_resolvedCenterpoint.GetCoordinate().FixRadialDistance(new MagneticCourse(120m, _magVar), Size),
		];
	}

	public sealed record Box(PossiblyResolvedWaypoint Centerpoint, decimal Size) : GeoSymbol(Centerpoint, Size)
	{
		public override IEnumerable<ICoordinate?> Draw() => [
			_resolvedCenterpoint.GetCoordinate().FixRadialDistance(new MagneticCourse(045m, _magVar), Size),
			_resolvedCenterpoint.GetCoordinate().FixRadialDistance(new MagneticCourse(135m, _magVar), Size),
			_resolvedCenterpoint.GetCoordinate().FixRadialDistance(new MagneticCourse(225m, _magVar), Size),
			_resolvedCenterpoint.GetCoordinate().FixRadialDistance(new MagneticCourse(315m, _magVar), Size),
			_resolvedCenterpoint.GetCoordinate().FixRadialDistance(new MagneticCourse(045m, _magVar), Size),
		];
	}

	public sealed record Star(PossiblyResolvedWaypoint Centerpoint, decimal Size) : GeoSymbol(Centerpoint, Size)
	{
		const decimal INNER_SCALE = 0.5m;
		public override IEnumerable<ICoordinate?> Draw() =>
			Enumerable.Range(0, 11)
			.Select(r => new MagneticCourse(r * 36, _magVar))
			.Select(r => (ICoordinate?)_resolvedCenterpoint.GetCoordinate().FixRadialDistance(r, (int)r.Degrees % 72 == 0 ? Size : Size * INNER_SCALE));
	}
}

public abstract record GeoConnector(PossiblyResolvedWaypoint[] Points) : IDrawableGeo
{
	protected ICoordinate[] _resolvedPoints = [];

	public bool Resolve(CIFP cifp)
	{
		try
		{
			_resolvedPoints = [.. Points.Select(p => p.Resolve(cifp))];
			return true;
		}
		catch { return false; }
	}

	public abstract IEnumerable<ICoordinate?> Draw();

	public Coordinate[] ReferencePoints => [.. _resolvedPoints.Select(c => c.GetCoordinate())];

	public sealed record Line(PossiblyResolvedWaypoint[] Points) : GeoConnector(Points)
	{
		public override IEnumerable<ICoordinate?> Draw() => _resolvedPoints;
	}

	/// <param name="Size">STARS default 1nmi.</param>
	public sealed record Dash(PossiblyResolvedWaypoint[] Points, decimal Size) : GeoConnector(Points)
	{
		public override IEnumerable<ICoordinate?> Draw()
		{
			if (_resolvedPoints.Length < 2)
				yield break;

			bool lastReturned = false;
			foreach (var (from, to) in _resolvedPoints[..^1].Zip(_resolvedPoints[1..]))
			{
				if (from.GetCoordinate().GetBearingDistance(to.GetCoordinate()).bearing is not TrueCourse direction) continue;
				Coordinate next;

				for (Coordinate startPoint = from.GetCoordinate(); startPoint.DistanceTo(to.GetCoordinate()) > Size; startPoint = next)
				{
					next = startPoint.FixRadialDistance(direction, Math.Min(Size, startPoint.DistanceTo(to.GetCoordinate())));

					if (lastReturned)
					{
						yield return startPoint;
						yield return next;
					}
					else
						yield return null;

					lastReturned = !lastReturned;
				}
			}
		}
	}

	/// <param name="Size">STARS default 0.5nmi.</param>
	public sealed record Arrow(PossiblyResolvedWaypoint[] Points, decimal Size) : GeoConnector(Points)
	{
		public override IEnumerable<ICoordinate?> Draw()
		{
			if (_resolvedPoints.Length < 2)
				yield break;

			foreach (var (from, to) in _resolvedPoints[..^1].Zip(_resolvedPoints[1..]))
			{
				if (to.GetCoordinate().GetBearingDistance(from.GetCoordinate()).bearing is not Course direction) continue;

				yield return from;
				yield return to;
				yield return to.GetCoordinate().FixRadialDistance(direction + 30m, Size);
				yield return to;
				yield return to.GetCoordinate().FixRadialDistance(direction - 30m, Size);
				yield return to;
			}
		}
	}

	public sealed record Arc(PossiblyResolvedWaypoint[] Points, Arc.Direction Towards) : GeoConnector(Points)
	{
		public enum Direction : ushort
		{
			North = 0,
			South = 180,
			East = 90,
			West = 270
		}

		public override IEnumerable<ICoordinate?> Draw()
		{
			if (_resolvedPoints.Length == 0) yield break;

			yield return _resolvedPoints[0];

			if (_resolvedPoints.Length == 1) yield break;

			foreach (var (from, to) in _resolvedPoints[..^1].Zip(_resolvedPoints[1..]).Select(p => (p.First.GetCoordinate(), p.Second.GetCoordinate())))
			{
				var fromToTo = from.GetBearingDistance(to);
				Coordinate centerpoint = from.FixRadialDistance(fromToTo.bearing ?? new(0), fromToTo.distance / 2);

				TrueCourse startAngle = centerpoint.GetBearingDistance(from).bearing ?? new(0),
							 endAngle = centerpoint.GetBearingDistance(to).bearing ?? new(0);
				int step = Math.Sign(startAngle.Angle(new TrueCourse((decimal)Towards)));
				step = (step == 0 ? 1 : step) * 15;

				for (Course angle = startAngle; (int)angle.Degrees % 360 != (int)endAngle.Degrees % 360; angle += (Math.Abs(endAngle.Angle(angle)) < Math.Abs(step)) ? angle.Angle(endAngle) : step)
					yield return centerpoint.FixRadialDistance(angle, fromToTo.distance / 2);

				if (to != _resolvedPoints[^1].GetCoordinate())
					yield return to;
			}

			if (_resolvedPoints.Length > 1)
				yield return _resolvedPoints[^1];
		}
	}

	/// <summary>Dashes are 10°.</summary>
	public sealed record DashArc(PossiblyResolvedWaypoint[] Points, Arc.Direction Towards) : GeoConnector(Points)
	{
		public override IEnumerable<ICoordinate?> Draw()
		{
			if (_resolvedPoints.Length == 0) yield break;

			yield return _resolvedPoints[0];

			if (_resolvedPoints.Length == 1) yield break;

			foreach (var (from, to) in _resolvedPoints[..^1].Zip(_resolvedPoints[1..]).Select(p => (p.First.GetCoordinate(), p.Second.GetCoordinate())))
			{
				var fromToTo = from.GetBearingDistance(to);
				Coordinate centerpoint = from.FixRadialDistance(fromToTo.bearing ?? new(0), fromToTo.distance / 2);

				TrueCourse startAngle = centerpoint.GetBearingDistance(from).bearing ?? new(0),
							 endAngle = centerpoint.GetBearingDistance(to).bearing ?? new(0);
				int step = Math.Sign(startAngle.Angle(new TrueCourse((decimal)Towards)));
				step = (step == 0 ? 1 : step) * 10;
				Coordinate? last = null;

				for (Course angle = startAngle; (int)angle.Degrees % 360 != (int)endAngle.Degrees % 360; angle += (Math.Abs(endAngle.Angle(angle)) < Math.Abs(step)) ? angle.Angle(endAngle) : step)
				{
					yield return last;

					Coordinate next = centerpoint.FixRadialDistance(angle, fromToTo.distance / 2);
					if (last is Coordinate l)
					{
						yield return next;
						last = null;
					}
					else
					{
						yield return null;
						last = next;
					}
				}

				if (to != _resolvedPoints[^1].GetCoordinate())
					yield return to;
			}

			if (_resolvedPoints.Length > 1)
				yield return _resolvedPoints[^1];
		}
	}
}
