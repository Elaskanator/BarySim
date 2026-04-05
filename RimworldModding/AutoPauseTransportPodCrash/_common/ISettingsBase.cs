using System.Collections.Generic;
using Verse;

namespace BunkRimworldTweaks
{
	public interface ISettingsBase : IExposable
	{
		bool Enabled { get; set; }
		string FriendlyName(string propertyName);

		Dictionary<string, bool> PropertiesEnabled { get; }
		IReadOnlyList<(string headerLabel, ISettingsBase settings)> ChildSections { get; }
	}

	public abstract class AFlatSimpleSettingsBase : ISettingsBase
	{
		protected bool _enabled = true;
		public bool Enabled { get => _enabled; set => _enabled = value; }

		public abstract string FriendlyName(string propertyName);

		protected Dictionary<string, bool> _propertiesEnabled = new Dictionary<string, bool>();
		public Dictionary<string, bool> PropertiesEnabled => _propertiesEnabled;

		private static readonly List<(string headerLabel, ISettingsBase settings)> _childSections
			= new List<(string headerLabel, ISettingsBase settings)>();

		public IReadOnlyList<(string headerLabel, ISettingsBase settings)> ChildSections => _childSections;

		public void ExposeData()
		{
			Scribe_Values.Look(ref _enabled, "Enabled", true);
			ExposeAdditionalParameters();
		}
		protected virtual void ExposeAdditionalParameters() { }
	}
}
