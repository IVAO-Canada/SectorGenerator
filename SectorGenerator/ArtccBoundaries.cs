using System.Text.RegularExpressions;

namespace SectorGenerator;

internal static class ArtccBoundaries
{
	static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30)};

	const string ZNY_OCEANIC_INJECT = @"ZNY,0,2000,36420900N,072395800W,ZDC
ZNY,0,2000,35054000N,072395800W,ZWY
ZNY,0,2000,32120700N,076490600W,ZWY
ZNY,0,3000,32154400N,077000000W,ZJX
ZNY,0,3000,32580400N,076464600W,ZJX
ZNY,0,2000,33250900N,076285200W,ZDC
ZNY,0,2000,34360100N,075410200W,ZDC
ZNY,0,2000,35182900N,075112400W,ZDC
ZNY,0,2000,34140000N,073570000W,ZDC
ZNY,0,2000,34214600N,073450600W,ZDC
ZNY,0,2000,34291700N,073342300W,ZDC
ZNY,0,2000,35294900N,074561300W,ZDC
ZNY,0,2000,36471600N,074355900W,ZDC
ZNY,0,2000,36470181N,074292976W,ZDC
ZNY,0,2000,36420900N,072395800W,ZDC";

	const string ZMA_TEG_BODLO_INJECT = @"ZMA,0,3000,20250000N,073000000W,TEG";

	const string PACIFIC_INJECT = @"ZAK,0,5500,49400000N,180000000E,ZAN
ZAK,0,5500,45420000N,162550000E,RJJJ
ZAK,0,5500,43000000N,165000000E,RJJJ
ZAK,0,5500,27000000N,165000000E,RJJJ
ZAK,0,5500,27000000N,155000000E,RJJJ
ZAK,0,5500,21000000N,155000000E,RJJJ
ZAK,0,5500,21000000N,130000000E,RJJJ
ZAK,0,5500,07000000N,130000000E,RPHI
ZAK,0,5500,03300000N,133000000E,WAAZ
ZAK,0,5500,03300000N,141000000E,WAAZ
ZAK,0,5500,00000000N,141000000E,WAAZ
ZAK,0,5500,00000000N,160000000E,AYPY
ZAK,0,5500,03300000N,160000000E,ANAU
ZAK,0,5500,03300000N,180000000E,NFFF
ZAK,0,5500,05000000S,180000000E,NFFF
ZAK,0,5500,05000000S,155000000W,NZZO
ZAK,0,5500,03300000N,145000000W,NTTT
ZAK,0,5500,03300000N,120000000W,XX03
ZAK,0,5500,30000000N,120000000W,MMZT
ZAK,0,5500,30450000N,120490000W,ZLA
ZAK,0,5500,36000000N,124110000W,ZOA
ZAK,0,5500,35300000N,125490000W,ZOA
ZAK,0,5500,36230000N,126550000W,ZOA
ZAK,0,5500,37300000N,127000000W,ZOA
ZAK,0,5500,40490000N,127000000W,ZOA
ZAK,0,5500,40580000N,126540000W,ZSE
ZAK,0,5500,45000000N,126300000W,ZSE
ZAK,0,5500,48190000N,128000000W,ZVR
ZAK,0,5500,51000000N,133450000W,ZVR
ZAK,0,5500,52430000N,135000000W,ZVR
ZAK,0,5500,56460000N,151450000W,ZAN
ZAK,0,5500,56000000N,153000000W,ZAN
ZAK,0,5500,53300000N,160000000W,ZAN
ZAK,0,5500,51230000N,167490000W,ZAN
ZAK,0,5500,50070000N,176340000W,ZAN
ZAK,0,5500,49400000N,180000000E,ZAN
ZAN,0,20310,64595960N,168582405W,Arctic
ZAN,0,20310,68001212N,168582405W,Arctic
ZAN,0,20310,68075725N,168420204W,Arctic
ZAN,0,20310,68154721N,168251515W,Arctic
ZAN,0,20310,68231530N,168090044W,Arctic
ZAN,0,20310,68311544N,167512052W,Arctic
ZAN,0,20310,68393827N,167323253W,Arctic
ZAN,0,20310,68463704N,167163938W,Arctic
ZAN,0,20310,68551213N,166564717W,Arctic
ZAN,0,20310,69015108N,166411012W,Arctic
ZAN,0,20310,69101056N,166211700W,Arctic
ZAN,0,20310,69165738N,166045112W,Arctic
ZAN,0,20310,69250158N,165445800W,Arctic
ZAN,0,20310,69315619N,165274114W,Arctic
ZAN,0,20310,69400404N,165070004W,Arctic
ZAN,0,20310,69464653N,164493852W,Arctic
ZAN,0,20310,69550114N,164275844W,Arctic
ZAN,0,20310,70012902N,164104240W,Arctic
ZAN,0,20310,70093411N,163484345W,Arctic
ZAN,0,20310,70160227N,163305110W,Arctic
ZAN,0,20310,70240328N,163081833W,Arctic
ZAN,0,20310,70302649N,162500251W,Arctic
ZAN,0,20310,70375100N,162283116W,Arctic
ZAN,0,20310,70444147N,162081616W,Arctic
ZAN,0,20310,70525425N,161433055W,Arctic
ZAN,0,20310,70584701N,161252951W,Arctic
ZAN,0,20310,71061553N,161020853W,Arctic
ZAN,0,20310,71124210N,160414207W,Arctic
ZAN,0,20310,71201319N,160172153W,Arctic
ZAN,0,20310,71262649N,159565131W,Arctic
ZAN,0,20310,71335858N,159313258W,Arctic
ZAN,0,20310,71400036N,159105634W,Arctic
ZAN,0,20310,71470353N,158462145W,Arctic
ZAN,0,20310,71532306N,158235543W,Arctic
ZAN,0,20310,71564155N,158120002W,Arctic
ZAN,0,20310,71595960N,158000007W,Arctic
ZAN,0,20310,71595960N,157434841W,Arctic
ZAN,0,20310,71595960N,157000007W,Arctic
ZAN,0,20310,71595960N,156000007W,Arctic
ZAN,0,20310,71595960N,155000007W,Arctic
ZAN,0,20310,72000000N,154000007W,Arctic
ZAN,0,20310,72000000N,153000007W,Arctic
ZAN,0,20310,72000000N,152000007W,Arctic
ZAN,0,20310,72000000N,151000007W,Arctic
ZAN,0,20310,72000000N,150000007W,Arctic
ZAN,0,20310,72000000N,149000007W,Arctic
ZAN,0,20310,72000000N,148000007W,Arctic
ZAN,0,20310,72000000N,147000007W,Arctic
ZAN,0,20310,72000000N,146000007W,Arctic
ZAN,0,20310,72000000N,145000007W,Arctic
ZAN,0,20310,72000000N,144000007W,Arctic
ZAN,0,20310,72000000N,143000007W,Arctic
ZAN,0,20310,72000001N,142000007W,ZEG
ZAN,0,20310,72000001N,141000007W,ZEG
ZAN,0,20310,60182301N,141000804W,ZEG
ZAN,0,20310,60133301N,140311904W,ZEG
ZAN,0,20310,60184101N,140272004W,ZEG
ZAN,0,20310,60110501N,139583804W,ZEG
ZAN,0,20310,60202201N,139402804W,ZEG
ZAN,0,20310,60210001N,139040004W,ZEG
ZAN,0,20310,60193801N,139032904W,ZEG
ZAN,0,20310,60053301N,139111004W,ZEG
ZAN,0,20310,59593801N,139024004W,ZEG
ZAN,0,20310,59544001N,138422104W,ZEG
ZAN,0,20310,59483001N,138400004W,ZEG
ZAN,0,20310,59461801N,138372504W,ZEG
ZAN,0,20310,59143001N,137355404W,ZEG
ZAN,0,20310,58592801N,137293404W,ZEG
ZAN,0,20310,58550001N,137311504W,ZEG
ZAN,0,20310,58543801N,137264504W,ZEG
ZAN,0,20310,58585601N,137184204W,ZEG
ZAN,0,20310,59002701N,137151504W,ZEG
ZAN,0,20310,59092901N,136492404W,ZEG
ZAN,0,20310,59101201N,136343304W,ZEG
ZAN,0,20310,59163501N,136290004W,ZEG
ZAN,0,20310,59165301N,136272804W,ZEG
ZAN,0,20310,59275601N,136275104W,ZEG
ZAN,0,20310,59271001N,136240304W,ZEG
ZAN,0,20310,59272801N,136201704W,ZEG
ZAN,0,20310,59275400N,136174344W,ZEG
ZAN,0,20310,59323601N,136132604W,ZEG
ZAN,0,20310,59333401N,136135004W,ZEG
ZAN,0,20310,59361601N,136203004W,ZEG
ZAN,0,20310,59370901N,136173404W,ZEG
ZAN,0,20310,59383101N,136102004W,ZEG
ZAN,0,20310,59400101N,135562004W,ZEG
ZAN,0,20310,59480001N,135285904W,ZEG
ZAN,0,20310,59442001N,135212704W,ZEG
ZAN,0,20310,59393101N,135125404W,ZEG
ZAN,0,20310,59374201N,135095504W,ZEG
ZAN,0,20310,59375101N,135074204W,ZEG
ZAN,0,20310,59340901N,135014404W,ZEG
ZAN,0,20310,59282501N,135015104W,ZEG
ZAN,0,20310,59273401N,135035704W,ZEG
ZAN,0,20310,59255501N,135054204W,ZEG
ZAN,0,20310,59232201N,134592604W,ZEG
ZAN,0,20310,59210101N,135020104W,ZEG
ZAN,0,20310,59164201N,134565104W,ZEG
ZAN,0,20310,59150501N,134413304W,ZEG
ZAN,0,20310,59113601N,134402604W,ZEG
ZAN,0,20310,59074401N,134333504W,ZEG
ZAN,0,20310,59080001N,134290004W,ZEG
ZAN,0,20310,59023001N,134230004W,ZEG
ZAN,0,20310,58584701N,134240604W,ZEG
ZAN,0,20310,58574501N,134190004W,ZEG
ZAN,0,20310,58553701N,134195604W,ZEG
ZAN,0,20310,58511401N,134141804W,ZEG
ZAN,0,20310,58434401N,133501604W,ZEG
ZAN,0,20310,58252801N,133214004W,ZEG
ZAN,0,20310,58225701N,133262104W,ZEG
ZAN,0,20310,58093501N,133101104W,ZEG
ZAN,0,20310,58002701N,133044104W,ZEG
ZAN,0,20310,57505901N,132524604W,ZEG
ZAN,0,20310,57125101N,132134404W,ZEG
ZAN,0,20310,57052001N,132220004W,ZEG
ZAN,0,20310,57024701N,132023204W,ZVR
ZAN,0,20310,57000001N,132035804W,ZVR
ZAN,0,20310,56595959N,132035737W,ZVR
ZAN,0,20310,56482901N,131522004W,ZVR
ZAN,0,20310,56450901N,131534804W,ZVR
ZAN,0,20310,56423901N,131514204W,ZVR
ZAN,0,20310,56360301N,131495104W,ZVR
ZAN,0,20310,56365201N,131350004W,ZVR
ZAN,0,20310,56331201N,131282104W,ZVR
ZAN,0,20310,56243001N,131050004W,ZVR
ZAN,0,20310,56220001N,130470004W,ZVR
ZAN,0,20310,56163001N,130380004W,ZVR
ZAN,0,20310,56144201N,130280904W,ZVR
ZAN,0,20310,56082701N,130250204W,ZVR
ZAN,0,20310,56082001N,130231804W,ZVR
ZAN,0,20310,56055301N,130144004W,ZVR
ZAN,0,20310,56072801N,130062604W,ZVR
ZAN,0,20310,56000001N,130003804W,ZVR
ZAN,0,20310,55553001N,130010204W,ZVR
ZAN,0,20310,55545301N,130002904W,ZVR
ZAN,0,20310,55541101N,130003804W,ZVR
ZAN,0,20310,55535601N,130000604W,ZVR
ZAN,0,20310,55485001N,130051004W,ZVR
ZAN,0,20310,55461401N,130084404W,ZVR
ZAN,0,20310,55433801N,130090304W,ZVR
ZAN,0,20310,55424301N,130083604W,ZVR
ZAN,0,20310,55414501N,130075504W,ZVR
ZAN,0,20310,55402101N,130061504W,ZVR
ZAN,0,20310,55385601N,130060904W,ZVR
ZAN,0,20310,55380731N,130062307W,ZVR
ZAN,0,20310,55371001N,130065504W,ZVR
ZAN,0,20310,55363401N,130065804W,ZVR
ZAN,0,20310,55353901N,130072004W,ZVR
ZAN,0,20310,55330601N,130071204W,ZVR
ZAN,0,20310,55323201N,130063404W,ZVR
ZAN,0,20310,55304401N,130052104W,ZVR
ZAN,0,20310,55292001N,130045204W,ZVR
ZAN,0,20310,55271701N,130022604W,ZVR
ZAN,0,20310,55255801N,130014904W,ZVR
ZAN,0,20310,55203001N,130013104W,ZVR
ZAN,0,20310,55174001N,129583203W,ZVR
ZAN,0,20310,55165501N,129584003W,ZVR
ZAN,0,20310,55113001N,130060103W,ZVR
ZAN,0,20310,55085001N,130083203W,ZVR
ZAN,0,20310,55040001N,130111503W,ZVR
ZAN,0,20310,54560801N,130181403W,ZVR
ZAN,0,20310,54491801N,130292603W,ZVR
ZAN,0,20310,54474801N,130321803W,ZVR
ZAN,0,20310,54461501N,130383203W,ZVR
ZAN,0,20310,54454501N,130390003W,ZVR
ZAN,0,20310,54423001N,130363003W,ZVR
ZAN,0,20310,54394401N,132410304W,ZVR
ZAN,0,20310,54130001N,134570004W,ZVR
ZAN,0,20310,54000001N,136000004W,ZVR
ZAN,0,20310,52430354N,134594718W,ZAK
ZAN,0,20310,52430001N,135000003W,ZAK
ZAN,0,20310,53220301N,137000003W,ZAK
ZAN,0,20310,56454200N,151450004W,ZAK
ZAN,0,20310,56000000N,153000004W,ZAK
ZAN,0,20310,53300000N,160000004W,ZAK
ZAN,0,20310,51235960N,167490003W,RJJJ
ZAN,0,20310,50281030N,180000000E,RJJJ
ZAN,0,20310,51045960N,173435957E,RJJJ
ZAN,0,20310,51295959N,169595957E,RJJJ
ZAN,0,20310,54401333N,169593654E,UHMM
ZAN,0,20310,59595959N,179595956E,UHMM
ZAN,0,20310,60000002N,180000000E,UHMM
ZAN,0,20310,61201159N,177450604W,UHMM
ZAN,0,20310,64025960N,172120004W,UHMM
ZAN,0,20310,64595960N,168582405W,UHMM";

	public static async Task<(Dictionary<string, (double Latitude, double Longitude)[]> Boundaries, Dictionary<string, string[]> Neighbours, string[] Faa)> GetBoundariesAsync(string? link = null)
	{
		string boundaryFileContents;

		if (link is null)
		{
			Regex eramLink = new(@"https://aeronav.faa.gov/Upload_313-d/ERAM_ARTCC_Boundaries/Ground_Level_ARTCC_Boundary_Data_\d+-\d+-\d+.csv", RegexOptions.CultureInvariant);

			string[] allLinks =
				eramLink.Matches(await _http.GetStringAsync(@"https://www.faa.gov/air_traffic/flight_info/aeronav/Aero_Data/Center_Surface_Boundaries/"))
				.Select(m => m.Value)
				.ToArray();

			List<string> boundaryLines = [];
			
			foreach (string matchLink in allLinks.Order().Reverse())
			{
				try
				{
					boundaryLines = [.. (await _http.GetStringAsync(matchLink)).Split("\r\n").Where(l => l.Length > 3 && l[3] == ',')];
					break;
				}
				catch (HttpRequestException) { /* Link hasn't been fulfilled yet. Iterate back. */ }
			}
			int idx = boundaryLines.IndexOf("ZNY,0,2000,36420900N,072395800W,ZDC");
			boundaryLines[idx] = ZNY_OCEANIC_INJECT;

			idx = boundaryLines.IndexOf("ZMA,0,3000,21142100N,067390200W,ZWY");
			boundaryLines[idx] = boundaryLines[idx][..^3] + "ZSU";
			boundaryLines.Insert(++idx, "ZSU" + boundaryLines[idx - 1][3..^3] + "ZMA");

			while (boundaryLines[++idx] != "ZMA,0,3000,19000000N,068000000W,DCS")
				boundaryLines[idx] = "ZSU" + boundaryLines[idx][3..];

			boundaryLines.Insert(idx, "ZSU" + boundaryLines[idx][3..^3] + "ZMA");

			idx = boundaryLines.IndexOf("ZMA,0,3000,20250000N,071400000W,DCS") + 1;
			while (boundaryLines[idx] != "ZMA,0,3000,20000000N,073200000W,HAV")
				boundaryLines.RemoveAt(idx);

			boundaryLines.Insert(idx, ZMA_TEG_BODLO_INJECT.ReplaceLineEndings("\r\n"));
			boundaryLines.Add(PACIFIC_INJECT.ReplaceLineEndings("\r\n"));

			boundaryFileContents = string.Join("\r\n", boundaryLines);
		}
		else
			boundaryFileContents = await _http.GetStringAsync(link);

		Dictionary<string, string[][]> boundaryPoints =
			boundaryFileContents
			.Split("\r\n")
			.Where(l => l.Length > 3 && l[0] == 'Z' && l[3] == ',')
			.GroupBy(l => l.Split(',')[0])
			.ToDictionary(g => g.Key, g => g.Select(l => l.Split(',')).ToArray());

		foreach (string ctr in boundaryPoints.Values.SelectMany(v => v.Select(l => l[5])).Distinct().ToArray())
		{
			if (boundaryPoints.ContainsKey(ctr))
				continue;

			string[][] points = [..boundaryPoints.Values.Select(v => v.Where(l => l[5] == ctr).ToArray()).Where(g => g.Length > 0).SelectMany(i => i).Distinct()];
			boundaryPoints.Add(ctr, points);
		}

		return (
			boundaryPoints.Select(b => new KeyValuePair<string, (double, double)[]>(
				b.Key,
				b.Value.Select(l => (
					ConvertEramCoordPartToDouble(l[3]),
					ConvertEramCoordPartToDouble(l[4])
				)).ToArray()
			)).ToDictionary(),

			boundaryPoints.Select(b => new KeyValuePair<string, string[]>(
				b.Key,
				b.Value.Select(l => l[5]).Distinct().ToArray()
			)).ToDictionary(),

			[..boundaryFileContents.Split("\r\n").Select(l => l.Split(',')[0]).Distinct().Order()]
		);
	}

	static double ConvertEramCoordPartToDouble(string eramCoordPart)
	{
		int degrees = int.Parse(eramCoordPart[..^7]);
		int minutes = int.Parse(eramCoordPart[^7..^5]);
		double seconds = double.Parse(eramCoordPart[^5..^3] + "." + eramCoordPart[^3..^1]);

		return (degrees + ((minutes + seconds / 60) / 60)) * (eramCoordPart[^1] is 'S' or 'W' ? -1 : 1);
	}
}
