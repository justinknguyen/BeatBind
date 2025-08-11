namespace BeatBind.Domain.Entities
{
    public class PlaybackState
    {
        public bool IsPlaying { get; set; }
        public bool ShuffleState { get; set; }
        public RepeatMode RepeatState { get; set; }
        public int Volume { get; set; }
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
