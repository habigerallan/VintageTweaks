using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using VintageTweaks.Config;

namespace VintageTweaks.Zoom
{
    internal class VintageTweaksZoom : IDisposable
    {
        private readonly ICoreClientAPI _capi;
        private readonly VintageTweaksConfig _cfg;

        private const string ZoomHotkeyCode = "vintagetweaks_zoom";

        private int _originalFov, _targetFov, _currentFov, _startFov;
        private float _elaspedTime;
        private bool _internalFovWrite, _zoomKeyHeld;
        private GlKeys _currentZoomKey;
        private readonly long _tickListenerId;


        public VintageTweaksZoom(ICoreClientAPI capi, VintageTweaksConfig cfg)
        {
            _capi = capi;
            _cfg = cfg;

            if (!_cfg.AllowZoom) return;

            _capi.Input.RegisterHotKey(
                ZoomHotkeyCode,
                "Zoom (VintageTweaks)",
                _cfg.ZoomKey,
                HotkeyType.CharacterControls
            );

            _originalFov = _capi.Settings.Int["fieldOfView"];
            _currentFov = _originalFov;
            _targetFov = _originalFov;
            _startFov = _originalFov;
            _internalFovWrite = false;
            _elaspedTime = 0F;

            _tickListenerId = _capi.Event.RegisterGameTickListener(OnClientTick, 0);
            _capi.Settings.AddWatcher<int>("fieldOfView", OnFovChanged);
            _capi.Input.SetHotKeyHandler(ZoomHotkeyCode, OnKeyDown);
            _capi.Event.KeyUp += OnKeyUp;

        }

        public void Dispose()
        {
            _capi.Event.KeyUp -= OnKeyUp;
            _capi.Event.UnregisterGameTickListener(_tickListenerId);
        }

        private bool OnKeyDown(KeyCombination comb)
        {
            _zoomKeyHeld = true;
            _currentZoomKey = (GlKeys)comb.KeyCode;

            _startFov = _currentFov;
            _targetFov = (int)Math.Round(_originalFov * _cfg.ZoomPercent / 100d);
            GameMath.Clamp(_targetFov, 20, 160);

            _elaspedTime = 0f;
            return true;
        }

        private void OnKeyUp(KeyEvent e)
        {
            if (!_zoomKeyHeld || (GlKeys)e.KeyCode != _currentZoomKey) return;
            _zoomKeyHeld = false;

            _startFov = _currentFov;
            _targetFov = _originalFov;
            _elaspedTime = 0f;
        }

        private void OnFovChanged(int newVal)
        {
            _currentFov = newVal;
            if (_internalFovWrite) return;
            _originalFov = newVal;
            _targetFov = newVal;
        }

        private void OnClientTick(float dt)
        {
            if (!_cfg.AllowZoom || _currentFov == _targetFov) return;
            _elaspedTime += dt;

            float clampedTime = GameMath.Clamp(_elaspedTime / _cfg.ZoomSpeed, 0f, 1f);
            float interp = GameMath.Lerp(_startFov, _targetFov, clampedTime);
            int newFov = (int)Math.Round(interp);

            _internalFovWrite = true;
            _capi.Settings.Int["fieldOfView"] = newFov;
            _internalFovWrite = false;
        }
    }
}
