using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace VintageTweaks.Code.Features.Client
{
    internal class Zoom : IDisposable
    {
        private const string _zoomHotkeyCode = "vintagetweaks_zoom";

        private readonly ICoreClientAPI _capi;
        private readonly Config.Zoom _cfg;
        private readonly long _tickListenerId;

        private int _originalFov, _targetFov, _currentFov, _startFov;
        private float _elapsedTime;
        private bool _internalFovWrite, _zoomKeyHeld;
        private GlKeys _currentZoomKey;

        public Zoom(ICoreClientAPI capi, Config.Zoom cfg)
        {
            _capi = capi;
            _cfg = cfg;

            if (!_cfg.AllowZoom) return;

            _originalFov = _capi.Settings.Int["fieldOfView"];
            _currentFov = _originalFov;
            _targetFov = _originalFov;
            _startFov = _originalFov;
            _internalFovWrite = false;
            _elapsedTime = 0f;

            _capi.Input.RegisterHotKey(
                _zoomHotkeyCode,
                "Zoom (VintageTweaks)",
                _cfg.ZoomKey,
                HotkeyType.CharacterControls
            );

            _tickListenerId = _capi.Event.RegisterGameTickListener(OnClientTick, 0);
            _capi.Settings.AddWatcher<int>("fieldOfView", OnFovChanged);
            _capi.Input.SetHotKeyHandler(_zoomHotkeyCode, OnKeyDown);
            _capi.Event.KeyUp += OnKeyUp;

        }

        private bool OnKeyDown(KeyCombination comb)
        {
            if (!_cfg.AllowZoom) return false;

            _zoomKeyHeld = true;
            _currentZoomKey = (GlKeys)comb.KeyCode;

            _startFov = _currentFov;
            _targetFov = (int)Math.Round(_originalFov * _cfg.ZoomPercent / 100d);
            _targetFov = GameMath.Clamp(_targetFov, 20, 160);

            _elapsedTime = 0f;
            return true;
        }

        private void OnKeyUp(KeyEvent e)
        {
            if (!_cfg.AllowZoom) return;
            if (!_zoomKeyHeld || (GlKeys)e.KeyCode != _currentZoomKey) return;
            
            _zoomKeyHeld = false;

            _startFov = _currentFov;
            _targetFov = _originalFov;
            _elapsedTime = 0f;
        }

        private void OnFovChanged(int newVal)
        {
            if (!_cfg.AllowZoom) return;

            _currentFov = newVal;

            if (_internalFovWrite) return;

            _originalFov = newVal;
            _targetFov = newVal;
        }

        private void OnClientTick(float dt)
        {
            if (!_cfg.AllowZoom || _currentFov == _targetFov) return;

            _elapsedTime += dt;

            float clampedTime = GameMath.Clamp(_elapsedTime / _cfg.ZoomSpeed, 0f, 1f);
            float lerped = GameMath.Lerp(_startFov, _targetFov, clampedTime);
            int newFov = (int)Math.Round(lerped);

            _internalFovWrite = true;
            _capi.Settings.Int["fieldOfView"] = newFov;
            _internalFovWrite = false;
        }

        public void Dispose()
        {
            _capi.Event.KeyUp -= OnKeyUp;

            if (_tickListenerId == 0) return;
            _capi.Event.UnregisterGameTickListener(_tickListenerId);
        }
    }
}
