using Godot;

public partial class MusicPlayer : AudioStreamPlayer
{
	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
	}

	public void PlayMusic(AudioStream music)
	{
		if (Stream == music && Playing) return;

		Stream = music;
		VolumeDb = -10; // adjust later
		Play();
	}

	public void StopMusic()
	{
		Stop();
	}
}
