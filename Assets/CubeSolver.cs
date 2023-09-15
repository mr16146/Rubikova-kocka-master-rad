using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kociemba;
using CubeColor = StateReader.CubeColor;
using CubeSide = StateReader.CubeSide;
//using CubeMove = CubeSolver.CubeMove;
using System.Linq;
using System.Text;
using static StateReader;
using System.IO;
using System;
using Random = UnityEngine.Random;
using System.Text.RegularExpressions;
using System.Diagnostics;

public class CubeSolver : MonoBehaviour
{
    private StateReader stateReader;
    private CubeRotation cubeRotation;
    private FridrichMethod fridrichMethod;
    private BeginnerAlgorithm beginnerAlgorithm;
    // Kada je isSolving == true tada se ne moze zapoceti novo resavanje kocke jer je trenutno aktivno resavanje kocke
    private bool isSolving = false;
    private List<CubeMove[]> shuffleHistory = new List<CubeMove[]>();
    private bool testingMode = false;
    private bool currentlyTesting = false;
    private const int minimumShuffleMoves = 20;
    private const int maximumShuffleMoves = 26;
    private const string testingResultsFilePath = "testingResults.txt";
    private const string statisticsFilePath = "statistics.txt";
    private float currentSolvingTime = 0;
    private int currentSolvingMovesCount = 0;
    private string currentSolvingMoves = string.Empty;
    private float startTestingTime = 0;
    private float endTestingTime = 0;
    private string kociembaSolutionString = string.Empty;
    private int currentShuffleMovesCount = 0;
    public const float minRotationSpeed = 0.2f;
    public const float maxRotationSpeed = 0.00001f;


    public bool IsSolving {
        get { return isSolving; }
        set { isSolving = value; }
    }

    public int CurrentSolvingMovesCount
    {
        get { return currentSolvingMovesCount; }
        set { currentSolvingMovesCount = value; }
    }

    public string CurrentSolvingMoves
    {
        get { return this.currentSolvingMoves; }
        set { this.currentSolvingMoves = value; }
    }

    public bool TestingModeEnabled
    {
        get { return this.testingMode; }
    }

    public float CurrentSolvingTime
    {
        get { return this.currentSolvingTime; }
        set { this.currentSolvingTime = value; }
    }

    public int CurrentShuffleMovesCount
    {
        get { return this.currentShuffleMovesCount; }
    }

    // Start is called before the first frame update
    void Start()
    {
        this.stateReader = GetComponent<StateReader>();
        this.cubeRotation = GetComponent<CubeRotation>();
        this.fridrichMethod = GetComponent<FridrichMethod>();
        this.beginnerAlgorithm = GetComponent<BeginnerAlgorithm>();

        // Tehnicki poziv koji sluzi za celokupno ucitavanje biblioteke za Kociembin algoritam
        Search.solution("UUUUUULLLURRURRURRFFFFFFFFFRRRDDDDDDLLDLLDLLDBBBBBBBBB", out string info);
    }

    // Update is called once per frame
    void Update()
    {
        if (this.testingMode && !this.IsSolving && !this.currentlyTesting)
        {
            StartCoroutine(DoTesting());
        }
    }

    public IEnumerator DoTesting()
    {
        this.currentlyTesting = true;
        yield return StartCoroutine(this.DoShuffleEvenly());

        if (!this.testingMode)
        {
            this.currentlyTesting = false;
            yield break;
        }

        this.WriteToFile("Shuffle Moves: " + Helper.MovesArrayToString(this.shuffleHistory[this.shuffleHistory.Count - 1]) + "\n");

        yield return StartCoroutine(this.fridrichMethod.SolveFridrich());
        this.WriteToFile("Fridrich Method: " + this.GetTestResultString());

        if (!this.IsCubeSolved())
        {
            this.WriteToFile("FAILED Fridrich Method: " + this.shuffleHistory[this.shuffleHistory.Count - 1]);
            yield return StartCoroutine(this.SolveKociemba(true));
        }

        this.cubeRotation.UpdateLastSolvingMovesCountAndTime();

        if (!this.testingMode)
        {
            this.currentlyTesting = false;
            yield break;
        }

        yield return StartCoroutine(this.DoLastShuffle());

        if (!this.testingMode)
        {
            this.currentlyTesting = false;
            yield break;
        }

        yield return StartCoroutine(this.beginnerAlgorithm.SolveBeginner());
        this.WriteToFile("Beginner Algorithm: " + this.GetTestResultString());

        if (!this.IsCubeSolved())
        {
            this.WriteToFile("FAILED Beginner Algorithm: " + this.shuffleHistory[this.shuffleHistory.Count - 1]);
            yield return StartCoroutine(this.SolveKociemba(true));
        }

        this.cubeRotation.UpdateLastSolvingMovesCountAndTime();

        if (!this.testingMode)
        {
            this.currentlyTesting = false;
            yield break;
        }

        yield return StartCoroutine(this.DoLastShuffle());

        if (!this.testingMode)
        {
            this.currentlyTesting = false;
            yield break;
        }

        yield return StartCoroutine(this.SolveKociemba());
        this.WriteToFile("Kociemba Method: " + this.GetTestResultString());

        this.cubeRotation.UpdateLastSolvingMovesCountAndTime();

        this.currentlyTesting = false;
    }

    public void CalculateStatistics()
    {
        var fridrichMovesCount = new List<int>();
        var fridrichTimes = new List<int>();
        var beginnerMovesCount = new List<int>();
        var beginnerTimes = new List<int>();
        var kociembaMovesCount = new List<int>();
        var kociembaTimes = new List<int>();

        string[] fileLines = File.ReadAllLines(testingResultsFilePath);

        Regex timeRegex = new Regex(@"Time: (\d+)ms");
        Regex movesCountRegex = new Regex(@"Moves Count: (\d+)");

        foreach (string fileLine in fileLines)
        {
            if (fileLine.StartsWith("Fridrich Method:"))
            {
                Match timeMatch = timeRegex.Match(fileLine);
                if (timeMatch.Success)
                {
                    fridrichTimes.Add(int.Parse(timeMatch.Groups[1].Value));
                }

                Match movesCountMatch = movesCountRegex.Match(fileLine);
                if (movesCountMatch.Success)
                {
                    fridrichMovesCount.Add(int.Parse(movesCountMatch.Groups[1].Value));
                }
            }
            else if (fileLine.StartsWith("Beginner Algorithm:"))
            {
                Match timeMatch = timeRegex.Match(fileLine);
                if (timeMatch.Success)
                {
                    beginnerTimes.Add(int.Parse(timeMatch.Groups[1].Value));
                }

                Match movesCountMatch = movesCountRegex.Match(fileLine);
                if (movesCountMatch.Success)
                {
                    beginnerMovesCount.Add(int.Parse(movesCountMatch.Groups[1].Value));
                }
            }
            else if (fileLine.StartsWith("Kociemba Method:"))
            {
                Match timeMatch = timeRegex.Match(fileLine);
                if (timeMatch.Success)
                {
                    kociembaTimes.Add(int.Parse(timeMatch.Groups[1].Value));
                }

                Match movesCountMatch = movesCountRegex.Match(fileLine);
                if (movesCountMatch.Success)
                {
                    kociembaMovesCount.Add(int.Parse(movesCountMatch.Groups[1].Value));
                }
            }
        }

        if (fridrichMovesCount.Count == 0 || beginnerMovesCount.Count == 0 || kociembaMovesCount.Count == 0)
        {
            return;
        }

        this.WriteStatisticsToFile(fridrichTimes, fridrichMovesCount, "Fridrich");
        this.WriteStatisticsToFile(beginnerTimes, beginnerMovesCount, "Beginner");
        this.WriteStatisticsToFile(kociembaTimes, kociembaMovesCount, "Kociemba");
    }

    public void TestingMode()
    {
        this.testingMode = !this.testingMode;
    }

    public void SpeedUpDown()
    {
        this.cubeRotation.RotationSpeed = this.cubeRotation.RotationSpeed == minRotationSpeed ? maxRotationSpeed : minRotationSpeed;
    }

    public void SolveFridrichMethod()
    {
        StartCoroutine(this.fridrichMethod.SolveFridrich());
    }

    public void SolveBeginnerAlgorithm()
    {
        StartCoroutine(this.beginnerAlgorithm.SolveBeginner());
    }

    public void SolveKociembaMethod()
    {
        StartCoroutine(this.SolveKociemba());
    }

    public void LastShuffle()
    {
        if (this.shuffleHistory.Count == 0)
            return;

        StartCoroutine(this.DoLastShuffle());
    }

    public void ResetCube()
    {
        if (!this.isSolving && !this.testingMode)
        {
            StartCoroutine(this.DoResetCube());
        }
    }

    public void ShuffleCube()
    {
        StartCoroutine(this.DoShuffle());
    }

    public void ShuffleCubeEvenly()
    {
        StartCoroutine(this.DoShuffleEvenly());
    }

    public IEnumerator DoLastShuffle()
    {
        if (this.shuffleHistory.Count == 0)
            yield break;

        yield return StartCoroutine(this.DoResetCube());
        yield return this.cubeRotation.ExecuteMoves(this.shuffleHistory[this.shuffleHistory.Count - 1]);
    }

    public IEnumerator DoShuffle()
    {
        if (this.isSolving)
            yield break;

        this.IsSolving = true;

        int shuffleMovesCount = Random.Range(minimumShuffleMoves, maximumShuffleMoves);
        var shuffleCubeMoves = new CubeMove[shuffleMovesCount];

        for (int shuffleMoveIndex = 0; shuffleMoveIndex < shuffleMovesCount; shuffleMoveIndex++)
        {
            CubeSide cubeSide = (CubeSide)Random.Range(1, System.Enum.GetValues(typeof(CubeSide)).Length);
            bool clockwise = Random.Range(0, 2) == 1;
            bool doubleMove = Random.Range(0, 2) != 1;

            shuffleCubeMoves[shuffleMoveIndex] = new CubeMove(cubeSide, clockwise, doubleMove);
        }

        this.shuffleHistory.Add(shuffleCubeMoves);
        yield return this.cubeRotation.ExecuteMoves(shuffleCubeMoves);
    }

    // Ravnomeran broj svakog poteza u jednom mesanju kocke
    public IEnumerator DoShuffleEvenly()
    {
        if (this.isSolving)
            yield break;

        this.IsSolving = true;

        int shuffleMovesCount = Random.Range(minimumShuffleMoves, maximumShuffleMoves);
        CubeMove[] shuffleCubeMoves = new CubeMove[shuffleMovesCount];

        Dictionary<CubeSide, int> moveCountsBySide = new Dictionary<CubeSide, int>
        {
        { CubeSide.Front, 0 },
        { CubeSide.Right, 0 },
        { CubeSide.Back, 0 },
        { CubeSide.Left, 0 },
        { CubeSide.Up, 0 },
        { CubeSide.Down, 0 }
        };

        for (int shuffleMoveIndex = 0; shuffleMoveIndex < shuffleMovesCount; shuffleMoveIndex++)
        {
            List<CubeSide> availableSides = new List<CubeSide>();
            double averageCount = moveCountsBySide.Values.Average();

            foreach (KeyValuePair<CubeSide, int> moveCountBySide in moveCountsBySide)
            {
                if (moveCountBySide.Value < averageCount + 1)
                {
                    availableSides.Add(moveCountBySide.Key);
                }
            }

            CubeSide cubeSide = availableSides[Random.Range(0, availableSides.Count)];
            moveCountsBySide[cubeSide]++;

            bool clockwise = Random.Range(0, 2) == 1;
            bool doubleMove = Random.Range(0, 2) != 1;

            shuffleCubeMoves[shuffleMoveIndex] = new CubeMove(cubeSide, clockwise, doubleMove);
        }

        this.shuffleHistory.Add(shuffleCubeMoves);
        this.currentShuffleMovesCount = shuffleCubeMoves.Count();
        yield return this.cubeRotation.ExecuteMoves(shuffleCubeMoves);
    }

    public IEnumerator DoResetCube()
    {
        float currentRotationSpeed = this.cubeRotation.RotationSpeed;
        this.cubeRotation.RotationSpeed = maxRotationSpeed;

        yield return StartCoroutine(this.SolveKociemba(true));

        this.cubeRotation.RotationSpeed = currentRotationSpeed;
    }

    public bool IsCubeSolved(bool solving = false)
    {
        Dictionary<CubeSide, CubeColor[]> cubeState = solving ? this.stateReader.SolvingCubeStateWrapper.CubeStateData.CubeState : this.stateReader.CubeStateWrapper.CubeStateData.CubeState;
        foreach (KeyValuePair<CubeSide, CubeColor[]> sideState in cubeState)
        {
            CubeSide cubeSide = sideState.Key;
            CubeColor currentSideColor = sideState.Value[4];

            foreach (CubeColor facetColor in sideState.Value)
            {
                if (facetColor != currentSideColor)
                    return false;
            }
        }

        return true;
    }

    public void SaveCurrentResults(List<CubeMove> solutionMoves)
    {
        this.CurrentSolvingMovesCount = solutionMoves.Count;
        this.CurrentSolvingMoves = Helper.MovesArrayToString(solutionMoves.ToArray());
    }

    private string GetTestResultString()
    {
        int timeInMs = (int)(this.currentSolvingTime);
        return "Time: " + timeInMs + "ms, Moves Count: " + this.currentSolvingMovesCount + "\nMoves: " + this.currentSolvingMoves + "\n";
    }


    private void WriteStatisticsToFile(List<int> times, List<int> movesCount, string methodName)
    {
        int averageTime = (int)times.Average();
        int averageMovesCount = (int)movesCount.Average();

        double timeStandardDeviation = Math.Sqrt(times.Select(x => Math.Pow(x - averageTime, 2)).Sum() / times.Count);
        double movesCountStandardDeviation = Math.Sqrt(movesCount.Select(x => Math.Pow(x - averageMovesCount, 2)).Sum() / movesCount.Count);

        this.WriteToFile($"{methodName} method average time: {averageTime}ms", true);
        this.WriteToFile($"{methodName} method minimum time: {times.Min()}ms", true);
        this.WriteToFile($"{methodName} method maximum time: {times.Max()}ms", true);

        this.WriteToFile($"{methodName} method average moves count:{averageMovesCount} ", true);
        this.WriteToFile($"{methodName} method minimum moves count: {movesCount.Min()}", true);
        this.WriteToFile($"{methodName} method maximum moves count: {movesCount.Max()}", true);

        this.WriteToFile($"{methodName} method time standard deviation: {timeStandardDeviation}", true);
        this.WriteToFile($"{methodName} method moves count standard deviation: {movesCountStandardDeviation}\n", true);
    }

    private void WriteToFile(string testResult, bool statistics = false)
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(statistics ? statisticsFilePath : testingResultsFilePath, true))
            {
                writer.WriteLine(testResult);
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine("An error occurred while writing to file: " + exception.Message);
        }
    }

    // NAPOMENA: Ova metoda je importovana, nisam je ja pisao, izvor: https://github.com/Megalomatt/Kociemba/tree/Unity
    #region Kociemba

    public IEnumerator SolveKociemba(bool resetCubePurpose = false)
    {
        if (this.isSolving)
            yield break;

        this.IsSolving = true;

        

        // Transformise se stanje kocke iz aplikacije u trazeni oblik za Kociemba biblioteku
        string inputState = this.GetKociembaInputState();

        // BuildTables se koristi samo pri prvom pozivanju, nakon toga se naprave lokalno na disku
        //string solutionString = SearchRunTime.solution(inputState, out string info, buildTables: true);
        Stopwatch stopwatch = Stopwatch.StartNew();
        yield return StartCoroutine(this.CalculateKociemba());
        stopwatch.Stop();

        string[] solutionKociembaMoves = this.kociembaSolutionString.Split(' ');
        CubeMove[] solutionCubeMoves = new CubeMove[solutionKociembaMoves.Length];

        for (int i = 0; i < solutionKociembaMoves.Length; i++)
        {
            solutionCubeMoves[i] = Helper.StringToCubeMove(solutionKociembaMoves[i]);
        }

        solutionCubeMoves = solutionCubeMoves.Where(move => move.CubeSide != CubeSide.NoSide).ToArray();

        if (!resetCubePurpose && solutionCubeMoves.Count() > 0)
        {
            this.cubeRotation.CurrentMethodText = "Kociemba Method";
            this.currentSolvingTime = stopwatch.ElapsedMilliseconds;
            this.SaveCurrentResults(solutionCubeMoves.ToList());
        }

        yield return this.cubeRotation.ExecuteMoves(solutionCubeMoves);
        this.cubeRotation.CurrentMethodText = "None";
    }

    private string GetKociembaInputState()
    {
        StringBuilder inputState = new StringBuilder(string.Empty, 54);

        inputState.Append(this.SideToKociembaSideState(CubeSide.Up));
        inputState.Append(this.SideToKociembaSideState(CubeSide.Right));
        inputState.Append(this.SideToKociembaSideState(CubeSide.Front));
        inputState.Append(this.SideToKociembaSideState(CubeSide.Down));
        inputState.Append(this.SideToKociembaSideState(CubeSide.Left));
        inputState.Append(this.SideToKociembaSideState(CubeSide.Back));

        return inputState.ToString();
    }

    private IEnumerator CalculateKociemba()
    {
        var inputState = this.GetKociembaInputState();
        this.kociembaSolutionString = Search.solution(inputState, out string info);
        yield return this.kociembaSolutionString;
    }

    private StringBuilder SideToKociembaSideState(CubeSide cubeSide)
    {
        StringBuilder sideState = new StringBuilder(string.Empty, 9);

        CubeColor[] sideColors = this.stateReader.CubeStateWrapper.CubeStateData.CubeState[cubeSide];

        for (int i = 0; i < sideColors.Length; i++)
        {
            sideState.Append(this.sideByColor[sideColors[i]]);
        }

        return sideState;
    }

    private Dictionary<CubeColor, char> sideByColor = new Dictionary<CubeColor, char>
    {
        { CubeColor.Blue, 'F' },
        { CubeColor.Green, 'B'},
        { CubeColor.Yellow, 'U' },
        { CubeColor.White, 'D' },
        { CubeColor.Orange, 'L' },
        { CubeColor.Red, 'R' }
    };

    #endregion
}
