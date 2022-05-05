using System;
using System.Collections.Generic;
using UnityEngine;
using Warner.AnimationTool;

namespace Warner
    {
    [RequireComponent(typeof(Character))]
    public class CharacterMovements : MonoBehaviour
        {
        #region MEMBER FIELDS

        public bool isAirCharacter;
        [Range(0f, 10f)]
        public float defaultGravity = 3f;
        public MovementSettings horizontal;
        public MovementSettings vertical;
        public JumpSettings jumpSettings;

        [NonSerialized] public Character character;
        [NonSerialized] public Rigidbody2D rigidBody;
        [NonSerialized] public bool autoMoving;
        [NonSerialized] public int movingSideX;
        [NonSerialized] public int lastMovingSideX;
        [NonSerialized] public int movingSideY;
        [NonSerialized] public int lastMovingSideY;
        [NonSerialized] public LayerMask groundLayer;
        [NonSerialized] public bool jumping;
        [NonSerialized] public JumpType jumpType;
        [NonSerialized] public Vector2 currentSpeedFactor;
        [NonSerialized] public bool reachedMaxHorizontalSpeed;
        [NonSerialized] public bool reachedMaxVerticalSpeed;
        [NonSerialized] public bool handleMovement = true;
        [NonSerialized] public bool grounded;

        public enum JumpType { Normal, Air, Big, BigHit }

        [Serializable]
        public struct MovementSettings
            {
            [Range(0f, 10f)] public float startSpeed;
            [Range(0f, 1f)] public float startSpeedScalar;
            [Range(0f, 20f)] public float constantSpeed;
            [Range(0f, 1f)] public float easeInScalar;
            [Range(-1f, 5f)] public float turnEaseInScalar;
            [Range(0f, 0.5f)] public float easeOut;
            }


        [Serializable]
        public struct JumpSettings
            {
            public bool jump;
            public bool airJump;
            [Range(0, 10)] public int maxAirJumpCount;
            [Range(1f, 20f)]
            public float force;
            [Range(1f, 20f)]
            public float airForce;
            [Range(1f, 40f)]
            public float bigAirForce;
            [Range(1f, 40f)]
            public float bigAirHitForce;
            [Range(0f, 0.1f)]
            public float forceDelay;
            [Range(0f, 10f)]
            public float pressedForce;
            [Range(0, 40)]
            public int pressedForceDuration;
            [Range(-1f, 1f)]
            public float gravityForce;
            [Range(0f, 1f)]
            public float afterHitGravityForce;
            [Range(0, 10)]
            public int gravityForceStartOffset;
            [Range(0f, 1f)]
            public float airMovement;
            [Range(0f, 1f)]
            public float bigAirMovement;
            [Range(0f, 1f)]
            public float noMovementDecay;
            [Range(0f, 10f)]
            public float landingStopSlide;
            }

        public delegate void EventsHandler(object data = null);

        public event EventsHandler onAimingRightChange;
        public event EventsHandler onStartedMoving;
        public event EventsHandler onStoppedMovingHorizontally;
        public event EventsHandler onHorizontalDirectionChanged;
        public event EventsHandler onStoppedMovingVertically;
        public event EventsHandler onVerticalDirectionChanged;
        public event EventsHandler onJump;
        public event EventsHandler onLandedFromJump;
        public event EventsHandler onBlock;
        public event EventsHandler onBlockHold;
        public event EventsHandler onBlockReleased;
        public event EventsHandler onDodge;
        public event EventsHandler onDodgeEnd;
        public event EventsHandler onVaulting;
        public event EventsHandler onVaultingEnd;
        public event EventsHandler onTaunt;
        public event EventsHandler onTauntEnd;
        public event EventsHandler onReachedMaxHorizontalSpeed;
        public event EventsHandler onReachedMaxVerticalSpeed;

        private bool _aimingRight;
        private float jumpForce;
        private float timeWeStartedJumping;
        private float timeWeLandedFromJump;
        private float timeWeStartedFalling;
        private bool resetBigFallFlag;
        private bool applyInitialJumpForce;
        private int jumpFrameCount;
        private bool weMovedDuringJump;
        private float horizontalEaseInScalar;
        private float verticalEaseInScalar;
        private float horizontalMaxSpeed;
        private float verticalMaxSpeed;
        private IEnumerator<float> moveToPositionRoutine;
        private int airJumpCount;
        private float framesWhileNotGrounded;

        #endregion



        #region INIT

        private void Awake()
            {
            rigidBody = GetComponent<Rigidbody2D>();
            character = GetComponent<Character>();
            rigidBody.gravityScale = (isAirCharacter) ? 0f : defaultGravity;
            resetHorizontalMovement();
            }


        private void Start()
            {
            groundLayer = LevelMaster.instance.layers.ground;
            aimingRight = true;
            }


        private void OnEnable()
            {

            }


        #endregion



        #region DESTROY

        private void OnDisable()
            {

            }

        #endregion



        #region FRAME UPDATES

        private void FixedUpdate()
            {
            handleMove();
            handleJump();
            updateVisualDirection();
            }

        private void LateUpdate()
            {
            airBorneCheck();
            }

        #endregion



        #region COLLISIONS

        private void OnCollisionEnter2D(Collision2D collision)
            {
            if (jumping && groundLayer.contains(collision.collider.gameObject.layer))
                {
                landedFromJump();
                }

            grounded = true;
            framesWhileNotGrounded = 0;
            }


        private void OnCollisionExit2D(Collision2D collision)
            {
            grounded = false;
            }


        #endregion



        #region HORIZONTAL MOVEMENT

        private void handleMove()
            {
            if (TimeManager.instance.paused || !handleMovement)
                return;

            checkForHorizontalMovementEvents();
            checkForVerticalMovementEvents();

            if (character.state == CharacterState.IdleTurn
                || character.state == CharacterState.IdleToRun)
                return;

            Vector2 velocity = calculateVelocity();
            rigidBody.velocity = velocity;

            //speed factor goes 0 to 1, one means full speed, we use this if we need to check the
            //current percent we are at reach full speed
            currentSpeedFactor.x = Mathf.Min(1f, Mathf.Abs(rigidBody.velocity.x) / horizontal.constantSpeed);
            currentSpeedFactor.y = Mathf.Min(1f, Mathf.Abs(rigidBody.velocity.y) / horizontal.constantSpeed);
            lastMovingSideX = movingSideX;
            lastMovingSideY = movingSideY;
            }


        private void checkForHorizontalMovementEvents()
            {
            if (lastMovingSideX != movingSideX && movingSideX != 0 && ((aimingRight && movingSideX < 0) || (!aimingRight && movingSideX > 0)))
                directionChangedHorizontally();

            if (lastMovingSideX != 0 && movingSideX == 0)
                stopppedMovingHorizontally();
            else
                {
                if (character.state != CharacterState.Run
                    && character.state != CharacterState.IdleTurn
                    && movingSideX != 0 && lastMovingSideX == 0)
                    startedMoving();
                }
            }


        private void checkForVerticalMovementEvents()
            {
            if (!isAirCharacter)
                return;

            if (lastMovingSideY != movingSideY && movingSideY != 0)
                directionChangedVertically();

            if (lastMovingSideY != 0 && movingSideY == 0)
                stopppedMovingVertically();
            }


        private Vector2 calculateVelocity()
            {
            Vector2 velocity = isAirCharacter ? new Vector2(0f, 0f) :
                 new Vector2(0f, rigidBody.velocity.y);

            bool easingOutX = false;
            bool easingOutY = false;

            if (movingSideX == 0 && !jumping && rigidBody.velocity.x != 0)//ease out horizontal stop
                {
                velocity.x = rigidBody.velocity.x * (0.5f + horizontal.easeOut);
                easingOutX = true;
                }

            if (isAirCharacter && movingSideY == 0)//ease out vertical stop
                {
                velocity.y = rigidBody.velocity.y * (0.5f + vertical.easeOut);
                easingOutY = true;
                }

            if (!easingOutX)
                {
                velocity.x = rigidBody.velocity.x + (movingSideX * calculateHorizontalAcceleration());

                //if jumping or down from slope we let the y velocity carry on
                if (!isAirCharacter || jumping || (!jumping && rigidBody.velocity.y < 0))
                    velocity.y = rigidBody.velocity.y;

                velocity.x = forceSteadyHorizontalVelocity(velocity.x);
                velocity.x = limitToHorizontalMaxVelocity(velocity.x);
                }

            if (isAirCharacter && !easingOutY)
                {
                velocity.y = rigidBody.velocity.y + (movingSideY * calculateVerticalAcceleration());
                velocity.y = forceSteadyVerticalVelocity(velocity.y);
                velocity.y = limitToVerticalMaxVelocity(velocity.y);
                }

            return velocity;
            }


        private float calculateHorizontalAcceleration()
            {
            if (weAreTurningHorizontally())
                {
                if (!jumping)
                    resetHorizontalMovement();

                horizontalEaseInScalar = (1f - horizontal.turnEaseInScalar) * 0.1f;
                }
            else
                {
                horizontalEaseInScalar = Mathf.Min(1f, horizontalEaseInScalar + ((1f - horizontal.easeInScalar) * 0.1f));
                }

            if (jumping)
                horizontalEaseInScalar *= jumpType == JumpType.Big ? jumpSettings.bigAirMovement : jumpSettings.airMovement;

            return horizontal.constantSpeed * horizontalEaseInScalar;
            }


        private float calculateVerticalAcceleration()
            {
            verticalEaseInScalar = Mathf.Min(1f, verticalEaseInScalar + ((1f - vertical.easeInScalar) * 0.1f));
            return vertical.constantSpeed * verticalEaseInScalar;
            }


        private float forceSteadyHorizontalVelocity(float xVelocity)
            {
            if (movingSideX == 0 || movingSideX != lastMovingSideX || jumping)
                return xVelocity;

            //Once we are moving full speed with player, dont allow lower speeds unless we are releasing the buttons
            //so we dont get weird speeds due small fluctuations on ground colliders
            if (Mathf.Abs(xVelocity) >= horizontal.constantSpeed)
                {
                if (!reachedMaxHorizontalSpeed && onReachedMaxHorizontalSpeed != null)
                    onReachedMaxHorizontalSpeed();

                reachedMaxHorizontalSpeed = true;
                }
            else
                if (reachedMaxHorizontalSpeed)//means we are erroneously slowing down by small fractions, lets keep it steady
                {
                if (movingSideX > 0)
                    xVelocity = horizontal.constantSpeed;
                else
                    if (movingSideX < 0)
                    xVelocity = -horizontal.constantSpeed;
                }

            return xVelocity;
            }


        private float forceSteadyVerticalVelocity(float yVelocity)
            {
            if (movingSideY == 0 || movingSideY != lastMovingSideY)
                return yVelocity;

            //Once we are moving full speed with player, dont allow lower speeds unless we are releasing the buttons
            //so we dont get weird speeds due small fluctuations on ground colliders
            if (Mathf.Abs(yVelocity) >= vertical.constantSpeed)
                {
                if (!reachedMaxVerticalSpeed && onReachedMaxVerticalSpeed != null)
                    onReachedMaxVerticalSpeed();

                reachedMaxVerticalSpeed = true;
                }
            else
                if (reachedMaxVerticalSpeed)//means we are erroneously slowing down by small fractions, lets keep it steady
                {
                if (movingSideY > 0)
                    yVelocity = vertical.constantSpeed;
                else
                    if (movingSideY < 0)
                    yVelocity = -vertical.constantSpeed;
                }

            return yVelocity;
            }


        private float limitToHorizontalMaxVelocity(float xVelocity)
            {
            horizontalMaxSpeed = Mathf.Min(horizontalMaxSpeed + horizontal.startSpeedScalar, horizontal.constantSpeed);

            if (xVelocity > 0 && xVelocity > horizontalMaxSpeed)
                xVelocity = horizontalMaxSpeed;
            else
                if (xVelocity < 0 && xVelocity < -horizontalMaxSpeed)
                xVelocity = -horizontalMaxSpeed;

            return xVelocity;
            }


        private float limitToVerticalMaxVelocity(float yVelocity)
            {
            verticalMaxSpeed = Mathf.Min(verticalMaxSpeed + vertical.startSpeedScalar, vertical.constantSpeed);

            if (yVelocity > 0 && yVelocity > verticalMaxSpeed)
                yVelocity = verticalMaxSpeed;
            else
                if (yVelocity < 0 && yVelocity < -verticalMaxSpeed)
                yVelocity = -verticalMaxSpeed;

            return yVelocity;
            }

        #endregion



        #region HORIZONTAL MOVEMENT EVENTS

        private void startedMoving()
            {
            if (autoMoving)
                return;

            if (!jumping)//we want fluid jump movements to carry
                resetHorizontalMovement();

            if (character.state == CharacterState.JumpIdleLanding
                || (character.state.isAttack() && !jumping))
                character.setState(CharacterState.IdleToRun);
            else
                if (character.state == CharacterState.Idle
                    || character.state == CharacterState.RunStop
                    || character.state == CharacterState.DodgeToIdle)
                character.setState(CharacterState.IdleToRun);

            if (onStartedMoving != null)
                onStartedMoving();
            }


        private void stopppedMovingHorizontally()
            {
            if (!jumping)
                resetHorizontalMovement();

            if (character.state == CharacterState.Run)
                character.setState(CharacterState.RunStop);

            if (onStoppedMovingHorizontally != null)
                onStoppedMovingHorizontally();
            }


        private void directionChangedHorizontally()
            {
            if (onHorizontalDirectionChanged != null)
                onHorizontalDirectionChanged();

            resetHorizontalMaxSpeed();
            if (jumping)
                {
                resetHorizontalScalar();
                return;
                }

            switch (character.state)
                {
                case CharacterState.RunTurn:
                    return;
                case CharacterState.Run:
                case CharacterState.RunStop:
                case CharacterState.JumpRunLanding:
                case CharacterState.DodgeToIdle:
                    if (character.lastState == CharacterState.DodgeBack)
                        character.setState(CharacterState.DodgeBackTurn);
                    else
                        character.setState(CharacterState.RunTurn);
                    break;
                case CharacterState.IdleTurn:
                    resetHorizontalMovement();
                    break;
                default:
                    if (rigidBody.velocity.x != 0f || (Time.fixedTime - timeWeLandedFromJump < 0.25f))
                        character.setState(CharacterState.RunTurn);
                    else
                        character.setState(CharacterState.IdleTurn);
                    break;
                }
            }


        private void stopppedMovingVertically()
            {
            resetVerticalMovement();

            if (onStoppedMovingVertically != null)
                onStoppedMovingVertically();
            }


        private void directionChangedVertically()
            {
            if (onVerticalDirectionChanged != null)
                onVerticalDirectionChanged();

            resetVerticalMaxSpeed();
            }


        public void resetHorizontalMovement()
            {
            resetHorizontalScalar();
            resetHorizontalMaxSpeed();
            }

        public void resetVerticalMovement()
            {
            resetVerticalScalar();
            resetVerticalMaxSpeed();
            }


        public void resetHorizontalScalar()
            {
            horizontalEaseInScalar = 0;
            horizontalMaxSpeed = horizontal.startSpeed;
            }


        public void resetHorizontalMaxSpeed()
            {
            reachedMaxHorizontalSpeed = false;
            }


        public void resetVerticalScalar()
            {
            verticalEaseInScalar = 0;
            verticalMaxSpeed = vertical.startSpeed;
            }


        public void resetVerticalMaxSpeed()
            {
            reachedMaxVerticalSpeed = false;
            }


        private bool weAreTurningHorizontally()
            {
            return character.state == CharacterState.RunTurn
                || character.state == CharacterState.IdleTurn
                || character.state == CharacterState.DodgeBackTurn;
            }

        #endregion



        #region JUMP

        public void jump(JumpType type = JumpType.Normal, bool force = false)
            {
            if (isAirCharacter || !jumpSettings.jump && !force || character.state.isVaulting())
                return;

            if (jumping)
                {
                //check if we can vault
                if (tryToVault())
                    return;

                if (!jumpSettings.airJump)
                    return;

                if (airJumpCount >= jumpSettings.maxAirJumpCount)
                    return;

                type = JumpType.Air;
                airJumpCount++;
                }

            jumpType = type;
            jumping = true;

            switch (jumpType)
                {
                case JumpType.Air:
                    character.control.disableInput(CharacterControl.InputType.Attack);
                    character.control.disableInput(CharacterControl.InputType.Block);
                    jumpForce = jumpSettings.airForce;
                    //make sure vertical velocity is starting at zero when we start a doublejump
                    rigidBody.velocity = new Vector2(rigidBody.velocity.x, 0);
                    break;
                case JumpType.Big:
                    jumpForce = jumpSettings.bigAirForce;
                    break;
                case JumpType.Normal:
                    jumpForce = jumpSettings.force;
                    break;
                case JumpType.BigHit:
                    jumpForce = jumpSettings.bigAirHitForce;
                    break;
                }

            timeWeStartedJumping = Time.fixedTime;
            resetBigFallFlag = true;
            applyInitialJumpForce = true;

            character.setState(jumpType == JumpType.Air ? CharacterState.AirJump : CharacterState.UpJump);

            jumpFrameCount = 0;

            if (jumpType != JumpType.Air)
                weMovedDuringJump = false;

            if (onJump != null)
                onJump(jumpType);
            }

        #endregion



        #region VERTICAL MOVEMENT EVENTS

        private void airBorneCheck()
            {
            if (grounded || character.state.isVaulting())
                return;

            framesWhileNotGrounded++;

            if (framesWhileNotGrounded > 2)
                {
                if (!jumping && rigidBody.velocity.y < -5f)
                    {
                    jumping = true;
                    resetBigFallFlag = true;
                    character.setState(CharacterState.DownJump);
                    }
                }
            }

        private void handleJump()
            {
            if (isAirCharacter || !jumping || ((Time.fixedTime - timeWeStartedJumping) < jumpSettings.forceDelay))
                return;

            jumpFrameCount++;

            if (character.control.rawMovementDirection.x != 0)
                weMovedDuringJump = true;

            if (applyInitialJumpForce)
                {
                rigidBody.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
                applyInitialJumpForce = false;
                return;
                }

            Vector2 velocity = Vector2.zero;


            if (jumpFrameCount > jumpSettings.gravityForceStartOffset)
                {
                velocity.y -= character.lastState.isAirHit()
                    ? jumpSettings.afterHitGravityForce : jumpSettings.gravityForce;//account different gravity when falling after hits
                }

            if (rigidBody.velocity.y < 0)
                {
                if (jumpType != JumpType.Air && character.state == CharacterState.UpJump)
                    character.setState(CharacterState.DownJump);
                }

            if (lastMovingSideX == 0)//slowly easeout horizontal velocity while jumping and not moving
                {
                if (rigidBody.velocity.x > 0)
                    velocity.x = velocity.x - jumpSettings.noMovementDecay;
                else
                if (rigidBody.velocity.x < 0)
                    velocity.x = velocity.x + jumpSettings.noMovementDecay;
                }

            if ((character.control.jumpPressed && jumpFrameCount <= jumpSettings.pressedForceDuration))
                {
                //we want to apply more force at the begining of the jump so we check the elapesed percent
                float percent = (float)jumpFrameCount / jumpSettings.pressedForceDuration;
                velocity.y += (jumpSettings.pressedForce * 0.1f) * (1 - percent);
                }

            if (resetBigFallFlag && rigidBody.velocity.y < 0)
                {
                timeWeStartedFalling = Time.fixedTime;
                resetBigFallFlag = false;
                }


            if (!resetBigFallFlag && Time.fixedTime - timeWeStartedFalling > 0.35f)
                {
                if (rigidBody.velocity.y <= (-jumpSettings.gravityForce) * 0.95f)
                    {
                    velocity.y = 0;
                    //rigidBody.velocity = rigidBody.velocity.setY((-jumpSettings.gravityForce) * 0.95f);
                    }
                }


            rigidBody.velocity += velocity;
            }


        private void landedFromJump()
            {
            if (isAirCharacter || (Time.fixedTime - timeWeStartedJumping < 0.15f))//watch out we are not detecting a collision when we started a jump
                return;

            timeWeLandedFromJump = Time.fixedTime;

            jumping = false;
            airJumpCount = 0;

            if (character.pendingDeath && character.state != CharacterState.AirFinisherHit)
                {
                character.die();
                return;
                }


            if (character.state == CharacterState.AirFinisherHit)
                {
                character.attacks.checkForFinsherDeathOrFinisherHit(true);
                }
            else
                {
                if (character.control.rawMovementDirection.x == 0 || character.control.blockPressed)
                    {
                    resetHorizontalMovement();
                    character.setState(CharacterState.JumpIdleLanding);

                    if (weMovedDuringJump)
                        rigidBody.AddForce(new Vector2(jumpSettings.landingStopSlide * (aimingRight ? 1 : -1), 0),
                            ForceMode2D.Impulse);
                    }
                else
                    {
                    if (character.control.movementDirection.x != 0)
                        {
                        if (jumpType == JumpType.Air)
                            reachedMaxHorizontalSpeed = true;//allow smooth landings to quickly run

                        character.setState(CharacterState.JumpRunLanding);
                        }
                    else
                        character.setState(CharacterState.JumpIdleLanding);
                    }
                }

            if (onLandedFromJump != null)
                onLandedFromJump();
            }

        #endregion




        #region VAULTING


        private bool tryToVault()
            {
            Collider2D collider = Physics2D.OverlapCircle(transform.position, 0.3f, LevelMaster.instance.layers.vaultingGround);

            if (collider == null)
                return false;

            vault(collider.bounds.min);

            WorldPlatform platform = collider.transform.parent.GetComponent<WorldPlatform>();

            platform.spriteRenderer.sortingLayerName = LevelMaster.instance.sortingLayers.platformsBehindCharacters.name;

            return true;
            }

        public void vault(Vector2 position)
            {
            if (grounded || !character.stateAvailable(CharacterState.Vaulting))
                return;

            jumping = false;
            airJumpCount = 0;
            character.setState(CharacterState.Vaulting, vaultCoRoutine(position));
            }


        private IEnumerator<float> vaultCoRoutine(Vector2 position)
            {
            if (onVaulting != null)
                onVaulting();

            rigidBody.velocity = Vector2.zero;

            transform.position = transform.position.setY(position.y - 0.9f);

            yield break;
            }


        public void vaultEnded()
            {
            if (onVaultingEnd != null)
                onVaultingEnd();
            }

        #endregion



        #region DODGE

        public void dodge()
            {
            if (jumping || !(character.stateAvailable(CharacterState.DodgeFront) && character.stateAvailable(CharacterState.DodgeBack)))
                return;

            bool isFront = movingRawSameDirection();

            if ((character.state == CharacterState.DodgeBack && !isFront)
                || (character.state == CharacterState.DodgeFront && isFront))
                return;

            if (character.control.rawMovementDirection.x != character.control.movementDirection.x)
                resetHorizontalMovement();

            character.setState(isFront ? CharacterState.DodgeFront : CharacterState.DodgeBack,
                dodgeCoRoutine(isFront));
            }


        private IEnumerator<float> dodgeCoRoutine(bool isFront)
            {
            if (isFront)
                movingSideX = aimingRight ? 1 : -1;

            if (onDodge != null)
                onDodge(isFront);

            yield break;
            }


        public void dodgeEnded(bool isFront)
            {
            if (onDodgeEnd != null)
                onDodgeEnd(isFront);
            }

        #endregion



        #region BLOCK

        public void block()
            {
            if (character.state == CharacterState.AirBlock
                || character.state == CharacterState.AirBlockHold
                || character.state == CharacterState.AirBlockDownJump
                || character.state == CharacterState.Block
                || character.state == CharacterState.BlockHold
                || character.state == CharacterState.BlockToIdle)
                return;

            if (jumping && !character.stateAvailable(CharacterState.AirBlock))
                return;

            if (!jumping && !character.stateAvailable(CharacterState.Block))
                return;

            if (!jumping)
                resetHorizontalMovement();

            forceAimingRightUpdateOnRawControl();


            character.setState(jumping ? CharacterState.AirBlock : CharacterState.Block,
                blockCoRoutine(), false, !jumping);

            character.control.disableInput(CharacterControl.InputType.Attack);
            }

        private IEnumerator<float> blockCoRoutine()
            {
            if (onBlock != null)
                onBlock();

            yield return Timing.waitForRoutine(character.waitForAnimationDuration());

            if (character.control.blockPressed)
                {
                character.setState(jumping ? CharacterState.AirBlockHold : CharacterState.BlockHold, false);

                if (onBlockHold != null)
                    onBlockHold();
                }

            while (character.control.blockPressed)
                yield return 0;

            if (onBlockReleased != null)
                onBlockReleased();

            character.releaseActions();

            if (jumping)
                character.setState(CharacterState.AirBlockDownJump, false);
            else
                character.setState(CharacterState.BlockToIdle, false);
            }

        #endregion



        #region TAUNT

        public void taunt()
            {
            if (jumping || !character.stateAvailable(CharacterState.Taunt)
                || character.state == CharacterState.Taunt)
                return;

            character.setState(CharacterState.Taunt, tauntCoRoutine());
            }


        private IEnumerator<float> tauntCoRoutine()
            {
            if (onTaunt != null)
                onTaunt();

            yield break;
            }


        public void tauntEnded()
            {
            if (onTauntEnd != null)
                onTauntEnd();
            }

        #endregion



        #region AIMING RIGHT

        public void forceAimingRightUpdateOnRawControl()
            {
            if ((character.control.rawMovementDirection.x < 0 && aimingRight)
               || (character.control.rawMovementDirection.x > 0 && !aimingRight))
                aimingRight = !aimingRight;
            }


        public void updateVisualDirection()
            {
            if (movingSideX > 0)
                aimingRight = true;
            else
                if (movingSideX < 0)
                aimingRight = false;
            }


        public bool aimingRight
            {
            get
                {
                return _aimingRight;
                }
            set
                {
                if (value == _aimingRight)
                    return;

                _aimingRight = value;

                character.transforms.flipable.localScale = character.transforms.flipable.localScale.setX((_aimingRight) ? 1 : -1);

                if (onAimingRightChange != null)
                    onAimingRightChange();
                }
            }

        #endregion



        #region MISC

        public bool isVelocitySameFacingSide()
            {
            return (rigidBody.velocity.x > 0 && aimingRight) || (rigidBody.velocity.x < 0 && !aimingRight);
            }


        public void moveToPosition(Vector2 offset, bool disableInterpolation = true)
            {
            if (moveToPositionRoutine != null)
                {
                Timing.kill(moveToPositionRoutine);
                rigidBody.interpolation = RigidbodyInterpolation2D.Interpolate;
                }

            if (!disableInterpolation)
                {
                rigidBody.MovePosition(rigidBody.position + offset);
                return;
                }

            moveToPositionRoutine = moveToPositionCoRoutine(offset);
            Timing.run(moveToPositionRoutine, Timing.Segment.FixedUpdate);
            }


        public IEnumerator<float> moveToPositionCoRoutine(Vector2 offset)
            {
            Vector2 lastPos = rigidBody.position;

            rigidBody.interpolation = RigidbodyInterpolation2D.None;

            rigidBody.MovePosition(rigidBody.position + offset);

            while (lastPos == rigidBody.position)//holdPosition for the rigidbody to update to the new position
                yield return 0;

            rigidBody.interpolation = RigidbodyInterpolation2D.Interpolate;
            }


        public void addForce(Vector2 force, bool adjustWithVelocity = false,
            ForceMode2D mode = ForceMode2D.Impulse)
            {
            bool reversed = false;

            if (force.x < 0)
                {
                force.x *= -1;
                reversed = true;
                }

            if ((!aimingRight && force.x > 0))
                force.x *= -1;

            if (adjustWithVelocity &&
                ((!aimingRight && force.x < 0 && rigidBody.velocity.x < 0)
                || (aimingRight && force.x > 0 && rigidBody.velocity.x > 0)))
                force.x *= -1;

            if (reversed)
                force.x *= -1;

            rigidBody.AddForce(force, mode);
            }


        public bool sameXDirectionForce(float force)
            {
            return force > 0 && rigidBody.velocity.x > 0 || force < 0 && rigidBody.velocity.x < 0;
            }

        public bool sameYirectionForce(float force)
            {
            return force > 0 && rigidBody.velocity.y > 0 || force < 0 && rigidBody.velocity.y < 0;
            }

        public bool movingRawSameDirection()
            {
            return ((character.control.rawMovementDirection.x > 0 && aimingRight)
                || (character.control.rawMovementDirection.x < 0 && !aimingRight));
            }

        public bool movingOppositeRawDirection()
            {
            return ((character.control.rawMovementDirection.x > 0 && !aimingRight)
                || (character.control.rawMovementDirection.x < 0 && aimingRight));
            }

        #endregion



        #region FREEZE

        public void freeze(string axis)
            {
            switch (axis)
                {
                case "x":
                    rigidBody.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
                    break;
                case "y":
                    rigidBody.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
                    break;
                default:
                    rigidBody.constraints = RigidbodyConstraints2D.FreezeAll;
                    break;
                }
            }

        public void unFreeze()
            {
            rigidBody.constraints = RigidbodyConstraints2D.FreezeRotation;
            }

        #endregion
        }
    }