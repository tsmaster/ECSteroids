using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vectrosity;

public class VectrosityPolygon : MonoBehaviour {
    List<Vector3> Points;
    public Color Color = Color.white;
    public float Width = 2.0f;
    VectorLine Line;

	// Use this for initialization
	void Start () {
        Debug.Log("making vectrosity polygon component");
        Points = new List<Vector3>();
        Points.Add(new Vector3(0.0f, 0.0f, 0.0f));
        Points.Add(new Vector3(1.0f, 0.0f, 0.0f));
        Points.Add(new Vector3(0.0f, 1.0f, 0.0f));

        int r = Random.Range(100, 10000);
        Line = new VectorLine("polygon" + r, Points, Width, LineType.Continuous);
        Line.color = Color;
	}
	
	// Update is called once per frame
	void Update () {
        Line.drawTransform = this.gameObject.transform;
        Line.Draw();
        //Line.Draw3D();
	}

    // called when destroying
    void OnDestroy() {
        VectorLine.Destroy(ref Line);
    }

    public void SetPoints(List<Vector3> points)
    {
        if (Points != null) {
            Points.Clear();
            Points.AddRange(points);
        }
    }

    public void SetColor(Color color)
    {
        this.Color = color;
        if (Line != null) {
            Line.color = color;
        }
    }
}
