using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(VelocityCalculator))]
public class MeteorScript : MonoBehaviour {

    [Header("Variables")]
    [SerializeField]
    float scaleMultiplier;
    [SerializeField]
    float slowestRotation, fastestRotation;
    [SerializeField]
    float slowestSpeed, fastestSpeed;
    [SerializeField]
    float minGrowthDelay, maxGrowthDelay;
    [SerializeField]
    float growthSpeed;
    [SerializeField]
    float delayBeforeRegistering;

    public static List<MeteorScript> meteors;
    internal Transform tr;
    internal VelocityCalculator velocityCalculator;
    internal int stage;
    internal MeteorSpawner.MeteorColor color;
    internal float speed;

    Rigidbody2D rb;
    MeteorSpawner.MeteorColor[] meteorColors;
    SpriteRenderer sprite;
    Vector3 bigScale;
    Vector3 rotation;
    Vector2 direction;
    Vector2 smallScale;
    bool isVisible;

    void Awake() {

        if (meteors == null) {
            meteors = new List<MeteorScript>();
        }

        velocityCalculator = GetComponent<VelocityCalculator>();
        tr = GetComponent<Transform>();
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        smallScale = tr.localScale;
        bigScale = tr.localScale * scaleMultiplier;
        tr.tag = Strings.METEORTAG;
    }

    void Update() {

        if (!isVisible) {
            if (PlayArea.singleton.InPlayArea(tr.position)) {
                isVisible = true;
                StartCoroutine(RegisterMeteor());
            }
        }

        else {
            if (!PlayArea.singleton.InPlayArea(tr.position)) {
                if (meteors.Contains(this)) {
                    meteors.Remove(this);
                    Destroy(gameObject);
                }
            }
        }
    }

    void FixedUpdate() {

        tr.Rotate(rotation * Time.deltaTime);
        rb.MovePosition((Vector2)tr.position + (direction * speed * Time.fixedDeltaTime));
    }

    public void OnBecameInvisible() {
        Destroy(gameObject);
    }

    void OnDestroy() {
        if (meteors.Contains(this)) {
            meteors.Remove(this);
        }
    }

    public void Init(Vector2 _dir, MeteorSpawner.ColorInfo info) {

        stage = 0;
        tr.localScale = smallScale;
        sprite.color = info.spriteColor;
        color = info.meteorColor;
        float zRot = UnityEngine.Random.Range(slowestRotation, fastestRotation);
        rotation = new Vector3(0f, 0f, zRot);
        direction = _dir;
        speed = UnityEngine.Random.Range(slowestSpeed, fastestSpeed);

        StartCoroutine(Grow());
    }

    IEnumerator Grow() {
        float delay = UnityEngine.Random.Range(minGrowthDelay, maxGrowthDelay);
        yield return new WaitForSeconds(delay);
        stage = 1;
        tr.DOScale(bigScale, growthSpeed);
    }

    IEnumerator RegisterMeteor() {
        yield return new WaitForSeconds(delayBeforeRegistering);
        if (!meteors.Contains(this)) {
            meteors.Add(this);
        }
    }
}