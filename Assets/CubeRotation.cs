using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using static UnityEngine.GridBrushBase;
using CubeSide = StateReader.CubeSide;
using UnityEngine.UI;

public class CubeRotation : MonoBehaviour
{
    public GameObject Cube;
    private CubeSolver cubeSolver;
    private StateReader stateReader;
    public GameObject RotationObject;
    private Vector2 startPosition;
    private Vector2 endPosition;
    // Podesava se brzina rotacija kocke, tj. brzina animacija
    private float rotationSpeed = CubeSolver.minRotationSpeed;
    private bool allowRotation = true;
    private RotationObjectType currentRotatingObject = RotationObjectType.None;
    // Menja smer poteza unetih preko tastature
    private bool clockwiseMoves = true;
    public GameObject inverseModeState;
    public Text inverseModeState_text;
    public GameObject testingModeState;
    public Text testingModeState_text;
    public GameObject currentStep;
    public Text currentStep_text;
    private string currentStepText;
    public GameObject currentMethod;
    public Text currentMethod_text;
    private string currentMethodText;
    public GameObject lastSolvingTime;
    public Text lastSolvingTime_text;
    private string lastSolvingTimeText;
    public GameObject lastSolvingMovesCount;
    public Text lastSolvingMovesCount_text;
    private string lastSolvingMovesCountText;
    public GameObject currentShuffleMovesCount;
    public Text currentShuffleMovesCount_text;
    private string currentShuffleMovesCountText;

    #region Properties

    public string CurrentStepText
    {
        get { return this.currentStepText; }
        set { this.currentStepText = value; }
    }

    public string CurrentMethodText
    {
        get { return this.currentMethodText; }
        set { this.currentMethodText = value; }
    }

    public string LastSolvingTimeText
    {
        get { return this.lastSolvingTimeText; }
        set { this.lastSolvingTimeText = value; }
    }

    public string LastSolvingMovesCountText
    {
        get { return this.lastSolvingMovesCountText; }
        set { this.lastSolvingMovesCountText = value; }
    }

    public float RotationSpeed
    {
        get { return this.rotationSpeed; }
        set { this.rotationSpeed = value; }
    }

    #endregion

    // Start is called before the first frame update
    void Start()
    {
        this.stateReader = GetComponent<StateReader>();
        this.cubeSolver = GetComponent<CubeSolver>();
        this.inverseModeState_text = inverseModeState.GetComponent<Text>();
        this.testingModeState_text = testingModeState.GetComponent<Text>();
        this.currentStep_text = currentStep.GetComponent<Text>();
        this.currentMethod_text = currentMethod.GetComponent<Text>();
        this.lastSolvingTime_text = lastSolvingTime.GetComponent<Text>();
        this.lastSolvingMovesCount_text = lastSolvingMovesCount.GetComponent<Text>();
        this.currentShuffleMovesCount_text = currentShuffleMovesCount.GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        this.RotateCube();
        this.currentShuffleMovesCount_text.text = this.cubeSolver.CurrentShuffleMovesCount == 0 ? "None" : this.cubeSolver.CurrentShuffleMovesCount.ToString();

        this.inverseModeState_text.text = !this.clockwiseMoves ? "ON" : "OFF";
        this.inverseModeState_text.color = !this.clockwiseMoves ? UnityEngine.Color.red : UnityEngine.Color.green;

        this.testingModeState_text.text = this.cubeSolver.TestingModeEnabled ? "ON" : "OFF";
        this.testingModeState_text.color = this.cubeSolver.TestingModeEnabled ? UnityEngine.Color.red : UnityEngine.Color.green;

        this.currentStep_text.text = this.currentStepText == null || this.currentStepText.Equals(string.Empty) ? "None" : this.currentStepText;
        this.currentMethod_text.text = this.currentMethodText == null || this.currentMethodText.Equals(string.Empty) ? "None" : this.currentMethodText;

        this.UpdateLastSolvingMovesCountAndTime();

        if (Input.GetKeyDown(KeyCode.C))
        {
            this.clockwiseMoves = false;
        }
        if (Input.GetKeyUp(KeyCode.C))
        {
            this.clockwiseMoves = true;
        }
        else
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (this.allowRotation)
                StartCoroutine(this.RotateFace(new CubeMove(CubeSide.Left, this.clockwiseMoves)));
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (this.allowRotation)
                StartCoroutine(this.RotateFace(new CubeMove(CubeSide.Right, this.clockwiseMoves)));
        }
        if (Input.GetKeyDown(KeyCode.U))
        {
            if (this.allowRotation)
                StartCoroutine(this.RotateFace(new CubeMove(CubeSide.Up, this.clockwiseMoves)));
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            if (this.allowRotation)
                StartCoroutine(this.RotateFace(new CubeMove(CubeSide.Down, this.clockwiseMoves)));
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (this.allowRotation)
                StartCoroutine(this.RotateFace(new CubeMove(CubeSide.Front, this.clockwiseMoves)));
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (this.allowRotation)
                StartCoroutine(this.RotateFace(new CubeMove(CubeSide.Back, this.clockwiseMoves)));
        }
    }

    public void UpdateLastSolvingMovesCountAndTime()
    {
        this.lastSolvingTime_text.text = this.cubeSolver.CurrentSolvingTime == 0 ? "None" : this.cubeSolver.CurrentSolvingTime + "ms";
        this.lastSolvingMovesCount_text.text = this.cubeSolver.CurrentSolvingMovesCount == 0 ? "None" : this.cubeSolver.CurrentSolvingMovesCount.ToString();
    }

    #region Moves

    // Metoda vrsi niz poteza cubeMoves nad 3D modelom, ukoliko je solvingCalculationMode == true onda se niz poteza vrsi samo nad privremenim stanjem za resavanje kocke
    public void DoMoves(CubeMove[] cubeMoves, bool solvingCalculationMode = false)
    {
        if (!allowRotation)
            return;

        if (!solvingCalculationMode)
        {
            StartCoroutine(ExecuteMoves(cubeMoves));
        }
        else
        {
            foreach (CubeMove cubeMove in cubeMoves)
            {
                this.stateReader.SolvingCubeStateWrapper.RotateFace(cubeMove);
            }
        }
    }

    public IEnumerator ExecuteMovesWithStepsNames(Dictionary<string, CubeMove[]> cubeMovesByStepNames, string methodName)
    {
        this.currentMethodText = methodName;

        foreach(var cubeMovesByStepName in cubeMovesByStepNames) {
            this.currentStepText = cubeMovesByStepName.Key;

            yield return StartCoroutine(this.ExecuteMoves(cubeMovesByStepName.Value));
        }

        this.currentMethodText = "None";
        this.currentStepText = "None";
    }

    public IEnumerator ExecuteMoves(CubeMove[] cubeMoves)
    {
        this.cubeSolver.IsSolving = true;

        CubeMove[] filteredMoves = this.FilterInvalidMoves(cubeMoves);

        allowRotation = false;
        foreach (CubeMove cubeMove in filteredMoves)
        {
            yield return StartCoroutine(this.RotateFace(cubeMove));
        }

        allowRotation = true;

        this.cubeSolver.IsSolving = false;
    }

    private CubeMove[] FilterInvalidMoves(CubeMove[] cubeMoves)
    {
        CubeMove[] rotationMoves = cubeMoves.Where(move => move.CubeSide != CubeSide.NoSide).Select(move =>
        {
            move.CubeSide = this.stateReader.CubeStateWrapper.CubeStateData.RotationToSideMapping[move.CubeSide];

            return move;
        }).ToArray();

        return rotationMoves;
    }

    #endregion

    #region FaceRotations

    // Rotiranje jedne strane kocke
    private IEnumerator RotateFace(CubeMove cubeMove)
    {
        this.stateReader.CubeStateWrapper.RotateFace(cubeMove);

        this.allowRotation = false;
        var sideCubes = new List<Transform>();

        Transform centralCubeOfSide = null;

        // Odredjuju se sve kockice na trazenoj strani, kao i centralna oko koje ce se vrsiti rotacija strane
        foreach (Transform cube in Cube.transform)
        {
            if (this.CubieOnSide(cube, cubeMove.CubeSide))
                sideCubes.Add(cube);

            if (this.IsCentralSideCube(cube, cubeMove.CubeSide))
                centralCubeOfSide = cube;
        }

        // Grupisu se sve kockice trazene strane kako bi se zajedno rotirale
        if (centralCubeOfSide != null)
        {
            foreach (Transform cube in sideCubes)
                cube.SetParent(centralCubeOfSide);
        }

        // Pravi se privremeni objekat koji ce prvi biti zarotiran, a zatim ce se grupisane kockice rotirati ka privremenom objektu, tj. pravi se animacija
        Quaternion initialRotation = centralCubeOfSide.rotation;
        var tempGameObject = new GameObject("temporaryGameObject");
        Transform tempTransform = tempGameObject.transform;
        tempTransform.position = centralCubeOfSide.position;
        tempTransform.rotation = centralCubeOfSide.rotation;
        tempTransform.localScale = centralCubeOfSide.localScale;

        this.RotateTempObject(cubeMove, tempTransform);

        float elapsedTime = 0;

        // Animacija rotacije
        while (elapsedTime < rotationSpeed)
        {
            centralCubeOfSide.rotation = Quaternion.Slerp(initialRotation, tempGameObject.transform.rotation, elapsedTime / rotationSpeed);
            elapsedTime += Time.deltaTime;

            yield return null;
        }

        centralCubeOfSide.rotation = tempGameObject.transform.rotation;

        foreach (var cube in sideCubes)
        {
            cube.SetParent(Cube.transform);
        }

        sideCubes.Clear();
        Destroy(tempGameObject);

        this.allowRotation = true;
    }

    private void RotateTempObject(CubeMove cubeMove, Transform tempTransform)
    {
        var doubleRotateMultipler = cubeMove.DoubleMove ? 2 : 1;
        int clockwiseMultiplier = cubeMove.Clockwise ? 1 : -1;

        switch (cubeMove.CubeSide)
        {
            case CubeSide.Right:
                tempTransform.Rotate(0, 0, doubleRotateMultipler * clockwiseMultiplier * -90, Space.World);
                break;
            case CubeSide.Left:
                tempTransform.Rotate(0, 0, doubleRotateMultipler * clockwiseMultiplier * 90, Space.World);
                break;
            case CubeSide.Up:
                tempTransform.Rotate(0, doubleRotateMultipler * clockwiseMultiplier * 90, 0, Space.World);
                break;
            case CubeSide.Down:
                tempTransform.Rotate(0, doubleRotateMultipler * clockwiseMultiplier * -90, 0, Space.World);
                break;
            case CubeSide.Front:
                tempTransform.Rotate(doubleRotateMultipler * clockwiseMultiplier * -90, 0, 0, Space.World);
                break;
            case CubeSide.Back:
                tempTransform.Rotate(doubleRotateMultipler * clockwiseMultiplier * 90, 0, 0, Space.World);
                break;
        }
    }

    #endregion

    #region CubeRotation

    // Metoda koja omogucava rotiranje kocke desnim klikom
    private void RotateCube()
    {
        if (!this.allowRotation)
            return;

        // Na pritisak desnog klika se pamti trenutna pozicija na ekranu kao startPosition
        if (Input.GetMouseButtonDown(1))
        {
            this.startPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            this.currentRotatingObject = RotationObjectType.Cube;
        }

        // Na pustanje desnog klika se racuna da li je trenutna pozicija dovoljno udaljena od startPosition i ukoliko jeste proverava se koja je rotacija u pitanju
        if (Input.GetMouseButtonUp(1) && this.currentRotatingObject == RotationObjectType.Cube)
        {
            Ray cubeCollisionRay = Camera.main.ScreenPointToRay(this.startPosition);

            // Ispaljuje se zrak iz pozicije kamere do pozicije misa i proverava se da li je udario u neki od collider-a, a samo kockice imaju collider-e
            if (Physics.Raycast(cubeCollisionRay, out RaycastHit cubeHit) && cubeHit.collider.gameObject != null)
            {
                this.allowRotation = false;

                this.endPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                var rotationVector = new Vector2(this.startPosition.x - this.endPosition.x, this.startPosition.y - this.endPosition.y);

                CubeRotationDirection rotationDirection = this.GetCubeRotationDirection(rotationVector, this.startPosition);
                this.stateReader.CubeStateWrapper.RotateCube(rotationDirection);

                switch (rotationDirection)
                {
                    case CubeRotationDirection.Right:
                        this.RotationObject.transform.Rotate(0, -90, 0, Space.World);
                        StartCoroutine(this.SlowlyRotate());
                        break;
                    case CubeRotationDirection.Left:
                        this.RotationObject.transform.Rotate(0, 90, 0, Space.World);
                        StartCoroutine(this.SlowlyRotate());
                        break;
                    case CubeRotationDirection.UpLeft:
                        this.RotationObject.transform.Rotate(0, 0, -90, Space.World);
                        StartCoroutine(this.SlowlyRotate());
                        break;
                    case CubeRotationDirection.UpRight:
                        this.RotationObject.transform.Rotate(90, 0, 0, Space.World);
                        StartCoroutine(this.SlowlyRotate());
                        break;
                    case CubeRotationDirection.DownRight:
                        this.RotationObject.transform.Rotate(-90, 0, 0, Space.World);
                        StartCoroutine(this.SlowlyRotate());
                        break;
                    case CubeRotationDirection.DownLeft:
                        this.RotationObject.transform.Rotate(0, 0, 90, Space.World);
                        StartCoroutine(this.SlowlyRotate());
                        break;
                    default:
                        this.allowRotation = true;
                        break;
                }

            }
        }
    }

    private CubeRotationDirection GetCubeRotationDirection(Vector2 rotationVector, Vector2 startVector)
    {
        // Generalizuje se pozicija u odnosu na rezoluciju ekrana
        var normalizedPosition = this.NormalizePosition(rotationVector);

        // Ove metode proveravaju da li je dovoljno velika razlika izmedju startPosition i endPosition
        bool horizontalMove = this.ValidateHorizontalMove(normalizedPosition);
        bool verticalMove = this.ValidateVerticalMove(normalizedPosition);

        bool leftSideCoordinateX = startVector.x < Screen.width / 2;

        if (horizontalMove || verticalMove)
        {
            if (horizontalMove && !verticalMove)
                return rotationVector.x < 0 ? CubeRotationDirection.Right : CubeRotationDirection.Left;
            else if (leftSideCoordinateX)
                return rotationVector.y < 0 ? CubeRotationDirection.UpLeft : CubeRotationDirection.DownLeft;
            else
                return rotationVector.y < 0 ? CubeRotationDirection.UpRight : CubeRotationDirection.DownRight;
        }

        return CubeRotationDirection.NoRotation;
    }

    #endregion

    // Animacija za rotaciju kocke
    private IEnumerator SlowlyRotate()
    {
        this.allowRotation = false;

        Quaternion initialRotation = this.Cube.transform.rotation;
        float elapsedTime = 0;

        while (elapsedTime < rotationSpeed)
        {
            this.Cube.transform.rotation = Quaternion.Slerp(initialRotation, this.RotationObject.transform.rotation, elapsedTime / rotationSpeed);
            elapsedTime += Time.deltaTime;

            yield return null;
        }

        this.Cube.transform.rotation = this.RotationObject.transform.rotation;
        this.allowRotation = true;
    }

    #region PositionOperations

    private bool ValidateRotationIntensity(Vector2 rotationVector, RotationObjectType rotationObjectType)
    {
        switch (rotationObjectType)
        {
            case RotationObjectType.Cube:
                return rotationVector.x < 0.1 && rotationVector.x > -0.1;
            case RotationObjectType.Face:
                return rotationVector.x < 0.1 && rotationVector.x > -0.1 && rotationVector.y < 0.1 && rotationVector.y > -0.1;
            default:
                return false;
        }
    }

    private bool ValidateHorizontalMove(Vector2 rotationVector)
    {
        return rotationVector.x > 0.1 || rotationVector.x < -0.1;
    }

    private bool ValidateVerticalMove(Vector2 rotationVector)
    {
        return rotationVector.y > 0.1 || rotationVector.y < -0.1;
    }

    private Vector2 NormalizePosition(Vector2 inputVector)
    {
        return new Vector2(inputVector.x / Screen.width, inputVector.y / Screen.height);
    }

    #endregion

    private List<CubeFace> GetCubieFaces(Transform cube)
    {
        var cubieFaces = new List<CubeFace>();
        if (this.DownCubie(cube))
            cubieFaces.Add(CubeFace.Down);
        if (this.UpCubie(cube))
            cubieFaces.Add(CubeFace.Up);
        if (this.FrontCubie(cube))
            cubieFaces.Add(CubeFace.Front);
        if (this.BackCubie(cube))
            cubieFaces.Add(CubeFace.Back);
        if (this.LeftCubie(cube))
            cubieFaces.Add(CubeFace.Left);
        if (this.RightCubie(cube))
            cubieFaces.Add(CubeFace.Right);

        return cubieFaces;
    }

    #region CubeTypeChecks

    // Proverava da li je odredjena kockica na odredjenoj strani kocke
    private bool CubieOnSide(Transform cubie, CubeSide side)
    {
        switch (side)
        {
            case CubeSide.Right:
                return this.RightCubie(cubie);
            case CubeSide.Left:
                return this.LeftCubie(cubie);
            case CubeSide.Up:
                return this.UpCubie(cubie);
            case CubeSide.Down:
                return this.DownCubie(cubie);
            case CubeSide.Front:
                return this.FrontCubie(cubie);
            case CubeSide.Back:
                return this.BackCubie(cubie);
            default: return false;
        }
    }

    // Proverava da li je kockica centralna kockica trazene strane
    private bool IsCentralSideCube(Transform cubie, CubeSide side)
    {
        switch (side)
        {
            case CubeSide.Right:
                return this.CentralRightCubie(cubie);
            case CubeSide.Left:
                return this.CentralLeftCubie(cubie);
            case CubeSide.Up:
                return this.CentralUpCubie(cubie);
            case CubeSide.Down:
                return this.CentralDownCubie(cubie);
            case CubeSide.Front:
                return this.CentralFrontCubie(cubie);
            case CubeSide.Back:
                return this.CentralBackCubie(cubie);
            default: return false;
        }
    }

    private bool UpCubie(Transform cube)
    {
        return cube.position.y > Cube.transform.position.y + 0.5f;
    }

    private bool DownCubie(Transform cube)
    {
        return cube.position.y < Cube.transform.position.y - 0.5f;
    }

    private bool FrontCubie(Transform cube)
    {
        return cube.position.x < Cube.transform.position.x - 0.5f;
    }

    private bool BackCubie(Transform cube)
    {
        return cube.position.x > Cube.transform.position.x + 0.5f;
    }

    private bool LeftCubie(Transform cube)
    {
        return cube.position.z > Cube.transform.position.z + 0.5f;
    }

    private bool RightCubie(Transform cube)
    {
        return cube.position.z < Cube.transform.position.z - 0.5f;
    }

    private bool CentralUpCubie(Transform cube)
    {
        return Mathf.Abs(cube.position.x - Cube.transform.position.x) < 0.01f && Mathf.Abs(cube.position.z - Cube.transform.position.z) < 0.01f && cube.position.y > 0.01f;
    }

    private bool CentralDownCubie(Transform cube)
    {
        return Mathf.Abs(cube.position.x - Cube.transform.position.x) < 0.01f && Mathf.Abs(cube.position.z - Cube.transform.position.z) < 0.01f && cube.position.y < -0.01f;
    }

    private bool CentralBackCubie(Transform cube)
    {
        return Mathf.Abs(cube.position.y - Cube.transform.position.y) < 0.01f && Mathf.Abs(cube.position.z - Cube.transform.position.z) < 0.01f && cube.position.x > 0.01f;
    }

    private bool CentralLeftCubie(Transform cube)
    {
        return Mathf.Abs(cube.position.x - Cube.transform.position.x) < 0.01f && Mathf.Abs(cube.position.y - Cube.transform.position.y) < 0.01f && cube.position.z > 0.01f;
    }

    private bool CentralFrontCubie(Transform cube)
    {
        return Mathf.Abs(cube.position.y - Cube.transform.position.y) < 0.01f && Mathf.Abs(cube.position.z - Cube.transform.position.z) < 0.01f && cube.position.x < -0.01f;
    }

    private bool CentralRightCubie(Transform cube)
    {
        return Mathf.Abs(cube.position.x - Cube.transform.position.x) < 0.01f && Mathf.Abs(cube.position.y - Cube.transform.position.y) < 0.01f && cube.position.z < -0.01f;
    }

    #endregion

    #region Enums

    public enum CubeRotationDirection
    {
        NoRotation,
        Right,
        Left,
        UpRight,
        UpLeft,
        DownLeft,
        DownRight
    }

    private enum CubeFace
    {
        Up,
        Down,
        Front,
        Back,
        Left,
        Right
    }

    private enum FaceRotationDirection
    {
        NoRotation,
        UpRight,
        UpLeft,
        DownLeft,
        DownRight
    }

    private enum RotationObjectType
    {
        None,
        Cube,
        Face
    }

    #endregion
}