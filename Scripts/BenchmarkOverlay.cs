using Godot;
using System;
using System.Collections.Generic;

public partial class BenchmarkOverlay : CanvasLayer
{
    //  Settings
    private const float DISPLAY_UPDATE_RATE = 0.5f;
    private const float RECORD_DURATION     = 30f;

    // Visual LOD modes
    private const float LOD_BIAS_MAX         = 10000f;
    private const float LOD_BIAS_MIN         = 0f;

    // "Off" values for toggles
    private const int   COLLISION_RADIUS_OFF = 99;
    private const int   CHUNKS_PER_FRAME_OFF = 999;

    //  State
    private enum BenchState { Idle, Recording }
    private BenchState _state          = BenchState.Idle;
    private float      _recordingTimer = 0f;
    private float      _elapsedTime    = 0f;
    private float      _autoStartTimer = 5f;
    private bool       _hasResults     = false;

    //  Frame time buffer (unbounded)
    private readonly List<float> _buf = new();

    //  Stats cache
    private float _avgFps, _low1Fps, _low01Fps, _currentFps;
    private float _displayTimer = 0f;

    [Export] public bool StartVisible = true;

    public static long SceneChangeTimestamp = 0;
    private long _loadTimeMs = -1;

    //  Toggle states
    private int  _visualLodMode = 0; // 0=default, 1=max, 2=min
    private bool _collisionLOD  = true;
    private bool _chunkLoading  = true;
    private bool _visible       = true;

    //  Baseline values
    private float _baseLodBias;
    private int   _baseCollisionRadius;
    private int   _baseChunksPerFrame;

    //  References
    private ChunkedAsteroidField _asteroidField;
    private Label _label;

    //  Godot lifecycle

    public override void _Ready()
    {
        Layer = 100;
        _visible = StartVisible;

        _label = new Label();
        _label.Position = new Vector2(12, 12);
        _label.AddThemeColorOverride("font_color", new Color(1f, 0.95f, 0.3f));
        _label.AddThemeColorOverride("font_shadow_color", Colors.Black);
        _label.AddThemeConstantOverride("shadow_offset_x", 1);
        _label.AddThemeConstantOverride("shadow_offset_y", 1);
        _label.AddThemeFontSizeOverride("font_size", 20);
        _label.Visible = _visible;
        AddChild(_label);

        _asteroidField = GetTree().GetFirstNodeInGroup("asteroid_field") as ChunkedAsteroidField;
        if (_asteroidField != null)
        {
            _baseLodBias         = _asteroidField.LodBias;
            _baseCollisionRadius = _asteroidField.CollisionRadius;
            _baseChunksPerFrame  = _asteroidField.ChunksPerFrame;
        }

        if (SceneChangeTimestamp > 0)
        {
            _loadTimeMs = (long)Time.GetTicksMsec() - SceneChangeTimestamp;
            SceneChangeTimestamp = 0;
        }

        UpdateDisplay();
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _currentFps = dt > 0f ? 1f / dt : 0f;

        if (_state == BenchState.Recording)
        {
            _buf.Add(dt);
            _elapsedTime    += dt;
            _recordingTimer -= dt;

            _displayTimer += dt;
            if (_displayTimer >= DISPLAY_UPDATE_RATE)
            {
                _displayTimer = 0f;
                ComputeStats();
                UpdateDisplay();
            }

            if (_recordingTimer <= 0f)
                StopRecording();
        }
        else
        {
            if (!_hasResults && _autoStartTimer > 0f)
            {
                _autoStartTimer -= dt;
                if (_autoStartTimer <= 0f)
                    StartRecording();
            }

            _displayTimer += dt;
            if (_displayTimer >= DISPLAY_UPDATE_RATE)
            {
                _displayTimer = 0f;
                UpdateDisplay();
            }
        }
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is not InputEventKey key || !key.Pressed || key.Echo) return;

        switch (key.Keycode)
        {
            case Key.F12: ToggleOverlayVisibility(); break;
            case Key.F1:  CycleVisualLod(); break;
            case Key.F2:  Toggle(ref _collisionLOD, ApplyCollisionLOD); break;
            case Key.F3:  Toggle(ref _chunkLoading, ApplyChunkLoading); break;
            case Key.F4:  ToggleRecording(); break;
        }
    }

    //  Toggle helpers

    private void Toggle(ref bool flag, Action<bool> apply)
    {
        flag = !flag;
        apply(flag);
        UpdateDisplay();
    }

    private void ToggleOverlayVisibility()
    {
        _visible = !_visible;
        _label.Visible = _visible;
    }

    private void ToggleRecording()
    {
        if (_state == BenchState.Recording)
            StopRecording();
        else
            StartRecording();
    }

    private void StartRecording()
    {
        _buf.Clear();
        _elapsedTime    = 0f;
        _recordingTimer = RECORD_DURATION;
        _displayTimer   = 0f;
        _state          = BenchState.Recording;
        UpdateDisplay();
    }

    private void StopRecording()
    {
        _state      = BenchState.Idle;
        _hasResults = _buf.Count > 0;
        ComputeStats();
        UpdateDisplay();
    }

    //  Apply functions

    private void CycleVisualLod()
    {
        _visualLodMode = (_visualLodMode + 1) % 3;
        ApplyVisualLodMode();
        UpdateDisplay();
    }

    private void ApplyVisualLodMode()
    {
        if (_asteroidField == null) return;
        float bias = _visualLodMode switch
        {
            1 => LOD_BIAS_MAX,
            2 => LOD_BIAS_MIN,
            _ => _baseLodBias,
        };
        _asteroidField.LodBias = bias;
        _asteroidField.ApplyLodBias(bias);
    }

    private void ApplyCollisionLOD(bool on)
    {
        if (_asteroidField == null) return;
        _asteroidField.CollisionRadius = on ? _baseCollisionRadius : COLLISION_RADIUS_OFF;
        _asteroidField.RefreshCollisionBodies();
    }

    private void ApplyChunkLoading(bool on)
    {
        if (_asteroidField == null) return;
        _asteroidField.ChunksPerFrame = on ? _baseChunksPerFrame : CHUNKS_PER_FRAME_OFF;
    }

    //  Stats

    private void ComputeStats()
    {
        int count = _buf.Count;
        if (count == 0) return;

        var sorted = _buf.ToArray();
        Array.Sort(sorted);

        float total = 0f;
        foreach (var f in _buf) total += f;

        float slowP1  = sorted[(int)((count - 1) * 0.99f)];
        float slowP01 = sorted[(int)((count - 1) * 0.999f)];
        float avgDt   = total / count;

        _low1Fps  = slowP1  > 0f ? 1f / slowP1  : 0f;
        _low01Fps = slowP01 > 0f ? 1f / slowP01 : 0f;
        _avgFps   = avgDt   > 0f ? 1f / avgDt   : 0f;
    }

    //  Display

    private void UpdateDisplay()
    {
        if (!_visible) return;

        string on  = "[ON ]";
        string off = "[OFF]";

        string header = _state == BenchState.Recording
            ? $" RECORDING {_elapsedTime:F1}s ({_buf.Count} frames) "
            : _hasResults
                ? $" BENCHMARK ({_buf.Count} frames, {_elapsedTime:F1}s) "
                : $" BENCHMARK ";

        string stats = (_hasResults || _state == BenchState.Recording)
            ? $"FPS Current : {_currentFps:F1} fps\n" +
              $"FPS Average : {_avgFps:F1} fps\n" +
              $"FPS Low 1%  : {_low1Fps:F1} fps\n" +
              $"FPS Low 0.1%: {_low01Fps:F1} fps\n"
            : _autoStartTimer > 0f
                ? $"FPS Current : {_currentFps:F1} fps\n" +
                  $"Starting in {_autoStartTimer:F1}s — F4 to start now\n"
                : $"FPS Current : {_currentFps:F1} fps\n" +
                  "No data — press F4 to start\n";

        string loadLine = _loadTimeMs >= 0
            ? $"Load time   : {_loadTimeMs} ms\n"
            : "";

        _label.Text =
            header + "\n" +
            stats +
            loadLine +
            $"─\n" +
            $"F1  {(_visualLodMode == 0 ? "[DEF]" : _visualLodMode == 1 ? "[MAX]" : "[MIN]")} Visual LOD    ({(_visualLodMode == 0 ? $"bias={_baseLodBias}" : _visualLodMode == 1 ? "max detail" : "min detail")})\n" +
            $"F2  {(_collisionLOD ? on : off)} Collision LOD (R={(_collisionLOD ? _baseCollisionRadius : COLLISION_RADIUS_OFF)})\n" +
            $"F3  {(_chunkLoading ? on : off)} Chunk Loading ({(_chunkLoading ? _baseChunksPerFrame : CHUNKS_PER_FRAME_OFF)}/frame)\n" +
            $"F4  {(_state == BenchState.Recording ? "[STOP]" : "[REC] ")} Start/Stop    F12 Hide/Show";
    }
}
