using UnityEngine;

public class SixPackInstruments : MonoBehaviour
{
    [Header("Six Pack Instruments")]
    [SerializeField] private FlightInstrument airspeedIndicator;
    [SerializeField] private FlightInstrument attitudeIndicator;
    [SerializeField] private FlightInstrument altimeter;
    [SerializeField] private FlightInstrument turnCoordinator;
    [SerializeField] private FlightInstrument headingIndicator;
    [SerializeField] private FlightInstrument verticalSpeedIndicator;

    [Header("Data Source")]
    [SerializeField] private Rigidbody aircraftRigidbody;
    [SerializeField] private Transform aircraftTransform;

    [Header("Settings")]
    [SerializeField] private float altitudeMultiplier = 3.28084f; // meters to feet
    [SerializeField] private float speedMultiplier = 1.94384f; // m/s to knots

    private Vector3 lastVelocity;
    private float lastAltitude;

    private void Update()
    {
        if (aircraftTransform == null) return;

        UpdateAirspeed();
        UpdateAttitude();
        UpdateAltimeter();
        UpdateTurnCoordinator();
        UpdateHeading();
        UpdateVerticalSpeed();

        lastVelocity = aircraftRigidbody != null ? aircraftRigidbody.linearVelocity : Vector3.zero;
        lastAltitude = aircraftTransform.position.y;
    }

    private void UpdateAirspeed()
    {
        if (airspeedIndicator == null || aircraftRigidbody == null) return;
        float speed = aircraftRigidbody.linearVelocity.magnitude * speedMultiplier;
        airspeedIndicator.SetTargetValue(speed);
    }

    private void UpdateAttitude()
    {
        if (attitudeIndicator == null || aircraftTransform == null) return;
        float pitch = aircraftTransform.eulerAngles.x;
        if (pitch > 180f) pitch -= 360f;
        attitudeIndicator.SetTargetValue(pitch);
    }

    private void UpdateAltimeter()
    {
        if (altimeter == null || aircraftTransform == null) return;
        float altitude = aircraftTransform.position.y * altitudeMultiplier;
        altimeter.SetTargetValue(altitude);
    }

    private void UpdateTurnCoordinator()
    {
        if (turnCoordinator == null || aircraftTransform == null) return;
        float roll = aircraftTransform.eulerAngles.z;
        if (roll > 180f) roll -= 360f;
        turnCoordinator.SetTargetValue(roll);
    }

    private void UpdateHeading()
    {
        if (headingIndicator == null || aircraftTransform == null) return;
        float heading = aircraftTransform.eulerAngles.y;
        headingIndicator.SetTargetValue(heading);
    }

    private void UpdateVerticalSpeed()
    {
        if (verticalSpeedIndicator == null || aircraftTransform == null) return;
        float vs = (aircraftTransform.position.y - lastAltitude) / Time.deltaTime * altitudeMultiplier * 60f; // ft/min
        verticalSpeedIndicator.SetTargetValue(vs);
    }
}
