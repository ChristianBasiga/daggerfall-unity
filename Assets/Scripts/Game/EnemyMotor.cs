// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2018 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:    Allofich
// 
// Notes:
//

using UnityEngine;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.MagicAndEffects;
using System.Collections.Generic;

namespace DaggerfallWorkshop.Game
{
    /// <summary>
    /// Enemy motor and AI combat decision-making logic.
    /// </summary>
    [RequireComponent(typeof(EnemySenses))]
    [RequireComponent(typeof(EnemyAttack))]
    [RequireComponent(typeof(EnemyBlood))]
    [RequireComponent(typeof(EnemySounds))]
    [RequireComponent(typeof(CharacterController))]
    public class EnemyMotor : MonoBehaviour
    {
        public float OpenDoorDistance = 2f;         // Maximum distance to open door

        public const float AttackSpeedDivisor = 3f; // How much to slow down during attack animations
        float stopDistance = 1.7f;                  // Used to prevent orbiting
        int giveUpTimer;                            // Timer before enemy gives up
        bool isHostile;                             // Is enemy hostile to player
        bool flies;                                 // The enemy can fly
        bool swims;                                 // The enemy can swim
        bool pausePursuit;                          // pause to wait for the player to come closer to ground
        int enemyLayerMask;                         // Layer mask for Enemies to optimize collision checks
        bool isLevitating;                          // Allow non-flying enemy to levitate
        float classicUpdateTimer;                   // Timer for matching classic's update loop
        bool classicUpdate;                         // True when reached a classic update
        float knockBackSpeed;                       // While non-zero, this enemy will be knocked backwards at this speed
        Vector3 knockBackDirection;                 // Direction to travel while being knocked back
        float moveInForAttackTimer;                 // Time until next pursue/retreat decision
        bool moveInForAttack;                       // False = retreat. True = pursue.
        float retreatDistanceMultiplier;            // How far to back off while retreating
        float changeStateTimer;                     // Time until next change in behavior. Padding to prevent instant reflexes.
        bool pursuing;                              // Is pursuing
        bool retreating;                            // Is retreating
        bool fallDetected;                          // Detected a fall in front of us, so don't move there
        bool obstacleDetected;
        Vector3 lastPosition;                       // Used to track whether we have moved or not
        Vector3 lastDirection;                      // Used to track whether we have rotated or not
        bool rotating;                              // Used to track whether we have rotated or not
        float avoidObstaclesTimer;
        bool lookingForDetour;
        bool checkingClockWise;
        int checkingClockWiseCounter;
        float lastYPos;
        int detourNumber;
        float lastTimeWasStuck;

        EnemySenses senses;
        Vector3 targetPos;
        Vector3 tempMovePos;
        CharacterController controller;
        DaggerfallMobileUnit mobile;
        DaggerfallEntityBehaviour entityBehaviour;
        EntityEffectManager entityEffectManager;
        EntityEffectBundle selectedSpell;
        EnemyAttack attack;
        EnemyEntity entity;

        public bool IsLevitating
        {
            get { return isLevitating; }
            set { isLevitating = value; }
        }

        public bool IsHostile
        {
            get { return isHostile; }
            set { isHostile = value; }
        }

        public float KnockBackSpeed
        {
            get { return knockBackSpeed; }
            set { knockBackSpeed = value; }
        }

        public Vector3 KnockBackDirection
        {
            get { return knockBackDirection; }
            set { knockBackDirection = value; }
        }

        void Start()
        {
            senses = GetComponent<EnemySenses>();
            controller = GetComponent<CharacterController>();
            mobile = GetComponentInChildren<DaggerfallMobileUnit>();
            isHostile = mobile.Summary.Enemy.Reactions == MobileReactions.Hostile;
            flies = mobile.Summary.Enemy.Behaviour == MobileBehaviour.Flying ||
                    mobile.Summary.Enemy.Behaviour == MobileBehaviour.Spectral;
            swims = mobile.Summary.Enemy.Behaviour == MobileBehaviour.Aquatic;
            enemyLayerMask = LayerMask.GetMask("Enemies");
            entityBehaviour = GetComponent<DaggerfallEntityBehaviour>();
            entityEffectManager = GetComponent<EntityEffectManager>();
            entity = entityBehaviour.Entity as EnemyEntity;
            attack = GetComponent<EnemyAttack>();

            // Classic AI moves only as close as melee range
            if (!DaggerfallUnity.Settings.EnhancedCombatAI)
                stopDistance = attack.MeleeDistance;
        }

        void FixedUpdate()
        {
            classicUpdateTimer += Time.deltaTime;
            if (classicUpdateTimer >= PlayerEntity.ClassicUpdateInterval)
            {
                classicUpdateTimer = 0;
                classicUpdate = true;
            }
            else
                classicUpdate = false;

            Move();
            OpenDoors();
        }

        #region Public Methods

        /// <summary>
        /// Immediately become hostile towards attacker and know attacker's location.
        /// </summary>
        public void MakeEnemyHostileToAttacker(DaggerfallEntityBehaviour attacker)
        {
            if (attacker && senses)
            {
                // Assign target if don't already have target, or original target isn't seen or adjacent
                if (entityBehaviour.Target == null || !senses.TargetInSight || senses.DistanceToTarget > 2f)
                    entityBehaviour.Target = attacker;
                senses.LastKnownTargetPos = attacker.transform.position;
                senses.OldLastKnownTargetPos = attacker.transform.position;
                senses.PredictedTargetPos = attacker.transform.position;
                giveUpTimer = 200;
            }

            if (attacker == GameManager.Instance.PlayerEntityBehaviour)
                isHostile = true;
        }

        /// <summary>
        /// Attempts to find the ground position below enemy, even if player is flying/falling
        /// </summary>
        /// <param name="distance">Distance to fire ray.</param>
        /// <returns>Hit point on surface below enemy, or enemy position if hit not found in distance.</returns>
        public Vector3 FindGroundPosition(float distance = 16)
        {
            RaycastHit hit;
            Ray ray = new Ray(transform.position, Vector3.down);
            if (Physics.Raycast(ray, out hit, distance))
                return hit.point;

            return transform.position;
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Avoid other AI characters by checking for collisions with them along the planned motion vector.
        /// </summary>
        /// <param name="plannedMotion">Path to check for collisions. This will be updated if a collision is found.</param>
        void AvoidEnemies(ref Vector3 plannedMotion)
        {
            // Compute the capsule start/end points for the casting operation
            var capsuleStart = transform.position;
            var capsuleEnd = transform.position;
            capsuleStart.y += (controller.height / 2) - controller.radius;
            capsuleEnd.y -= (controller.height / 2) + controller.radius;

            // We capsule cast because a ray might grace the edge of an enemy and allow it to move across & over
            // We use cast all to detect a collision at the start of the cast as well
            // To optimize, cast only in enemy layer and don't cause triggers to fire
            var hits = Physics.CapsuleCastAll(capsuleStart, capsuleEnd, controller.radius, plannedMotion,
                controller.radius * 2, enemyLayerMask, QueryTriggerInteraction.Ignore);

            // Note: CapsuleCastAll doesn't know about the "source", so "this" enemy will always count as a collision.
            if (hits.Length <= 1)
                return;

            // Simplest approach: Stop moving.
            plannedMotion *= 0;

            if (DaggerfallUnity.Settings.EnhancedCombatAI)
            {
                SetChangeStateTimer();
                pursuing = false;
                retreating = false;
            }

            // Slightly better approach: Route around.
            // This isn't perfect. In some cases enemies may still stack. It seems to happen when enemies are very close.
            // Always choose one direction. If this is random, the enemy will wiggle behind the other enemy because it's
            // computed so frequently. We could choose a direction at a lower rate to still give some randomness.
            // plannedMotion = Quaternion.Euler(0, 90, 0) * plannedMotion;
        }

        /// <summary>
        /// Make decision about what movement action to take.
        /// </summary>
        void Move()
        {
            // Cancel movement and animations if paralyzed, but still allow gravity to take effect
            // This will have the (intentional for now) side-effect of making paralyzed flying enemies fall out of the air
            // Paralyzed swimming enemies will just freeze in place
            // Freezing anims also prevents the attack from triggering until paralysis cleared
            if (entityBehaviour.Entity.IsParalyzed)
            {
                mobile.FreezeAnims = true;

                if ((swims || flies) && !isLevitating)
                    controller.Move(Vector3.zero);
                else
                    controller.SimpleMove(Vector3.zero);

                return;
            }
            mobile.FreezeAnims = false;

            // Apply gravity to non-moving AI if active (has a combat target) or nearby
            if ((entityBehaviour.Target != null || senses.WouldBeSpawnedInClassic) && !flies && !swims)
            {
                controller.SimpleMove(Vector3.zero);
            }

            // If hit, get knocked back
            if (knockBackSpeed > 0)
            {
                // Limit knockBackSpeed. This can be higher than what is actually used for the speed of motion,
                // making it last longer and do more damage if the enemy collides with something (TODO).
                if (knockBackSpeed > (40 / (PlayerSpeedChanger.classicToUnitySpeedUnitRatio / 10)))
                    knockBackSpeed = (40 / (PlayerSpeedChanger.classicToUnitySpeedUnitRatio / 10));

                if (knockBackSpeed > (5 / (PlayerSpeedChanger.classicToUnitySpeedUnitRatio / 10)) &&
                    mobile.Summary.EnemyState != MobileStates.PrimaryAttack)
                {
                    mobile.ChangeEnemyState(MobileStates.Hurt);
                }

                // Actual speed of motion is limited
                Vector3 motion;
                if (knockBackSpeed <= (25 / (PlayerSpeedChanger.classicToUnitySpeedUnitRatio / 10)))
                    motion = knockBackDirection * knockBackSpeed;
                else
                    motion = knockBackDirection * (25 / (PlayerSpeedChanger.classicToUnitySpeedUnitRatio / 10));

                // Move in direction of knockback
                if (swims)
                    WaterMove(motion);
                else if (flies || isLevitating)
                    controller.Move(motion * Time.deltaTime);
                else
                    controller.SimpleMove(motion);

                // Remove remaining knockback and restore animation
                if (classicUpdate)
                {
                    knockBackSpeed -= (5 / (PlayerSpeedChanger.classicToUnitySpeedUnitRatio / 10));
                    if (knockBackSpeed <= (5 / (PlayerSpeedChanger.classicToUnitySpeedUnitRatio / 10))
                        && mobile.Summary.EnemyState != MobileStates.PrimaryAttack)
                    {
                        mobile.ChangeEnemyState(MobileStates.Move);
                    }
                }

                // If a decent hit got in, reconsider whether to continue current tactic
                if (knockBackSpeed > (10 / (PlayerSpeedChanger.classicToUnitySpeedUnitRatio / 10)))
                {
                    EvaluateMoveInForAttack();
                }

                return;
            }

            // Monster speed of movement follows the same formula as for when the player walks
            float moveSpeed = (entity.Stats.LiveSpeed + PlayerSpeedChanger.dfWalkBase) * MeshReader.GlobalScale;

            // Reduced speed if playing a one-shot animation with enhanced AI
            if (mobile.IsPlayingOneShot() && DaggerfallUnity.Settings.EnhancedCombatAI)
                moveSpeed /= AttackSpeedDivisor;

            // As long as the target is detected,
            // giveUpTimer is reset to full
            if (senses.DetectedTarget)
                giveUpTimer = 200;

            // GiveUpTimer value is from classic, so decrease at the speed of classic's update loop
            if (!senses.DetectedTarget
                && giveUpTimer > 0 && classicUpdate)
            {
                giveUpTimer--;
            }

            // Change to idle animation if haven't moved or rotated
            if (!mobile.IsPlayingOneShot())
            {
                // Rotation is done at classic update rate, so check at classic update rate
                if (classicUpdate)
                {
                    Vector3 currentDirection = transform.forward;
                    currentDirection.y = 0;

                    if (lastPosition == transform.position && lastDirection == currentDirection)
                    {
                        mobile.ChangeEnemyState(MobileStates.Idle);
                        rotating = false;
                    }
                    else
                        mobile.ChangeEnemyState(MobileStates.Move);

                    lastDirection = currentDirection;
                }
                // Movement is done at regular update rate, so check at regular update rate
                else if (!rotating && lastPosition == transform.position)
                    mobile.ChangeEnemyState(MobileStates.Idle);
                else
                    mobile.ChangeEnemyState(MobileStates.Move);

                lastPosition = transform.position;
            }

            // Do nothing if no target or after giving up finding the target
            if (entityBehaviour.Target == null || giveUpTimer == 0)
            {
                SetChangeStateTimer();

                return;
            }

            // Get predicted target position
            if (avoidObstaclesTimer == 0 && !lookingForDetour)
            {
                targetPos = senses.PredictedTargetPos;
                // Flying enemies and slaughterfish aim for target face
                if (flies || isLevitating || (swims && mobile.Summary.Enemy.ID == (int)MonsterCareers.Slaughterfish))
                    targetPos.y += 0.9f;
                else
                {
                    // Ground enemies target at their own height
                    // This avoids short enemies from stepping on each other as they approach the target
                    // Otherwise, their target vector aims up towards the target
                    var playerController = GameManager.Instance.PlayerController;
                    var deltaHeight = (playerController.height - controller.height) / 2;
                    targetPos.y -= deltaHeight;
                }
                tempMovePos = targetPos;
            }
            else
            {
                targetPos = tempMovePos;
            }

            // Get direction & distance.
            var direction = targetPos - transform.position;
            float distance = (targetPos - transform.position).magnitude;

            // Ranged attacks
            if (senses.TargetInSight && 360 * MeshReader.GlobalScale < senses.DistanceToTarget && senses.DistanceToTarget < 2048 * MeshReader.GlobalScale)
            {
                bool evaluateBow = mobile.Summary.Enemy.HasRangedAttack1 && mobile.Summary.Enemy.ID > 129 && mobile.Summary.Enemy.ID != 132;
                bool evaluateRangedMagic = false;
                if (!evaluateBow)
                    evaluateRangedMagic = CanCastRangedSpell();

                if (evaluateBow || evaluateRangedMagic)
                {
                    if (senses.TargetIsWithinYawAngle(22.5f, senses.LastKnownTargetPos))
                    {
                        if (!mobile.IsPlayingOneShot())
                        {
                            if (evaluateBow)
                            {
                                // Random chance to shoot bow
                                if (classicUpdate && DFRandom.rand() < 1000)
                                {
                                    if (mobile.Summary.Enemy.HasRangedAttack1 && !mobile.Summary.Enemy.HasRangedAttack2)
                                        mobile.ChangeEnemyState(MobileStates.RangedAttack1);
                                    else if (mobile.Summary.Enemy.HasRangedAttack2)
                                        mobile.ChangeEnemyState(MobileStates.RangedAttack2);
                                }
                            }
                            // Random chance to shoot spell
                            else if (classicUpdate && DFRandom.rand() % 40 == 0
                                && entityEffectManager.SetReadySpell(selectedSpell))
                            {
                                mobile.ChangeEnemyState(MobileStates.Spell);
                            }
                        }
                    }
                    else
                        TurnToTarget(direction.normalized);

                    return;
                }
            }

            if (senses.TargetInSight && attack.MeleeTimer == 0 && senses.DistanceToTarget <= attack.MeleeDistance +
                senses.TargetRateOfApproach && CanCastTouchSpell() && entityEffectManager.SetReadySpell(selectedSpell))
            {
                if (mobile.Summary.EnemyState != MobileStates.Spell)
                    mobile.ChangeEnemyState(MobileStates.Spell);

                attack.ResetMeleeTimer();
                return;
            }

            // Update melee decision
            if (moveInForAttackTimer <= 0 && avoidObstaclesTimer == 0 && !lookingForDetour)
                EvaluateMoveInForAttack();
            if (moveInForAttackTimer > 0)
                moveInForAttackTimer -= Time.deltaTime;

            if (avoidObstaclesTimer > 0)
                avoidObstaclesTimer -= Time.deltaTime;
            if (avoidObstaclesTimer < 0)
                avoidObstaclesTimer = 0;

            if (changeStateTimer > 0)
                changeStateTimer -= Time.deltaTime;

            // Looking for detour
            if (lookingForDetour)
            {
                CombatMove(direction, moveSpeed);
            }
            // Approach target until we are close enough to be on-guard, or continue to melee range if attacking
            else if ((!retreating && distance >= (stopDistance * 2.75))
                    || (distance > stopDistance && moveInForAttack))
            {
                // If state change timer is done, or we are already pursuing, we can move
                if (changeStateTimer <= 0 || pursuing)
                    CombatMove(direction, moveSpeed);
                // Otherwise, just keep an eye on target until timer finishes
                else if (!senses.TargetIsWithinYawAngle(22.5f, targetPos))
                    TurnToTarget(direction.normalized);
            }
            // Back away if right next to target, if retreating, or if cooling down from attack
            // Classic AI never backs away
            else if (DaggerfallUnity.Settings.EnhancedCombatAI && (senses.TargetInSight && (distance < stopDistance * .50 ||
                (!moveInForAttack && distance < (stopDistance * retreatDistanceMultiplier)))))
            {
                // If state change timer is done, or we are already retreating, we can move
                if (changeStateTimer <= 0 || retreating)
                    CombatMove(direction, moveSpeed / 2, true);
                // Otherwise, just keep an eye on target until timer finishes
                else if (!senses.TargetIsWithinYawAngle(22.5f, targetPos))
                    TurnToTarget(direction.normalized);
            }
            else if (!senses.TargetIsWithinYawAngle(22.5f, targetPos))
                TurnToTarget(direction.normalized);
            else if (avoidObstaclesTimer > 0 && distance > 0.1f)
            {
                CombatMove(direction, moveSpeed);
            }
            else // Next to target
            {
                SetChangeStateTimer();
                pursuing = false;
                retreating = false;

                avoidObstaclesTimer = 0;
            }
        }

        /// <summary>
        /// Selects a ranged spell from this enemy's list and returns true if it can be cast.
        /// </summary>
        bool CanCastRangedSpell()
        {
            if (entity.CurrentMagicka <= 0)
                return false;

            EffectBundleSettings[] spells = entity.GetSpells();
            List<EffectBundleSettings> rangeSpells = new List<EffectBundleSettings>();
            int count = 0;
            foreach (EffectBundleSettings spell in spells)
            {
                if (spell.TargetType == TargetTypes.SingleTargetAtRange
                    || spell.TargetType == TargetTypes.AreaAtRange)
                {
                    rangeSpells.Add(spell);
                    count++;
                }
            }

            if (count == 0)
                return false;

            EffectBundleSettings selectedSpellSettings = rangeSpells[Random.Range(0, count)];
            selectedSpell = new EntityEffectBundle(selectedSpellSettings, entityBehaviour);

            int totalGoldCostUnused;
            int readySpellCastingCost;

            Formulas.FormulaHelper.CalculateTotalEffectCosts(selectedSpell.Settings.Effects, selectedSpell.Settings.TargetType, out totalGoldCostUnused, out readySpellCastingCost);
            if (entity.CurrentMagicka < readySpellCastingCost)
                return false;

            if (EffectsAlreadyOnTarget(selectedSpell))
                return false;

            return true;
        }

        /// <summary>
        /// Selects a touch spell from this enemy's list and returns true if it can be cast.
        /// </summary>
        bool CanCastTouchSpell()
        {
            if (entity.CurrentMagicka <= 0)
                return false;

            EffectBundleSettings[] spells = entity.GetSpells();
            List<EffectBundleSettings> rangeSpells = new List<EffectBundleSettings>();
            int count = 0;
            foreach (EffectBundleSettings spell in spells)
            {
                // Classic AI considers ByTouch and CasterOnly here
                if (!DaggerfallUnity.Settings.EnhancedCombatAI)
                {
                    if (spell.TargetType == TargetTypes.ByTouch
                        || spell.TargetType == TargetTypes.CasterOnly)
                    {
                        rangeSpells.Add(spell);
                        count++;
                    }
                }
                else // Enhanced AI considers ByTouch and AreaAroundCaster. TODO: CasterOnly logic
                {
                    if (spell.TargetType == TargetTypes.ByTouch
                        || spell.TargetType == TargetTypes.AreaAroundCaster)
                    {
                        rangeSpells.Add(spell);
                        count++;
                    }
                }
            }

            if (count == 0)
                return false;

            EffectBundleSettings selectedSpellSettings = rangeSpells[Random.Range(0, count)];
            selectedSpell = new EntityEffectBundle(selectedSpellSettings, entityBehaviour);

            int totalGoldCostUnused;
            int readySpellCastingCost;

            Formulas.FormulaHelper.CalculateTotalEffectCosts(selectedSpell.Settings.Effects, selectedSpell.Settings.TargetType, out totalGoldCostUnused, out readySpellCastingCost);
            if (entity.CurrentMagicka < readySpellCastingCost)
                return false;

            if (EffectsAlreadyOnTarget(selectedSpell))
                return false;

            return true;
        }

        /// <summary>
        /// Checks whether the target already is affected by all of the effects of the given spell.
        /// </summary>
        bool EffectsAlreadyOnTarget(EntityEffectBundle spell)
        {
            if (entityBehaviour.Target)
            {
                EntityEffectManager targetEffectManager = entityBehaviour.Target.GetComponent<EntityEffectManager>();
                LiveEffectBundle[] bundles = targetEffectManager.EffectBundles;

                for (int i = 0; i < spell.Settings.Effects.Length; i++)
                {
                    bool foundEffect = false;
                    // Get effect template
                    IEntityEffect effectTemplate = GameManager.Instance.EntityEffectBroker.GetEffectTemplate(spell.Settings.Effects[i].Key);
                    for (int j = 0; j < bundles.Length && !foundEffect; j++)
                    {
                        for (int k = 0; k < bundles[j].liveEffects.Count && !foundEffect; k++)
                        {
                            if (bundles[j].liveEffects[k].GetType() == effectTemplate.GetType())
                                foundEffect = true;
                        }
                    }

                    if (!foundEffect)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Maneuver in combat with target
        /// </summary>
        void CombatMove(Vector3 direction, float moveSpeed, bool backAway = false)
        {
            if (!backAway)
            {
                pursuing = true;
                retreating = false;
            }
            else
            {
                retreating = true;
                pursuing = false;
            }

            if (!senses.TargetIsWithinYawAngle(5.625f, targetPos))
            {
                TurnToTarget(direction.normalized);
                // Classic always turns in place. Enhanced only does so if enemy is not in sight.
                if (!DaggerfallUnity.Settings.EnhancedCombatAI || !senses.TargetInSight)
                    return;
            }

            Vector3 motion = transform.forward * moveSpeed;
            if (backAway)
                motion *= -1;

            // If using enhanced combat, avoid moving directly below targets
            if (!backAway && DaggerfallUnity.Settings.EnhancedCombatAI && avoidObstaclesTimer == 0 && !lookingForDetour)
            {
                bool withinPitch = senses.TargetIsWithinPitchAngle(45.0f);
                if (!pausePursuit && !withinPitch)
                {
                    if (flies || isLevitating || swims)
                    {
                        if (!senses.TargetIsAbove())
                            motion = -transform.up * moveSpeed;
                        else
                            motion = transform.up * moveSpeed;
                    }
                    // Causes a random delay after being out of pitch range
                    else if (senses.TargetIsAbove() && changeStateTimer <= 0)
                    {
                        SetChangeStateTimer();
                        pausePursuit = true;
                    }
                }
                else if (pausePursuit && withinPitch)
                    pausePursuit = false;

                if (pausePursuit)
                {
                    if (senses.TargetIsAbove() && !senses.TargetIsWithinPitchAngle(55.0f) && changeStateTimer <= 0)
                    {
                        // Back away from target
                        motion = -transform.forward * moveSpeed * 0.75f;
                    }
                    else
                    {
                        // Stop moving
                        return;
                    }
                }
            }

            // Avoid other enemies, and stop enemies from moving on top of shorter enemies
            //AvoidEnemies(ref motion);

            // Return if AvoidEnemies set change timer
            //if (changeStateTimer > 0 && !pursuing && !retreating)
                //return;

            SetChangeStateTimer();
            if (swims)
                WaterMove(motion);
            else if (flies || isLevitating)
                controller.Move(motion * Time.deltaTime);
            else
                MoveIfNoFallDetected(motion);
        }

        /// <summary>
        /// Check for a large fall, and proceed with move if none found.
        /// </summary>
        void MoveIfNoFallDetected(Vector3 motion)
        {
            // Check at classic rate to limit ray casts
            if (classicUpdate)
            {
                obstacleDetected = false;
                fallDetected = false;
                float currentYPos = transform.position.y;

                // First check if there is something to collide with directly in movement direction, such as upward sloping ground.
                // If there is, we assume we won't fall.
                RaycastHit hit;
                Vector3 motion2d = motion.normalized;
                motion2d.y = 0;
                int checkDistance = 2;
                Vector3 rayOrigin = transform.position;
                rayOrigin.y -= controller.height / 4;

                if (targetPos.y > transform.position.y + controller.height / 2)
                {
                    rayOrigin.y += controller.height / 2;
                }

                Ray ray = new Ray(rayOrigin, motion2d);

                if (Physics.Raycast(ray, out hit, checkDistance))
                {
                    fallDetected = false;
                    obstacleDetected = true;

                    if (lastYPos < currentYPos)
                        obstacleDetected = false;

                    DaggerfallEntityBehaviour entityBehaviour2 = hit.transform.GetComponent<DaggerfallEntityBehaviour>();
                    if (entityBehaviour2 == entityBehaviour.Target)
                        obstacleDetected = false;

                    DaggerfallActionDoor door = hit.transform.GetComponent<DaggerfallActionDoor>();
                    if (door)
                        obstacleDetected = false;

                    DaggerfallLoot loot = hit.transform.GetComponent<DaggerfallLoot>();
                    if (loot)
                        obstacleDetected = false;
                }
                // Nothing to collide with. Check for a long fall.
                else
                {
                    motion2d *= checkDistance;
                    ray = new Ray(rayOrigin + motion2d, Vector3.down);
                    fallDetected = !Physics.Raycast(ray, out hit, 5);
                }

                if ((fallDetected || obstacleDetected) && DaggerfallUnity.Settings.EnhancedCombatAI)
                    FindDetour(motion);

                lastYPos = currentYPos;
            }

            if (!fallDetected && !obstacleDetected)
            {
                controller.SimpleMove(motion);

                if (lookingForDetour)
                {
                    lookingForDetour = false;
                    avoidObstaclesTimer = .5f;
                    lastTimeWasStuck = Time.time;
                    detourNumber--;
                }
            }
            if (Time.time - lastTimeWasStuck > 3f)
                detourNumber = 0;
        }

        void FindDetour(Vector3 motion)
        {
            Vector3 motion2d = motion;
            motion2d.y = 0;

            // First get whether we check clockwise or counterclockwise
            if (checkingClockWiseCounter == 0)
            {
                Vector3 toTarget = targetPos - transform.position;
                Vector3 directionToTarget = toTarget.normalized;
                float angleToTarget = Vector3.SignedAngle(directionToTarget, motion, Vector3.up);

                if (angleToTarget > 0)
                {
                    checkingClockWise = false;
                }
                else
                    checkingClockWise = true;


                if (checkingClockWise)
                    angleToTarget = 30;
                else
                    angleToTarget = -30;
                RaycastHit hit;
                Vector3 testAngle = Quaternion.AngleAxis(angleToTarget, Vector3.up) * motion;
                motion2d.y = 0;
                int checkDistance = 2;
                Vector3 rayOrigin = transform.position;
                rayOrigin.y -= controller.height / 4;

                if (targetPos.y > transform.position.y + controller.height / 2)
                {
                    rayOrigin.y += controller.height / 2;
                }

                Ray ray = new Ray(rayOrigin, testAngle);

                if (Physics.Raycast(ray, out hit, checkDistance))
                {
                    bool testObstacleDetected = true;
                    if (lastYPos < transform.position.y)
                        testObstacleDetected = false;

                    DaggerfallEntityBehaviour entityBehaviour2 = hit.transform.GetComponent<DaggerfallEntityBehaviour>();
                    if (entityBehaviour2 == entityBehaviour.Target)
                        testObstacleDetected = false;

                    DaggerfallActionDoor door = hit.transform.GetComponent<DaggerfallActionDoor>();
                    if (door)
                        testObstacleDetected = false;

                    DaggerfallLoot loot = hit.transform.GetComponent<DaggerfallLoot>();
                    if (loot)
                        testObstacleDetected = false;

                    if (testObstacleDetected)
                        // Tested 30 degrees in the clockwise/counter-clockwise direction we chose,
                        // but hit something, so try other one.
                        checkingClockWise = !checkingClockWise;
                }
                checkingClockWiseCounter = 5;
            }
            else
                checkingClockWiseCounter--;

            float angle = 15;
            if (!checkingClockWise)
                angle *= -1;

            Vector3 detour;
            if (detourNumber == 0)
                detour = Quaternion.AngleAxis(angle, Vector3.up) * motion;
            else if (detourNumber == 1)
            {
                angle *= 2;
                detour = Quaternion.AngleAxis(angle, Vector3.up) * motion;
            }
            else if (detourNumber == 2)
            {
                angle *= 3;
                detour = Quaternion.AngleAxis(angle, Vector3.up) * motion;
            }
            else if (detourNumber == 3)
            {
                angle *= 4;
                detour = Quaternion.AngleAxis(angle, Vector3.up) * motion;
            }
            else if (detourNumber == 4)
            {
                angle *= 5;
                detour = Quaternion.AngleAxis(angle, Vector3.up) * motion;
            }
            else if(detourNumber == 5)
            {
                angle *= 6;
                detour = Quaternion.AngleAxis(angle, Vector3.up) * motion;
            }
            else if (detourNumber == 6)
            {
                angle *= -1;
                detour = Quaternion.AngleAxis(angle, Vector3.up) * motion;
            }
            else if (detourNumber == 7)
            {
                angle *= -2;
                detour = Quaternion.AngleAxis(angle, Vector3.up) * motion;
            }
            else if (detourNumber == 8)
            {
                angle *= -3;
                detour = Quaternion.AngleAxis(angle, Vector3.up) * motion;
            }
            else if (detourNumber == 9)
            {
                angle *= -4;
                detour = Quaternion.AngleAxis(angle, Vector3.up) * motion;
            }
            else if (detourNumber == 10)
            {
                angle *= -5;
                detour = Quaternion.AngleAxis(angle, Vector3.up) * motion;
            }
            else if (detourNumber == 11)
            {
                angle *= -6;
                detour = Quaternion.AngleAxis(angle, Vector3.up) * motion;
            }
            else if (detourNumber == 12)
            {
                angle *= 7;
                detour = Quaternion.AngleAxis(angle, Vector3.up) * motion;
            }
            else if (detourNumber == 13)
            {
                angle *= -7;
                detour = Quaternion.AngleAxis(angle, Vector3.up) * motion;
            }
            else if (detourNumber == 14)
            {
                angle *= 8;
                detour = Quaternion.AngleAxis(angle, Vector3.up) * motion;
            }
            else
            {
                angle *= -8;
                detour = Quaternion.AngleAxis(angle, Vector3.up) * motion;
            }

            detourNumber++;
            if (detourNumber == 16)
                detourNumber = 0;

            tempMovePos = transform.position + detour.normalized * 3;
            tempMovePos.y = transform.position.y;

            lookingForDetour = true;

            moveInForAttack = true;
        }

        /// <summary>
        /// Decide whether or not to pursue enemy, based on perceived combat odds.
        /// </summary>
        void EvaluateMoveInForAttack()
        {
            // Classic always attacks
            if (!DaggerfallUnity.Settings.EnhancedCombatAI)
            {
                moveInForAttack = true;
                return;
            }

            // No retreat from unseen opponent
            if (!senses.TargetInSight)
            {
                moveInForAttack = true;
                return;
            }

            // No retreat if enemy is paralyzed
            if (entityBehaviour.Target != null)
            {
                EntityEffectManager targetEffectManager = entityBehaviour.Target.GetComponent<EntityEffectManager>();
                if (targetEffectManager.FindIncumbentEffect<MagicAndEffects.MagicEffects.Paralyze>() != null)
                {
                    moveInForAttack = true;
                    return;
                }

                // No retreat if enemy's back is turned
                if (senses.TargetHasBackTurned())
                {
                    moveInForAttack = true;
                    return;
                }

                // No retreat if enemy is player with bow or weapon not out
                if (entityBehaviour.Target == GameManager.Instance.PlayerEntityBehaviour
                    && GameManager.Instance.WeaponManager.ScreenWeapon
                    && (GameManager.Instance.WeaponManager.ScreenWeapon.WeaponType == WeaponTypes.Bow
                    || !GameManager.Instance.WeaponManager.ScreenWeapon.ShowWeapon))
                {
                    moveInForAttack = true;
                    return;
                }
            }
            else
            {
                return;
            }

            const float retreatDistanceBaseMult = 2.25f;

            // Level difference affects likelihood of backing away.
            moveInForAttackTimer = Random.Range(1, 3);
            int levelMod = (entity.Level - entityBehaviour.Target.Entity.Level) / 2;
            if (levelMod > 4)
                levelMod = 4;
            if (levelMod < -4)
                levelMod = -4;

            int roll = Random.Range(0 + levelMod, 10 + levelMod);

            moveInForAttack = roll > 4;

            // Chose to retreat
            if (!moveInForAttack)
                retreatDistanceMultiplier = (float)(retreatDistanceBaseMult + (retreatDistanceBaseMult * (0.25 * (2 - roll))));
        }

        /// <summary>
        /// Set timer for padding between state changes, for non-perfect reflexes.
        /// </summary>
        void SetChangeStateTimer()
        {
            // No timer without enhanced AI
            if (!DaggerfallUnity.Settings.EnhancedCombatAI)
                return;

            if (changeStateTimer <= 0)
                changeStateTimer = Random.Range(0.2f, .8f);
        }

        /// <summary>
        /// Movement for water enemies.
        /// </summary>
        void WaterMove(Vector3 motion)
        {
            // Don't allow aquatic enemies to go above the water level of a dungeon block
            if (GameManager.Instance.PlayerEnterExit.blockWaterLevel != 10000
                    && controller.transform.position.y
                    < GameManager.Instance.PlayerEnterExit.blockWaterLevel * -1 * MeshReader.GlobalScale)
            {
                if (motion.y > 0 && controller.transform.position.y + (100 * MeshReader.GlobalScale)
                        >= GameManager.Instance.PlayerEnterExit.blockWaterLevel * -1 * MeshReader.GlobalScale)
                {
                    motion.y = 0;
                }
                controller.Move(motion * Time.deltaTime);
            }
        }

        /// <summary>
        /// Rotate toward target.
        /// </summary>
        void TurnToTarget(Vector3 targetDirection)
        {
            const float turnSpeed = 20f;
            //Classic speed is 11.25f, too slow for Daggerfall Unity's agile player movement

            if (classicUpdate)
            {
                transform.forward = Vector3.RotateTowards(transform.forward, targetDirection, turnSpeed * Mathf.Deg2Rad, 0.0f);
                    rotating = true;
            }
        }

        /// <summary>
        /// Open doors that are in the way.
        /// </summary>
        void OpenDoors()
        {
            // Try to open doors blocking way
            if (mobile.Summary.Enemy.CanOpenDoors && senses.LastKnownDoor != null
                && senses.DistanceToDoor < OpenDoorDistance && !senses.LastKnownDoor.IsOpen)
            {
                senses.LastKnownDoor.ToggleDoor();
            }
        }

        #endregion
    }
}
