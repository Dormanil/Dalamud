namespace Dalamud.Plugin
{
    /// <summary>
    /// This enum reflects reasons for loading a plugin.
    /// </summary>
    public enum PluginLoadReason
    {
        /// <summary>
        /// We don't know why this plugin was loaded.
        /// </summary>
        Unknown,

        /// <summary>
        /// This plugin was loaded because it was installed with the plugin installer.
        /// </summary>
        Installer,

        /// <summary>
        /// This plugin was loaded because the game was started or Dalamud was reinjected.
        /// </summary>
        Boot,

        /// <summary>
        /// This plugin was loaded because the dev hotloading mechanism requested it.
        /// </summary>
        HotLoad,

        /// <summary>
        /// This plugin was loaded because the dev hotloading mechanism detected a change.
        /// </summary>
        HotLoadChange,
    }
}
