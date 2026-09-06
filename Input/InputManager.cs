using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace XeviShot.Input;

/// <summary>
/// キーボードおよびXInputゲームパッドの入力を統合管理するクラス
/// </summary>
public class InputManager
{
    // キーボードの押下状態
    private readonly HashSet<Keys> _pressedKeys = new();

    // ゲームパッド接続状態
    public bool IsGamepadConnected { get; private set; } = false;

    // ゲームパッド入力状態
    public float GamepadAxisX { get; private set; } = 0f;
    public float GamepadAxisY { get; private set; } = 0f;
    public bool GamepadButtonA { get; private set; } = false;
    public bool GamepadButtonB { get; private set; } = false;
    public bool GamepadAnyButton { get; private set; } = false;

    // 振動タイマー管理
    private DateTime _rumbleEndTime = DateTime.MinValue;

    #region XInput Native API P/Invoke
    [StructLayout(LayoutKind.Sequential)]
    private struct XINPUT_GAMEPAD
    {
        public ushort wButtons;
        public byte bLeftTrigger;
        public byte bRightTrigger;
        public short sThumbLX;
        public short sThumbLY;
        public short sThumbRX;
        public short sThumbRY;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XINPUT_STATE
    {
        public uint dwPacketNumber;
        public XINPUT_GAMEPAD Gamepad;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XINPUT_VIBRATION
    {
        public ushort wLeftMotorSpeed;
        public ushort wRightMotorSpeed;
    }

    private const int ERROR_SUCCESS = 0;
    private const ushort XINPUT_GAMEPAD_A = 0x1000;
    private const ushort XINPUT_GAMEPAD_B = 0x2000;

    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
    private static extern int XInputGetState14(int dwUserIndex, out XINPUT_STATE pState);

    [DllImport("xinput1_4.dll", EntryPoint = "XInputSetState")]
    private static extern int XInputSetState14(int dwUserIndex, ref XINPUT_VIBRATION pVibration);

    [DllImport("xinput9_1_0.dll", EntryPoint = "XInputGetState")]
    private static extern int XInputGetState91(int dwUserIndex, out XINPUT_STATE pState);

    [DllImport("xinput9_1_0.dll", EntryPoint = "XInputSetState")]
    private static extern int XInputSetState91(int dwUserIndex, ref XINPUT_VIBRATION pVibration);

    private static bool _useFallbackDll = false;
    private static bool _dllChecked = false;

    private static int NativeGetState(int userIndex, out XINPUT_STATE state)
    {
        if (!_dllChecked)
        {
            try
            {
                var ret = XInputGetState14(userIndex, out state);
                _dllChecked = true;
                _useFallbackDll = false;
                return ret;
            }
            catch
            {
                _useFallbackDll = true;
                _dllChecked = true;
            }
        }

        if (_useFallbackDll)
        {
            try
            {
                return XInputGetState91(userIndex, out state);
            }
            catch
            {
                state = default;
                return -1;
            }
        }

        try
        {
            return XInputGetState14(userIndex, out state);
        }
        catch
        {
            state = default;
            return -1;
        }
    }

    private static int NativeSetState(int userIndex, ref XINPUT_VIBRATION vibration)
    {
        try
        {
            if (_useFallbackDll)
                return XInputSetState91(userIndex, ref vibration);
            return XInputSetState14(userIndex, ref vibration);
        }
        catch
        {
            return -1;
        }
    }
    #endregion

    public void OnKeyDown(Keys key)
    {
        _pressedKeys.Add(key);
    }

    public void OnKeyUp(Keys key)
    {
        _pressedKeys.Remove(key);
    }

    public bool IsKeyDown(Keys key)
    {
        return _pressedKeys.Contains(key);
    }

    /// <summary>
    /// 毎フレーム呼び出してゲームパッド入力と振動状態を更新します。
    /// </summary>
    public void Update()
    {
        try
        {
            // 振動タイマー終了確認
            if (_rumbleEndTime != DateTime.MinValue && DateTime.UtcNow >= _rumbleEndTime)
            {
                _rumbleEndTime = DateTime.MinValue;
                SetRumbleInternal(0, 0);
            }

            // ゲームパッド入力ポーリング（プレイヤー1）
            var res = NativeGetState(0, out var state);
            if (res == ERROR_SUCCESS)
            {
                IsGamepadConnected = true;

                // スティック入力（デッドゾーン考慮）
                // short.MinValue (-32768) に対する Math.Abs による OverflowException を防ぐため float にキャスト
                const float DeadZone = 7849f;
                float fx = state.Gamepad.sThumbLX;
                float fy = state.Gamepad.sThumbLY;

                GamepadAxisX = Math.Abs(fx) > DeadZone ? (fx > 0 ? (fx - DeadZone) / (32767f - DeadZone) : (fx + DeadZone) / (32768f - DeadZone)) : 0f;
                GamepadAxisY = Math.Abs(fy) > DeadZone ? (fy > 0 ? (fy - DeadZone) / (32767f - DeadZone) : (fy + DeadZone) / (32768f - DeadZone)) : 0f;

                // 十字キー (D-Pad) による移動もサポート
                if ((state.Gamepad.wButtons & 0x0004) != 0) GamepadAxisX = -1f; // Left
                if ((state.Gamepad.wButtons & 0x0008) != 0) GamepadAxisX = 1f;  // Right
                if ((state.Gamepad.wButtons & 0x0001) != 0) GamepadAxisY = 1f;  // Up
                if ((state.Gamepad.wButtons & 0x0002) != 0) GamepadAxisY = -1f; // Down

                GamepadButtonA = (state.Gamepad.wButtons & XINPUT_GAMEPAD_A) != 0;
                GamepadButtonB = (state.Gamepad.wButtons & XINPUT_GAMEPAD_B) != 0;
                GamepadAnyButton = state.Gamepad.wButtons != 0;
            }
            else
            {
                IsGamepadConnected = false;
                GamepadAxisX = 0f;
                GamepadAxisY = 0f;
                GamepadButtonA = false;
                GamepadButtonB = false;
                GamepadAnyButton = false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"InputManager.Update エラー: {ex.Message}");
            IsGamepadConnected = false;
        }
    }

    /// <summary>
    /// ゲームパッドを振動させます。
    /// </summary>
    /// <param name="lowMotor">低周波（重い振動）0.0〜1.0</param>
    /// <param name="highMotor">高周波（軽い振動）0.0〜1.0</param>
    /// <param name="durationMs">振動時間（ミリ秒）</param>
    public void TriggerRumble(float lowMotor, float highMotor, int durationMs)
    {
        if (!IsGamepadConnected) return;

        ushort left = (ushort)Math.Clamp((int)(lowMotor * 65535), 0, 65535);
        ushort right = (ushort)Math.Clamp((int)(highMotor * 65535), 0, 65535);

        SetRumbleInternal(left, right);
        _rumbleEndTime = DateTime.UtcNow.AddMilliseconds(durationMs);
    }

    private void SetRumbleInternal(ushort left, ushort right)
    {
        var vib = new XINPUT_VIBRATION { wLeftMotorSpeed = left, wRightMotorSpeed = right };
        NativeSetState(0, ref vib);
    }

    // 入力方向のヘルパー
    public bool Up => IsKeyDown(Keys.Up) || IsKeyDown(Keys.W) || GamepadAxisY > 0.3f;
    public bool Down => IsKeyDown(Keys.Down) || IsKeyDown(Keys.S) || GamepadAxisY < -0.3f;
    public bool Left => IsKeyDown(Keys.Left) || IsKeyDown(Keys.A) || GamepadAxisX < -0.3f;
    public bool Right => IsKeyDown(Keys.Right) || IsKeyDown(Keys.D) || GamepadAxisX > 0.3f;

    // 攻撃ボタン
    public bool FireAir => IsKeyDown(Keys.Z) || GamepadButtonA;
    public bool FireGround => IsKeyDown(Keys.X) || GamepadButtonB;
    public bool FireBoth => IsKeyDown(Keys.C);

    // デバッグ・スキップ
    public bool BossTestKey => IsKeyDown(Keys.T);
    public bool CityTestKey => IsKeyDown(Keys.D1) || IsKeyDown(Keys.NumPad1);
    public bool FortressTestKey => IsKeyDown(Keys.D2) || IsKeyDown(Keys.NumPad2);

    // 任意の入力があったか
    public bool AnyActionPressed => _pressedKeys.Count > 0 || GamepadAnyButton;
}
