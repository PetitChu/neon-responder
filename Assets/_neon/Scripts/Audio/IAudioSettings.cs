namespace BrainlessLabs.Neon
{
    internal interface IAudioSettings : ISettings
    {
        AudioConfigurationAsset SfxConfiguration { get; }
        AudioConfigurationAsset MusicConfiguration { get; }
    }
}
