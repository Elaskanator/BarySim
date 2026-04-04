using System.Text.RegularExpressions;

namespace BunkRimworldTweaks {
	public static class Shared {
		public static readonly Regex EnumStringSplitterRegex = new Regex(@"(?<!^)(?=[A-Z])", RegexOptions.Compiled);
		public static readonly Regex MultiSpaceRegex = new Regex(@"\s+", RegexOptions.Compiled);
	}
}
