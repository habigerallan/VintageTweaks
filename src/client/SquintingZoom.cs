using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace VintageTweaks.src.client;

internal sealed class SquintingZoom : IDisposable
{
    private const string ZoomHotkeyCode = "vintagetweaks_zoom";

    private readonly ICoreClientAPI _capi;
    private readonly VintageTweaksSystem.ZoomConfig _config;
    private readonly long _tickListenerId;

    private int _originalFieldOfView;
    private int _targetFieldOfView;
    private int _currentFieldOfView;
    private int _startFieldOfView;
    private float _elapsedTransitionTime;
    private bool _internalFieldOfViewWrite;
    private bool _zoomKeyHeld;
    private GlKeys _activeZoomKey;

    public SquintingZoom(ICoreClientAPI capi, VintageTweaksSystem.ZoomConfig config)
    {
        _capi = capi;
        _config = config;

        if (!_config.AllowZoom)
        {
            return;
        }

        _originalFieldOfView = _capi.Settings.Int["fieldOfView"];
        _currentFieldOfView = _originalFieldOfView;
        _targetFieldOfView = _originalFieldOfView;
        _startFieldOfView = _originalFieldOfView;

        _capi.Input.RegisterHotKey(
            ZoomHotkeyCode,
            "Zoom (VintageTweaks)",
            _config.ZoomKey,
            HotkeyType.CharacterControls
        );

        _tickListenerId = _capi.Event.RegisterGameTickListener(OnClientTick, 0);
        _capi.Settings.AddWatcher<int>("fieldOfView", OnFieldOfViewChanged);
        _capi.Input.SetHotKeyHandler(ZoomHotkeyCode, OnZoomKeyDown);
        _capi.Event.KeyUp += OnKeyUp;
    }

    private bool OnZoomKeyDown(KeyCombination combination)
    {
        if (!_config.AllowZoom)
        {
            return false;
        }

        _zoomKeyHeld = true;
        _activeZoomKey = (GlKeys)combination.KeyCode;
        _startFieldOfView = _currentFieldOfView;
        _targetFieldOfView = (int)Math.Round(_originalFieldOfView * _config.ZoomPercent / 100d);
        _targetFieldOfView = GameMath.Clamp(_targetFieldOfView, 20, 160);
        _elapsedTransitionTime = 0f;

        return true;
    }

    private void OnKeyUp(KeyEvent keyEvent)
    {
        if (!_config.AllowZoom || !_zoomKeyHeld || (GlKeys)keyEvent.KeyCode != _activeZoomKey)
        {
            return;
        }

        _zoomKeyHeld = false;
        _startFieldOfView = _currentFieldOfView;
        _targetFieldOfView = _originalFieldOfView;
        _elapsedTransitionTime = 0f;
    }

    private void OnFieldOfViewChanged(int newFieldOfView)
    {
        if (!_config.AllowZoom)
        {
            return;
        }

        _currentFieldOfView = newFieldOfView;
        if (_internalFieldOfViewWrite)
        {
            return;
        }

        _originalFieldOfView = newFieldOfView;
        _targetFieldOfView = newFieldOfView;
    }

    private void OnClientTick(float deltaTime)
    {
        if (!_config.AllowZoom || _currentFieldOfView == _targetFieldOfView)
        {
            return;
        }

        _elapsedTransitionTime += deltaTime;

        float completion = GameMath.Clamp(_elapsedTransitionTime / _config.ZoomSpeed, 0f, 1f);
        float interpolatedFieldOfView = GameMath.Lerp(_startFieldOfView, _targetFieldOfView, completion);
        int nextFieldOfView = (int)Math.Round(interpolatedFieldOfView);

        _internalFieldOfViewWrite = true;
        _capi.Settings.Int["fieldOfView"] = nextFieldOfView;
        _internalFieldOfViewWrite = false;
    }

    public void Dispose()
    {
        _capi.Event.KeyUp -= OnKeyUp;

        if (_tickListenerId != 0)
        {
            _capi.Event.UnregisterGameTickListener(_tickListenerId);
        }
    }
}
