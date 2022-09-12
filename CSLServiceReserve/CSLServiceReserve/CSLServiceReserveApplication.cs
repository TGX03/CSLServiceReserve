using ColossalFramework;
using ICities;
//using ColossalFramework.Steamworks;

namespace CSLServiceReserve
{
    internal class CslServiceReserveApplication : IApplication
    {
        public IManagers managers
        {
            get
            {
                Helper.dbgLog("someone called me.");
                return Singleton<SimulationManager>.instance.m_ManagersWrapper;
            }
        }

        public string currentVersion
        {
            get
            {
                Helper.dbgLog("someone called me.");
                return BuildConfig.applicationVersionFull;
            }
        }

        public bool SupportsVersion(int a, int b, int c)
        {
            Helper.dbgLog("------Supports Version------------" + a + b + c);
            return BuildConfig.SupportsVersion(BuildConfig.MakeVersionNumber((uint)a, (uint)b, (uint)c, BuildConfig.ReleaseType.Final, 1u, BuildConfig.BuildType.Unknown));
        }

        public bool SupportsExpansion(Expansion expansion)
        {
            Helper.dbgLog("someone called me.");
            return Singleton<LoadingManager>.instance.m_supportsExpansion[(int)expansion];
        }
    }
}
