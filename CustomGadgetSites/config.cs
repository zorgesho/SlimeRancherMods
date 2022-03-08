#if !DEBUG
using SRML.Config.Attributes;
#endif

namespace CustomGadgetSites
{
#if !DEBUG
	[ConfigFile("config")]
#endif
	static class Config
	{
		public static readonly bool showSiteInfo = true;
	}
}