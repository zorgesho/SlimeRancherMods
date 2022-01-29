using System.Reflection;
using UnityEngine;

namespace Common
{
	class Mod
	{
		public static readonly string id = Assembly.GetExecutingAssembly().GetName().Name;

		protected void init()
		{
#if DEBUG
			System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
			Object.Destroy(SentrySdk._instance);
#endif
			"Mod inited".logDbg();
		}
	}
}