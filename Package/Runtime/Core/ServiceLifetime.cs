namespace MDI.Core
{
    /// <summary>
    /// Service'in yaşam süresini belirler
    /// </summary>
    public enum ServiceLifetime
    {
        /// <summary>
        /// Her resolve'da yeni instance oluşturulur
        /// </summary>
        Transient = 0,

        /// <summary>
        /// Container scope'unda tek instance
        /// </summary>
        Scoped = 1,

        /// <summary>
        /// Container boyunca tek instance
        /// </summary>
        Singleton = 2,

        /// <summary>
        /// Lazy loading ile oluşturulur
        /// </summary>
        Lazy = 3
    }
}
