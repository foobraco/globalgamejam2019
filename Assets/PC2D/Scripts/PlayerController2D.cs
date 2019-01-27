using UnityEngine;
using UnityEngine.Timeline;
using InControl;

/// <summary>
/// This class is a simple example of how to build a controller that interacts with PlatformerMotor2D.
/// </summary>
[RequireComponent(typeof(PlatformerMotor2D))]
public class PlayerController2D : MonoBehaviour
{
    public enum CarryingState
    {
        NotCarrying,
        Carrying
    }

    [HideInInspector]
    public CarryingState carryingState;
    [HideInInspector]
    public GameObject carryingItem;

    [HideInInspector]
    public bool isChargingJump;
    [HideInInspector]
    public bool isCarryingItem;
    [HideInInspector]
    public bool hasNotMovedYet;
    [HideInInspector]
    public bool isStartingToMove;

    [SerializeField]
    private float rechargedJumpHeight = 6f;
    [SerializeField]
    private float rechargedJumpTime = 1f;
    [SerializeField]
    private float carryingGroundSpeed = 2f;
    [SerializeField]
    private float carryingJumpHeight = 2f;
    [SerializeField]
    private float carryingJumpSpeed = 2f;
    [SerializeField]
    private AudioClip walkClip;
    [SerializeField]
    private AudioClip jumpClip;


    private PlatformerMotor2D _motor;
    private bool _restored = true;
    private bool _enableOneWayPlatforms;
    private bool _oneWayPlatformsAreWalls;

    private float timeChargingJump;
    private float defaultJumpHeight;
    private float defaultGroundSpeed;
    private float defaultAirSpeed;
    private bool isInItem;
    private GameObject nearestItem;
    private AudioSource audioSource;

    // Use this for initialization
    void Start()
    {
        _motor = GetComponent<PlatformerMotor2D>();
        audioSource = GetComponent<AudioSource>();
        defaultJumpHeight = _motor.jumpHeight;
        defaultGroundSpeed = _motor.groundSpeed;
        defaultAirSpeed = _motor.airSpeed;
        hasNotMovedYet = true;
        isStartingToMove = false;
    }

    // before enter en freedom state for ladders
    void FreedomStateSave(PlatformerMotor2D motor)
    {
        if (!_restored) // do not enter twice
            return;

        _restored = false;
        _enableOneWayPlatforms = _motor.enableOneWayPlatforms;
        _oneWayPlatformsAreWalls = _motor.oneWayPlatformsAreWalls;
    }
    // after leave freedom state for ladders
    void FreedomStateRestore(PlatformerMotor2D motor)
    {
        if (_restored) // do not enter twice
            return;

        _restored = true;
        _motor.enableOneWayPlatforms = _enableOneWayPlatforms;
        _motor.oneWayPlatformsAreWalls = _oneWayPlatformsAreWalls;
    }

    // Update is called once per frame
    void Update()
    {
        if (InputManager.ActiveDevice != null)
        {
            UpdateWithController();
        }
        UpdateWithKeyboard();
    }

    private void UpdateWithController()
    {
        // use last state to restore some ladder specific values
        if (_motor.motorState != PlatformerMotor2D.MotorState.FreedomState)
        {
            // try to restore, sometimes states are a bit messy because change too much in one frame
            FreedomStateRestore(_motor);
        }

        //if (_motor.IsGrounded() && _motor.jumpHeight != defaultJumpHeight)
        //{
        //    _motor.jumpHeight = defaultJumpHeight;
        //}

        // Jump?
        // If you want to jump in ladders, leave it here, otherwise move it down
        if (InputManager.ActiveDevice.Action1.WasPressed)
        {
            if (hasNotMovedYet)
            {
                hasNotMovedYet = false;
                isStartingToMove = true;
            }
        }

        if (_motor.normalizedXMovement == 0f && InputManager.ActiveDevice.Direction.Down.IsPressed)
        {
            isChargingJump = true;
        }

        if (InputManager.ActiveDevice.Direction.Down.WasReleased)
        {
            isChargingJump = false;
        }

        if (InputManager.ActiveDevice.Action1.WasPressed)
        {
            if (hasNotMovedYet)
            {
                hasNotMovedYet = false;
                isStartingToMove = true;
            }
            if (InputManager.ActiveDevice.Direction.Down.IsPressed && !isCarryingItem)
            {
                _motor.jumpHeight = rechargedJumpHeight;
                _motor.Jump();
                _motor.DisableRestrictedArea();
                _motor.jumpHeight = defaultJumpHeight;
            }
            else
            {
                _motor.Jump();
                _motor.DisableRestrictedArea();
            }
            audioSource.PlayOneShot(jumpClip);
            timeChargingJump = 0f;
            isChargingJump = false;
        }

        //_motor.jumpingHeld = Input.GetButton(PC2D.Input.JUMP);

        // XY freedom movement
        if (_motor.motorState == PlatformerMotor2D.MotorState.FreedomState)
        {
            _motor.normalizedXMovement = InputManager.ActiveDevice.Direction.X;
            _motor.normalizedYMovement = InputManager.ActiveDevice.Direction.Y;

            return; // do nothing more
        }

        // X axis movement
        if (Mathf.Abs(InputManager.ActiveDevice.Direction.X) > PC2D.Globals.INPUT_THRESHOLD)
        {
            if (hasNotMovedYet)
            {
                hasNotMovedYet = false;
                isStartingToMove = true;
            }
            if (!isStartingToMove)
            {
                _motor.normalizedXMovement = InputManager.ActiveDevice.Direction.X;
                if (!audioSource.isPlaying && _motor.IsGrounded())
                {
                    audioSource.clip = walkClip;
                    audioSource.Play();
                }
            }
        }
        else
        {
            _motor.normalizedXMovement = 0;
        }

        if (InputManager.ActiveDevice.Direction.Y != 0)
        {
            if (hasNotMovedYet)
            {
                hasNotMovedYet = false;
                isStartingToMove = true;
            }
            bool up_pressed = InputManager.ActiveDevice.Direction.Y > 0;
            if (_motor.IsOnLadder())
            {
                if (
                    (up_pressed && _motor.ladderZone == PlatformerMotor2D.LadderZone.Top)
                    ||
                    (!up_pressed && _motor.ladderZone == PlatformerMotor2D.LadderZone.Bottom)
                 )
                {
                    // do nothing!
                }
                // if player hit up, while on the top do not enter in freeMode or a nasty short jump occurs
                else
                {
                    // example ladder behaviour

                    _motor.FreedomStateEnter(); // enter freedomState to disable gravity
                    _motor.EnableRestrictedArea();  // movements is retricted to a specific sprite bounds

                    // now disable OWP completely in a "trasactional way"
                    FreedomStateSave(_motor);
                    _motor.enableOneWayPlatforms = false;
                    _motor.oneWayPlatformsAreWalls = false;

                    // start XY movement
                    _motor.normalizedXMovement = InputManager.ActiveDevice.Direction.X;
                    _motor.normalizedYMovement = InputManager.ActiveDevice.Direction.Y;
                }
            }
        }
        else if (InputManager.ActiveDevice.Direction.Y < -PC2D.Globals.FAST_FALL_THRESHOLD)
        {
            if (hasNotMovedYet)
            {
                hasNotMovedYet = false;
                isStartingToMove = true;
            }
            _motor.fallFast = false;
        }

        if (InputManager.ActiveDevice.Action2.WasPressed && !isCarryingItem)
        {
            _motor.Dash();
            if (hasNotMovedYet)
            {
                hasNotMovedYet = false;
                isStartingToMove = true;
            }
        }

        if (InputManager.ActiveDevice.Action3.WasPressed && isInItem)
        {
            Carrying();
        }

        if (InputManager.ActiveDevice.Action3.WasReleased && isCarryingItem)
        {
            NotCarrying();
        }
    }

    private void UpdateWithKeyboard()
    {
        // use last state to restore some ladder specific values
        if (_motor.motorState != PlatformerMotor2D.MotorState.FreedomState)
        {
            // try to restore, sometimes states are a bit messy because change too much in one frame
            FreedomStateRestore(_motor);
        }

        //if (_motor.IsGrounded() && _motor.jumpHeight != defaultJumpHeight)
        //{
        //    _motor.jumpHeight = defaultJumpHeight;
        //}

        // Jump?
        // If you want to jump in ladders, leave it here, otherwise move it down
        if (Input.GetButtonDown(PC2D.Input.JUMP))
        {
            if (hasNotMovedYet)
            {
                hasNotMovedYet = false;
                isStartingToMove = true;
            }
        }

        if (_motor.normalizedXMovement == 0 && Input.GetAxis(PC2D.Input.VERTICAL) < 0f)
        {
            isChargingJump = true;
        }

        if (Input.GetAxis(PC2D.Input.VERTICAL) == 0f)
        {
            isChargingJump = false;
        }

        if (Input.GetButtonDown(PC2D.Input.JUMP))
        {
            if (hasNotMovedYet)
            {
                hasNotMovedYet = false;
                isStartingToMove = true;
            }
            if (Input.GetAxis(PC2D.Input.VERTICAL) < 0f && !isCarryingItem)
            {
                _motor.jumpHeight = rechargedJumpHeight;
                _motor.Jump();
                _motor.DisableRestrictedArea();
                _motor.jumpHeight = defaultJumpHeight;
            }
            else
            {
                _motor.Jump();
                _motor.DisableRestrictedArea();
            }
            audioSource.PlayOneShot(jumpClip);
            timeChargingJump = 0f;
            isChargingJump = false;
        }

        //_motor.jumpingHeld = Input.GetButton(PC2D.Input.JUMP);

        // XY freedom movement
        if (_motor.motorState == PlatformerMotor2D.MotorState.FreedomState)
        {
            if (hasNotMovedYet)
            {
                hasNotMovedYet = false;
                isStartingToMove = true;
            }
            _motor.normalizedXMovement = Input.GetAxis(PC2D.Input.HORIZONTAL);
            _motor.normalizedYMovement = Input.GetAxis(PC2D.Input.VERTICAL);

            return; // do nothing more
        }

        // X axis movement
        if (Mathf.Abs(Input.GetAxis(PC2D.Input.HORIZONTAL)) > PC2D.Globals.INPUT_THRESHOLD)
        {
            if (hasNotMovedYet)
            {
                hasNotMovedYet = false;
                isStartingToMove = true;
            }
            if (!isStartingToMove)
            {
                _motor.normalizedXMovement = Input.GetAxis(PC2D.Input.HORIZONTAL);
                if (!audioSource.isPlaying && _motor.IsGrounded())
                {
                    audioSource.clip = walkClip;
                    audioSource.Play();
                }
            }
        }
        else
        {
            _motor.normalizedXMovement = 0;
            audioSource.Stop();
            audioSource.clip = null;
        }

        if (Input.GetAxis(PC2D.Input.VERTICAL) != 0)
        {
            if (hasNotMovedYet)
            {
                hasNotMovedYet = false;
                isStartingToMove = true;
            }
            bool up_pressed = Input.GetAxis(PC2D.Input.VERTICAL) > 0;
            if (_motor.IsOnLadder())
            {
                if (
                    (up_pressed && _motor.ladderZone == PlatformerMotor2D.LadderZone.Top)
                    ||
                    (!up_pressed && _motor.ladderZone == PlatformerMotor2D.LadderZone.Bottom)
                 )
                {
                    // do nothing!
                }
                // if player hit up, while on the top do not enter in freeMode or a nasty short jump occurs
                else
                {
                    // example ladder behaviour

                    _motor.FreedomStateEnter(); // enter freedomState to disable gravity
                    _motor.EnableRestrictedArea();  // movements is retricted to a specific sprite bounds

                    // now disable OWP completely in a "trasactional way"
                    FreedomStateSave(_motor);
                    _motor.enableOneWayPlatforms = false;
                    _motor.oneWayPlatformsAreWalls = false;

                    // start XY movement
                    _motor.normalizedXMovement = Input.GetAxis(PC2D.Input.HORIZONTAL);
                    _motor.normalizedYMovement = Input.GetAxis(PC2D.Input.VERTICAL);
                }
            }
        }
        else if (Input.GetAxis(PC2D.Input.VERTICAL) < -PC2D.Globals.FAST_FALL_THRESHOLD)
        {
            _motor.fallFast = false;
        }

        if (Input.GetButtonDown(PC2D.Input.DASH) && !isCarryingItem)
        {
            if (hasNotMovedYet)
            {
                hasNotMovedYet = false;
                isStartingToMove = true;
            }
            _motor.Dash();
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && isInItem)
        {
            Carrying();
        }

        if (Input.GetKeyUp(KeyCode.LeftShift) && isCarryingItem)
        {
            NotCarrying();
        }
    }

    public void ItStartedToMove()
    {
        isStartingToMove = false;
    }

    private void NotCarrying()
    {
        isCarryingItem = false;
        carryingItem.GetComponent<Item>().ReleaseItem();
        _motor.groundSpeed = defaultGroundSpeed;
        _motor.jumpHeight = defaultJumpHeight;
        _motor.airSpeed = defaultAirSpeed;
    }

    public void ReturnNormalValues()
    {
        _motor.groundSpeed = defaultGroundSpeed;
        _motor.jumpHeight = defaultJumpHeight;
        _motor.airSpeed = defaultAirSpeed;
    }

    private void Carrying()
    {
        nearestItem.transform.parent = transform;
        carryingItem = nearestItem;
        carryingItem.GetComponent<SpriteRenderer>().enabled = false;
        isCarryingItem = true;
        _motor.groundSpeed = carryingGroundSpeed;
        _motor.jumpHeight = carryingJumpHeight;
        _motor.airSpeed = carryingJumpSpeed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Item"))
        {
            Debug.Log("Is collisioning with player");
            isInItem = true;
            nearestItem = collision.gameObject;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        isInItem = false;
        nearestItem = null;
    }
}
