// ContrailConditions.cs — SWEF Contrail & Exhaust Trail System (Phase 71)
using UnityEngine;

namespace SWEF.Contrail
{
    /// <summary>
    /// ScriptableObject that models the atmospheric conditions required for
    /// condensation contrail formation.
    ///
    /// <para>Author this asset via
    /// <c>Assets → Create → SWEF → Contrail → Contrail Conditions</c>
    /// and assign it to <see cref="ContrailManager.conditions"/>.</para>
    /// </summary>
    [CreateAssetMenu(fileName = "ContrailConditions", menuName = "SWEF/Contrail/Contrail Conditions")]
    public class ContrailConditions : ScriptableObject
    {
        #region Inspector

        [Header("Altitude Curves")]
        [Tooltip("Air temperature (°C) as a function of altitude (metres). Typical value: −56 °C above 11 000 m.")]
        /// <summary>Air temperature (°C) vs altitude (metres) curve.</summary>
        public AnimationCurve temperatureByAltitude = AnimationCurve.Linear(0f, 15f, 15000f, -56f);

        [Tooltip("Relative humidity (0–1) as a function of altitude (metres).")]
        /// <summary>Relative humidity (0–1) vs altitude (metres) curve.</summary>
        public AnimationCurve humidityByAltitude = AnimationCurve.Linear(0f, 0.8f, 15000f, 0.3f);

        [Header("Formation Thresholds")]
        [Tooltip("Minimum altitude (metres) at which condensation contrails can form.")]
        /// <summary>Minimum altitude (metres) at which condensation contrails can form.</summary>
        public float contrailMinAltitude = ContrailConfig.MinContrailAltitude;

        [Tooltip("Maximum altitude (metres) above which the atmosphere is too thin for visible contrails.")]
        /// <summary>Maximum altitude (metres) above which the atmosphere is too thin for visible contrails.</summary>
        public float contrailMaxAltitude = ContrailConfig.MaxContrailAltitude;

        [Tooltip("Air temperature threshold (°C). Below this value contrails form.")]
        /// <summary>Air temperature threshold (°C). Below this value contrails form.</summary>
        public float contrailTemperatureThreshold = ContrailConfig.ContrailTempThreshold;

        [Tooltip("Relative humidity (0–1) above which contrails persist rather than quickly evaporating.")]
        /// <summary>Relative humidity (0–1) above which contrails persist.</summary>
        [Range(0f, 1f)]
        public float humidityThreshold = ContrailConfig.ContrailHumidityThreshold;

        [Header("Trail Behaviour")]
        [Tooltip("Seconds of delay between the aircraft position and the point where the trail first becomes visible.")]
        /// <summary>Seconds of delay before the trail appears behind the aircraft.</summary>
        [Min(0f)]
        public float contrailFormationDelay = ContrailConfig.FormationDelay;

        #endregion

        #region Public API

        /// <summary>
        /// Determines whether a condensation contrail should form under the given
        /// atmospheric conditions.
        /// </summary>
        /// <param name="altitude">Aircraft altitude in metres.</param>
        /// <param name="temperature">Ambient air temperature in °C.</param>
        /// <param name="humidity">Relative humidity in the range [0, 1].</param>
        /// <returns>
        /// <c>true</c> when altitude is within the contrail band, temperature is
        /// below the threshold, and humidity exceeds the threshold.
        /// </returns>
        public bool ShouldFormContrail(float altitude, float temperature, float humidity)
        {
            return altitude >= contrailMinAltitude
                && altitude <= contrailMaxAltitude
                && temperature <= contrailTemperatureThreshold
                && humidity >= humidityThreshold;
        }

        /// <summary>
        /// Calculates a normalised contrail intensity (0–1) based on how far the
        /// current atmospheric conditions exceed each formation threshold.
        /// </summary>
        /// <param name="altitude">Aircraft altitude in metres.</param>
        /// <param name="temperature">Ambient air temperature in °C.</param>
        /// <param name="humidity">Relative humidity in the range [0, 1].</param>
        /// <returns>
        /// A value in [0, 1] where 0 means no contrail and 1 means maximum intensity.
        /// Returns 0 when <see cref="ShouldFormContrail"/> returns <c>false</c>.
        /// </returns>
        public float GetContrailIntensity(float altitude, float temperature, float humidity)
        {
            if (!ShouldFormContrail(altitude, temperature, humidity))
                return 0f;

            // Altitude factor: strongest in mid-band, tapering at the edges.
            float altMid = (contrailMinAltitude + contrailMaxAltitude) * 0.5f;
            float altRange = (contrailMaxAltitude - contrailMinAltitude) * 0.5f;
            float altFactor = 1f - Mathf.Abs(altitude - altMid) / altRange;
            altFactor = Mathf.Clamp01(altFactor);

            // Temperature factor: colder = stronger contrail.
            float tempFactor = Mathf.Clamp01(
                (contrailTemperatureThreshold - temperature) / Mathf.Abs(contrailTemperatureThreshold));

            // Humidity factor: more humid = more persistent / visible.
            float humFactor = Mathf.Clamp01(
                (humidity - humidityThreshold) / (1f - humidityThreshold));

            return altFactor * Mathf.Lerp(0.5f, 1f, tempFactor) * Mathf.Lerp(0.5f, 1f, humFactor);
        }

        /// <summary>
        /// Samples the temperature curve at the given altitude.
        /// </summary>
        /// <param name="altitude">Altitude in metres.</param>
        /// <returns>Estimated air temperature in °C.</returns>
        public float GetTemperatureAtAltitude(float altitude)
        {
            return temperatureByAltitude.Evaluate(altitude);
        }

        /// <summary>
        /// Samples the humidity curve at the given altitude.
        /// </summary>
        /// <param name="altitude">Altitude in metres.</param>
        /// <returns>Estimated relative humidity in [0, 1].</returns>
        public float GetHumidityAtAltitude(float altitude)
        {
            return Mathf.Clamp01(humidityByAltitude.Evaluate(altitude));
        }

        #endregion
    }
}
