// HandTrackingCalibration.cs — Phase 112: VR/XR Flight Experience
// Per-user hand size calibration, dominant hand setting, sensitivity adjustment.
// Namespace: SWEF.XR

using System;
using UnityEngine;

namespace SWEF.XR
{
    /// <summary>
    /// Manages per-user hand calibration for accurate gesture recognition.
    /// Measures palm width and finger length, then applies scale factors.
    /// Settings are persisted to PlayerPrefs.
    /// </summary>
    public class HandTrackingCalibration : MonoBehaviour
    {
        // ── PlayerPrefs keys ──────────────────────────────────────────────────────
        private const string KeyDominantHand      = "SWEF_XR_DominantHand";
        private const string KeyPalmWidth         = "SWEF_XR_PalmWidth";
        private const string KeyFingerLength      = "SWEF_XR_FingerLength";
        private const string KeyGestureSensitivity = "SWEF_XR_GestureSensitivity";
        private const string KeyIsCalibrated      = "SWEF_XR_IsCalibrated";

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Current calibration data.</summary>
        public XRHandCalibrationData CalibrationData { get; private set; } = new XRHandCalibrationData();

        /// <summary>Whether calibration has been completed.</summary>
        public bool IsCalibrated => CalibrationData.IsCalibrated;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when calibration completes successfully.</summary>
        public event Action<XRHandCalibrationData> OnCalibrationComplete;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            LoadCalibration();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Sets the dominant hand preference and saves it.</summary>
        public void SetDominantHand(XRHandedness hand)
        {
            CalibrationData.DominantHand = hand;
            SaveCalibration();
            Debug.Log($"[SWEF] HandTrackingCalibration: Dominant hand set to {hand}.");
        }

        /// <summary>Sets gesture sensitivity multiplier [0.5..2] and saves it.</summary>
        public void SetGestureSensitivity(float sensitivity)
        {
            CalibrationData.GestureSensitivity = Mathf.Clamp(sensitivity, 0.5f, 2f);
            SaveCalibration();
        }

        /// <summary>
        /// Performs measurement calibration from a hand state snapshot.
        /// Computes palm width from wrist-to-little-metacarpal distance.
        /// </summary>
        public void CalibrateFromHandState(XRHandState state)
        {
            if (!state.IsTracked || state.FingerTips == null || state.FingerTips.Length < 5)
            {
                Debug.LogWarning("[SWEF] HandTrackingCalibration: Cannot calibrate — hand not tracked.");
                return;
            }

            // Estimate palm width: distance between thumb tip and pinky tip projected
            float estimatedPalm = Vector3.Distance(state.FingerTips[0], state.FingerTips[4]);
            CalibrationData.PalmWidthMetres = Mathf.Clamp(estimatedPalm, 0.06f, 0.12f);

            // Estimate index finger length: palm centre to index tip
            float estimatedFinger = Vector3.Distance(state.PalmPosition, state.FingerTips[1]);
            CalibrationData.FingerLengthMetres = Mathf.Clamp(estimatedFinger, 0.05f, 0.10f);

            CalibrationData.IsCalibrated = true;
            SaveCalibration();
            OnCalibrationComplete?.Invoke(CalibrationData);
            Debug.Log("[SWEF] HandTrackingCalibration: Calibration complete.");
        }

        /// <summary>Resets calibration to default values.</summary>
        public void ResetCalibration()
        {
            CalibrationData = new XRHandCalibrationData();
            SaveCalibration();
            Debug.Log("[SWEF] HandTrackingCalibration: Calibration reset.");
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void SaveCalibration()
        {
            PlayerPrefs.SetInt(KeyDominantHand,        (int)CalibrationData.DominantHand);
            PlayerPrefs.SetFloat(KeyPalmWidth,          CalibrationData.PalmWidthMetres);
            PlayerPrefs.SetFloat(KeyFingerLength,       CalibrationData.FingerLengthMetres);
            PlayerPrefs.SetFloat(KeyGestureSensitivity, CalibrationData.GestureSensitivity);
            PlayerPrefs.SetInt(KeyIsCalibrated,         CalibrationData.IsCalibrated ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void LoadCalibration()
        {
            CalibrationData.DominantHand        = (XRHandedness)PlayerPrefs.GetInt(KeyDominantHand, (int)XRHandedness.Right);
            CalibrationData.PalmWidthMetres     = PlayerPrefs.GetFloat(KeyPalmWidth,          0.085f);
            CalibrationData.FingerLengthMetres  = PlayerPrefs.GetFloat(KeyFingerLength,       0.075f);
            CalibrationData.GestureSensitivity  = PlayerPrefs.GetFloat(KeyGestureSensitivity, 1f);
            CalibrationData.IsCalibrated        = PlayerPrefs.GetInt(KeyIsCalibrated, 0) == 1;
        }
    }
}
