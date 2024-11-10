using Microsoft.EntityFrameworkCore;

namespace CIFPReader;

public partial class AiracContext : DbContext
{
    private string _dbFilePath;

    public AiracContext(string dbFilePath) => _dbFilePath = dbFilePath;

	public AiracContext(string dbFilePath, DbContextOptions<AiracContext> options)
        : base(options) => _dbFilePath = dbFilePath;

	public virtual DbSet<TblAirport> TblAirports { get; set; }

    public virtual DbSet<TblAirportCommunication> TblAirportCommunications { get; set; }

    public virtual DbSet<TblAirportMsa> TblAirportMsas { get; set; }

    public virtual DbSet<TblControlledAirspace> TblControlledAirspaces { get; set; }

    public virtual DbSet<TblCruisingTable> TblCruisingTables { get; set; }

    public virtual DbSet<TblEnrouteAirway> TblEnrouteAirways { get; set; }

    public virtual DbSet<TblEnrouteAirwayRestriction> TblEnrouteAirwayRestrictions { get; set; }

    public virtual DbSet<TblEnrouteCommunication> TblEnrouteCommunications { get; set; }

    public virtual DbSet<TblEnrouteNdbnavaid> TblEnrouteNdbnavaids { get; set; }

    public virtual DbSet<TblEnrouteWaypoint> TblEnrouteWaypoints { get; set; }

    public virtual DbSet<TblFirUir> TblFirUirs { get; set; }

    public virtual DbSet<TblGate> TblGates { get; set; }

    public virtual DbSet<TblGl> TblGls { get; set; }

    public virtual DbSet<TblGridMora> TblGridMoras { get; set; }

    public virtual DbSet<TblHeader> TblHeaders { get; set; }

    public virtual DbSet<TblHolding> TblHoldings { get; set; }

    public virtual DbSet<TblIap> TblIaps { get; set; }

    public virtual DbSet<TblLocalizerMarker> TblLocalizerMarkers { get; set; }

    public virtual DbSet<TblLocalizersGlideslope> TblLocalizersGlideslopes { get; set; }

    public virtual DbSet<TblPathpoint> TblPathpoints { get; set; }

    public virtual DbSet<TblRestrictiveAirspace> TblRestrictiveAirspaces { get; set; }

    public virtual DbSet<TblRunway> TblRunways { get; set; }

    public virtual DbSet<TblSid> TblSids { get; set; }

    public virtual DbSet<TblStar> TblStars { get; set; }

    public virtual DbSet<TblTerminalNdbnavaid> TblTerminalNdbnavaids { get; set; }

    public virtual DbSet<TblTerminalWaypoint> TblTerminalWaypoints { get; set; }

    public virtual DbSet<TblVhfnavaid> TblVhfnavaids { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseSqlite($"Data Source={_dbFilePath}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TblAirport>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("tbl_airports");

            entity.Property(e => e.AirportIdentifier)
                .HasColumnType("TEXT(4)")
                .HasColumnName("airport_identifier");
            entity.Property(e => e.AirportIdentifier3letter)
                .HasColumnType("TEXT(3)")
                .HasColumnName("airport_identifier_3letter");
            entity.Property(e => e.AirportName)
                .HasColumnType("TEXT(3)")
                .HasColumnName("airport_name");
            entity.Property(e => e.AirportRefLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("airport_ref_latitude");
            entity.Property(e => e.AirportRefLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("airport_ref_longitude");
            entity.Property(e => e.AreaCode)
                .HasColumnType("TEXT(3)")
                .HasColumnName("area_code");
            entity.Property(e => e.Elevation)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("elevation");
            entity.Property(e => e.IataAtaDesignator)
                .HasColumnType("TEXT(3)")
                .HasColumnName("iata_ata_designator");
            entity.Property(e => e.IcaoCode)
                .HasColumnType("TEXT(2)")
                .HasColumnName("icao_code");
            entity.Property(e => e.Id)
                .HasColumnType("TEXT(15)")
                .HasColumnName("id");
            entity.Property(e => e.IfrCapability)
                .HasColumnType("TEXT(1)")
                .HasColumnName("ifr_capability");
            entity.Property(e => e.LongestRunwaySurfaceCode)
                .HasColumnType("TEXT(1)")
                .HasColumnName("longest_runway_surface_code");
            entity.Property(e => e.SpeedLimit)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("speed_limit");
            entity.Property(e => e.SpeedLimitAltitude)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("speed_limit_altitude");
            entity.Property(e => e.TransitionAltitude)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("transition_altitude");
            entity.Property(e => e.TransitionLevel)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("transition_level");
        });

        modelBuilder.Entity<TblAirportCommunication>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("tbl_airport_communication");

            entity.Property(e => e.AirportIdentifier)
                .HasColumnType("TEXT(4)")
                .HasColumnName("airport_identifier");
            entity.Property(e => e.AreaCode)
                .HasColumnType("TEXT(3)")
                .HasColumnName("area_code");
            entity.Property(e => e.Callsign)
                .HasColumnType("TEXT(25)")
                .HasColumnName("callsign");
            entity.Property(e => e.CommunicationFrequency)
                .HasColumnType("DOUBLE(7)")
                .HasColumnName("communication_frequency");
            entity.Property(e => e.CommunicationType)
                .HasColumnType("TEXT(3)")
                .HasColumnName("communication_type");
            entity.Property(e => e.FrequencyUnits)
                .HasColumnType("TEXT(1)")
                .HasColumnName("frequency_units");
            entity.Property(e => e.IcaoCode)
                .HasColumnType("TEXT(2)")
                .HasColumnName("icao_code");
            entity.Property(e => e.Latitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("latitude");
            entity.Property(e => e.Longitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("longitude");
            entity.Property(e => e.ServiceIndicator)
                .HasColumnType("TEXT(3)")
                .HasColumnName("service_indicator");
        });

        modelBuilder.Entity<TblAirportMsa>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("tbl_airport_msa");

            entity.Property(e => e.AirportIdentifier)
                .HasColumnType("TEXT(4)")
                .HasColumnName("airport_identifier");
            entity.Property(e => e.AreaCode)
                .HasColumnType("TEXT(3)")
                .HasColumnName("area_code");
            entity.Property(e => e.IcaoCode)
                .HasColumnType("TEXT(2)")
                .HasColumnName("icao_code");
            entity.Property(e => e.MagneticTrueIndicator)
                .HasColumnType("TEXT(1)")
                .HasColumnName("magnetic_true_indicator");
            entity.Property(e => e.MsaCenter)
                .HasColumnType("TEXT(5)")
                .HasColumnName("msa_center");
            entity.Property(e => e.MsaCenterLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("msa_center_latitude");
            entity.Property(e => e.MsaCenterLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("msa_center_longitude");
            entity.Property(e => e.MultipleCode)
                .HasColumnType("TEXT(1)")
                .HasColumnName("multiple_code");
            entity.Property(e => e.RadiusLimit)
                .HasColumnType("INTEGER(2)")
                .HasColumnName("radius_limit");
            entity.Property(e => e.SectorAltitude1)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("sector_altitude_1");
            entity.Property(e => e.SectorAltitude2)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("sector_altitude_2");
            entity.Property(e => e.SectorAltitude3)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("sector_altitude_3");
            entity.Property(e => e.SectorAltitude4)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("sector_altitude_4");
            entity.Property(e => e.SectorAltitude5)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("sector_altitude_5");
            entity.Property(e => e.SectorBearing1)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("sector_bearing_1");
            entity.Property(e => e.SectorBearing2)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("sector_bearing_2");
            entity.Property(e => e.SectorBearing3)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("sector_bearing_3");
            entity.Property(e => e.SectorBearing4)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("sector_bearing_4");
            entity.Property(e => e.SectorBearing5)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("sector_bearing_5");
        });

        modelBuilder.Entity<TblControlledAirspace>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("tbl_controlled_airspace");

            entity.Property(e => e.AirspaceCenter)
                .HasColumnType("TEXT(5)")
                .HasColumnName("airspace_center");
            entity.Property(e => e.AirspaceClassification)
                .HasColumnType("TEXT(1)")
                .HasColumnName("airspace_classification");
            entity.Property(e => e.AirspaceType)
                .HasColumnType("TEXT(1)")
                .HasColumnName("airspace_type");
            entity.Property(e => e.ArcBearing)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("arc_bearing");
            entity.Property(e => e.ArcDistance)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("arc_distance");
            entity.Property(e => e.ArcOriginLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("arc_origin_latitude");
            entity.Property(e => e.ArcOriginLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("arc_origin_longitude");
            entity.Property(e => e.AreaCode)
                .HasColumnType("TEXT(3)")
                .HasColumnName("area_code");
            entity.Property(e => e.BoundaryVia)
                .HasColumnType("TEXT(2)")
                .HasColumnName("boundary_via");
            entity.Property(e => e.ControlledAirspaceName)
                .HasColumnType("TEXT(30)")
                .HasColumnName("controlled_airspace_name");
            entity.Property(e => e.Flightlevel)
                .HasColumnType("TEXT(1)")
                .HasColumnName("flightlevel");
            entity.Property(e => e.IcaoCode)
                .HasColumnType("TEXT(2)")
                .HasColumnName("icao_code");
            entity.Property(e => e.Latitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("latitude");
            entity.Property(e => e.Longitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("longitude");
            entity.Property(e => e.LowerLimit)
                .HasColumnType("TEXT(5)")
                .HasColumnName("lower_limit");
            entity.Property(e => e.MultipleCode)
                .HasColumnType("TEXT(1)")
                .HasColumnName("multiple_code");
            entity.Property(e => e.Seqno)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("seqno");
            entity.Property(e => e.TimeCode)
                .HasColumnType("TEXT(1)")
                .HasColumnName("time_code");
            entity.Property(e => e.UnitIndicatorLowerLimit)
                .HasColumnType("TEXT(1)")
                .HasColumnName("unit_indicator_lower_limit");
            entity.Property(e => e.UnitIndicatorUpperLimit)
                .HasColumnType("TEXT(1)")
                .HasColumnName("unit_indicator_upper_limit");
            entity.Property(e => e.UpperLimit)
                .HasColumnType("TEXT(5)")
                .HasColumnName("upper_limit");
        });

        modelBuilder.Entity<TblCruisingTable>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("tbl_cruising_tables");

            entity.Property(e => e.CourseFrom)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("course_from");
            entity.Property(e => e.CourseTo)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("course_to");
            entity.Property(e => e.CruiseLevelFrom1)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("cruise_level_from1");
            entity.Property(e => e.CruiseLevelFrom2)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("cruise_level_from2");
            entity.Property(e => e.CruiseLevelFrom3)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("cruise_level_from3");
            entity.Property(e => e.CruiseLevelFrom4)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("cruise_level_from4");
            entity.Property(e => e.CruiseLevelTo1)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("cruise_level_to1");
            entity.Property(e => e.CruiseLevelTo2)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("cruise_level_to2");
            entity.Property(e => e.CruiseLevelTo3)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("cruise_level_to3");
            entity.Property(e => e.CruiseLevelTo4)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("cruise_level_to4");
            entity.Property(e => e.CruiseTableIdentifier)
                .HasColumnType("TEXT(2)")
                .HasColumnName("cruise_table_identifier");
            entity.Property(e => e.MagTrue)
                .HasColumnType("TEXT(1)")
                .HasColumnName("mag_true");
            entity.Property(e => e.Seqno)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("seqno");
            entity.Property(e => e.VerticalSeparation1)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("vertical_separation1");
            entity.Property(e => e.VerticalSeparation2)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("vertical_separation2");
            entity.Property(e => e.VerticalSeparation3)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("vertical_separation3");
            entity.Property(e => e.VerticalSeparation4)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("vertical_separation4");
        });

        modelBuilder.Entity<TblEnrouteAirway>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("tbl_enroute_airways");

            entity.Property(e => e.AreaCode)
                .HasColumnType("TEXT(3)")
                .HasColumnName("area_code");
            entity.Property(e => e.CrusingTableIdentifier)
                .HasColumnType("TEXT(2)")
                .HasColumnName("crusing_table_identifier");
            entity.Property(e => e.DirectionRestriction)
                .HasColumnType("TEXT(1)")
                .HasColumnName("direction_restriction");
            entity.Property(e => e.Flightlevel)
                .HasColumnType("TEXT(1)")
                .HasColumnName("flightlevel");
            entity.Property(e => e.IcaoCode)
                .HasColumnType("TEXT(2)")
                .HasColumnName("icao_code");
            entity.Property(e => e.Id)
                .HasColumnType("TEXT(15)")
                .HasColumnName("id");
            entity.Property(e => e.InboundCourse)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("inbound_course");
            entity.Property(e => e.InboundDistance)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("inbound_distance");
            entity.Property(e => e.MaximumAltitude)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("maximum_altitude");
            entity.Property(e => e.MinimumAltitude1)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("minimum_altitude1");
            entity.Property(e => e.MinimumAltitude2)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("minimum_altitude2");
            entity.Property(e => e.OutboundCourse)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("outbound_course");
            entity.Property(e => e.RouteIdentifier)
                .HasColumnType("TEXT(6)")
                .HasColumnName("route_identifier");
            entity.Property(e => e.RouteType)
                .HasColumnType("TEXT(1)")
                .HasColumnName("route_type");
            entity.Property(e => e.Seqno)
                .HasColumnType("INTEGER(4)")
                .HasColumnName("seqno");
            entity.Property(e => e.WaypointDescriptionCode)
                .HasColumnType("TEXT(4)")
                .HasColumnName("waypoint_description_code");
            entity.Property(e => e.WaypointIdentifier)
                .HasColumnType("TEXT(5)")
                .HasColumnName("waypoint_identifier");
            entity.Property(e => e.WaypointLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("waypoint_latitude");
            entity.Property(e => e.WaypointLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("waypoint_longitude");
        });

        modelBuilder.Entity<TblEnrouteAirwayRestriction>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("tbl_enroute_airway_restriction");

            entity.Property(e => e.AreaCode)
                .HasColumnType("TEXT(3)")
                .HasColumnName("area_code");
            entity.Property(e => e.BlockIndicator1)
                .HasColumnType("TEXT(1)")
                .HasColumnName("block_indicator1");
            entity.Property(e => e.BlockIndicator2)
                .HasColumnType("TEXT(1)")
                .HasColumnName("block_indicator2");
            entity.Property(e => e.BlockIndicator3)
                .HasColumnType("TEXT(1)")
                .HasColumnName("block_indicator3");
            entity.Property(e => e.BlockIndicator4)
                .HasColumnType("TEXT(1)")
                .HasColumnName("block_indicator4");
            entity.Property(e => e.BlockIndicator5)
                .HasColumnType("TEXT(1)")
                .HasColumnName("block_indicator5");
            entity.Property(e => e.BlockIndicator6)
                .HasColumnType("TEXT(1)")
                .HasColumnName("block_indicator6");
            entity.Property(e => e.BlockIndicator7)
                .HasColumnType("TEXT(1)")
                .HasColumnName("block_indicator7");
            entity.Property(e => e.EndDate)
                .HasColumnType("TEXT(7)")
                .HasColumnName("end_date");
            entity.Property(e => e.EndWaypointIdentifier)
                .HasColumnType("TEXT(5)")
                .HasColumnName("end_waypoint_identifier");
            entity.Property(e => e.EndWaypointLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("end_waypoint_latitude");
            entity.Property(e => e.EndWaypointLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("end_waypoint_longitude");
            entity.Property(e => e.RestrictionAltitude1)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("restriction_altitude1");
            entity.Property(e => e.RestrictionAltitude2)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("restriction_altitude2");
            entity.Property(e => e.RestrictionAltitude3)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("restriction_altitude3");
            entity.Property(e => e.RestrictionAltitude4)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("restriction_altitude4");
            entity.Property(e => e.RestrictionAltitude5)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("restriction_altitude5");
            entity.Property(e => e.RestrictionAltitude6)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("restriction_altitude6");
            entity.Property(e => e.RestrictionAltitude7)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("restriction_altitude7");
            entity.Property(e => e.RestrictionIdentifier)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("restriction_identifier");
            entity.Property(e => e.RestrictionNotes)
                .HasColumnType("TEXT(69)")
                .HasColumnName("restriction_notes");
            entity.Property(e => e.RestrictionType)
                .HasColumnType("TEXT(2)")
                .HasColumnName("restriction_type");
            entity.Property(e => e.RouteIdentifier)
                .HasColumnType("TEXT(5)")
                .HasColumnName("route_identifier");
            entity.Property(e => e.StartDate)
                .HasColumnType("TEXT(7)")
                .HasColumnName("start_date");
            entity.Property(e => e.StartWaypointIdentifier)
                .HasColumnType("TEXT(5)")
                .HasColumnName("start_waypoint_identifier");
            entity.Property(e => e.StartWaypointLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("start_waypoint_latitude");
            entity.Property(e => e.StartWaypointLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("start_waypoint_longitude");
            entity.Property(e => e.UnitsOfAltitude)
                .HasColumnType("TEXT(1)")
                .HasColumnName("units_of_altitude");
        });

        modelBuilder.Entity<TblEnrouteCommunication>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("tbl_enroute_communication");

            entity.Property(e => e.AreaCode)
                .HasColumnType("TEXT(3)")
                .HasColumnName("area_code");
            entity.Property(e => e.Callsign)
                .HasColumnType("TEXT(30)")
                .HasColumnName("callsign");
            entity.Property(e => e.CommunicationFrequency)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("communication_frequency");
            entity.Property(e => e.CommunicationType)
                .HasColumnType("TEXT(3)")
                .HasColumnName("communication_type");
            entity.Property(e => e.FirRdoIdent)
                .HasColumnType("TEXT(4)")
                .HasColumnName("fir_rdo_ident");
            entity.Property(e => e.FirUirIndicator)
                .HasColumnType("TEXT(1)")
                .HasColumnName("fir_uir_indicator");
            entity.Property(e => e.FrequencyUnits)
                .HasColumnType("TEXT(1)")
                .HasColumnName("frequency_units");
            entity.Property(e => e.Latitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("latitude");
            entity.Property(e => e.Longitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("longitude");
            entity.Property(e => e.RemoteName)
                .HasColumnType("TEXT(25)")
                .HasColumnName("remote_name");
            entity.Property(e => e.ServiceIndicator)
                .HasColumnType("TEXT(3)")
                .HasColumnName("service_indicator");
        });

        modelBuilder.Entity<TblEnrouteNdbnavaid>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("tbl_enroute_ndbnavaids");

            entity.Property(e => e.AreaCode)
                .HasColumnType("TEXT(3)")
                .HasColumnName("area_code");
            entity.Property(e => e.IcaoCode)
                .HasColumnType("TEXT(2)")
                .HasColumnName("icao_code");
            entity.Property(e => e.Id)
                .HasColumnType("TEXT(15)")
                .HasColumnName("id");
            entity.Property(e => e.NavaidClass)
                .HasColumnType("TEXT(5)")
                .HasColumnName("navaid_class");
            entity.Property(e => e.NdbFrequency)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("ndb_frequency");
            entity.Property(e => e.NdbIdentifier)
                .HasColumnType("TEXT(4)")
                .HasColumnName("ndb_identifier");
            entity.Property(e => e.NdbLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("ndb_latitude");
            entity.Property(e => e.NdbLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("ndb_longitude");
            entity.Property(e => e.NdbName)
                .HasColumnType("TEXT(30)")
                .HasColumnName("ndb_name");
            entity.Property(e => e.Range)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("range");
        });

        modelBuilder.Entity<TblEnrouteWaypoint>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("tbl_enroute_waypoints");

            entity.Property(e => e.AreaCode)
                .HasColumnType("TEXT(3)")
                .HasColumnName("area_code");
            entity.Property(e => e.IcaoCode)
                .HasColumnType("TEXT(2)")
                .HasColumnName("icao_code");
            entity.Property(e => e.Id)
                .HasColumnType("TEXT(15)")
                .HasColumnName("id");
            entity.Property(e => e.WaypointIdentifier)
                .HasColumnType("TEXT(5)")
                .HasColumnName("waypoint_identifier");
            entity.Property(e => e.WaypointLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("waypoint_latitude");
            entity.Property(e => e.WaypointLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("waypoint_longitude");
            entity.Property(e => e.WaypointName)
                .HasColumnType("TEXT(25)")
                .HasColumnName("waypoint_name");
            entity.Property(e => e.WaypointType)
                .HasColumnType("TEXT(3)")
                .HasColumnName("waypoint_type");
            entity.Property(e => e.WaypointUsage)
                .HasColumnType("TEXT(2)")
                .HasColumnName("waypoint_usage");
        });

        modelBuilder.Entity<TblFirUir>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("tbl_fir_uir");

            entity.Property(e => e.AdjacentFirIdentifier)
                .HasColumnType("TEXT(4)")
                .HasColumnName("adjacent_fir_identifier");
            entity.Property(e => e.AdjacentUirIdentifier)
                .HasColumnType("TEXT(4)")
                .HasColumnName("adjacent_uir_identifier");
            entity.Property(e => e.ArcBearing)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("arc_bearing");
            entity.Property(e => e.ArcDistance)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("arc_distance");
            entity.Property(e => e.ArcOriginLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("arc_origin_latitude");
            entity.Property(e => e.ArcOriginLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("arc_origin_longitude");
            entity.Property(e => e.AreaCode)
                .HasColumnType("TEXT(3)")
                .HasColumnName("area_code");
            entity.Property(e => e.BoundaryVia)
                .HasColumnType("TEXT(2)")
                .HasColumnName("boundary_via");
            entity.Property(e => e.CruiseTableIdentifier)
                .HasColumnType("TEXT(2)")
                .HasColumnName("cruise_table_identifier");
            entity.Property(e => e.FirUirAddress)
                .HasColumnType("TEXT(4)")
                .HasColumnName("fir_uir_address");
            entity.Property(e => e.FirUirIdentifier)
                .HasColumnType("TEXT(4)")
                .HasColumnName("fir_uir_identifier");
            entity.Property(e => e.FirUirIndicator)
                .HasColumnType("TEXT(1)")
                .HasColumnName("fir_uir_indicator");
            entity.Property(e => e.FirUirLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("fir_uir_latitude");
            entity.Property(e => e.FirUirLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("fir_uir_longitude");
            entity.Property(e => e.FirUirName)
                .HasColumnType("TEXT(25)")
                .HasColumnName("fir_uir_name");
            entity.Property(e => e.FirUpperLimit)
                .HasColumnType("TEXT(5)")
                .HasColumnName("fir_upper_limit");
            entity.Property(e => e.ReportingUnitsAltitude)
                .HasColumnType("INTEGER(1)")
                .HasColumnName("reporting_units_altitude");
            entity.Property(e => e.ReportingUnitsSpeed)
                .HasColumnType("INTEGER(1)")
                .HasColumnName("reporting_units_speed");
            entity.Property(e => e.Seqno)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("seqno");
            entity.Property(e => e.UirLowerLimit)
                .HasColumnType("TEXT(5)")
                .HasColumnName("uir_lower_limit");
            entity.Property(e => e.UirUpperLimit)
                .HasColumnType("TEXT(5)")
                .HasColumnName("uir_upper_limit");
        });

        modelBuilder.Entity<TblGate>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("tbl_gate");

            entity.Property(e => e.AirportIdentifier)
                .HasColumnType("TEXT(4)")
                .HasColumnName("airport_identifier");
            entity.Property(e => e.AreaCode)
                .HasColumnType("TEXT(3)")
                .HasColumnName("area_code");
            entity.Property(e => e.GateIdentifier)
                .HasColumnType("TEXT(5)")
                .HasColumnName("gate_identifier");
            entity.Property(e => e.GateLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("gate_latitude");
            entity.Property(e => e.GateLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("gate_longitude");
            entity.Property(e => e.IcaoCode)
                .HasColumnType("TEXT(2)")
                .HasColumnName("icao_code");
            entity.Property(e => e.Name)
                .HasColumnType("TEXT(25)")
                .HasColumnName("name");
        });

        modelBuilder.Entity<TblGl>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("tbl_gls");

            entity.Property(e => e.AirportIdentifier)
                .HasColumnType("TEXT(4)")
                .HasColumnName("airport_identifier");
            entity.Property(e => e.AreaCode)
                .HasColumnType("TEXT(3)")
                .HasColumnName("area_code");
            entity.Property(e => e.GlsApproachBearing)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("gls_approach_bearing");
            entity.Property(e => e.GlsApproachSlope)
                .HasColumnType("DOUBLE(4)")
                .HasColumnName("gls_approach_slope");
            entity.Property(e => e.GlsCategory)
                .HasColumnType("TEXT(1)")
                .HasColumnName("gls_category");
            entity.Property(e => e.GlsChannel)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("gls_channel");
            entity.Property(e => e.GlsRefPathIdentifier)
                .HasColumnType("TEXT(4)")
                .HasColumnName("gls_ref_path_identifier");
            entity.Property(e => e.GlsStationIdent)
                .HasColumnType("TEXT(4)")
                .HasColumnName("gls_station_ident");
            entity.Property(e => e.IcaoCode)
                .HasColumnType("TEXT(2)")
                .HasColumnName("icao_code");
            entity.Property(e => e.Id)
                .HasColumnType("TEXT(15)")
                .HasColumnName("id");
            entity.Property(e => e.MagenticVariation)
                .HasColumnType("DOUBLE(6)")
                .HasColumnName("magentic_variation");
            entity.Property(e => e.RunwayIdentifier)
                .HasColumnType("TEXT(5)")
                .HasColumnName("runway_identifier");
            entity.Property(e => e.StationElevation)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("station_elevation");
            entity.Property(e => e.StationLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("station_latitude");
            entity.Property(e => e.StationLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("station_longitude");
            entity.Property(e => e.StationType)
                .HasColumnType("TEXT(3)")
                .HasColumnName("station_type");
        });

        modelBuilder.Entity<TblGridMora>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("tbl_grid_mora");

            entity.Property(e => e.Mora01)
                .HasColumnType("TEXT(3)")
                .HasColumnName("mora01");
            entity.Property(e => e.Mora02)
                .HasColumnType("TEXT(3)")
                .HasColumnName("mora02");
            entity.Property(e => e.Mora03)
                .HasColumnType("TEXT(3)")
                .HasColumnName("mora03");
            entity.Property(e => e.Mora04)
                .HasColumnType("TEXT(3)")
                .HasColumnName("mora04");
            entity.Property(e => e.Mora05)
                .HasColumnType("TEXT(3)")
                .HasColumnName("mora05");
            entity.Property(e => e.Mora06)
                .HasColumnType("TEXT(3)")
                .HasColumnName("mora06");
            entity.Property(e => e.Mora07)
                .HasColumnType("TEXT(3)")
                .HasColumnName("mora07");
            entity.Property(e => e.Mora08)
                .HasColumnType("TEXT(3)")
                .HasColumnName("mora08");
            entity.Property(e => e.Mora09)
                .HasColumnType("TEXT(3)")
                .HasColumnName("mora09");
            entity.Property(e => e.Mora10)
                .HasColumnType("TEXT(3)")
                .HasColumnName("mora10");
            entity.Property(e => e.Mora11)
                .HasColumnType("TEXT(3)")
                .HasColumnName("mora11");
            entity.Property(e => e.Mora12)
                .HasColumnType("TEXT(3)")
                .HasColumnName("mora12");
            entity.Property(e => e.Mora13)
                .HasColumnType("TEXT(3)")
                .HasColumnName("mora13");
            entity.Property(e => e.Mora14)
                .HasColumnType("TEXT(3)")
                .HasColumnName("mora14");
            entity.Property(e => e.Mora15)
                .HasColumnType("TEXT(3)")
                .HasColumnName("mora15");
            entity.Property(e => e.Mora16)
                .HasColumnType("TEXT(3)")
                .HasColumnName("mora16");
            entity.Property(e => e.Mora17)
                .HasColumnType("TEXT(3)")
                .HasColumnName("mora17");
            entity.Property(e => e.Mora18)
                .HasColumnType("TEXT(3)")
                .HasColumnName("mora18");
            entity.Property(e => e.Mora19)
                .HasColumnType("TEXT(3)")
                .HasColumnName("mora19");
            entity.Property(e => e.Mora20)
                .HasColumnType("TEXT(3)")
                .HasColumnName("mora20");
            entity.Property(e => e.Mora21)
                .HasColumnType("TEXT(3)")
                .HasColumnName("mora21");
            entity.Property(e => e.Mora22)
                .HasColumnType("TEXT(3)")
                .HasColumnName("mora22");
            entity.Property(e => e.Mora23)
                .HasColumnType("TEXT(3)")
                .HasColumnName("mora23");
            entity.Property(e => e.Mora24)
                .HasColumnType("TEXT(3)")
                .HasColumnName("mora24");
            entity.Property(e => e.Mora25)
                .HasColumnType("TEXT(3)")
                .HasColumnName("mora25");
            entity.Property(e => e.Mora26)
                .HasColumnType("TEXT(3)")
                .HasColumnName("mora26");
            entity.Property(e => e.Mora27)
                .HasColumnType("TEXT(3)")
                .HasColumnName("mora27");
            entity.Property(e => e.Mora28)
                .HasColumnType("TEXT(3)")
                .HasColumnName("mora28");
            entity.Property(e => e.Mora29)
                .HasColumnType("TEXT(3)")
                .HasColumnName("mora29");
            entity.Property(e => e.Mora30)
                .HasColumnType("TEXT(3)")
                .HasColumnName("mora30");
            entity.Property(e => e.StartingLatitude)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("starting_latitude");
            entity.Property(e => e.StartingLongitude)
                .HasColumnType("INTEGER(4)")
                .HasColumnName("starting_longitude");
        });

        modelBuilder.Entity<TblHeader>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("tbl_header");

            entity.Property(e => e.Arincversion)
                .HasColumnType("TEXT(6)")
                .HasColumnName("arincversion");
            entity.Property(e => e.CurrentAirac)
                .HasColumnType("TEXT(4)")
                .HasColumnName("current_airac");
            entity.Property(e => e.EffectiveFromto)
                .HasColumnType("TEXT(10)")
                .HasColumnName("effective_fromto");
            entity.Property(e => e.ParsedAt)
                .HasColumnType("TEXT(22)")
                .HasColumnName("parsed_at");
            entity.Property(e => e.PreviousAirac)
                .HasColumnType("TEXT(4)")
                .HasColumnName("previous_airac");
            entity.Property(e => e.PreviousFromto)
                .HasColumnType("TEXT(10)")
                .HasColumnName("previous_fromto");
            entity.Property(e => e.RecordSet)
                .HasColumnType("TEXT(8)")
                .HasColumnName("record_set");
            entity.Property(e => e.Revision)
                .HasColumnType("TEXT(3)")
                .HasColumnName("revision");
            entity.Property(e => e.Version)
                .HasColumnType("TEXT(5)")
                .HasColumnName("version");
        });

        modelBuilder.Entity<TblHolding>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("tbl_holdings");

            entity.Property(e => e.AreaCode)
                .HasColumnType("TEXT(3)")
                .HasColumnName("area_code");
            entity.Property(e => e.DuplicateIdentifier)
                .HasColumnType("INTEGER(2)")
                .HasColumnName("duplicate_identifier");
            entity.Property(e => e.HoldingName)
                .HasColumnType("TEXT(25)")
                .HasColumnName("holding_name");
            entity.Property(e => e.HoldingSpeed)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("holding_speed");
            entity.Property(e => e.IcaoCode)
                .HasColumnType("TEXT(2)")
                .HasColumnName("icao_code");
            entity.Property(e => e.InboundHoldingCourse)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("inbound_holding_course");
            entity.Property(e => e.LegLength)
                .HasColumnType("DOUBLE(4)")
                .HasColumnName("leg_length");
            entity.Property(e => e.LegTime)
                .HasColumnType("DOUBLE(3)")
                .HasColumnName("leg_time");
            entity.Property(e => e.MaximumAltitude)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("maximum_altitude");
            entity.Property(e => e.MinimumAltitude)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("minimum_altitude");
            entity.Property(e => e.RegionCode)
                .HasColumnType("TEXT(4)")
                .HasColumnName("region_code");
            entity.Property(e => e.TurnDirection)
                .HasColumnType("TEXT(1)")
                .HasColumnName("turn_direction");
            entity.Property(e => e.WaypointIdentifier)
                .HasColumnType("TEXT(5)")
                .HasColumnName("waypoint_identifier");
            entity.Property(e => e.WaypointLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("waypoint_latitude");
            entity.Property(e => e.WaypointLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("waypoint_longitude");
        });

        modelBuilder.Entity<TblIap>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("tbl_iaps");

            entity.Property(e => e.AircraftCategory)
                .HasColumnType("TEXT(1)")
                .HasColumnName("aircraft_category");
            entity.Property(e => e.AirportIdentifier)
                .HasColumnType("TEXT(4)")
                .HasColumnName("airport_identifier");
            entity.Property(e => e.Altitude1)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("altitude1");
            entity.Property(e => e.Altitude2)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("altitude2");
            entity.Property(e => e.AltitudeDescription)
                .HasColumnType("TEXT(1)")
                .HasColumnName("altitude_description");
            entity.Property(e => e.ArcRadius)
                .HasColumnType("DOUBLE(7)")
                .HasColumnName("arc_radius");
            entity.Property(e => e.AreaCode)
                .HasColumnType("TEXT(3)")
                .HasColumnName("area_code");
            entity.Property(e => e.CenterId)
                .HasColumnType("TEXT(15)")
                .HasColumnName("center_id");
            entity.Property(e => e.CenterWaypoint)
                .HasColumnType("TEXT(5)")
                .HasColumnName("center_waypoint");
            entity.Property(e => e.CenterWaypointLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("center_waypoint_latitude");
            entity.Property(e => e.CenterWaypointLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("center_waypoint_longitude");
            entity.Property(e => e.DistanceTime)
                .HasColumnType("TEXT(1)")
                .HasColumnName("distance_time");
            entity.Property(e => e.Id)
                .HasColumnType("TEXT(15)")
                .HasColumnName("id");
            entity.Property(e => e.MagneticCourse)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("magnetic_course");
            entity.Property(e => e.PathTermination)
                .HasColumnType("TEXT(2)")
                .HasColumnName("path_termination");
            entity.Property(e => e.ProcedureIdentifier)
                .HasColumnType("TEXT(6)")
                .HasColumnName("procedure_identifier");
            entity.Property(e => e.RecommandedId)
                .HasColumnType("TEXT(15)")
                .HasColumnName("recommanded_id");
            entity.Property(e => e.RecommandedNavaid)
                .HasColumnType("TEXT(4)")
                .HasColumnName("recommanded_navaid");
            entity.Property(e => e.RecommandedNavaidLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("recommanded_navaid_latitude");
            entity.Property(e => e.RecommandedNavaidLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("recommanded_navaid_longitude");
            entity.Property(e => e.Rho)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("rho");
            entity.Property(e => e.Rnp)
                .HasColumnType("DOUBLE(4)")
                .HasColumnName("rnp");
            entity.Property(e => e.RouteDistanceHoldingDistanceTime)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("route_distance_holding_distance_time");
            entity.Property(e => e.RouteType)
                .HasColumnType("TEXT(1)")
                .HasColumnName("route_type");
            entity.Property(e => e.Seqno)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("seqno");
            entity.Property(e => e.SpeedLimit)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("speed_limit");
            entity.Property(e => e.SpeedLimitDescription)
                .HasColumnType("TEXT(1)")
                .HasColumnName("speed_limit_description");
            entity.Property(e => e.Theta)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("theta");
            entity.Property(e => e.TransitionAltitude)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("transition_altitude");
            entity.Property(e => e.TransitionIdentifier)
                .HasColumnType("TEXT(5)")
                .HasColumnName("transition_identifier");
            entity.Property(e => e.TurnDirection)
                .HasColumnType("TEXT(1)")
                .HasColumnName("turn_direction");
            entity.Property(e => e.VerticalAngle)
                .HasColumnType("DOUBLE(4)")
                .HasColumnName("vertical_angle");
            entity.Property(e => e.WaypointDescriptionCode)
                .HasColumnType("TEXT(4)")
                .HasColumnName("waypoint_description_code");
            entity.Property(e => e.WaypointIcaoCode)
                .HasColumnType("TEXT(2)")
                .HasColumnName("waypoint_icao_code");
            entity.Property(e => e.WaypointIdentifier)
                .HasColumnType("TEXT(5)")
                .HasColumnName("waypoint_identifier");
            entity.Property(e => e.WaypointLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("waypoint_latitude");
            entity.Property(e => e.WaypointLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("waypoint_longitude");
        });

        modelBuilder.Entity<TblLocalizerMarker>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("tbl_localizer_marker");

            entity.Property(e => e.AirportIdentifier)
                .HasColumnType("TEXT(4)")
                .HasColumnName("airport_identifier");
            entity.Property(e => e.AreaCode)
                .HasColumnType("TEXT(3)")
                .HasColumnName("area_code");
            entity.Property(e => e.IcaoCode)
                .HasColumnType("TEXT(2)")
                .HasColumnName("icao_code");
            entity.Property(e => e.Id)
                .HasColumnType("TEXT(15)")
                .HasColumnName("id");
            entity.Property(e => e.LlzIdentifier)
                .HasColumnType("TEXT(4)")
                .HasColumnName("llz_identifier");
            entity.Property(e => e.MarkerIdentifier)
                .HasColumnType("TEXT(5)")
                .HasColumnName("marker_identifier");
            entity.Property(e => e.MarkerLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("marker_latitude");
            entity.Property(e => e.MarkerLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("marker_longitude");
            entity.Property(e => e.MarkerType)
                .HasColumnType("TEXT(3)")
                .HasColumnName("marker_type");
            entity.Property(e => e.RunwayIdentifier)
                .HasColumnType("TEXT(5)")
                .HasColumnName("runway_identifier");
        });

        modelBuilder.Entity<TblLocalizersGlideslope>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("tbl_localizers_glideslopes");

            entity.Property(e => e.AirportIdentifier)
                .HasColumnType("TEXT(4)")
                .HasColumnName("airport_identifier");
            entity.Property(e => e.AreaCode)
                .HasColumnType("TEXT(3)")
                .HasColumnName("area_code");
            entity.Property(e => e.GsAngle)
                .HasColumnType("DOUBLE(4)")
                .HasColumnName("gs_angle");
            entity.Property(e => e.GsElevation)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("gs_elevation");
            entity.Property(e => e.GsLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("gs_latitude");
            entity.Property(e => e.GsLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("gs_longitude");
            entity.Property(e => e.IcaoCode)
                .HasColumnType("TEXT(2)")
                .HasColumnName("icao_code");
            entity.Property(e => e.Id)
                .HasColumnType("TEXT(15)")
                .HasColumnName("id");
            entity.Property(e => e.IlsMlsGlsCategory)
                .HasColumnType("TEXT(1)")
                .HasColumnName("ils_mls_gls_category");
            entity.Property(e => e.LlzBearing)
                .HasColumnType("DOUBLE(6)")
                .HasColumnName("llz_bearing");
            entity.Property(e => e.LlzFrequency)
                .HasColumnType("DOUBLE(6)")
                .HasColumnName("llz_frequency");
            entity.Property(e => e.LlzIdentifier)
                .HasColumnType("TEXT(4)")
                .HasColumnName("llz_identifier");
            entity.Property(e => e.LlzLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("llz_latitude");
            entity.Property(e => e.LlzLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("llz_longitude");
            entity.Property(e => e.LlzWidth)
                .HasColumnType("DOUBLE(6)")
                .HasColumnName("llz_width");
            entity.Property(e => e.RunwayIdentifier)
                .HasColumnType("TEXT(3)")
                .HasColumnName("runway_identifier");
            entity.Property(e => e.StationDeclination)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("station_declination");
        });

        modelBuilder.Entity<TblPathpoint>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("tbl_pathpoints");

            entity.Property(e => e.AirportIdentifier)
                .HasColumnType("TEXT(4)")
                .HasColumnName("airport_identifier");
            entity.Property(e => e.ApproachProcedureIdent)
                .HasColumnType("TEXT(6)")
                .HasColumnName("approach_procedure_ident");
            entity.Property(e => e.ApproachTypeIdentifier)
                .HasColumnType("TEXT(10)")
                .HasColumnName("approach_type_identifier");
            entity.Property(e => e.AreaCode)
                .HasColumnType("TEXT(3)")
                .HasColumnName("area_code");
            entity.Property(e => e.CourseWidthAtThreshold)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("course_width_at_threshold");
            entity.Property(e => e.FlightpathAlignmentLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("flightpath_alignment_latitude");
            entity.Property(e => e.FlightpathAlignmentLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("flightpath_alignment_longitude");
            entity.Property(e => e.FpapEllipsoidHeight)
                .HasColumnType("DOUBLE(6)")
                .HasColumnName("fpap_ellipsoid_height");
            entity.Property(e => e.FpapOrthometricHeight)
                .HasColumnType("DOUBLE(6)")
                .HasColumnName("fpap_orthometric_height");
            entity.Property(e => e.GlidepathAngle)
                .HasColumnType("DOUBLE(4)")
                .HasColumnName("glidepath_angle");
            entity.Property(e => e.GnssChannelNumber)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("gnss_channel_number");
            entity.Property(e => e.Hal)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("hal");
            entity.Property(e => e.IcaoCode)
                .HasColumnType("TEXT(2)")
                .HasColumnName("icao_code");
            entity.Property(e => e.LandingThresholdLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("landing_threshold_latitude");
            entity.Property(e => e.LandingThresholdLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("landing_threshold_longitude");
            entity.Property(e => e.LengthOffset)
                .HasColumnType("INTEGER(4)")
                .HasColumnName("length_offset");
            entity.Property(e => e.LtpEllipsoidHeight)
                .HasColumnType("DOUBLE(6)")
                .HasColumnName("ltp_ellipsoid_height");
            entity.Property(e => e.LtpOrthometricHeight)
                .HasColumnType("DOUBLE(6)")
                .HasColumnName("ltp_orthometric_height");
            entity.Property(e => e.PathPointTch)
                .HasColumnType("INTEGER(6)")
                .HasColumnName("path_point_tch");
            entity.Property(e => e.ReferencePathIdentifier)
                .HasColumnType("TEXT(2)")
                .HasColumnName("reference_path_identifier");
            entity.Property(e => e.RunwayIdentifier)
                .HasColumnType("TEXT(5)")
                .HasColumnName("runway_identifier");
            entity.Property(e => e.SbasServiceProviderIdentifier)
                .HasColumnType("INTEGER(2)")
                .HasColumnName("sbas_service_provider_identifier");
            entity.Property(e => e.TchUnitsIndicator)
                .HasColumnType("TEXT(1)")
                .HasColumnName("tch_units_indicator");
            entity.Property(e => e.Val)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("val");
        });

        modelBuilder.Entity<TblRestrictiveAirspace>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("tbl_restrictive_airspace");

            entity.Property(e => e.ArcBearing)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("arc_bearing");
            entity.Property(e => e.ArcDistance)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("arc_distance");
            entity.Property(e => e.ArcOriginLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("arc_origin_latitude");
            entity.Property(e => e.ArcOriginLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("arc_origin_longitude");
            entity.Property(e => e.AreaCode)
                .HasColumnType("TEXT(3)")
                .HasColumnName("area_code");
            entity.Property(e => e.BoundaryVia)
                .HasColumnType("TEXT(2)")
                .HasColumnName("boundary_via");
            entity.Property(e => e.Flightlevel)
                .HasColumnType("TEXT(1)")
                .HasColumnName("flightlevel");
            entity.Property(e => e.IcaoCode)
                .HasColumnType("TEXT(2)")
                .HasColumnName("icao_code");
            entity.Property(e => e.Latitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("latitude");
            entity.Property(e => e.Longitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("longitude");
            entity.Property(e => e.LowerLimit)
                .HasColumnType("TEXT(5)")
                .HasColumnName("lower_limit");
            entity.Property(e => e.MultipleCode)
                .HasColumnType("TEXT(1)")
                .HasColumnName("multiple_code");
            entity.Property(e => e.RestrictiveAirspaceDesignation)
                .HasColumnType("TEXT(10)")
                .HasColumnName("restrictive_airspace_designation");
            entity.Property(e => e.RestrictiveAirspaceName)
                .HasColumnType("TEXT(30)")
                .HasColumnName("restrictive_airspace_name");
            entity.Property(e => e.RestrictiveType)
                .HasColumnType("TEXT(1)")
                .HasColumnName("restrictive_type");
            entity.Property(e => e.Seqno)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("seqno");
            entity.Property(e => e.UnitIndicatorLowerLimit)
                .HasColumnType("TEXT(1)")
                .HasColumnName("unit_indicator_lower_limit");
            entity.Property(e => e.UnitIndicatorUpperLimit)
                .HasColumnType("TEXT(1)")
                .HasColumnName("unit_indicator_upper_limit");
            entity.Property(e => e.UpperLimit)
                .HasColumnType("TEXT(5)")
                .HasColumnName("upper_limit");
        });

        modelBuilder.Entity<TblRunway>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("tbl_runways");

            entity.Property(e => e.AirportIdentifier)
                .HasColumnType("TEXT(4)")
                .HasColumnName("airport_identifier");
            entity.Property(e => e.AreaCode)
                .HasColumnType("TEXT(3)")
                .HasColumnName("area_code");
            entity.Property(e => e.DisplacedThresholdDistance)
                .HasColumnType("INTEGER(4)")
                .HasColumnName("displaced_threshold_distance");
            entity.Property(e => e.IcaoCode)
                .HasColumnType("TEXT(2)")
                .HasColumnName("icao_code");
            entity.Property(e => e.Id)
                .HasColumnType("TEXT(15)")
                .HasColumnName("id");
            entity.Property(e => e.LandingThresholdElevation)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("landing_threshold_elevation");
            entity.Property(e => e.LlzIdentifier)
                .HasColumnType("TEXT(4)")
                .HasColumnName("llz_identifier");
            entity.Property(e => e.LlzMlsGlsCategory)
                .HasColumnType("TEXT(1)")
                .HasColumnName("llz_mls_gls_category");
            entity.Property(e => e.RunwayGradient)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("runway_gradient");
            entity.Property(e => e.RunwayIdentifier)
                .HasColumnType("TEXT(3)")
                .HasColumnName("runway_identifier");
            entity.Property(e => e.RunwayLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("runway_latitude");
            entity.Property(e => e.RunwayLength)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("runway_length");
            entity.Property(e => e.RunwayLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("runway_longitude");
            entity.Property(e => e.RunwayMagneticBearing)
                .HasColumnType("DOUBLE(6)")
                .HasColumnName("runway_magnetic_bearing");
            entity.Property(e => e.RunwayTrueBearing)
                .HasColumnType("DOUBLE(7)")
                .HasColumnName("runway_true_bearing");
            entity.Property(e => e.RunwayWidth)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("runway_width");
            entity.Property(e => e.SurfaceCode)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("surface_code");
            entity.Property(e => e.ThresholdCrossingHeight)
                .HasColumnType("INTEGER(2)")
                .HasColumnName("threshold_crossing_height");
        });

        modelBuilder.Entity<TblSid>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("tbl_sids");

            entity.Property(e => e.AircraftCategory)
                .HasColumnType("TEXT(1)")
                .HasColumnName("aircraft_category");
            entity.Property(e => e.AirportIdentifier)
                .HasColumnType("TEXT(4)")
                .HasColumnName("airport_identifier");
            entity.Property(e => e.Altitude1)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("altitude1");
            entity.Property(e => e.Altitude2)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("altitude2");
            entity.Property(e => e.AltitudeDescription)
                .HasColumnType("TEXT(1)")
                .HasColumnName("altitude_description");
            entity.Property(e => e.ArcRadius)
                .HasColumnType("DOUBLE(7)")
                .HasColumnName("arc_radius");
            entity.Property(e => e.AreaCode)
                .HasColumnType("TEXT(3)")
                .HasColumnName("area_code");
            entity.Property(e => e.CenterId)
                .HasColumnType("TEXT(15)")
                .HasColumnName("center_id");
            entity.Property(e => e.CenterWaypoint)
                .HasColumnType("TEXT(5)")
                .HasColumnName("center_waypoint");
            entity.Property(e => e.CenterWaypointLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("center_waypoint_latitude");
            entity.Property(e => e.CenterWaypointLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("center_waypoint_longitude");
            entity.Property(e => e.DistanceTime)
                .HasColumnType("TEXT(1)")
                .HasColumnName("distance_time");
            entity.Property(e => e.Id)
                .HasColumnType("TEXT(15)")
                .HasColumnName("id");
            entity.Property(e => e.MagneticCourse)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("magnetic_course");
            entity.Property(e => e.PathTermination)
                .HasColumnType("TEXT(2)")
                .HasColumnName("path_termination");
            entity.Property(e => e.ProcedureIdentifier)
                .HasColumnType("TEXT(6)")
                .HasColumnName("procedure_identifier");
            entity.Property(e => e.RecommandedId)
                .HasColumnType("TEXT(15)")
                .HasColumnName("recommanded_id");
            entity.Property(e => e.RecommandedNavaid)
                .HasColumnType("TEXT(4)")
                .HasColumnName("recommanded_navaid");
            entity.Property(e => e.RecommandedNavaidLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("recommanded_navaid_latitude");
            entity.Property(e => e.RecommandedNavaidLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("recommanded_navaid_longitude");
            entity.Property(e => e.Rho)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("rho");
            entity.Property(e => e.Rnp)
                .HasColumnType("DOUBLE(4)")
                .HasColumnName("rnp");
            entity.Property(e => e.RouteDistanceHoldingDistanceTime)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("route_distance_holding_distance_time");
            entity.Property(e => e.RouteType)
                .HasColumnType("TEXT(1)")
                .HasColumnName("route_type");
            entity.Property(e => e.Seqno)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("seqno");
            entity.Property(e => e.SpeedLimit)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("speed_limit");
            entity.Property(e => e.SpeedLimitDescription)
                .HasColumnType("TEXT(1)")
                .HasColumnName("speed_limit_description");
            entity.Property(e => e.Theta)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("theta");
            entity.Property(e => e.TransitionAltitude)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("transition_altitude");
            entity.Property(e => e.TransitionIdentifier)
                .HasColumnType("TEXT(5)")
                .HasColumnName("transition_identifier");
            entity.Property(e => e.TurnDirection)
                .HasColumnType("TEXT(1)")
                .HasColumnName("turn_direction");
            entity.Property(e => e.VerticalAngle)
                .HasColumnType("DOUBLE(4)")
                .HasColumnName("vertical_angle");
            entity.Property(e => e.WaypointDescriptionCode)
                .HasColumnType("TEXT(4)")
                .HasColumnName("waypoint_description_code");
            entity.Property(e => e.WaypointIcaoCode)
                .HasColumnType("TEXT(2)")
                .HasColumnName("waypoint_icao_code");
            entity.Property(e => e.WaypointIdentifier)
                .HasColumnType("TEXT(5)")
                .HasColumnName("waypoint_identifier");
            entity.Property(e => e.WaypointLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("waypoint_latitude");
            entity.Property(e => e.WaypointLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("waypoint_longitude");
        });

        modelBuilder.Entity<TblStar>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("tbl_stars");

            entity.Property(e => e.AircraftCategory)
                .HasColumnType("TEXT(1)")
                .HasColumnName("aircraft_category");
            entity.Property(e => e.AirportIdentifier)
                .HasColumnType("TEXT(4)")
                .HasColumnName("airport_identifier");
            entity.Property(e => e.Altitude1)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("altitude1");
            entity.Property(e => e.Altitude2)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("altitude2");
            entity.Property(e => e.AltitudeDescription)
                .HasColumnType("TEXT(1)")
                .HasColumnName("altitude_description");
            entity.Property(e => e.ArcRadius)
                .HasColumnType("DOUBLE(7)")
                .HasColumnName("arc_radius");
            entity.Property(e => e.AreaCode)
                .HasColumnType("TEXT(3)")
                .HasColumnName("area_code");
            entity.Property(e => e.CenterId)
                .HasColumnType("TEXT(15)")
                .HasColumnName("center_id");
            entity.Property(e => e.CenterWaypoint)
                .HasColumnType("TEXT(5)")
                .HasColumnName("center_waypoint");
            entity.Property(e => e.CenterWaypointLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("center_waypoint_latitude");
            entity.Property(e => e.CenterWaypointLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("center_waypoint_longitude");
            entity.Property(e => e.DistanceTime)
                .HasColumnType("TEXT(1)")
                .HasColumnName("distance_time");
            entity.Property(e => e.Id)
                .HasColumnType("TEXT(15)")
                .HasColumnName("id");
            entity.Property(e => e.MagneticCourse)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("magnetic_course");
            entity.Property(e => e.PathTermination)
                .HasColumnType("TEXT(2)")
                .HasColumnName("path_termination");
            entity.Property(e => e.ProcedureIdentifier)
                .HasColumnType("TEXT(6)")
                .HasColumnName("procedure_identifier");
            entity.Property(e => e.RecommandedId)
                .HasColumnType("TEXT(15)")
                .HasColumnName("recommanded_id");
            entity.Property(e => e.RecommandedNavaid)
                .HasColumnType("TEXT(4)")
                .HasColumnName("recommanded_navaid");
            entity.Property(e => e.RecommandedNavaidLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("recommanded_navaid_latitude");
            entity.Property(e => e.RecommandedNavaidLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("recommanded_navaid_longitude");
            entity.Property(e => e.Rho)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("rho");
            entity.Property(e => e.Rnp)
                .HasColumnType("DOUBLE(4)")
                .HasColumnName("rnp");
            entity.Property(e => e.RouteDistanceHoldingDistanceTime)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("route_distance_holding_distance_time");
            entity.Property(e => e.RouteType)
                .HasColumnType("TEXT(1)")
                .HasColumnName("route_type");
            entity.Property(e => e.Seqno)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("seqno");
            entity.Property(e => e.SpeedLimit)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("speed_limit");
            entity.Property(e => e.SpeedLimitDescription)
                .HasColumnType("TEXT(1)")
                .HasColumnName("speed_limit_description");
            entity.Property(e => e.Theta)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("theta");
            entity.Property(e => e.TransitionAltitude)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("transition_altitude");
            entity.Property(e => e.TransitionIdentifier)
                .HasColumnType("TEXT(5)")
                .HasColumnName("transition_identifier");
            entity.Property(e => e.TurnDirection)
                .HasColumnType("TEXT(1)")
                .HasColumnName("turn_direction");
            entity.Property(e => e.VerticalAngle)
                .HasColumnType("DOUBLE(4)")
                .HasColumnName("vertical_angle");
            entity.Property(e => e.WaypointDescriptionCode)
                .HasColumnType("TEXT(4)")
                .HasColumnName("waypoint_description_code");
            entity.Property(e => e.WaypointIcaoCode)
                .HasColumnType("TEXT(2)")
                .HasColumnName("waypoint_icao_code");
            entity.Property(e => e.WaypointIdentifier)
                .HasColumnType("TEXT(5)")
                .HasColumnName("waypoint_identifier");
            entity.Property(e => e.WaypointLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("waypoint_latitude");
            entity.Property(e => e.WaypointLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("waypoint_longitude");
        });

        modelBuilder.Entity<TblTerminalNdbnavaid>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("tbl_terminal_ndbnavaids");

            entity.Property(e => e.AirportIdentifier)
                .HasColumnType("TEXT(4)")
                .HasColumnName("airport_identifier");
            entity.Property(e => e.AreaCode)
                .HasColumnType("TEXT(3)")
                .HasColumnName("area_code");
            entity.Property(e => e.IcaoCode)
                .HasColumnType("TEXT(2)")
                .HasColumnName("icao_code");
            entity.Property(e => e.Id)
                .HasColumnType("TEXT(15)")
                .HasColumnName("id");
            entity.Property(e => e.NavaidClass)
                .HasColumnType("TEXT(5)")
                .HasColumnName("navaid_class");
            entity.Property(e => e.NdbFrequency)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("ndb_frequency");
            entity.Property(e => e.NdbIdentifier)
                .HasColumnType("TEXT(4)")
                .HasColumnName("ndb_identifier");
            entity.Property(e => e.NdbLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("ndb_latitude");
            entity.Property(e => e.NdbLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("ndb_longitude");
            entity.Property(e => e.NdbName)
                .HasColumnType("TEXT(30)")
                .HasColumnName("ndb_name");
            entity.Property(e => e.Range)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("range");
        });

        modelBuilder.Entity<TblTerminalWaypoint>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("tbl_terminal_waypoints");

            entity.Property(e => e.AreaCode)
                .HasColumnType("TEXT(3)")
                .HasColumnName("area_code");
            entity.Property(e => e.IcaoCode)
                .HasColumnType("TEXT(2)")
                .HasColumnName("icao_code");
            entity.Property(e => e.Id)
                .HasColumnType("TEXT(15)")
                .HasColumnName("id");
            entity.Property(e => e.RegionCode)
                .HasColumnType("TEXT(4)")
                .HasColumnName("region_code");
            entity.Property(e => e.WaypointIdentifier)
                .HasColumnType("TEXT(5)")
                .HasColumnName("waypoint_identifier");
            entity.Property(e => e.WaypointLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("waypoint_latitude");
            entity.Property(e => e.WaypointLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("waypoint_longitude");
            entity.Property(e => e.WaypointName)
                .HasColumnType("TEXT(25)")
                .HasColumnName("waypoint_name");
            entity.Property(e => e.WaypointType)
                .HasColumnType("TEXT(3)")
                .HasColumnName("waypoint_type");
        });

        modelBuilder.Entity<TblVhfnavaid>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("tbl_vhfnavaids");

            entity.Property(e => e.AirportIdentifier)
                .HasColumnType("TEXT(4)")
                .HasColumnName("airport_identifier");
            entity.Property(e => e.AreaCode)
                .HasColumnType("TEXT(3)")
                .HasColumnName("area_code");
            entity.Property(e => e.DmeElevation)
                .HasColumnType("INTEGER(5)")
                .HasColumnName("dme_elevation");
            entity.Property(e => e.DmeIdent)
                .HasColumnType("TEXT(4)")
                .HasColumnName("dme_ident");
            entity.Property(e => e.DmeLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("dme_latitude");
            entity.Property(e => e.DmeLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("dme_longitude");
            entity.Property(e => e.IcaoCode)
                .HasColumnType("TEXT(2)")
                .HasColumnName("icao_code");
            entity.Property(e => e.Id)
                .HasColumnType("TEXT(15)")
                .HasColumnName("id");
            entity.Property(e => e.IlsdmeBias)
                .HasColumnType("DOUBLE(3)")
                .HasColumnName("ilsdme_bias");
            entity.Property(e => e.MagneticVariation)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("magnetic_variation");
            entity.Property(e => e.NavaidClass)
                .HasColumnType("TEXT(5)")
                .HasColumnName("navaid_class");
            entity.Property(e => e.Range)
                .HasColumnType("INTEGER(3)")
                .HasColumnName("range");
            entity.Property(e => e.StationDeclination)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("station_declination");
            entity.Property(e => e.VorFrequency)
                .HasColumnType("DOUBLE(5)")
                .HasColumnName("vor_frequency");
            entity.Property(e => e.VorIdentifier)
                .HasColumnType("TEXT(4)")
                .HasColumnName("vor_identifier");
            entity.Property(e => e.VorLatitude)
                .HasColumnType("DOUBLE(9)")
                .HasColumnName("vor_latitude");
            entity.Property(e => e.VorLongitude)
                .HasColumnType("DOUBLE(10)")
                .HasColumnName("vor_longitude");
            entity.Property(e => e.VorName)
                .HasColumnType("TEXT(30)")
                .HasColumnName("vor_name");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
