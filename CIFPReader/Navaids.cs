using System.Text.Json;
using System.Text.Json.Serialization;

namespace CIFPReader;

#pragma warning disable IDE0059

[JsonConverter(typeof(NavaidJsonSerializer))]
public abstract record Navaid(string Header, string Identifier, Coordinate Position, decimal? MagneticVariation, string Name) : RecordLine(Header)
{
	public static new Navaid? Parse(string line) =>
		line[5] switch
		{
			' ' when line[28] == 'I' => NavaidILS.Parse(line),
			' ' when line[27] == 'V' => VOR.Parse(line),
			' ' => DME.Parse(line),

			'B' => NDB.Parse(line),

			_ => null
		};

	public enum ClassFacility
	{
		VOR = 'V',
		NDB = 'H',
		TACAN = ' ',
		SABH = 'S',
		MarineBacon = 'M'
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design",
		"CA1069:Enums values should not be duplicated",
		Justification = "Markers and navaids share letters.")]
	public enum ClassMarker
	{
		Inner = 'I',
		Middle = 'M',
		Outer = 'O',
		ILS = 'I',
		Back = 'C',
		TACAN = 'T',
		MilitaryTACAN = 'M',
		DME = 'D',
		None = ' '
	}

	public enum ClassPower
	{
		High = 'H',     // 200 watts + (min 75 nmi)
		Fifty = ' ',    // 50 - 199 watts (min 50 nmi)
		Medium = 'M',   // 25 - 49 watts (min 25 nmi)
		Low = 'L',       // 24 watts - (min 15 nmi)
		Terminal = 'T',
		ILS_TACAN = 'C',
		Undefined = 'U'
	}

	public enum ClassVoice
	{
		ATWB = 'A',
		ScheduledWeather = 'B',
		NoVoice = 'W',
		Voice = ' '
	}

	public class NavaidJsonSerializer : JsonConverter<Navaid>
	{
		public override Navaid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			string strVal = reader.GetString() ?? throw new JsonException();
			RecordLine? baseCall = JsonSerializer.Deserialize<RecordLine>(strVal, options);

			return baseCall?.Header switch
			{
				"DB" => JsonSerializer.Deserialize<NDB>(strVal, options),
				"DV" => JsonSerializer.Deserialize<VOR>(strVal, options),
				"DI" => JsonSerializer.Deserialize<NavaidILS>(strVal, options),
				"DD" => JsonSerializer.Deserialize<DME>(strVal, options),
				"PI" => JsonSerializer.Deserialize<ILS>(strVal, options),

				_ => throw new JsonException()
			};
		}

		public override void Write(Utf8JsonWriter writer, Navaid value, JsonSerializerOptions options)
		{
			switch (value)
			{
				case NDB n:
					writer.WriteStringValue(JsonSerializer.Serialize(n));
					break;

				case VOR v:
					writer.WriteStringValue(JsonSerializer.Serialize(v));
					break;

				case NavaidILS ni:
					writer.WriteStringValue(JsonSerializer.Serialize(ni));
					break;

				case DME d:
					writer.WriteStringValue(JsonSerializer.Serialize(d));
					break;

				case ILS i:
					writer.WriteStringValue(JsonSerializer.Serialize(i));
					break;

				default:
					throw new JsonException();
			}
		}
	}
}

public record NDB(
	string Identifier, ushort Channel, (Navaid.ClassFacility Facility, Navaid.ClassMarker Marker, Navaid.ClassPower Power, Navaid.ClassVoice Voice) Class,
	Coordinate Position, decimal? MagneticVariation, string Name) : Navaid("DB", Identifier, Position, MagneticVariation, Name)
{
	public static new NDB Parse(string line)
	{
		Check(line, 0, 1, "S");
		string client = line[1..4];

		if (line[4..6] != "PN")
			Check(line, 4, 6, "DB");

		string airport = line[6..10];
		string airportIcaoRegion = line[10..12];
		CheckEmpty(line, 12, 13);
		string identifier = line[13..17].TrimEnd();
		CheckEmpty(line, 17, 19);
		string icaoRegion = line[19..21];
		Check(line, 21, 22, "0");
		Check(line, 22, 23, "0");
		ushort channel = ushort.Parse(line[23..26]);
		Check(line, 26, 27, "0");

		(ClassFacility Facility, ClassMarker Marker, ClassPower Power, ClassVoice Voice) @class =
			((ClassFacility)line[27], (ClassMarker)line[28], (ClassPower)line[29], (ClassVoice)line[30]);
		CheckEmpty(line, 31, 32); // No BFO collocation in the CIFPs

		Coordinate position = new(line[32..51]);

		CheckEmpty(line, 51, 74);

		decimal magVar = decimal.Parse(line[75..79]) / 10;
		magVar *= line[74] == 'E' ? -1 : 1;

		CheckEmpty(line, 79, 90);
		Check(line, 90, 93, "NAR"); // Spheroid WGS-84
		
		string name = line[93..123].TrimEnd();

		int frn = int.Parse(line[123..128]);
		int cycle = int.Parse(line[128..132]);

		return new(identifier, channel, @class, position, magVar, name);
	}

	public static NDB Parse(TblVhfnavaid line)
	{
		string identifier = line.VorIdentifier.TrimEnd();
		string icaoRegion = line.AreaCode ?? "";
		ushort channel = (ushort?)line.VorFrequency ?? 0;

		(ClassFacility Facility, ClassMarker Marker, ClassPower Power, ClassVoice Voice) @class =
			((ClassFacility)line.NavaidClass[0], (ClassMarker)line.NavaidClass[1], (ClassPower)line.NavaidClass[2], line.NavaidClass.Length > 3 ? (ClassVoice)line.NavaidClass[3] : ClassVoice.NoVoice);

		if (@class.Facility is not ClassFacility.VOR and not ClassFacility.TACAN)
			throw new ArgumentException("Provided input string is not a VOR or TACAN");

		Coordinate position = new(line.VorLatitude!.Value, line.VorLongitude!.Value);

		decimal magVar = -(line.MagneticVariation ?? line.StationDeclination ?? 0m);

		string name = line.VorName?.TrimEnd() ?? "";

		return new(identifier, channel, @class, position, magVar, name);
	}
}

public record VOR(
	string Identifier, decimal Frequency, (Navaid.ClassFacility Facility, Navaid.ClassMarker Marker, Navaid.ClassPower Power, Navaid.ClassVoice Voice) Class,
	Coordinate Position, decimal? MagneticVariation, AltitudeMSL Elevation, string Name, DME? CollocatedDME) : Navaid("DV", Identifier, Position, MagneticVariation, Name)
{
	public static new VOR Parse(string line)
	{
		Check(line, 0, 1, "S");
		string client = line[1..4];
		Check(line, 4, 13, "D        ");
		string identifier = line[13..17].TrimEnd();
		Check(line, 17, 19, "  ");
		string icaoRegion = line[19..21];
		Check(line, 21, 22, "0");
		decimal frequency = decimal.Parse(line[22..27]) / 100;

		(ClassFacility Facility, ClassMarker Marker, ClassPower Power, ClassVoice Voice) @class =
			((ClassFacility)line[27], (ClassMarker)line[28], (ClassPower)line[29], (ClassVoice)line[30]);
		Check(line, 31, 32, " "); // No BFO collocation in the CIFPs

		if (@class.Facility != ClassFacility.VOR)
			throw new ArgumentException("Provided input string is not a VOR");

		Coordinate position = new(line[32..51]);

		DME? collocatedDME = null;

		if (@class.Marker == ClassMarker.DME || @class.Marker == ClassMarker.TACAN)
		{
			if (!string.IsNullOrWhiteSpace(line[51..55]))
				Check(line, 51, 55, identifier.PadRight(4, ' ')); // Does the DME identifier match the VOR identifier?

			collocatedDME = DME.Parse(line);
		}
		else
			CheckEmpty(line, 51, 74);

		decimal magVar = decimal.Parse(line[75..79]) / 10;
		magVar *= line[74] == 'E' ? -1 : 1;

		AltitudeMSL elevation = new((int)(decimal.Parse(line[79..85]) / 10));

		CheckEmpty(line, 85, 90);
		Check(line, 90, 93, "NAR"); // Spheroid WGS-84

		string name = line[93..123].TrimEnd();

		int frn = int.Parse(line[123..128]);
		int cycle = int.Parse(line[128..132]);

		return new(identifier, frequency, @class, position, magVar, elevation, name, collocatedDME);
	}

	public static VOR Parse(TblVhfnavaid line)
	{
		string identifier = line.VorIdentifier.TrimEnd();
		string icaoRegion = line.AreaCode ?? "";
		decimal frequency = line.VorFrequency ?? 0m;

		(ClassFacility Facility, ClassMarker Marker, ClassPower Power, ClassVoice Voice) @class =
			((ClassFacility)line.NavaidClass[0], (ClassMarker)line.NavaidClass[1], (ClassPower)line.NavaidClass[2], line.NavaidClass.Length > 3 ? (ClassVoice)line.NavaidClass[3] : ClassVoice.NoVoice);

		if (@class.Facility is not ClassFacility.VOR and not ClassFacility.TACAN)
			throw new ArgumentException("Provided input string is not a VOR or TACAN");

		Coordinate position = new(line.VorLatitude!.Value, line.VorLongitude!.Value);

		DME? collocatedDME = null;

		if (@class.Marker == ClassMarker.DME || @class.Marker == ClassMarker.TACAN)
			collocatedDME = DME.Parse(line);

		decimal magVar = -(line.MagneticVariation ?? line.StationDeclination ?? 0m);

		AltitudeMSL elevation = new(line.DmeElevation ?? 0);

		string name = line.VorName?.TrimEnd() ?? "";

		return new(identifier, frequency, @class, position, magVar, elevation, name, collocatedDME);
	}
}

public record NavaidILS(
	string Identifier, decimal Frequency, (Navaid.ClassFacility Facility, Navaid.ClassMarker Marker, Navaid.ClassPower Power, Navaid.ClassVoice Voice, bool OnField) Class,
	Coordinate Position, decimal? MagneticVariation, AltitudeMSL Elevation, string Name, DME CollocatedDME) : Navaid("DI", Identifier, Position, MagneticVariation, Name)
{
	public static new NavaidILS Parse(string line)
	{
		Check(line, 0, 1, "S");
		string client = line[1..4];
		Check(line, 4, 6, "D ");
		string airport = line[6..10];
		string airportRegion = line[10..12];

		string identifier = line[13..17].TrimEnd();
		CheckEmpty(line, 17, 19);
		string icaoRegion = line[19..21];
		Check(line, 21, 22, "0");
		decimal frequency = decimal.Parse(line[22..27]) / 10;

		(ClassFacility Facility, ClassMarker Marker, ClassPower Power, ClassVoice Voice, bool OnField) @class =
			((ClassFacility)line[27], (ClassMarker)line[28], (ClassPower)line[29], (ClassVoice)line[30], line[31] == ' ');

		if (@class.Marker != ClassMarker.Inner)
			throw new ArgumentException("Provided input string is not an ILS");

		if (!string.IsNullOrWhiteSpace(line[51..55]))
			Check(line, 51, 55, identifier.PadRight(4, ' ')); // Does the DME identifier match the VOR identifier?

		DME collocatedDME = DME.Parse(line);

		decimal magVar = decimal.Parse(line[75..79]) / 10;
		magVar *= line[74] == 'E' ? -1 : 1;

		AltitudeMSL elevation = new((int)(decimal.Parse(line[79..85]) / 10));

		CheckEmpty(line, 85, 90);
		Check(line, 90, 93, "NAR"); // Spheroid WGS-84

		string name = line[93..123].TrimEnd();

		int frn = int.Parse(line[123..128]);
		int cycle = int.Parse(line[128..132]);

		return new(identifier, frequency, @class, collocatedDME.Position, magVar, elevation, name, collocatedDME);
	}
	public static NavaidILS Parse(TblVhfnavaid line)
	{
		string airport = line.AirportIdentifier ?? "ZZZZ";
		string airportRegion = line.AreaCode ?? "";

		string identifier = line.Id?.TrimEnd() ?? "IZZZ";
		string icaoRegion = line.AreaCode ?? "";
		decimal frequency = line.VorFrequency ?? 0m;

		(ClassFacility Facility, ClassMarker Marker, ClassPower Power, ClassVoice Voice, bool OnField) @class =
			((ClassFacility)line.NavaidClass[0], (ClassMarker)line.NavaidClass[1], (ClassPower)line.NavaidClass[2], (ClassVoice)line.NavaidClass[3], line.NavaidClass[4] == ' ');

		if (@class.Marker != ClassMarker.Inner)
			throw new ArgumentException("Provided input string is not an ILS");

		DME collocatedDME = DME.Parse(line);

		decimal magVar = -(line.MagneticVariation ?? line.StationDeclination ?? 0m);

		AltitudeMSL elevation = new(line.DmeElevation ?? 0);

		string name = line.VorName?.TrimEnd() ?? "";
		return new(identifier, frequency, @class, collocatedDME.Position, magVar, elevation, name, collocatedDME);
	}
}

public record DME(
	string Identifier, ushort Channel, (Navaid.ClassFacility Facility, Navaid.ClassMarker Marker, Navaid.ClassPower Power, Navaid.ClassVoice Voice) Class,
	Coordinate Position, AltitudeMSL Elevation, string Name) : Navaid("DD", Identifier, Position, null, Name)
{
	public static new DME Parse(string line)
	{
		Check(line, 0, 1, "S");
		string client = line[1..4];
		Check(line, 4, 6, "D ");
		CheckEmpty(line, 17, 19);
		string icaoRegion = line[19..21];
		Check(line, 21, 22, "0");
		decimal frequency = decimal.Parse(line[22..27]) / 10;
		if (frequency >= 1000)
			frequency /= 10;

		ushort channel = (ushort)((int)(frequency * 10) switch
		{
			>= 1344 and < 1360 => (int)(frequency * 10) - 1344 + 1,
			>= 1080 and < 1123 => (int)(frequency * 10) - 1080 + 17,
			>= 1333 and < 1343 => (int)(frequency * 10) - 1333 + 60,
			>= 1123 and < 1180 => (int)(frequency * 10) - 1123 + 70,

			_ => throw new NotImplementedException()
		});

		(ClassFacility Facility, ClassMarker Marker, ClassPower Power, ClassVoice Voice) @class =
			((ClassFacility)line[27], (ClassMarker)line[28], (ClassPower)line[29], (ClassVoice)line[30]);

		bool bfoCollocated = line[31] != ' ';

		if (!new ClassMarker[] { ClassMarker.DME, ClassMarker.TACAN, ClassMarker.MilitaryTACAN, ClassMarker.ILS }.Contains(@class.Marker))
			throw new ArgumentException("Provided input does not have DME");

		string identifier = line[51..55].TrimEnd();
		Coordinate position = new(line[55..74]);

		AltitudeMSL elevation = new((int)(decimal.Parse(line[79..85]) / 10));

		CheckEmpty(line, 85, 90);
		Check(line, 90, 93, "NAR"); // Spheroid WGS-84

		string name = line[93..123].TrimEnd();

		int frn = int.Parse(line[123..128]);
		int cycle = int.Parse(line[128..132]);

		return new(identifier, channel, @class, position, elevation, name);
	}

	public static DME Parse(TblVhfnavaid line)
	{
		string icaoRegion = line.AreaCode ?? "";
		decimal frequency = line.VorFrequency ?? 0m;
		if (frequency >= 1000)
			frequency /= 10;

		ushort channel = (ushort)((int)(frequency * 10) switch {
			>= 1344 and < 1360 => (int)(frequency * 10) - 1344 + 1,
			>= 1080 and < 1123 => (int)(frequency * 10) - 1080 + 17,
			>= 1333 and < 1343 => (int)(frequency * 10) - 1333 + 60,
			>= 1123 and < 1180 => (int)(frequency * 10) - 1123 + 70,

			_ => throw new NotImplementedException()
		});

		(ClassFacility Facility, ClassMarker Marker, ClassPower Power, ClassVoice Voice) @class =
			((ClassFacility)line.NavaidClass[0], (ClassMarker)line.NavaidClass[1], (ClassPower)line.NavaidClass[2], line.NavaidClass.Length > 3 ? (ClassVoice)line.NavaidClass[3] : ClassVoice.NoVoice);

		bool bfoCollocated = line.NavaidClass.Length > 4 && line.NavaidClass[4] != ' ';

		if (!new ClassMarker[] { ClassMarker.DME, ClassMarker.TACAN, ClassMarker.MilitaryTACAN, ClassMarker.ILS }.Contains(@class.Marker))
			throw new ArgumentException("Provided input does not have DME");

		string identifier = line.DmeIdent?.TrimEnd() ?? "";
		Coordinate position = new(line.DmeLatitude!.Value, line.DmeLongitude!.Value);

		AltitudeMSL elevation = new(line.DmeElevation!.Value);

		string name = line.VorName?.TrimEnd() ?? "";

		return new(identifier, channel, @class, position, elevation, name);
	}
}

public record ILS(string Client,
	string Airport, string Identifier, Runway.RunwayApproachCategory Category, decimal Frequency, string Runway,
	Coordinate LocalizerPosition, MagneticCourse LocalizerCourse, Coordinate? GlideslopePosition,
	int FileRecordNumber, int Cycle) : Navaid("PI", Identifier, LocalizerPosition, LocalizerCourse.Variation, $"{Identifier} ({Airport} - {Runway})")
{
	public static new ILS Parse(string line)
	{
		Check(line, 0, 1, "S");
		string client = line[1..4];
		Check(line, 4, 6, "P ");

		string airport = line[6..10];
		string icaoRegion = line[10..12];
		Check(line, 12, 13, "I");
		string identifier = line[13..17];
		Runway.RunwayApproachCategory category = (Runway.RunwayApproachCategory)line[17];

		CheckEmpty(line, 18, 21);
		Check(line, 21, 22, "0");

		decimal frequency = decimal.Parse(line[22..27]) / 100;
		string runway = line[27..32];

		Coordinate localizerPosition = new(line[32..51]);
		decimal locMagCrs = decimal.Parse(line[51..55]) / 10;
		Coordinate? glideslopePosition = string.IsNullOrWhiteSpace(line[55..74]) ? null : new(line[55..74]);

		MagneticCourse localizerCourse = new(locMagCrs, decimal.Parse(line[91..95]) / 10 * (line[90] == 'E' ? -1 : 1));

		int frn = int.Parse(line[123..128]);
		int cycle = int.Parse(line[128..132]);

		return new(client, airport, identifier, category, frequency, runway, localizerPosition, localizerCourse, glideslopePosition, frn, cycle);
	}
}