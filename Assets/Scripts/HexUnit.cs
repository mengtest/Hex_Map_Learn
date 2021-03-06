﻿using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;

public class HexUnit: MonoBehaviour {

    const int visionRange = 3;

    public HexCell Location {
        get {
            return location;
        }
        set {
            if ( location ) {
                Grid.DecreaseVisibility(location, visionRange);
                location.Unit = null;
            }
            location = value;
            value.Unit = this;
            Grid.IncreaseVisibility(value, visionRange);
            transform.localPosition = value.Position;
        }
    }

    public float Orientation {
        get {
            return orientation;
        }
        set {
            orientation = value;
            transform.localRotation = Quaternion.Euler(0f, value, 0f);
        }
    }

    public HexGrid Grid { get; set; }

    public static HexUnit unitPrefab;

    const float travelSpeed = 4f;
    const float rotationSpeed = 180f;

    HexCell location, currentTravelLoaction;
    float orientation;

    List<HexCell> pathToTravel;

    public bool IsValidDestination( HexCell cell ) {
        return !cell.IsUnderwater && !cell.Unit;
    }

    public void ValidateLocation() {
        transform.localPosition = location.Position;
    }

    public void Die() {
        if ( location ) {
            Grid.DecreaseVisibility(location, visionRange);
        }
        location.Unit = null;
        Destroy(gameObject);
    }

    public void Save( BinaryWriter writer ) {
        location.coordinates.Save(writer);
        writer.Write(orientation);
    }

    public static void Load( BinaryReader reader, HexGrid grid ) {
        HexCoordinates coor = HexCoordinates.Load(reader);
        float orientation = reader.ReadSingle();

        grid.AddUnit(
                Instantiate(HexUnit.unitPrefab), grid.GetCell(coor), orientation
        );
    }

    public void Travel( List<HexCell> path ) {
        //Location = path[path.Count - 1];
        location.Unit = null;
        location = path[path.Count - 1];
        location.Unit = this;

        pathToTravel = path;
        StopAllCoroutines();
        StartCoroutine(TravelPath());
    }

    private void OnDrawGizmos() {
        if ( pathToTravel == null || pathToTravel.Count == 0 ) {
            return;
        }

        Vector3 a, b, c = pathToTravel[0].Position;
        for ( int i = 1; i < pathToTravel.Count; i++ ) {
            a = c;
            b = pathToTravel[i - 1].Position;
            c = ( b + pathToTravel[i].Position ) * 0.5f;
            for ( float t = 0f; t < 1f; t += 0.1f ) {
                Gizmos.DrawSphere(Bezier.Getpoint(a, b, c, t), 2f);
            }
        }
        a = c;
        b = pathToTravel[pathToTravel.Count - 1].Position;
        c = b;
        for ( float t = 0f; t < 1f; t += 0.1f ) {
            Gizmos.DrawSphere(Bezier.Getpoint(a, b, c, t), 2f);
        }
    }

    IEnumerator TravelPath() {
        Vector3 a, b, c = pathToTravel[0].Position;
        //transform.localPosition = c;
        yield return LookAt(pathToTravel[1].Position);
        Grid.DecreaseVisibility(
            currentTravelLoaction ? currentTravelLoaction : pathToTravel[0],
            visionRange
        );

        float t = Time.deltaTime * travelSpeed;
        for ( int i = 1; i < pathToTravel.Count; i++ ) {
            currentTravelLoaction = pathToTravel[i];
            a = c;
            b = pathToTravel[i - 1].Position;
            c = ( b + currentTravelLoaction.Position ) * 0.5f;
            Grid.IncreaseVisibility(pathToTravel[i], visionRange);
            for ( ; t < 1f; t += Time.deltaTime * travelSpeed ) {
                transform.localPosition = Bezier.Getpoint(a, b, c, t);
                Vector3 d = Bezier.GetDerivative(a, b, c, t);
                d.y = 0;
                transform.localRotation = Quaternion.LookRotation(d);
                yield return null;
            }
            Grid.DecreaseVisibility(pathToTravel[i], visionRange);
            t -= 1f;
        }
        currentTravelLoaction = null;
        a = c;
        b = location.Position;
        c = b;
        Grid.IncreaseVisibility(location, visionRange);
        for ( ; t < 1f; t += Time.deltaTime * travelSpeed ) {
            transform.localPosition = Bezier.Getpoint(a, b, c, t);
            Vector3 d = Bezier.GetDerivative(a, b, c, t);
            d.y = 0;
            transform.localRotation = Quaternion.LookRotation(d);
            yield return null;
        }
        transform.localPosition = location.Position;
        orientation = transform.localRotation.eulerAngles.y;

        ListPool<HexCell>.Add(pathToTravel);
        pathToTravel = null;
    }

    private void OnEnable() {
        if ( location ) {
            transform.localPosition = location.Position;
            if( currentTravelLoaction ) {
                Grid.IncreaseVisibility(location, visionRange);
                Grid.DecreaseVisibility(currentTravelLoaction, visionRange);
                currentTravelLoaction = null;
            }
        }
    }

    IEnumerator LookAt( Vector3 point ) {
        point.y = transform.localPosition.y;
        Quaternion fromRotation = transform.localRotation;
        Quaternion toRotation = Quaternion.LookRotation(point - transform.localPosition);
        float angle = Quaternion.Angle(fromRotation, toRotation);

        if ( angle > 0f ) {
            float speed = rotationSpeed / angle;
            for ( float t = Time.deltaTime * speed; t < 1f; t += Time.deltaTime * speed ) {
                transform.localRotation = Quaternion.Slerp(fromRotation, toRotation, t);
                yield return null;
            }
        }

        transform.LookAt(point);
        orientation = transform.localRotation.eulerAngles.y;     
    }
}