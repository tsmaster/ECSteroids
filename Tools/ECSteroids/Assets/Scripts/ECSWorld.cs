using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using BDG_ECS;
using ECSteroids;



/// <summary>
/// This is the Unity top level behavior that holds everything, runs our systems.
/// </summary>
public class ECSWorld : MonoBehaviour {
    public GameObject PolygonPrefab;

    // these pools should be somewhere else and data driven
    Dictionary<long, ECSteroids.Transform> cmp_transforms = new Dictionary<long, ECSteroids.Transform>();
    Dictionary<long, ECSteroids.Polygon> cmp_polygons = new Dictionary<long, ECSteroids.Polygon>();
    Dictionary<long, ECSteroids.Velocity> cmp_velocitys = new Dictionary<long, ECSteroids.Velocity>();
    Dictionary<long, ECSteroids.InputBuffer> cmp_inputBuffers = new Dictionary<long, ECSteroids.InputBuffer>();
    Dictionary<long, ECSteroids.InputDrivesThisEntityTempTag> cmp_inputTags = new Dictionary<long, ECSteroids.InputDrivesThisEntityTempTag>();
    Dictionary<long, ECSteroids.CollisionDisk> cmp_collisionDisks = new Dictionary<long, CollisionDisk>();
    Dictionary<long, ECSteroids.ShipTag> cmp_shipTags = new Dictionary<long, ShipTag>();
    Dictionary<long, ECSteroids.AsteroidTag> cmp_asteroidTags = new Dictionary<long, AsteroidTag>();

	// Use this for initialization
	void Start () {
        // TODO make this data driven
        //PreallocateComponentPool(ECSteroids.Transform

        MakeShip();

        int asteroidCount = 18;
        long entityID = 1;

        for (int i = 0; i < asteroidCount; ++i) {
            MakeAsteroid(entityID++);
        }

        MakeSingletons();
	}

    void MakeShip()
    {
        long firstEntityID = 0;

        ECSteroids.Transform firstTransform = new ECSteroids.Transform();
        firstTransform.pos = Vector2.zero;
        firstTransform.angle = 0.0f;
        firstTransform.EntityID = firstEntityID;

        ECSteroids.Polygon firstPolygon = new Polygon();
        firstPolygon.points = new List<Vector3>();

        firstPolygon.points.Add(new Vector3(2.0f, 0.0f));
        firstPolygon.points.Add(new Vector3(-1.0f, 1.0f));
        firstPolygon.points.Add(new Vector3(-.5f, 0.0f));
        firstPolygon.points.Add(new Vector3(-1.0f, -1.0f));

        firstPolygon.isClosed = true;
        firstPolygon.unityObject = GameObject.Instantiate(PolygonPrefab);
        firstPolygon.EntityID = firstEntityID;

        ECSteroids.Velocity firstVelocity = new Velocity();
        float firstSpeed = 2.0f;
        firstVelocity.vel = new Vector3(firstSpeed, 0.0f, 0.0f);
        firstVelocity.EntityID = firstEntityID;

        InputDrivesThisEntityTempTag inputTag = new InputDrivesThisEntityTempTag();
        inputTag.EntityID = firstEntityID;

        CollisionDisk disk = new CollisionDisk();
        disk.radius = 1.5f;
        disk.EntityID = firstEntityID;

        ShipTag tag = new ShipTag();
        tag.EntityID = firstEntityID;

        cmp_transforms[firstEntityID] = firstTransform;
        cmp_polygons[firstEntityID] = firstPolygon;
        cmp_velocitys[firstEntityID] = firstVelocity;
        cmp_inputTags[firstEntityID] = inputTag;
        cmp_collisionDisks[firstEntityID] = disk;
        cmp_shipTags[firstEntityID] = tag;
    }

    void MakeAsteroid(long entityID)
    {
        ECSteroids.Transform firstTransform = new ECSteroids.Transform();
        float angle = Random.Range(0.0f, Mathf.PI * 2.0f);
        float x = Mathf.Cos(angle);
        float y = Mathf.Sin(angle);

        float minRad = 7.0f;
        float maxRad = 21.0f;
        float startRad = Random.Range(minRad, maxRad);

        firstTransform.pos = new Vector3(x * startRad, y * startRad, 0.0f);
        firstTransform.angle = 0.0f;
        firstTransform.EntityID = entityID;

        ECSteroids.Polygon firstPolygon = new Polygon();
        firstPolygon.points = new List<Vector3>();

        float asteroidRadius = 1.5f;
        float variance = 0.4f;
        int points = 12;

        for (int i = 0; i < points; ++i) {
            float ast_angle = MapRange(i, 0, points, 0, Mathf.PI * 2);
            float ast_x = Mathf.Cos(ast_angle);
            float ast_y = Mathf.Sin(ast_angle);

            float r = Random.Range(asteroidRadius - variance, asteroidRadius + variance);

            firstPolygon.points.Add(new Vector3(ast_x * r, ast_y * r));
        }

        firstPolygon.isClosed = true;
        firstPolygon.unityObject = GameObject.Instantiate(PolygonPrefab);
        firstPolygon.EntityID = entityID;

        ECSteroids.Velocity firstVelocity = new Velocity();
        float firstSpeed = Random.Range(1.0f, 3.0f);

        float vAng = Random.Range(0, Mathf.PI * 2);
        firstVelocity.vel = new Vector3(Mathf.Cos(vAng) * firstSpeed, Mathf.Sin(vAng) * firstSpeed, 0.0f);
        firstVelocity.EntityID = entityID;

        CollisionDisk disk = new CollisionDisk();
        disk.radius = asteroidRadius;
        disk.EntityID = entityID;

        AsteroidTag asteroidTag = new AsteroidTag();
        asteroidTag.EntityID = entityID;

        cmp_transforms[entityID] = firstTransform;
        cmp_polygons[entityID] = firstPolygon;
        cmp_velocitys[entityID] = firstVelocity;
        cmp_collisionDisks[entityID] = disk;
        cmp_asteroidTags[entityID] = asteroidTag;
    }

    void MakeSingletons()
    {
        long NO_ENTITY = -1;
        ECSteroids.InputBuffer inputBuffer = new InputBuffer();
        inputBuffer.Reset();
        cmp_inputBuffers[NO_ENTITY] = inputBuffer;
    }

    float MapRange(float value, float inMin, float inMax, float outMin, float outMax)
    {
        float norm = (value - inMin) / (inMax - inMin);
        return (outMax - outMin) * norm + outMin;
    }
	
	// Update is called once per frame
	void Update () {
        // foreach system, system.Tick();
        ReadInputSystemTick();
        ApplyInputSystemTick();
        PhysicsSystemTick();
        CollisionDetectionSystemTick();
        PolygonSystemTick();

        DebugTick();
	}

    void DebugTick()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Randomizing");

            foreach (InputDrivesThisEntityTempTag inputTag in cmp_inputTags.Values) {
                long oldEntity = inputTag.EntityID;
                Debug.Log("old id: " + oldEntity);
                cmp_inputTags.Remove(oldEntity);

                long[] entityIDs = new long[cmp_transforms.Count];
                cmp_transforms.Keys.CopyTo(entityIDs, 0);
                int newIndex = Random.Range(0, entityIDs.Length);
                long newID = entityIDs[newIndex];
                Debug.Log("new id: " + newID);
                inputTag.EntityID = newID;
                cmp_inputTags[newID] = inputTag;
                break;
            }
        }
    }

    void ReadInputSystemTick()
    {
        // todo provide more abstraction here, also, read more buttons
        foreach (InputBuffer i in cmp_inputBuffers.Values) {
            i.steer = Input.GetAxis("Horizontal");
            i.throttle = Input.GetAxis("Vertical");
            i.isShooting = Input.GetButton("Fire1");
        }
    }

    void ApplyInputSystemTick()
    {
        // TODO support data-driven key bindings

        // TODO make this data driven somewhere
        const float STEER_RATE = -90.0f;
        const float THRUST_RATE = 0.1f;

        InputBuffer ib = cmp_inputBuffers[-1];
        foreach (InputDrivesThisEntityTempTag iTag in cmp_inputTags.Values) {
            long entity = iTag.EntityID;
            ECSteroids.Transform t = cmp_transforms[entity];
            ECSteroids.Velocity v = cmp_velocitys[entity];

            t.angle += ib.steer * Time.deltaTime * STEER_RATE;

            if (ib.throttle > 0.0f) {
                float angleRad = t.angle * Mathf.PI / 180.0f;
                float cx = Mathf.Cos(angleRad);
                float cy = Mathf.Sin(angleRad);
                v.vel += new Vector3(cx * THRUST_RATE, cy * THRUST_RATE, 0.0f);
            }
        }
    }

    void PhysicsSystemTick()
    {
        // todo put these into a component
        // see also https://answers.unity.com/questions/463453/2d-camera-size-in-orthographic-projection-vs-scale.html
        // for a discussion of how scale and ortho camera "size" fit together

        float aspectRatio = 1024.0f / 768.0f;
        float windowHeight = 48.0f;
        float windowWidth = windowHeight * aspectRatio;

        float minX = -windowWidth * 0.5f;
        float maxX = windowWidth * 0.5f;
        float minY = -windowHeight * 0.5f;
        float maxY = windowHeight * 0.5f;

        foreach (Velocity v in cmp_velocitys.Values) {
            ECSteroids.Transform t = cmp_transforms[v.EntityID];
            // TODO put this into some sort of data component
            t.pos += v.vel * Time.deltaTime;

            if (t.pos.x < minX) {
                t.pos.x += (maxX - minX);
            }
            if (t.pos.x > maxX) {
                t.pos.x -= (maxX - minX);
            }
            if (t.pos.y < minY) {
                t.pos.y += (maxY - minY);
            }
            if (t.pos.y > maxY) {
                t.pos.y -= (maxY - minY);
            }
        }
    }

    void CollisionDetectionSystemTick()
    {
        foreach (ShipTag st in cmp_shipTags.Values) {
            long shipEntity = st.EntityID;
            CollisionDisk shipDisk = cmp_collisionDisks[shipEntity];
            ECSteroids.Transform shipTransform = cmp_transforms[shipEntity];

            foreach (AsteroidTag at in cmp_asteroidTags.Values) {
                long asteroidEntity = at.EntityID;
                CollisionDisk asteroidDisk = cmp_collisionDisks[asteroidEntity];
                ECSteroids.Transform asteroidTransform = cmp_transforms[asteroidEntity];

                float thresh = asteroidDisk.radius + shipDisk.radius;
                float threshSqr = thresh * thresh;

                if ((asteroidTransform.pos - shipTransform.pos).sqrMagnitude < threshSqr) {
                    Debug.Log(System.String.Format("found collision between {0} and {1}", shipEntity, asteroidEntity));
                }
            }
        }
    }

    void PolygonSystemTick()
    {
        Vector3 zAxis = new Vector3(0.0f, 0.0f, 1.0f);

        foreach (Polygon p in cmp_polygons.Values) {
            ECSteroids.Transform t = cmp_transforms[p.EntityID];
            p.unityObject.transform.SetPositionAndRotation(t.pos, Quaternion.AngleAxis(t.angle, zAxis));

            LineRenderer lRend = p.unityObject.GetComponent<LineRenderer>();
            lRend.positionCount = p.points.Count;
            lRend.SetPositions(p.points.ToArray());

            float constWidth = 0.1f;
            lRend.startWidth = lRend.endWidth = constWidth;

            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0.0f, 1.0f);
            curve.AddKey(1.0f, 1.0f);
            lRend.widthCurve = curve;
            lRend.widthMultiplier = constWidth;
        }
    }
}
