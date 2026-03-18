using Godot;

public partial class ConfigFileHandler
{
	public void SaveRushHighScore(long score)
	{
		SaveKey("scores", "rush_high_score", score);
	}

	public long LoadRushHighScore()
	{
		if (config.HasSectionKey("scores", "rush_high_score"))
			return config.GetValue("scores", "rush_high_score").AsInt64();
		return 0;
	}

	public void SaveWaveHighScore(long score)
	{
		SaveKey("scores", "wave_high_score", score);
	}

	public long LoadWaveHighScore()
	{
		if (config.HasSectionKey("scores", "wave_high_score"))
			return config.GetValue("scores", "wave_high_score").AsInt64();
		return 0;
	}
}
