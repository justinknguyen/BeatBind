namespace BeatBind.Core.Entities
{
    public class PlaybackState
    {
        public bool IsPlaying { get; set; }
        public bool ShuffleState { get; set; }
        public RepeatMode RepeatState { get; set; }

        /// <summary>
        /// Device volume 0-100, or null when the device does not report a volume
        /// (Spotify returns volume_percent: null for some devices and sessions).
        /// </summary>
        public int? Volume { get; set; }
        public int ProgressMs { get; set; }
        public int DurationMs { get; set; }
        public Track? CurrentTrack { get; set; }
        public Device? Device { get; set; }
    }

    public enum RepeatMode
    {
        Off,
        Track,
        Context
    }
}
