namespace BrainlessLabs.Neon.Editor
{
    /// <summary>
    /// Represents the core project settings used within the application.
    /// This class provides access to key configuration assets and settings
    /// related to the bootstrap process and audio management.
    /// Implements the <see cref="IProjectSettings"/> interface for standardized access.
    /// </summary>
    class ProjectSettings : IProjectSettings
    {
        private static ProjectSettings s_Instance;
        
        internal static ProjectSettings instance => s_Instance ??= new ProjectSettings();
        
        private IBootstrapSettings _bootstrapSettings;
        private BootstrapSettingsAsset _bootstrapSettingsAsset;
        
        private IAudioSettings _audioSettings;
        private AudioSettingsAsset _audioSettingsAsset;
        
        
        /// <inheritdoc/>
        public IBootstrapSettings BootstrapSettings => _bootstrapSettings;
        
        /// <inheritdoc/>
        public BootstrapSettingsAsset BootstrapSettingsAsset => _bootstrapSettingsAsset;
        
        /// <inheritdoc/>
        public IAudioSettings AudioSettings => _audioSettings;
        
        /// <inheritdoc/>
        public AudioSettingsAsset AudioSettingsAsset => _audioSettingsAsset;


        private ProjectSettings()
        {
            _bootstrapSettingsAsset = BootstrapSettingsAsset.GetOrCreateSettingsAsset();
            _bootstrapSettings = _bootstrapSettingsAsset.Settings;
            
            _audioSettingsAsset = AudioSettingsAsset.GetOrCreateSettingsAsset();
            _audioSettings = _audioSettingsAsset.Settings;
        }
    }
}
