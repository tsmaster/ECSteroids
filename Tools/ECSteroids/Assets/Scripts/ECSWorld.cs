using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vectrosity;

using BDG_ECS;
using ECSteroids;



/// <summary>
/// This is the Unity top level behavior that holds everything, runs our systems.
/// </summary>
public class ECSWorld : MonoBehaviour {
    public const long NO_ENTITY = -1;

    public enum ECSteroidsGameState {
        NONE,
        Boot,
        Title,
        Credits,
        About,
        Backstory,
        Gameplay,
        GameOver,
    };

    // these pools should be somewhere else and data driven
    // also, probably a big dictionary: componentDict, keyed by a component enum, which then has values of dictionaries keying entity ID to components.

    Dictionary<long, ECSteroids.Transform> cmp_transforms = new Dictionary<long, ECSteroids.Transform>();
    Dictionary<long, ECSteroids.Polygon> cmp_polygons = new Dictionary<long, ECSteroids.Polygon>();
    Dictionary<long, ECSteroids.Velocity> cmp_velocitys = new Dictionary<long, ECSteroids.Velocity>();
    Dictionary<long, ECSteroids.InputBuffer> cmp_inputBuffers = new Dictionary<long, ECSteroids.InputBuffer>();
    Dictionary<long, ECSteroids.InputDrivesThisEntityTempTag> cmp_inputTags = new Dictionary<long, ECSteroids.InputDrivesThisEntityTempTag>();
    Dictionary<long, ECSteroids.CollisionDisk> cmp_collisionDisks = new Dictionary<long, CollisionDisk>();
    Dictionary<long, ECSteroids.ShipTag> cmp_shipTags = new Dictionary<long, ShipTag>();
    Dictionary<long, ECSteroids.AsteroidTag> cmp_asteroidTags = new Dictionary<long, AsteroidTag>();
    Dictionary<long, ECSteroids.BulletTag> cmp_bulletTags = new Dictionary<long, BulletTag>();
    Dictionary<long, ECSteroids.EntityLifetime> cmp_entityLifetimes = new Dictionary<long, EntityLifetime>();
    Dictionary<long, ECSteroids.PlayerData> cmp_playerDatas = new Dictionary<long, PlayerData>();
    Dictionary<long, ECSteroids.GameState> cmp_gameStates = new Dictionary<long, GameState>();
    Dictionary<long, ECSteroids.PendingGameState> cmp_pendingGameStates = new Dictionary<long, PendingGameState>();
    Dictionary<long, ECSteroids.TextMessage> cmp_textMessages = new Dictionary<long, TextMessage>();
    public Dictionary<long, ECSteroids.BootStateTag> cmp_bootStateTags = new Dictionary<long, BootStateTag>();
    public Dictionary<long, ECSteroids.TitleStateTag> cmp_titleStateTags = new Dictionary<long, TitleStateTag>();
    public Dictionary<long, ECSteroids.GameplayStateTag> cmp_gameplayStateTags = new Dictionary<long, GameplayStateTag>();
    Dictionary<long, ECSteroids.TemporaryInvulnerability> cmp_temporaryInvulnerabilitys = new Dictionary<long, TemporaryInvulnerability>();
    public Dictionary<long, ECSteroids.LevelDesc> cmp_levelDescs = new Dictionary<long, LevelDesc>();
    // also add to destroy

    List<long> UnusedEntityIDs = new List<long>();

    List<long> destroyQueue = new List<long>();

    VectorLine myTextVectorLine;

    ECSteroidsGameState GetGameState()
    {
        foreach (GameState gameState in cmp_gameStates.Values) {
            return gameState.currentState;
        }

        return ECSteroidsGameState.NONE;
    }

    void SetGameState(ECSteroidsGameState newState)
    {
        Debug.Log("SetGameState requested for " + newState);
        ECSteroidsGameState currentState = GetGameState();
        Debug.Log("current state " + currentState);
        switch (currentState) {
        case ECSteroidsGameState.Boot:
            BootState.Exit(this);
            break;
        case ECSteroidsGameState.Title:
            TitleState.Exit(this);
            break;
        case ECSteroidsGameState.Credits:
            CreditsState.Exit(this);
            break;
        case ECSteroidsGameState.About:
            AboutState.Exit(this);
            break;
        case ECSteroidsGameState.Backstory:
            BackStoryState.Exit(this);
            break;
        case ECSteroidsGameState.Gameplay:
            GamePlayState.Exit(this);
            break;
        case ECSteroidsGameState.GameOver:
            GameOverState.Exit(this);
            break;
        }

        if (currentState == ECSteroidsGameState.NONE) {
            GameState gameState = new GameState();
            gameState.EntityID = NO_ENTITY;
            cmp_gameStates[NO_ENTITY] = gameState;
        }

        foreach (GameState gameState in cmp_gameStates.Values) {
            gameState.currentState = newState;
        }

        switch (newState) {
        case ECSteroidsGameState.Boot:
            BootState.Enter(this);
            break;
        case ECSteroidsGameState.Title:
            TitleState.Enter(this);
            break;
        case ECSteroidsGameState.Credits:
            CreditsState.Enter(this);
            break;
        case ECSteroidsGameState.About:
            AboutState.Enter(this);
            break;
        case ECSteroidsGameState.Backstory:
            BackStoryState.Enter(this);
            break;
        case ECSteroidsGameState.Gameplay:
            GamePlayState.Enter(this);
            break;
        case ECSteroidsGameState.GameOver:
            GameOverState.Enter(this);
            break;
        }
    }

    public void QueueStateChange(ECSteroidsGameState newState, float secondsFromNow)
    {
        PendingGameState pend = new PendingGameState();
        pend.nextState = newState;
        pend.secondsRemaining = secondsFromNow;
        pend.EntityID = NO_ENTITY;
        cmp_pendingGameStates[NO_ENTITY] = pend;
    }

    public long AddTextMessage(string message, Vector3 pos, float scale, Color color, float width, BaseComponent stateTag)
    {
        long eid = GetUnusedEntityID();
        Debug.Log("adding text message " + message);
        Debug.Log("text message EID " + eid);
        TextMessage msg = new TextMessage();
        msg.EntityID = eid;
        msg.text = message;
        msg.pos = pos;
        msg.scale = scale;
        msg.color = color;
        msg.width = width;
        msg.vectLine = new VectorLine("message " + eid + " " + message, new List<Vector3>(), width);
        msg.vectLine.MakeText(message, pos, scale);
        msg.vectLine.color = color;
        cmp_textMessages[eid] = msg;

        if (stateTag != null) {
            stateTag.EntityID = eid;
        }
        return eid;
    }

    bool isWaveComplete()
    {
        foreach (AsteroidTag t in cmp_asteroidTags.Values) {
            return false;
        }

        // TODO include other objects, like flying saucers, coins

        return true;
    }

    public long GetUnusedEntityID()
    {
        if (UnusedEntityIDs.Count > 0) {
            long val = UnusedEntityIDs[0];
            UnusedEntityIDs.RemoveAt(0);
            return val;
        }

        return GetHighestUsedEntityID() + 1;
    }

    long GetHighestUsedEntityID()
    {
        // todo ungrossify this

        long entityID = NO_ENTITY;

        foreach (long ev in cmp_shipTags.Keys) {
            if (ev > entityID) {
                entityID = ev;
            }
        }
        foreach (long ev in cmp_asteroidTags.Keys) {
            if (ev > entityID) {
                entityID = ev;
            }
        }
        foreach (long ev in cmp_bulletTags.Keys) {
            if (ev > entityID) {
                entityID = ev;
            }
        }
        foreach (long tv in cmp_textMessages.Keys) {
            if (tv > entityID) {
                entityID = tv;
            }
        }
        foreach (long ev in cmp_levelDescs.Keys) {
            if (ev > entityID) {
                entityID = ev;
            }
        }

        return entityID;
    }

	// Use this for initialization
	void Start () {
        // TODO make this data driven
        //PreallocateComponentPool(ECSteroids.Transform

        SetGameState(ECSteroidsGameState.Boot);

        // TODO move most of this to GamePlayState.Enter


        MakeSingletons();
            
        // this should go into a gameplay score display component
        List<Vector3> textPoints = new List<Vector3>();
        textPoints.Add(new Vector3(0, 0, 0));
        textPoints.Add(new Vector3(10, 10, 0));
        myTextVectorLine = new VectorLine("textVectorLine", textPoints, 1.0f);
        myTextVectorLine.color = new Color(0, 0.8f, 0);
        myTextVectorLine.MakeText("Hello, ECS-teroids", new Vector3(0.0f, 20.0f, 0.0f), 1.0f);
	}

    public void MakeShip()
    {
        long firstEntityID = GetUnusedEntityID();

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
        firstPolygon.points.Add(new Vector3(2.0f, 0.0f));

        firstPolygon.color = Color.green;
        firstPolygon.lineWidth = 2.0f;
        firstPolygon.isDirty = true;
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

        // TODO this belongs outside the ship
        PlayerData playerData = new PlayerData();
        playerData.score = 0;
        playerData.lives = 3;
        playerData.waveIndex = 1;
        playerData.EntityID = firstEntityID;

        TemporaryInvulnerability tmpInv = new TemporaryInvulnerability();
        tmpInv.EntityID = firstEntityID;
        tmpInv.secondsRemaining = 3.0f;

        GameplayStateTag gpsTag = new GameplayStateTag();
        gpsTag.EntityID = firstEntityID;

        cmp_transforms[firstEntityID] = firstTransform;
        cmp_polygons[firstEntityID] = firstPolygon;
        cmp_velocitys[firstEntityID] = firstVelocity;
        cmp_inputTags[firstEntityID] = inputTag;
        cmp_collisionDisks[firstEntityID] = disk;
        cmp_shipTags[firstEntityID] = tag;
        cmp_playerDatas[firstEntityID] = playerData;
        cmp_temporaryInvulnerabilitys[firstEntityID] = tmpInv;
        cmp_gameplayStateTags[firstEntityID] = gpsTag;
    }

    public void PopulateForWave()
    {
        int asteroidCount = 0;
        int waveIndex = -1;

        foreach (PlayerData pd in cmp_playerDatas.Values) {
            waveIndex = pd.waveIndex;
            break;
        }

        if (waveIndex == -1) {
            return;
        }

        Debug.Log("wave index: "+ waveIndex);
        long eid = LevelFactory.GetLevelDescByWaveIndex(this, waveIndex);
        Debug.Log("level desc id: " + eid);

        LevelDesc ld = cmp_levelDescs[eid];
        asteroidCount = ld.numAsteroids;

        for (int i = 0; i < asteroidCount; ++i) {
            MakeInitialAsteroid();
        }

        long msgid = AddTextMessage(ld.name, Vector3.zero, 1.0f, Color.green, 1.0f, null);
        EntityLifetime elt = new EntityLifetime();
        elt.EntityID = msgid;
        elt.secondsRemaining = 2.0f;
        cmp_entityLifetimes[msgid] = elt;
    }

    public void MakeInitialAsteroid()
    {
        float angle = Random.Range(0.0f, Mathf.PI * 2.0f);
        float x = Mathf.Cos(angle);
        float y = Mathf.Sin(angle);

        float minRad = 7.0f;
        float maxRad = 21.0f;
        float startRad = Random.Range(minRad, maxRad);

        Vector3 pos = new Vector3(x * startRad, y * startRad, 0.0f);

        float asteroidRadius = 2.5f;

        float firstSpeed = Random.Range(1.0f, 3.0f);

        float vAng = Random.Range(0, Mathf.PI * 2);
        Vector3 vel = new Vector3(Mathf.Cos(vAng) * firstSpeed, Mathf.Sin(vAng) * firstSpeed, 0.0f);

        MakeAsteroid(pos, asteroidRadius, vel);
    }

    void MakeAsteroid(Vector3 pos, float radius, Vector3 vel)
    {
        long entityID = GetUnusedEntityID();

        ECSteroids.Transform firstTransform = new ECSteroids.Transform();

        firstTransform.pos = pos;
        firstTransform.angle = 0.0f;
        firstTransform.EntityID = entityID;

        ECSteroids.Polygon firstPolygon = new Polygon();
        firstPolygon.points = new List<Vector3>();

        float variance = radius * 0.3f;
        int points = 12;

        for (int i = 0; i < points; ++i) {
            float ast_angle = MapRange(i, 0, points, 0, Mathf.PI * 2);
            float ast_x = Mathf.Cos(ast_angle);
            float ast_y = Mathf.Sin(ast_angle);

            float r = Random.Range(radius - variance, radius + variance);

            firstPolygon.points.Add(new Vector3(ast_x * r, ast_y * r));
        }
        // close it off
        firstPolygon.points.Add(firstPolygon.points[0]);

        firstPolygon.color = Color.green;
        firstPolygon.lineWidth = 1.5f;
        firstPolygon.isDirty = true;
        firstPolygon.EntityID = entityID;

        ECSteroids.Velocity firstVelocity = new Velocity();
        firstVelocity.vel = vel;
        firstVelocity.EntityID = entityID;

        CollisionDisk disk = new CollisionDisk();
        disk.radius = radius;
        disk.EntityID = entityID;

        AsteroidTag asteroidTag = new AsteroidTag();
        asteroidTag.EntityID = entityID;

        GameplayStateTag gpsTag = new GameplayStateTag();
        gpsTag.EntityID = entityID;

        cmp_transforms[entityID] = firstTransform;
        cmp_polygons[entityID] = firstPolygon;
        cmp_velocitys[entityID] = firstVelocity;
        cmp_collisionDisks[entityID] = disk;
        cmp_asteroidTags[entityID] = asteroidTag;
        cmp_gameplayStateTags[entityID] = gpsTag;
    }

    void MakeBullet(Vector3 pos, Vector3 velocity, float lifetime)
    {
        long entityID = GetUnusedEntityID();

        ECSteroids.Transform firstTransform = new ECSteroids.Transform();
        firstTransform.pos = pos;
        firstTransform.angle = 0.0f;
        firstTransform.EntityID = entityID;

        ECSteroids.Polygon firstPolygon = new Polygon();
        firstPolygon.points = new List<Vector3>();

        float bulletSize = 0.15f;
        int points = 6;

        for (int i = 0; i < points; ++i) {
            float bullet_angle = MapRange(i, 0, points, 0, Mathf.PI * 2);
            float bullet_x = Mathf.Cos(bullet_angle);
            float bullet_y = Mathf.Sin(bullet_angle);

            firstPolygon.points.Add(new Vector3(bullet_x * bulletSize, bullet_y * bulletSize));
        }
        firstPolygon.points.Add(firstPolygon.points[0]);

        firstPolygon.color = Color.green;
        firstPolygon.lineWidth = 2.0f;
        firstPolygon.isDirty = true;
        firstPolygon.EntityID = entityID;

        ECSteroids.Velocity firstVelocity = new Velocity();
        firstVelocity.vel = velocity;
        firstVelocity.EntityID = entityID;

        CollisionDisk disk = new CollisionDisk();
        disk.radius = bulletSize * 2; // cheat it a little larger
        disk.EntityID = entityID;

        BulletTag bulletTag = new BulletTag();
        bulletTag.EntityID = entityID;

        EntityLifetime entityLifetime = new EntityLifetime();
        entityLifetime.EntityID = entityID;
        entityLifetime.secondsRemaining = lifetime;

        GameplayStateTag gpsTag = new GameplayStateTag();
        gpsTag.EntityID = entityID;

        cmp_transforms[entityID] = firstTransform;
        cmp_polygons[entityID] = firstPolygon;
        cmp_velocitys[entityID] = firstVelocity;
        cmp_collisionDisks[entityID] = disk;
        cmp_bulletTags[entityID] = bulletTag;
        cmp_entityLifetimes[entityID] = entityLifetime;
        cmp_gameplayStateTags[entityID] = gpsTag;
    }

    void MakeSingletons()
    {
        ECSteroids.InputBuffer inputBuffer = new InputBuffer();
        inputBuffer.Reset();
        inputBuffer.maxCooldown = 0.25f;
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
        LifetimeSystemTick();
        DrawScoreSystemTick();
        PendingGameStateSystemTick();
        DrawTextMessages();
        WaveSystemTick();

        DestroyQueuedEntities();

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

    Vector3 vectorFromAngleMagnitude(float angle, float magnitude)
    {
        float x = Mathf.Cos(angle);
        float y = Mathf.Sin(angle);
        return new Vector3(x * magnitude, y * magnitude, 0.0f);
    }

    void ApplyInputSystemTick()
    {
        // TODO support data-driven key bindings

        // TODO make this data driven somewhere
        const float STEER_RATE = -Mathf.PI/2.0f;
        const float THRUST_RATE = 0.1f;
        // TODO move to a component
        float BULLET_SPEED = 10.0f;
        float BULLET_LIFETIME = 2.8f;
        int MAX_BULLET_COUNT = 6;

        int curBulletCount = cmp_bulletTags.Count;

        InputBuffer ib = cmp_inputBuffers[NO_ENTITY];

        foreach (InputDrivesThisEntityTempTag iTag in cmp_inputTags.Values) {
            long entity = iTag.EntityID;
            ECSteroids.Transform t = cmp_transforms[entity];
            ECSteroids.Velocity v = cmp_velocitys[entity];

            t.angle += ib.steer * Time.deltaTime * STEER_RATE;

            if (ib.throttle > 0.0f) {
                float cx = Mathf.Cos(t.angle);
                float cy = Mathf.Sin(t.angle);
                v.vel += new Vector3(cx * THRUST_RATE, cy * THRUST_RATE, 0.0f);
            }

            if (ib.shootCooldown > 0.0f) {
                ib.shootCooldown -= Time.deltaTime;
            }

            if (ib.isShooting && (curBulletCount < MAX_BULLET_COUNT) && (ib.shootCooldown <= 0.0f)) {
                ib.shootCooldown = ib.maxCooldown;

                Vector3 bulletVec = vectorFromAngleMagnitude(t.angle, BULLET_SPEED);
                MakeBullet(t.pos, bulletVec, BULLET_LIFETIME);
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
        List<KeyValuePair<long, long>> shipCollisions = new List<KeyValuePair<long, long>>();

        foreach (ShipTag st in cmp_shipTags.Values) {
            long shipEntity = st.EntityID;
            if (cmp_temporaryInvulnerabilitys.ContainsKey(shipEntity)) {
                continue;
            }

            CollisionDisk shipDisk = cmp_collisionDisks[shipEntity];
            ECSteroids.Transform shipTransform = cmp_transforms[shipEntity];

            foreach (AsteroidTag at in cmp_asteroidTags.Values) {
                long asteroidEntity = at.EntityID;
                CollisionDisk asteroidDisk = cmp_collisionDisks[asteroidEntity];
                ECSteroids.Transform asteroidTransform = cmp_transforms[asteroidEntity];

                float thresh = asteroidDisk.radius + shipDisk.radius;
                float threshSqr = thresh * thresh;

                if ((asteroidTransform.pos - shipTransform.pos).sqrMagnitude < threshSqr) {
                    shipCollisions.Add(new KeyValuePair<long, long>(shipEntity, asteroidEntity));
                }
            }
        }

        List<KeyValuePair<long, long>> bulletCollisions = new List<KeyValuePair<long, long>>();

        foreach (BulletTag bt in cmp_bulletTags.Values) {
            long bulletEntity = bt.EntityID;
            CollisionDisk bulletDisk = cmp_collisionDisks[bulletEntity];
            ECSteroids.Transform bulletTransform = cmp_transforms[bulletEntity];

            foreach (AsteroidTag at in cmp_asteroidTags.Values) {
                long asteroidEntity = at.EntityID;
                CollisionDisk asteroidDisk = cmp_collisionDisks[asteroidEntity];
                ECSteroids.Transform asteroidTransform = cmp_transforms[asteroidEntity];

                float thresh = asteroidDisk.radius + bulletDisk.radius;
                float threshSqr = thresh * thresh;

                if ((asteroidTransform.pos - bulletTransform.pos).sqrMagnitude < threshSqr) {
                    bulletCollisions.Add(new KeyValuePair<long, long>(bulletEntity, asteroidEntity));
                }
            }
        }

        foreach (KeyValuePair<long, long> kvp in shipCollisions) {
            collideShipWithAsteroid(kvp.Key, kvp.Value);
        }

        foreach (KeyValuePair<long, long> kvp in bulletCollisions) {
            collideBulletWithAsteroid(kvp.Key, kvp.Value);
        }
    }

    void addScore(int points)
    {
        // TODO only give points to the actual owner
        foreach (PlayerData pd in cmp_playerDatas.Values) {
            pd.score += points;
        }
    }

    void collideShipWithAsteroid(long shipEntity, long asteroidEntity)
    {
        FlagEntityForDestruction(asteroidEntity);

        cmp_playerDatas[shipEntity].lives -= 1;

        if (cmp_playerDatas[shipEntity].lives == 0) {
            QueueStateChange(ECSteroidsGameState.GameOver, 0.0f);
        }
        else {
            addInvulnerabilityToShip(shipEntity, 2.0f);
        }
    }

    void addInvulnerabilityToShip(long shipIndex, float duration)
    {
        TemporaryInvulnerability tmpInv = new TemporaryInvulnerability();
        tmpInv.EntityID = shipIndex;
        tmpInv.secondsRemaining = duration;
        cmp_temporaryInvulnerabilitys[shipIndex] = tmpInv;
    }

    void collideBulletWithAsteroid(long bulletEntity, long asteroidEntity)
    {
        addScore(100);

        // TODO move into a component
        const float MIN_RADIUS = 1.2f;

        CollisionDisk acd = cmp_collisionDisks[asteroidEntity];
        float oldSize = acd.radius;

        if (oldSize > MIN_RADIUS) {
            ECSteroids.Transform at = cmp_transforms[asteroidEntity];
            ECSteroids.Velocity av = cmp_velocitys[asteroidEntity];
            ECSteroids.Velocity bv = cmp_velocitys[bulletEntity];

            // break asteroid up, if it's big enough

            float frac = Random.Range(0.3f, 0.5f);

            float newRadius1 = oldSize * frac;
            float newRadius2 = oldSize * (1.0f - frac);

            const float BULLET_DISCOUNT = 0.35f;

            Vector3 newVelocity1 = av.vel + bv.vel * (1.0f - frac) * BULLET_DISCOUNT;
            Vector3 newVelocity2 = av.vel + bv.vel * frac * BULLET_DISCOUNT;

            MakeAsteroid(at.pos, newRadius1, newVelocity1);
            MakeAsteroid(at.pos, newRadius2, newVelocity2);
        }
        FlagEntityForDestruction(asteroidEntity);
        FlagEntityForDestruction(bulletEntity);
    }

    void PolygonSystemTick()
    {
        Vector3 zAxis = new Vector3(0.0f, 0.0f, 1.0f);
        Vector3 identityScale = new Vector3(1.0f, 1.0f, 1.0f);

        foreach (Polygon p in cmp_polygons.Values) {
            ECSteroids.Transform t = cmp_transforms[p.EntityID];
            float angleDeg = t.angle * 180 / Mathf.PI;

            if (p.isDirty) {
                p.vectLine = new VectorLine("polygon eid:" + p.EntityID, p.points, 2.0f);
                p.vectLine.lineType = LineType.Continuous;
                p.vectLine.color = p.color;
                p.isDirty = false;
            }

            Matrix4x4 tMat = Matrix4x4.TRS(t.pos, Quaternion.AngleAxis(angleDeg, zAxis), identityScale);
            p.vectLine.matrix = tMat;

            if (cmp_temporaryInvulnerabilitys.ContainsKey(p.EntityID)) {
                TemporaryInvulnerability tmpInv = cmp_temporaryInvulnerabilitys[p.EntityID];

                tmpInv.secondsRemaining -= Time.deltaTime;
                if (tmpInv.secondsRemaining <= 0) {
                    cmp_temporaryInvulnerabilitys.Remove(p.EntityID);
                    p.vectLine.color = p.color;
                }
                else {
                    float baseR = p.color.r;
                    float baseG = p.color.g;
                    float baseB = p.color.b;

                    float solidR = 1.0f;
                    float solidG = 1.0f;
                    float solidB = 1.0f;

                    float pulse = Mathf.Cos(tmpInv.secondsRemaining * (4.0f * Mathf.PI));

                    float iR = MapRange(pulse, -1.0f, 1.0f, baseR, solidR);
                    float iG = MapRange(pulse, -1.0f, 1.0f, baseG, solidG);
                    float iB = MapRange(pulse, -1.0f, 1.0f, baseB, solidB);

                    p.vectLine.color = new Color(iR, iG, iB);
                }
            }

            p.vectLine.Draw();
        }
    }

    void LifetimeSystemTick()
    {
        foreach (EntityLifetime el in cmp_entityLifetimes.Values) {
            el.secondsRemaining -= Time.deltaTime;
            //Debug.Log("seconds remaining:" + el.secondsRemaining);
            if (el.secondsRemaining <= 0.0f) {
                //Debug.Log("flagging");
                FlagEntityForDestruction(el.EntityID);
            }
        }
    }

    void DrawScoreSystemTick()
    {
        foreach (PlayerData pd in cmp_playerDatas.Values) {
            string scoreString = string.Format("Score: {0:D8} Ships: {1}", pd.score, pd.lives);
            myTextVectorLine.MakeText(scoreString, new Vector3(5.0f, 22.0f, 0.0f), 1.0f);
            myTextVectorLine.Draw();
        }
    }

    void PendingGameStateSystemTick()
    {
        if (cmp_pendingGameStates.ContainsKey(NO_ENTITY)) {
            PendingGameState pgs = cmp_pendingGameStates[NO_ENTITY];
            pgs.secondsRemaining -= Time.deltaTime;
            if (pgs.secondsRemaining <= 0.0f) {
                cmp_pendingGameStates.Remove(NO_ENTITY);
                SetGameState(pgs.nextState);
            }
        }
    }

    void DrawTextMessages()
    {
        foreach (TextMessage msg in cmp_textMessages.Values) {
            msg.vectLine.Draw();
        }
    }

    void WaveSystemTick()
    {
        if (isWaveComplete()) {
            foreach (PlayerData pd in cmp_playerDatas.Values) {
                pd.waveIndex++;
            }

            foreach (ShipTag st in cmp_shipTags.Values) {
                addInvulnerabilityToShip(st.EntityID, 2.0f);
            }

            // TODO write the wave name
            PopulateForWave();
        }
    }

    public void FlagEntityForDestruction(long entityID)
    {
        destroyQueue.Add(entityID);
    }

    void DestroyQueuedEntities()
    {
        foreach (long entityId in destroyQueue) {
            Debug.Log("removing " + entityId);
            // TODO make this cleaner
            cmp_transforms.Remove(entityId);
            if (cmp_polygons.ContainsKey(entityId))
            {
                // so gross!
                //Destroy(cmp_polygons[entityId].unityObject);
                VectorLine.Destroy(ref cmp_polygons[entityId].vectLine);
                cmp_polygons.Remove(entityId);
            }
            cmp_velocitys.Remove(entityId);
            cmp_inputBuffers.Remove(entityId);
            cmp_inputTags.Remove(entityId);
            cmp_collisionDisks.Remove(entityId);
            cmp_shipTags.Remove(entityId);
            cmp_asteroidTags.Remove(entityId);
            cmp_bulletTags.Remove(entityId);
            cmp_entityLifetimes.Remove(entityId);
            cmp_playerDatas.Remove(entityId);
            cmp_pendingGameStates.Remove(entityId);
            if (cmp_textMessages.ContainsKey(entityId)) {
                VectorLine.Destroy(ref cmp_textMessages[entityId].vectLine);
                cmp_textMessages.Remove(entityId);
            }
            cmp_bootStateTags.Remove(entityId);
            cmp_titleStateTags.Remove(entityId);
            cmp_gameplayStateTags.Remove(entityId);
            cmp_temporaryInvulnerabilitys.Remove(entityId);
            cmp_levelDescs.Remove(entityId);

            if (entityId != NO_ENTITY) {
                UnusedEntityIDs.Add(entityId);
            }
        }
        destroyQueue.Clear();
    }
}
