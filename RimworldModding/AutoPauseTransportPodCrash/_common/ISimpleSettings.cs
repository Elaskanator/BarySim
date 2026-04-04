using System.Collections.Generic;

namespace BunkRimworldTweaks
{
	public interface ISimpleSettings
	{
		bool MasterEnabled { get; set; }
		Dictionary<string, bool> PropertiesEnabled { get; }
		IReadOnlyList<(string headerLabel, ISimpleSettings settings)> ChildSections { get; }

		string FriendlyName(string propertyName);
	}
}
