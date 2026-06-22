using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Player : MonoBehaviour
{
    public Transform cameraTransform;
    public float moveSpeed = 3f;
    public float rotSpeed = 5f;
    public float jumpPower = 5f;
    public float groundedThreshold = .15f;
    public float minimumRespawnY = -50f;
    const float joystickActiveTolerance = 3f * 10e-3f;
    public GameObject manaBar;

    public float dashSpeed = 5f;
    public float dashTime = 1f;
    public float mana = 50f;
    public float dashManaCost = 5f;
    public float freezeManaCost = 5f;
    public float freezeTime = 3f;

    List<PlayerState> playerStates;
    bool isReversing = false;
    public bool IsReversing => isReversing;
    float frameCounter = 0;
    bool isWounded = false;

    public float health = 100f;
    public float maxHealth = 100f;

    Vector3 initPos;
    Vector3 moveDir;
    TextMeshProUGUI manaText;

    Rigidbody rigidbody;
    Animator animator;
    CapsuleCollider capsule;
    bool isGrounded = true;
    bool isDashing = false;
    private Coroutine attackCoroutine;
    private bool isShieldActive = false;
    public bool IsShieldActive => isShieldActive;

    // Start is called before the first frame update
    void Start()
    {
        //Debug.Log(joystickActiveTolerance.ToString());
        rigidbody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        capsule = GetComponent<CapsuleCollider>();
        initPos = transform.position;
        if (manaBar != null)
        {
            manaText = manaBar.GetComponent<TextMeshProUGUI>();
            if (manaText != null)
            {
                manaText.text = mana.ToString();
            }
        }
        playerStates = new List<PlayerState>();
    }

    void FixedUpdate()
    {
        if (!isReversing)
        {
            frameCounter++;
            if (frameCounter >= 5)
            {
                playerStates.Add(new PlayerState(transform.position, transform.rotation));
                frameCounter = 0;

                // Ensure we only store a maximum of 60 positions
                if (playerStates.Count > 60)
                {
                    playerStates.RemoveAt(0);
                }
            }
        }
        else
        {
            frameCounter -= 1.5f;
            if (frameCounter <= 0f)
            {
                frameCounter += 5f;

                if (playerStates.Count <= 0) {
                    isReversing = false;
                    return;
                }
                transform.position = playerStates[playerStates.Count - 1].Position;
                transform.rotation = playerStates[playerStates.Count - 1].Rotation;
                playerStates.RemoveAt(playerStates.Count - 1);
            } else {
                transform.position = Vector3.Lerp(playerStates[playerStates.Count - 1].Position, transform.position, frameCounter / 5f);
                transform.rotation = Quaternion.Lerp(playerStates[playerStates.Count - 1].Rotation, transform.rotation, frameCounter / 5f);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && !isDashing) {
            StartCoroutine(Dash());
        }

        if (Input.GetMouseButtonDown(0)) {
            StartCoroutine(FreezeEnemies());
        }

        if (Input.GetKeyDown(KeyCode.E)) {
            if (attackCoroutine != null) {
                StopCoroutine(attackCoroutine);
            }
            attackCoroutine = StartCoroutine(PlaySnappyAttack());
        }
        
        if (Input.GetKeyDown(KeyCode.Q) && mana >= 10f && !isShieldActive) {
            StartCoroutine(ShieldRoutine());
        }

        if(Input.GetKey(KeyCode.R) && playerStates.Count > 10)
        {
            isReversing = true;
        }
        else
        {
            isReversing = false;
        }

        UpdateMana();

        GetMoveDir();

        SetAnimatorMoveParams();

        HandleJump();

        ApplyRootRotation();
    }
    
    private void OnAnimatorMove()
    {
        MovePlayer();
    }

    private void SetAnimatorMoveParams()
    {
        Vector3 characterSpaceMoveDir = transform.InverseTransformVector(moveDir) * 1.2f;
        animator.SetFloat("Forward", characterSpaceMoveDir.z);
        animator.SetFloat("Right", characterSpaceMoveDir.x);
        animator.SetBool("Reversing", isReversing);
    }

    private void HandleJump()
    {
        //Ray ray = new Ray();
        //ray.origin = transform.position + Vector3.up * groundedThreshold;
        //ray.direction = Vector3.down;
        //isGrounded = Physics.Raycast(ray, 2 * groundedThreshold);
        Vector3 bottomCapsuleSphereCenter = transform.position + Vector3.up * (capsule.radius + groundedThreshold);
        Vector3 topCapsuleSphereCenter = transform.position + Vector3.up * (capsule.height - capsule.radius + groundedThreshold);
        isGrounded = Physics.CapsuleCast(bottomCapsuleSphereCenter, topCapsuleSphereCenter,
                                         capsule.radius, Vector3.down, groundedThreshold * 2f);
        animator.SetBool("Grounded", isGrounded);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rigidbody.AddForce(Vector3.up * jumpPower, ForceMode.VelocityChange);
        }

        if (transform.position.y < minimumRespawnY)
            transform.position = initPos;
    }

    private void ApplyRootRotation()
    {
        Vector3 lookDir = transform.forward;
        if (moveDir.magnitude > joystickActiveTolerance) //there is movement
            lookDir = moveDir;

        Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
        float rotSlerpFactor = Mathf.Clamp01(rotSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotSlerpFactor);
    }

    private void GetMoveDir()
    {
        if (isReversing) {
            //moveDir = (transform.position - playerStates[playerStates.Count - 1].Position).normalized;
            if (playerStates.Count >= 2)
                moveDir = (playerStates[playerStates.Count - 1].Position - playerStates[playerStates.Count - 2].Position).normalized;
            return;
        }
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 cameraFwd_xOz = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 cameraRight_xOz = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;

        moveDir = x * cameraRight_xOz + z * cameraFwd_xOz;
        moveDir = moveDir.normalized * Mathf.Max(Mathf.Abs(moveDir.x), Mathf.Abs(moveDir.z));
    }

    private void MovePlayer()
    {
        //transform.position += moveDir * moveSpeed * Time.deltaTime;
        if (!isGrounded)
            return;
        float velY = rigidbody.velocity.y;
        //Vector3 newVel = moveDir * moveSpeed;
        Vector3 newVel = animator.deltaPosition / Time.deltaTime * moveSpeed;
        rigidbody.velocity = new Vector3(newVel.x, velY, newVel.z);
    }

    private void UpdateMana() {
        if (manaText != null)
        {
            manaText.text = mana.ToString();
        }
    }

    private void increaseMana() {
        mana = Mathf.Min(mana + 25f, 100f);
    }

    private void increaseHealth() {
        health = Mathf.Min(health + 25f, maxHealth);
        if (health >= maxHealth) isWounded = false;
    }

    IEnumerator Dash() {
        isDashing = true;

        // Create a temporary object for the blue trail and sparks
        GameObject trailObj = new GameObject("DashTrail");
        trailObj.transform.SetParent(transform, false);
        trailObj.transform.localPosition = new Vector3(0f, 0.85f, 0f); // Lower in character space

        // 1. Trail Renderer for the solid blue trail (vertical strip)
        TrailRenderer tr = trailObj.AddComponent<TrailRenderer>();
        tr.time = 0.35f;
        tr.startWidth = 0.2f;
        tr.endWidth = 0f;
        tr.minVertexDistance = 0.05f;
        tr.alignment = LineAlignment.TransformZ;

        Material trailMat = new Material(Shader.Find("Standard"));
        trailMat.SetFloat("_Mode", 3f); // Transparent
        trailMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        trailMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        trailMat.SetInt("_ZWrite", 0);
        trailMat.EnableKeyword("_ALPHABLEND_ON");
        trailMat.renderQueue = 3000;

        Color blueColor = new Color(0.1f, 0.6f, 1.0f, 0.6f);
        trailMat.color = blueColor;
        trailMat.EnableKeyword("_EMISSION");
        trailMat.SetColor("_EmissionColor", new Color(0.1f, 0.6f, 1.0f) * 3f);
        tr.material = trailMat;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.1f, 0.8f, 1.0f), 0.0f), new GradientColorKey(new Color(0f, 0.2f, 0.8f), 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.7f, 0.0f), new GradientAlphaKey(0f, 1.0f) }
        );
        tr.colorGradient = gradient;

        // Point light casting a blue glow along the dash path
        Light trailLight = trailObj.AddComponent<Light>();
        trailLight.type = LightType.Point;
        trailLight.color = new Color(0.1f, 0.7f, 1.0f);
        trailLight.intensity = 2f;
        trailLight.range = 3f;

        // 2. Mesh Particle System for floating sparks in world space
        ParticleSystem ps = trailObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // Particles are left in world coordinates
        main.startLifetime = 0.5f;
        main.startSize = 0.04f;
        main.startColor = new Color(0.1f, 0.75f, 1.0f, 0.8f);
        main.maxParticles = 50;
        main.duration = dashTime;
        main.loop = false;
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.rateOverTime = 60;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        ParticleSystemRenderer psr = trailObj.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            psr.renderMode = ParticleSystemRenderMode.Mesh;
            GameObject tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Mesh sphereMesh = tempSphere.GetComponent<MeshFilter>().sharedMesh;
            Destroy(tempSphere);
            psr.mesh = sphereMesh;

            Material pMat = new Material(Shader.Find("Standard"));
            pMat.color = new Color(0.1f, 0.75f, 1.0f, 0.8f);
            pMat.EnableKeyword("_EMISSION");
            pMat.SetColor("_EmissionColor", new Color(0.1f, 0.6f, 1.0f) * 3f);
            psr.material = pMat;
        }

        Vector3 startPosition = transform.position;
        Vector3 dashDirection = moveDir.normalized;
        if (dashDirection.magnitude < 0.01f)
        {
            dashDirection = transform.forward;
        }
        Vector3 endPosition = startPosition + dashDirection * dashSpeed;

        float elapsedTime = 0f;
        while (elapsedTime < dashTime) {
            transform.position = Vector3.Lerp(startPosition, endPosition, elapsedTime / dashTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = endPosition;

        // Detach the trail so it stays behind in the world and fades out naturally
        trailObj.transform.SetParent(null);
        if (ps != null)
        {
            ps.Stop();
        }
        StartCoroutine(FadeOutTrailLight(trailLight, 0.35f));
        Destroy(trailObj, 0.5f);

        isDashing = false;
        mana -= dashManaCost;
    }

    private IEnumerator FadeOutTrailLight(Light l, float duration)
    {
        float elapsed = 0f;
        float startIntensity = l != null ? l.intensity : 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (l != null)
            {
                l.intensity = Mathf.Lerp(startIntensity, 0f, elapsed / duration);
            }
            yield return null;
        }
    }

    IEnumerator FreezeEnemies() {
        if (attackCoroutine != null) {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
        animator.speed = 1.8f; // Speed up animation to 1.8x
        animator.Play("Attack", 0, 0f); // Force instantly into the Attack state

        // Wait for the hand-raise frame of the sped-up animation (0.18s real time = 0.32s animation time)
        yield return new WaitForSeconds(0.18f);

        mana -= freezeManaCost;
        
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("enemy");
        foreach (GameObject enemy in enemies) {
            Debug.Log("freezing enemy");
            enemy.GetComponent<Enemy>().getFrozen(freezeTime);
        }

        TimeFreezableObject[] freezables = FindObjectsOfType<TimeFreezableObject>();
        foreach (TimeFreezableObject freezable in freezables) {
            freezable.Freeze(freezeTime);
        }

        GetComponent<TimeScreenEffectController>()?.TriggerFreezePulse(freezeTime);
        SpawnCastEffect();

        // Wait for animation to finish, then restore default speed
        yield return new WaitForSeconds(0.22f);
        animator.speed = 1.0f;
    }

    void OnTriggerEnter(Collider collision) {
        if (collision.gameObject.tag == "Mana") {
            increaseMana();
            Destroy(collision.gameObject);
        }
        if (collision.gameObject.tag == "Health") {
            increaseHealth();
            Destroy(collision.gameObject);
        }
    }

    void OnCollisionEnter(Collision collision) {
    //     } else if (collision.gameObject.tag == "enemy") {
    //         handleEnemyCollision();
    //     }
    }

    private void SpawnCastEffect()
    {
        Transform hand = FindHandTransform();
        StartCoroutine(PlayMagicCastRoutine(hand));
    }

    private IEnumerator PlayMagicCastRoutine(Transform handTransform)
    {
        GameObject magicEffect = new GameObject("SpellCastEffect");
        magicEffect.transform.SetParent(handTransform, false);
        
        if (handTransform == transform)
        {
            magicEffect.transform.localPosition = new Vector3(0.2f, 1.1f, 0.6f);
        }
        else
        {
            // Offset locally on the Y-axis so it spawns in the palm instead of the bone joint
            magicEffect.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        }

        ParticleSystem ps = magicEffect.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = new Color(0.1f, 0.75f, 1f, 0.7f);
        main.startSize = 0.02f; // way smaller particles
        main.startLifetime = 0.8f; // linger longer
        main.maxParticles = 60;
        main.duration = 0.7f;
        main.loop = false;

        var emission = ps.emission;
        emission.rateOverTime = 80;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.03f; // compact emitter radius

        ParticleSystemRenderer psr = magicEffect.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            // Set ParticleSystem to render tiny 3D spheres instead of billboards to completely avoid flat square sheets!
            psr.renderMode = ParticleSystemRenderMode.Mesh;
            
            GameObject tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Mesh sphereMesh = tempSphere.GetComponent<MeshFilter>().sharedMesh;
            Destroy(tempSphere);
            
            psr.mesh = sphereMesh;

            Material particleMat = new Material(Shader.Find("Standard"));
            particleMat.color = new Color(0.1f, 0.6f, 1f, 0.6f);
            particleMat.EnableKeyword("_EMISSION");
            particleMat.SetColor("_EmissionColor", new Color(0.1f, 0.6f, 1f) * 1.5f);
            psr.material = particleMat;
        }

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.transform.SetParent(magicEffect.transform, false);
        visual.transform.localScale = Vector3.zero;
        Destroy(visual.GetComponent<Collider>());

        Material magicMat = new Material(Shader.Find("Standard"));
        magicMat.color = new Color(0.1f, 0.7f, 1f);
        magicMat.EnableKeyword("_EMISSION");
        magicMat.SetColor("_EmissionColor", new Color(0.1f, 0.7f, 1f) * 2f);
        visual.GetComponent<MeshRenderer>().material = magicMat;

        float elapsed = 0f;
        float duration = 0.8f; // longer duration (from 0.35s to 0.8s)
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = Mathf.Sin(t * Mathf.PI) * 0.03f; // smaller visual
            visual.transform.localScale = new Vector3(scale, scale, scale);
            yield return null;
        }

        Destroy(magicEffect);
    }

    private Transform FindHandTransform()
    {
        Transform[] allChildren = GetComponentsInChildren<Transform>();
        foreach (Transform child in allChildren)
        {
            string nameLower = child.name.ToLower();
            if (nameLower.Contains("righthand") ||
                nameLower.Contains("r_hand") || 
                nameLower.Contains("rhand") ||
                nameLower.Contains("right_hand") ||
                nameLower.Contains("r.hand") ||
                nameLower.Contains("hand.r") ||
                nameLower.Contains("hand_r"))
            {
                return child;
            }
        }
        foreach (Transform child in allChildren)
        {
            if (child.name.ToLower().Contains("hand"))
            {
                return child;
            }
        }
        return transform;
    }

    public void TakeDamage(float amount, Vector3 respawnFallback)
    {
        if (isReversing || isShieldActive) return;

        health = Mathf.Max(health - amount, 0f);
        isWounded = true;

        if (health <= 0)
        {
            Respawn(respawnFallback);
        }
    }

    public void Respawn(Vector3 respawnPosition)
    {
        health = maxHealth;
        isWounded = false;
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.position = respawnPosition;
        Debug.Log("Player respawned at: " + respawnPosition);
    }

    private IEnumerator PlaySnappyAttack() {
        animator.speed = 1.8f;
        animator.Play("Attack", 0, 0f);
        yield return new WaitForSeconds(0.40f);
        animator.speed = 1.0f;
        attackCoroutine = null;
    }

    private IEnumerator ShieldRoutine()
    {
        isShieldActive = true;
        mana = Mathf.Max(mana - 10f, 0f); // Costs 10 mana

        // Create the yellow Quen shield visual orb
        GameObject shieldObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shieldObj.name = "QuenShield";
        shieldObj.transform.SetParent(transform, false);
        shieldObj.transform.localPosition = new Vector3(0f, 1.0f, 0f); // Centered on player body
        shieldObj.transform.localScale = Vector3.zero; // Starts at 0 for growth flash animation
        Destroy(shieldObj.GetComponent<Collider>()); // Don't block player physics

        Material shieldMat = new Material(Shader.Find("Standard"));
        // Configure transparent rendering mode in standard shader
        shieldMat.SetFloat("_Mode", 3f); // Transparent mode
        shieldMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        shieldMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        shieldMat.SetInt("_ZWrite", 0);
        shieldMat.DisableKeyword("_ALPHATEST_ON");
        shieldMat.EnableKeyword("_ALPHABLEND_ON");
        shieldMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        shieldMat.renderQueue = 3000;
        
        // Initial setup for color and emission
        shieldMat.color = new Color(1.0f, 0.85f, 0.15f, 0.8f); // Starts less transparent for flash
        shieldMat.EnableKeyword("_EMISSION");
        shieldMat.SetColor("_EmissionColor", new Color(1.0f, 0.8f, 0.1f) * 4.0f); // Starts very bright
        
        shieldObj.GetComponent<MeshRenderer>().material = shieldMat;

        // Golden glowing point light inside the shield
        Light shieldLight = shieldObj.AddComponent<Light>();
        shieldLight.type = LightType.Point;
        shieldLight.color = new Color(1.0f, 0.85f, 0.15f);
        shieldLight.intensity = 5.0f; // Start very bright for flash
        shieldLight.range = 5f;

        // Create swirling sparks particle system
        GameObject sparksObj = new GameObject("ShieldSparks");
        sparksObj.transform.SetParent(shieldObj.transform, false);
        ParticleSystem ps = sparksObj.AddComponent<ParticleSystem>();
        
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = 0.8f;
        main.startSize = 0.035f;
        main.startColor = new Color(1.0f, 0.85f, 0.2f, 0.9f);
        main.maxParticles = 60;
        main.duration = 2.0f;
        main.loop = true;
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.rateOverTime = 40;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-0.1f, 0.5f);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);

        ParticleSystemRenderer psr = sparksObj.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            psr.renderMode = ParticleSystemRenderMode.Mesh;
            
            GameObject tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Mesh sphereMesh = tempSphere.GetComponent<MeshFilter>().sharedMesh;
            Destroy(tempSphere);
            
            psr.mesh = sphereMesh;

            Material particleMat = new Material(Shader.Find("Standard"));
            particleMat.color = new Color(1.0f, 0.85f, 0.2f, 0.9f);
            particleMat.EnableKeyword("_EMISSION");
            particleMat.SetColor("_EmissionColor", new Color(1.0f, 0.75f, 0.15f) * 3f);
            psr.material = particleMat;
        }

        // Perform the activation flash and scale up
        float initElapsed = 0f;
        float initDuration = 0.15f;
        while (initElapsed < initDuration)
        {
            initElapsed += Time.deltaTime;
            float t = initElapsed / initDuration;
            float scale = Mathf.Lerp(0f, 1.3f, t);
            shieldObj.transform.localScale = new Vector3(scale, scale, scale);
            
            float lightIntensity = Mathf.Lerp(5.0f, 2.0f, t);
            shieldLight.intensity = lightIntensity;
            
            float emissionMult = Mathf.Lerp(4.0f, 1.5f, t);
            shieldMat.SetColor("_EmissionColor", new Color(1.0f, 0.8f, 0.1f) * emissionMult);
            
            float alpha = Mathf.Lerp(0.8f, 0.18f, t);
            shieldMat.color = new Color(1.0f, 0.85f, 0.15f, alpha);
            
            yield return null;
        }

        // Ensure target end values are set
        shieldObj.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
        shieldMat.color = new Color(1.0f, 0.85f, 0.15f, 0.18f);
        shieldMat.SetColor("_EmissionColor", new Color(1.0f, 0.8f, 0.1f) * 1.5f);
        shieldLight.intensity = 2.0f;

        // Add animator to handle persistent micro-animations (pulsing and rotation)
        shieldObj.AddComponent<ShieldVisualAnimator>();

        // Maintain shield for the remaining time of the 2 seconds
        yield return new WaitForSeconds(1.85f);

        // Stop sparks emission so they die out naturally
        if (ps != null)
        {
            ps.Stop();
        }

        // Smooth fade out
        float fadeElapsed = 0f;
        float fadeDuration = 0.25f;
        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.deltaTime;
            float t = fadeElapsed / fadeDuration;
            float alpha = Mathf.Lerp(0.18f, 0f, t);
            shieldMat.color = new Color(1.0f, 0.85f, 0.15f, alpha);
            shieldMat.SetColor("_EmissionColor", new Color(1.0f, 0.8f, 0.1f) * (1.5f * (1f - t)));
            shieldLight.intensity = Mathf.Lerp(2.0f, 0f, t);
            yield return null;
        }

        Destroy(shieldObj);
        isShieldActive = false;
    }

    private struct PlayerState
    {
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }

        public PlayerState(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }
}

public class ShieldVisualAnimator : MonoBehaviour
{
    void Update()
    {
        // Witcher-like crackle scale pulse
        float pulse = 1.3f + Mathf.Sin(Time.time * 16f) * 0.03f;
        transform.localScale = new Vector3(pulse, pulse, pulse);
        
        // Slowly rotate shield elements
        transform.Rotate(0f, 45f * Time.deltaTime, 15f * Time.deltaTime);
    }
}