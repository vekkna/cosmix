using System.Collections.Generic;
using System.Linq;
using UnityEngine;
/// <summary>
/// Script to control the AI
/// </summary>
public class AlienAIScript : MonoBehaviour {

    [Space(10)]
    [Header("Script References")]
    [SerializeField]
    UFOScoringScript player;

    [Space(10)]
    [Header("Movement")]
    [SerializeField]
    float speed;
    [SerializeField]
    float mass;
    UFOScoringScript ai;
    [SerializeField]
    float minMove;

    [Space(10)]
    [Header("Avoiding Player")]
    [SerializeField]
    float playerRepulsionRange;
    [SerializeField]
    float playerRepulsiveForce;

    [Space(10)]
    [Header("Delaying Collection")]
    [Tooltip("The AI will collect the meteor when the player is this close to it")]
    [SerializeField]
    float closeToPlayerFactor;
    [SerializeField]
    float collectionDistance;
    [SerializeField]
    float loiterDistance;
    [SerializeField]
    float lookAheadOfMeteorRange;
    [SerializeField]
    float meteorRepulsionRange, meteorRepulsiveForce;
    [SerializeField]
    BoxCollider2D playArea;
    [SerializeField]
    float meteorLookAheadDistance;

    internal static AlienAIScript singleton;
    internal int score = 0, playerScore = 0;

    Rigidbody2D rb;
    Transform tr;
    Vector2 velocity;
    MeteorSpawner.MeteorColor aiNeededColor, playerNeededColor;
    VelocityCalculator velocityCalculator, playerVelocityCalculator;


    void Awake() {
        singleton = this;
        ai = GetComponent<UFOScoringScript>();
        velocityCalculator = GetComponent<VelocityCalculator>();
        playerVelocityCalculator = player.GetComponent<VelocityCalculator>();
        rb = GetComponent<Rigidbody2D>();
        tr = GetComponent<Transform>();

    }

    void FixedUpdate() {

        Vector2 targetPos = DetermineTargetPosition();
        ApplyVelocity(targetPos);

    }

    /// <summary>
    /// Gets the position the AI should move towards
    /// </summary>
    /// <returns></returns>
    Vector2 DetermineTargetPosition() {

        Vector2 targetPos;
        // Try to find a best meteor to chase
        var bestMeteor = BestMeteor();


        if (bestMeteor != null) {
            // If one can be found, see if the AI should collect it immediately or wait till it grows in value
            if (ShouldWait(bestMeteor)) {

                // If it should wait, loiter between the meteor and the player
                targetPos = GetLoiteringPos(bestMeteor);
            }
            else {
                // otherwise just go for it
                targetPos = bestMeteor.tr.position;
            }
        }
        else {
            // If no good target, hang around the centre of the screen
            targetPos = Vector2.zero;
        }
        return targetPos;
    }

    /// <summary>
    /// Move the AI towards its target
    /// </summary>
    /// <param name="_targetPos"></param>
    void ApplyVelocity(Vector2 _targetPos) {

        velocity += Seek(_targetPos) / mass;
        velocity += CalculateRevulsionFromPlayer();
        velocity = velocity.normalized * speed;
        if (velocity.sqrMagnitude < minMove) {
            return;
        }
        rb.MovePosition((Vector2)tr.position + velocity * Time.fixedDeltaTime);
    }

    /// <summary>
    /// Finds the best meteor to chase
    /// </summary>
    /// <returns></returns>
    MeteorScript BestMeteor() {

        /// AI knows what meteors it and the player need most.
        /// Get the closest of each
        var aiClosestNeededMeteor = GetClosestMeteor(aiNeededColor);
        var playerClosestNeededMeteor = GetClosestMeteor(playerNeededColor);

        /// If neither the AI nor the player need any particular meteor...
        if (aiClosestNeededMeteor == null && playerClosestNeededMeteor == null) {
            return null;
        }
        /// If the player doesn't need any particular meteor, go after the AI's most needed
        else if (playerClosestNeededMeteor == null) {
            return aiClosestNeededMeteor;
        }
        /// If the AI doesn't need one, go after the player's
        else if (aiClosestNeededMeteor == null) {
            return playerClosestNeededMeteor;
        }
        /// If both player and AI need a particular meteor, go after the closer
        else {

            if ((Vector2.SqrMagnitude(tr.position - aiClosestNeededMeteor.tr.position) < Vector2.SqrMagnitude(tr.position - playerClosestNeededMeteor.tr.position))) {
                return aiClosestNeededMeteor;
            }
            else {
                return playerClosestNeededMeteor;
            }
        }
    }

    /// <summary>
    /// Given a needed meteor colour, finds the closest of that colour
    /// </summary>
    /// <param name="_neededColor"></param>
    /// <returns></returns>
    MeteorScript GetClosestMeteor(MeteorSpawner.MeteorColor _neededColor) {

        if (MeteorScript.meteors.Count == 0) {
            return null;
        }

        // Get all the meteors that are in play and of that colour.
        var meteorsOfNeededColor = MeteorScript.meteors.FindAll(m => m.color == _neededColor);

        /// If there are more than zero of the needed colour, get the closest
        if (meteorsOfNeededColor.Count > 0) {
            // var met = meteorsOfNeededColor.OrderBy(m => DistanceFromThis(m.transform)).First();
            return GetClosestMeteorInList(meteorsOfNeededColor);
        }
        // otherwise just get the closest of all of them
        else {
            // return MeteorScript.meteors.OrderBy(m => DistanceFromThis(m.transform)).First();
            return GetClosestMeteorInList(MeteorScript.meteors);
        }
    }
    /// <summary>
    /// Given a list (either all meteors or all of a colour) returns the closest
    /// </summary>
    /// <param name="_list"></param>
    /// <returns></returns>
    MeteorScript GetClosestMeteorInList(List<MeteorScript> _list) {

        MeteorScript closest = _list[0];
        float distance = DistanceFromThis(closest.tr);
        for (int i = 0; i < _list.Count; i++) {
            float d = DistanceFromThis(_list[i].tr);
            if (d < distance) {
                distance = d;
                closest = _list[i];
            }
        }
        return closest;
    }

    /// <summary>
    /// Returns the force that the player repels the AI, so that they don't want to collide
    /// </summary>
    /// <returns></returns>
    Vector2 CalculateRevulsionFromPlayer() {

        if (velocityCalculator.SpeedSqd() < playerVelocityCalculator.SpeedSqd()) {
            return Vector2.zero;
        }
        else {
            return Repulsion(PlayerSteer.tr.position, playerRepulsionRange, playerRepulsiveForce);
        }
    }
    /// <summary>
    /// Returns what colour the player or AI needs most
    /// </summary>
    /// <param name="isAi"></param>
    public void UpdateNeededColor(bool isAi) {
        // TODO redo this with dirty flags etc
        // TODO non linq implementation
        if (isAi) {
            // order the scores by value and get the key (a colour) of the lowest value
            var orderedScores = ai.scores.OrderBy(k => k.Value);
            score = orderedScores.FirstOrDefault().Value;
            // If AI is beating the player, go after what the player needs to protect the lead
            if (score > playerScore) {
                aiNeededColor = playerNeededColor;
                return;
            }
            // If AI is not beating the player, needed colour is lowest points, if meteor of that colour exists, otherwise second lowest points
            MeteorSpawner.MeteorColor c1 = orderedScores.FirstOrDefault().Key;
            if (MeteorScript.meteors.Exists(m => m.color == c1)) {
                aiNeededColor = c1;
            }
            else {
                aiNeededColor = orderedScores.ElementAt(1).Key;
            }
        }
        // If no meteors the AI needs, then AI needs player's most needed colour
        else {
            var orderedScores = player.scores.OrderBy(k => k.Value);
            playerScore = orderedScores.FirstOrDefault().Value;
            playerNeededColor = orderedScores.FirstOrDefault().Key;
        }
    }

    float DistanceFromThis(Transform _tr) {
        return Vector2.SqrMagnitude(tr.position - _tr.position);
    }

    Vector2 Seek(Vector2 _targetPos) {
        return ((_targetPos - (Vector2)tr.position).normalized * speed) - velocity;
    }

    Vector2 Repulsion(Vector2 _point, float _range, float _repulsiveForce) {

        if (Vector2.SqrMagnitude((Vector2)tr.position - _point) > _range) {
            return Vector2.zero;
        }

        Vector2 repulsion = (Vector2)tr.position - _point;
        repulsion /= Vector2.SqrMagnitude(repulsion); // TODO can I get away without this?
        repulsion *= _repulsiveForce;

        return repulsion;
    }

    /// <summary>
    /// Figures out whether the AI should collect its most needed meteor immediately, or wait.
    /// </summary>
    /// <param name="_meteor"></param>
    /// <returns></returns>
    bool ShouldWait(MeteorScript _meteor) {

        bool shouldWait = true;
        bool shouldNotWait = false;

        // If it's chasing after the player's most needed colour, to deny it to him, no need to wait
        if (_meteor.color == playerNeededColor) {
            return shouldNotWait;
        }
        // If there are plenty of other meteors it could be collecting, don't wait
        if (MeteorScript.meteors.Count > 3) {
            return shouldNotWait;
        }
        // If it's already big, don't wait
        if (_meteor.stage == 1) {
            return shouldNotWait;
        }
        // If its far away, don't wait (once it gets near enough it will wait)
        var distanceToMeteor = Vector2.SqrMagnitude(tr.position - _meteor.tr.position);
        if (distanceToMeteor > collectionDistance) {
            return shouldNotWait;
        }
        // If the player is close to the meteor, don't wait
        var playerDistanceToMeteor = Vector2.SqrMagnitude(PlayerSteer.tr.position - _meteor.tr.position);
        if (playerDistanceToMeteor * closeToPlayerFactor < collectionDistance) {
            return shouldNotWait;
        }
        // If the meteor will move off the screen soon, don't wait
        Vector2 aheadOfMeteor = (Vector2)_meteor.tr.position + _meteor.velocityCalculator.velocity.normalized * lookAheadOfMeteorRange;
        if (!PlayArea.singleton.InPlayArea(aheadOfMeteor)) {
            return shouldNotWait;
        }
        return shouldWait;
    }
    // Finds a point between the player and the meteor the AI is waiting to collect
    Vector2 GetLoiteringPos(MeteorScript _meteor) {
        var line = PlayerSteer.tr.position - _meteor.tr.position;
        line.Normalize();
        line *= loiterDistance;
        return _meteor.tr.position + line;
    }
}