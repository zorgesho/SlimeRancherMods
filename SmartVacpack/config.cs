#if !DEBUG
using SRML.Config.Attributes;
#endif

namespace SmartVacpack
{
#if !DEBUG
	[ConfigFile("config")]
#endif
	static class Config
	{
	}
}