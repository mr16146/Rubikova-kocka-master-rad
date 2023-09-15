using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static CubeSolver;
using static StateReader;
using static CubeRotation;
using CubeColor = StateReader.CubeColor;
using CubeSide = StateReader.CubeSide;
using static BeginnerAlgorithm;
using System.Linq;
using UnityEngine.XR;
using static Helper;
using EdgeCubePosition = BeginnerAlgorithm.EdgeCubePosition;
using CornerCubePosition = BeginnerAlgorithm.CornerCubePosition;
using System.Diagnostics;


public class FridrichMethod : MonoBehaviour
{
    // Begginer algorithm referenca, zbog citljivosti
    public BeginnerAlgorithm baRef;

    // Start is called before the first frame update
    void Start()
    {
        this.baRef = GetComponent<BeginnerAlgorithm>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public IEnumerator SolveFridrich()
    {
        if (this.baRef.cubeSolver.IsSolving || this.baRef.cubeSolver.IsCubeSolved())
            yield break;

        this.baRef.cubeSolver.IsSolving = true;

        Stopwatch stopwatch = Stopwatch.StartNew();

        this.baRef.stateReader.SolvingCubeStateWrapper.CubeStateData = new CubeStateData(this.baRef.stateReader.CubeStateWrapper.CubeStateData);

        var solutionMovesByStepName = new Dictionary<string, CubeMove[]>();

        // Pravi beli krst na zutoj strani kocke
        solutionMovesByStepName.Add("White Cross ", Helper.RemoveRedundantMoves(this.baRef.WhiteCrossOnYellowSide()).ToArray());
        // Pravi beli krst na beloj strani kocke
        solutionMovesByStepName.Add("White Cross", Helper.RemoveRedundantMoves(this.baRef.WhiteCross()).ToArray());
        // Objasnjenje svih 41 slucaja: https://ruwix.com/the-rubiks-cube/advanced-cfop-fridrich/first-two-layers-f2l/
        solutionMovesByStepName.Add("First Two Layers", Helper.RemoveRedundantMoves(this.FirstTwoLayers()).ToArray());
        // Pravi zuti krst na gornjem sloju, ovaj korak je isti kao i prvi korak, 1look, algoritma  2nd look oll 
        solutionMovesByStepName.Add("First Look OLL", Helper.RemoveRedundantMoves(this.baRef.YellowCross()).ToArray());
        // Prebacuje sva polja boje gornje strane na gornju stranu, objasnjenje poteza za 2nd look oll: https://cubingcheatsheet.com/algs3x_2loll.html
        solutionMovesByStepName.Add("Second Look OLL", Helper.RemoveRedundantMoves(this.SecondLookOll()).ToArray());
        // Pozicionira i prijentise ugaone kockice na gornjem sloju, objasnjenje poteza za 2nd look pll: https://cubingcheatsheet.com/algs3x_2lpll.html
        solutionMovesByStepName.Add("First Look PLL", Helper.RemoveRedundantMoves(this.FirstLookPll()).ToArray());
        // Pozicionira i prijentise ivicne kockice na gornjem sloju
        solutionMovesByStepName.Add("Second Look PLL", Helper.RemoveRedundantMoves(this.SecondLookPll()).ToArray());

        stopwatch.Stop();

        this.baRef.cubeSolver.CurrentSolvingTime = stopwatch.ElapsedMilliseconds;
        this.baRef.cubeSolver.SaveCurrentResults(solutionMovesByStepName.Values.SelectMany(el => el).ToList());

        yield return this.baRef.cubeRotation.ExecuteMovesWithStepsNames(solutionMovesByStepName, "Fridrich Method");

    }

    private List<CubeMove> FirstLookPll()
    {
        var solutionMoves = new List<CubeMove>();

        if (this.baRef.cubeSolver.IsCubeSolved(true))
            return solutionMoves;

        Dictionary<CornerCubePosition, CornerCube> upCorners = this.GetUpCornerCubes();

        // Provera da li je ovaj korak uopste potreban
        if (upCorners[CornerCubePosition.UpLeft].ColorBySide[CubeSide.Up] == upCorners[CornerCubePosition.UpRight].ColorBySide[CubeSide.Up] &&
            upCorners[CornerCubePosition.DownLeft].ColorBySide[CubeSide.Down] == upCorners[CornerCubePosition.DownRight].ColorBySide[CubeSide.Down])
            return solutionMoves;

        foreach (KeyValuePair<CubeSide, CubeColor[]> sideState in this.baRef.CubeState)
        {
            CubeSide cubeSide = sideState.Key;

            if (cubeSide == CubeSide.Up || cubeSide == CubeSide.Down)
                continue;

            this.baRef.RotateToSide(cubeSide);

            upCorners = this.GetUpCornerCubes();

            if (upCorners[CornerCubePosition.UpLeft].ColorBySide[CubeSide.Up] == upCorners[CornerCubePosition.UpRight].ColorBySide[CubeSide.Up])
            {
                solutionMoves.AddRange(this.HeadlightsBack());

                return solutionMoves;
            }
        }

        this.baRef.RotateToSide(CubeSide.Front);
        solutionMoves.AddRange(this.NoHeadlights());

        return solutionMoves;
    }

    private List<CubeMove> SecondLookPll()
    {
        var solutionMoves = new List<CubeMove>();

        if (this.baRef.cubeSolver.IsCubeSolved(true))
            return solutionMoves;

        foreach (KeyValuePair<CubeSide, CubeColor[]> sideState in this.baRef.CubeState)
        {
            CubeSide cubeSide = sideState.Key;

            if (cubeSide == CubeSide.Up || cubeSide == CubeSide.Down)
                continue;

            this.baRef.RotateToSide(cubeSide);

            Dictionary<EdgeCubePosition, EdgeCube> upEdges = this.GetUpEdgeCubes();
            Dictionary<CornerCubePosition, CornerCube> upCorners = this.GetUpCornerCubes();
            CubeColor rightSideColor = upCorners[CornerCubePosition.DownRight].ColorBySide[CubeSide.Right];
            CubeColor leftSideColor = upCorners[CornerCubePosition.DownLeft].ColorBySide[CubeSide.Left];
            CubeColor currentSideColor = upCorners[CornerCubePosition.DownRight].ColorBySide[CubeSide.Down];
            CubeColor backSideColor = upCorners[CornerCubePosition.UpRight].ColorBySide[CubeSide.Up];

            if (upEdges[EdgeCubePosition.Right].ColorBySide[CubeSide.Right] == backSideColor &&
                upEdges[EdgeCubePosition.Up].ColorBySide[CubeSide.Up] == leftSideColor &&
                upEdges[EdgeCubePosition.Left].ColorBySide[CubeSide.Left] == rightSideColor)
            {
                solutionMoves.AddRange(this.CounterClockwiseEdges());
                break;
            }
            else if (upEdges[EdgeCubePosition.Right].ColorBySide[CubeSide.Right] == leftSideColor &&
                upEdges[EdgeCubePosition.Up].ColorBySide[CubeSide.Up] == rightSideColor &&
                upEdges[EdgeCubePosition.Left].ColorBySide[CubeSide.Left] == backSideColor)
            {
                solutionMoves.AddRange(this.ClockwiseEdges());
                break;
            }
            else if (upEdges[EdgeCubePosition.Left].ColorBySide[CubeSide.Left] == currentSideColor &&
                upEdges[EdgeCubePosition.Down].ColorBySide[CubeSide.Down] == leftSideColor)
            {
                solutionMoves.AddRange(this.AdjecentEdges());
                break;
            }
            else if (upEdges[EdgeCubePosition.Left].ColorBySide[CubeSide.Left] == rightSideColor &&
                upEdges[EdgeCubePosition.Right].ColorBySide[CubeSide.Right] == leftSideColor)
            {
                solutionMoves.AddRange(this.OppositeEdges());
                break;
            }
        }

        this.baRef.RotateToSide(CubeSide.Front);
        CubeColor frontSideColor = this.baRef.GetCurrentSideColor(CubeSide.Front);

        int upMoves = 0;
        while (this.baRef.CubeState[CubeSide.Front][0] != frontSideColor)
        {
            this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
            upMoves++;
        }

        solutionMoves.AddRange(this.DoSolutionUpMoves(upMoves));

        return solutionMoves;
    }

    private List<CubeMove> SecondLookOll()
    {
        var solutionMoves = new List<CubeMove>();

        if (this.IsUpSideOneColored())
            return solutionMoves;

        this.baRef.RotateToSide(CubeSide.Front);

        Dictionary<CornerCubePosition, CornerCube> upCorners = this.GetUpCornerCubes();
        CubeColor upSideColor = this.baRef.GetCurrentSideColor(CubeSide.Up);

        int yellowCornersCount = 0;

        foreach (CornerCube upCorner in upCorners.Values)
        {
            if (upCorner.ColorBySide[CubeSide.Front] == upSideColor)
                yellowCornersCount++;
        }

        switch (yellowCornersCount)
        {
            case 0:
                solutionMoves.AddRange(ZeroYellowUpCorners());
                break;
            case 1:
                solutionMoves.AddRange(OneYellowUpCorner());
                break;
            case 2:
                solutionMoves.AddRange(TwoYellowUpCorners());
                break;
            default:
                break;
        }

        return solutionMoves;
    }

    private List<CubeMove> TwoYellowUpCorners()
    {
        var solutionMoves = new List<CubeMove>();
        CubeColor upSideColor = this.baRef.GetCurrentSideColor(CubeSide.Up);

        foreach (KeyValuePair<CubeSide, CubeColor[]> sideState in this.baRef.CubeState)
        {
            CubeSide cubeSide = sideState.Key;

            if (cubeSide == CubeSide.Up || cubeSide == CubeSide.Down)
                continue;

            this.baRef.RotateToSide(cubeSide);

            Dictionary<CornerCubePosition, CornerCube> upCorners = this.GetUpCornerCubes();

            if (upCorners[CornerCubePosition.DownLeft].ColorBySide[CubeSide.Down] == upSideColor &&
                upCorners[CornerCubePosition.DownRight].ColorBySide[CubeSide.Down] == upSideColor)
            {
                solutionMoves.AddRange(this.HeadlightsPattern());
                break;
            }
            else if (upCorners[CornerCubePosition.DownLeft].ColorBySide[CubeSide.Down] == upSideColor &&
                upCorners[CornerCubePosition.UpLeft].ColorBySide[CubeSide.Up] == upSideColor)
            {
                solutionMoves.AddRange(this.PatternT());
                break;
            }
            else if (upCorners[CornerCubePosition.DownLeft].ColorBySide[CubeSide.Left] == upSideColor &&
                upCorners[CornerCubePosition.UpRight].ColorBySide[CubeSide.Up] == upSideColor)
            {
                solutionMoves.AddRange(this.BowtiePattern());
                break;
            }
        }

        return solutionMoves;
    }

    private List<CubeMove> ZeroYellowUpCorners()
    {
        var solutionMoves = new List<CubeMove>();
        CubeColor upSideColor = this.baRef.GetCurrentSideColor(CubeSide.Up);

        foreach (KeyValuePair<CubeSide, CubeColor[]> sideState in this.baRef.CubeState)
        {
            CubeSide cubeSide = sideState.Key;

            if (cubeSide == CubeSide.Up || cubeSide == CubeSide.Down)
                continue;

            this.baRef.RotateToSide(cubeSide);

            Dictionary<CornerCubePosition, CornerCube> upCorners = this.GetUpCornerCubes();

            if (!(upCorners[CornerCubePosition.DownLeft].ColorBySide[CubeSide.Down] == upSideColor &&
                upCorners[CornerCubePosition.DownRight].ColorBySide[CubeSide.Down] == upSideColor))
                continue;

            if (upCorners[CornerCubePosition.UpLeft].ColorBySide[CubeSide.Up] == upSideColor &&
                upCorners[CornerCubePosition.UpRight].ColorBySide[CubeSide.Up] == upSideColor)
            {
                solutionMoves.AddRange(this.PatternH());
                break;
            }
            else
            {
                this.baRef.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.Left);
                solutionMoves.AddRange(this.PatternPi());
                break;
            }
        }

        return solutionMoves;
    }

    private List<CubeMove> OneYellowUpCorner()
    {
        var solutionMoves = new List<CubeMove>();
        CubeColor upSideColor = this.baRef.GetCurrentSideColor(CubeSide.Up);

        foreach (KeyValuePair<CubeSide, CubeColor[]> sideState in this.baRef.CubeState)
        {
            CubeSide cubeSide = sideState.Key;

            if (cubeSide == CubeSide.Up || cubeSide == CubeSide.Down)
                continue;

            this.baRef.RotateToSide(cubeSide);

            Dictionary<CornerCubePosition, CornerCube> upCorners = this.GetUpCornerCubes();

            if (upCorners[CornerCubePosition.DownLeft].ColorBySide[CubeSide.Front] != upSideColor)
                continue;

            if (upCorners[CornerCubePosition.DownRight].ColorBySide[CubeSide.Down] == upSideColor)
            {
                solutionMoves.AddRange(this.SunePattern());
                break;
            }
            else
            {
                this.baRef.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.Right);
                solutionMoves.AddRange(this.AntiSunePattern());
                break;
            }
        }

        return solutionMoves;
    }
    
    private List<CubeMove> FirstTwoLayers()
    {
        var solutionMoves = new List<CubeMove>();
        int solutionMovesCount = -1;

        while (solutionMoves.Count != solutionMovesCount && !this.AreFirstTwoLayersDone())
        {
            solutionMovesCount = solutionMoves.Count;

            solutionMoves.AddRange(this.FirstTwoLayerSolvingMoves());
        }

        //// U slucaju da se algoritam nasao u zaglavljenoj situaciji, tj. ne moze da se primeni ni jedan od 41 slucaja
        if (solutionMoves.Count == solutionMovesCount && !this.AreFirstTwoLayersDone())
        {
            int unStuckTries = 4;

            while (unStuckTries-- > 0)
            {
                solutionMoves.AddRange(this.UnstuckFirstTwoLayers());

                if (this.AreFirstTwoLayersDone())
                    break;
            }
        }

        // Ako i pored odglavljivanja prva 2 sloja nisu resena, primenjuje se resavanje osnovnog algoritma za prva 2 sloja
        if (!this.AreFirstTwoLayersDone())
        {
            solutionMoves.AddRange(this.baRef.FirstLayer());
            solutionMoves.AddRange(this.baRef.SecondLayer());
        }

        return solutionMoves;
    }

    private List<CubeMove> UnstuckFirstTwoLayers()
    {
        var startingSolvingCubeData = new CubeStateData(this.baRef.stateReader.SolvingCubeStateWrapper.CubeStateData);
        var bestSolvingCubeData = new CubeStateData(this.baRef.stateReader.SolvingCubeStateWrapper.CubeStateData);
        var bestSolutionMoves = new List<CubeMove>();

        foreach (KeyValuePair<CubeSide, CubeColor[]> sideState in this.baRef.CubeState)
        {
            CubeSide cubeSide = sideState.Key;

            if (cubeSide == CubeSide.Up || cubeSide == CubeSide.Down)
                continue;

            List<CubeMove> currentSolutionMoves = this.UnstuckFromSide(cubeSide);

            if (this.AreFirstTwoLayersDone())
            {
                return currentSolutionMoves;
            }

            if (currentSolutionMoves.Count > bestSolutionMoves.Count)
            {
                bestSolvingCubeData = this.baRef.stateReader.SolvingCubeStateWrapper.CubeStateData;
                bestSolutionMoves = currentSolutionMoves;
            }

            this.baRef.stateReader.SolvingCubeStateWrapper.CubeStateData = new CubeStateData(startingSolvingCubeData);
        }

        this.baRef.stateReader.SolvingCubeStateWrapper.CubeStateData = new CubeStateData(bestSolvingCubeData);
        return bestSolutionMoves;
    }

    private List<CubeMove> UnstuckFromSide(CubeSide cubeSide)
    {
        var solutionMoves = new List<CubeMove>();

        this.baRef.RotateToSide(cubeSide);

        Dictionary<CornerCubePosition, CornerCube> cornerCubes = this.baRef.CollectCurrentCornerCubes();
        Dictionary<EdgeCubePosition, EdgeCube> edgeCubes = this.baRef.CollectCurrentEdgeCubes();

        if (!this.baRef.IsCurrentCornerCubeOrientated(cornerCubes[CornerCubePosition.DownRight]) ||
            !this.baRef.IsCurrentEdgeCubeOrientated(edgeCubes[EdgeCubePosition.Right]))
        {
            // Na ovaj nacin se donja desna ugaona kockica i desna ivicna kockica prebacuju u gornji sloj
            solutionMoves.AddRange(this.StringToSolutionMoves("R U R'"));
            solutionMoves.AddRange(this.FirstTwoLayerSolvingMoves());
        }

        return solutionMoves;
    }

    private List<CubeMove> FirstTwoLayerSolvingMoves()
    {
        var solutionMoves = new List<CubeMove>();

        solutionMoves.AddRange(this.FirstGroupCases());
        solutionMoves.AddRange(this.SecondGroupCases());
        solutionMoves.AddRange(this.ThirdGroupCases());
        solutionMoves.AddRange(this.FourthGroupCases());
        solutionMoves.AddRange(this.FifthGroupCases());
        solutionMoves.AddRange(this.SixthGroupCases());

        return solutionMoves;
    }

    private List<CubeMove> SixthGroupCases()
    {
        var solutionMoves = new List<CubeMove>();

        foreach (KeyValuePair<CubeSide, CubeColor[]> sideState in this.baRef.CubeState)
        {
            CubeSide cubeSide = sideState.Key;

            if (cubeSide == CubeSide.Up || cubeSide == CubeSide.Down)
                continue;

            this.baRef.RotateToSide(cubeSide);

            HashSet<CubeColor> targetEdgeColors = this.baRef.GetTargetEdgeCubeColors(EdgeCubePosition.Right);
            HashSet<CubeColor> targetCornerColors = this.baRef.GetTargetCornerCubeColors(CornerCubePosition.DownRight);

            Dictionary<CornerCubePosition, CornerCube> cornerCubes = this.baRef.CollectCurrentCornerCubes();
            Dictionary<EdgeCubePosition, EdgeCube> edgeCubes = this.baRef.CollectCurrentEdgeCubes();
            CubeColor currentSideColor = this.baRef.GetCurrentSideColor(CubeSide.Front);
            CubeColor rightSideColor = this.baRef.GetCurrentSideColor(CubeSide.Right);

            // Ako su donja desna ugaona kockica i desna ivicna kockica na svom mestu
            if (this.baRef.CheckCornerCubeColors(cornerCubes[CornerCubePosition.DownRight], targetCornerColors) &&
                this.baRef.CheckEdgeCubeColors(edgeCubes[EdgeCubePosition.Right], targetEdgeColors))
            {
                if (edgeCubes[EdgeCubePosition.Right].ColorBySide[CubeSide.Front] == rightSideColor)
                {
                    // slucaj 1
                    if (cornerCubes[CornerCubePosition.DownRight].ColorBySide[CubeSide.Right] == rightSideColor &&
                    cornerCubes[CornerCubePosition.DownRight].ColorBySide[CubeSide.Front] == currentSideColor)
                    {
                        solutionMoves.AddRange(this.Group6Case1());
                    }
                    //slucaj 3
                    else if (cornerCubes[CornerCubePosition.DownRight].ColorBySide[CubeSide.Front] == rightSideColor &&
                    cornerCubes[CornerCubePosition.DownRight].ColorBySide[CubeSide.Down] == currentSideColor)
                    {
                        solutionMoves.AddRange(this.Group6Case3());
                    }
                    //slucaj 5
                    else if (cornerCubes[CornerCubePosition.DownRight].ColorBySide[CubeSide.Down] == rightSideColor &&
                    cornerCubes[CornerCubePosition.DownRight].ColorBySide[CubeSide.Right] == currentSideColor)
                    {
                        solutionMoves.AddRange(this.Group6Case5());
                    }
                }
                else
                {
                    //slucaj 2
                    if (cornerCubes[CornerCubePosition.DownRight].ColorBySide[CubeSide.Front] == rightSideColor &&
                    cornerCubes[CornerCubePosition.DownRight].ColorBySide[CubeSide.Down] == currentSideColor)
                    {
                        solutionMoves.AddRange(this.Group6Case2());
                    }
                    //slucaj 4
                    else if (cornerCubes[CornerCubePosition.DownRight].ColorBySide[CubeSide.Down] == rightSideColor &&
                    cornerCubes[CornerCubePosition.DownRight].ColorBySide[CubeSide.Right] == currentSideColor)
                    {
                        solutionMoves.AddRange(this.Group6Case4());
                    }
                }
            }
        }

        return solutionMoves;
    }

    private List<CubeMove> FifthGroupCases()
    {
        var solutionMoves = new List<CubeMove>();

        foreach (KeyValuePair<CubeSide, CubeColor[]> sideState in this.baRef.CubeState)
        {
            CubeSide cubeSide = sideState.Key;

            if (cubeSide == CubeSide.Up || cubeSide == CubeSide.Down)
                continue;

            this.baRef.RotateToSide(cubeSide);

            HashSet<CubeColor> targetEdgeColors = this.baRef.GetTargetEdgeCubeColors(EdgeCubePosition.Right);
            HashSet<CubeColor> targetCornerColors = this.baRef.GetTargetCornerCubeColors(CornerCubePosition.DownRight);

            Dictionary<CornerCubePosition, CornerCube> cornerCubes = this.baRef.CollectCurrentCornerCubes();
            Dictionary<EdgeCubePosition, EdgeCube> upEdgeCubes = this.GetUpEdgeCubes();
            Dictionary<CornerCubePosition, CornerCube> upCornerCubes = this.GetUpCornerCubes();

            // Ako su donja desna ugaona kockica i desna ivicna kockica u gornjem sloju
            if (upEdgeCubes.Values.Any(cube => this.baRef.CheckEdgeCubeColors(cube, targetEdgeColors)) &&
            upCornerCubes.Values.Any(cube => this.baRef.CheckCornerCubeColors(cube, targetCornerColors)))
            {
                int upMovesCount = 0;
                while (!this.baRef.CheckCornerCubeColors(cornerCubes[CornerCubePosition.UpRight], targetCornerColors))
                {
                    this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
                    upMovesCount++;

                    cornerCubes = this.baRef.CollectCurrentCornerCubes();
                }

                CubeColor currentSideColor = this.baRef.GetCurrentSideColor(CubeSide.Front);
                CubeColor rightSideColor = this.baRef.GetCurrentSideColor(CubeSide.Right);

                // Ako gornja desna ugaona kockica, koja je trenutno trazena donja desna ugaona kockica, nije zarotirana tako da je donja boja kocke okrenuta na gore - prekid metode
                if (!(cornerCubes[CornerCubePosition.UpRight].ColorBySide[CubeSide.Right] == currentSideColor &&
                cornerCubes[CornerCubePosition.UpRight].ColorBySide[CubeSide.Front] == rightSideColor))
                {
                    if (upMovesCount > 0)
                        this.baRef.DoSolvingStateUpRotations(4 - upMovesCount);

                    return solutionMoves;
                }

                upEdgeCubes = this.GetUpEdgeCubes();

                // slucaj 1
                if (upEdgeCubes[EdgeCubePosition.Down].ColorBySide[CubeSide.Down] == rightSideColor &&
                    upEdgeCubes[EdgeCubePosition.Down].ColorBySide[CubeSide.Front] == currentSideColor)
                {
                    solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                    solutionMoves.AddRange(this.Group5Case1());
                    continue;
                }
                // slucaj 2
                else if (upEdgeCubes[EdgeCubePosition.Left].ColorBySide[CubeSide.Left] == rightSideColor &&
                    upEdgeCubes[EdgeCubePosition.Left].ColorBySide[CubeSide.Front] == currentSideColor)
                {
                    solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                    solutionMoves.AddRange(this.Group5Case2());
                    continue;
                }
                // slucaj 3
                else if (upEdgeCubes[EdgeCubePosition.Up].ColorBySide[CubeSide.Up] == rightSideColor &&
                    upEdgeCubes[EdgeCubePosition.Up].ColorBySide[CubeSide.Front] == currentSideColor)
                {
                    solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                    solutionMoves.AddRange(this.Group5Case3());
                    continue;
                }
                // slucaj 4
                else if (upEdgeCubes[EdgeCubePosition.Right].ColorBySide[CubeSide.Right] == rightSideColor &&
                    upEdgeCubes[EdgeCubePosition.Right].ColorBySide[CubeSide.Front] == currentSideColor)
                {
                    solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                    solutionMoves.AddRange(this.Group5Case4());
                    continue;
                }
                // slucaj 5
                else if (upEdgeCubes[EdgeCubePosition.Right].ColorBySide[CubeSide.Front] == rightSideColor &&
                    upEdgeCubes[EdgeCubePosition.Right].ColorBySide[CubeSide.Right] == currentSideColor)
                {
                    solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                    solutionMoves.AddRange(this.Group5Case5());
                    continue;
                }
                // slucaj 6
                else if (upEdgeCubes[EdgeCubePosition.Up].ColorBySide[CubeSide.Front] == rightSideColor &&
                    upEdgeCubes[EdgeCubePosition.Up].ColorBySide[CubeSide.Up] == currentSideColor)
                {
                    solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                    solutionMoves.AddRange(this.Group5Case6());
                    continue;
                }
                // slucaj 7
                else if (upEdgeCubes[EdgeCubePosition.Left].ColorBySide[CubeSide.Front] == rightSideColor &&
                    upEdgeCubes[EdgeCubePosition.Left].ColorBySide[CubeSide.Left] == currentSideColor)
                {
                    solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                    solutionMoves.AddRange(this.Group5Case7());
                    continue;
                }
                // slucaj 8
                else if (upEdgeCubes[EdgeCubePosition.Down].ColorBySide[CubeSide.Front] == rightSideColor &&
                    upEdgeCubes[EdgeCubePosition.Down].ColorBySide[CubeSide.Down] == currentSideColor)
                {
                    solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                    solutionMoves.AddRange(this.Group5Case8());
                    continue;
                }

                if (upMovesCount > 0)
                    this.baRef.DoSolvingStateUpRotations(4 - upMovesCount);
            }
        }

        return solutionMoves;
    }

    private List<CubeMove> FourthGroupCases()
    {
        var solutionMoves = new List<CubeMove>();

        foreach (KeyValuePair<CubeSide, CubeColor[]> sideState in this.baRef.CubeState)
        {
            CubeSide cubeSide = sideState.Key;

            if (cubeSide == CubeSide.Up || cubeSide == CubeSide.Down)
                continue;

            this.baRef.RotateToSide(cubeSide);

            HashSet<CubeColor> targetEdgeColors = this.baRef.GetTargetEdgeCubeColors(EdgeCubePosition.Right);
            HashSet<CubeColor> targetCornerColors = this.baRef.GetTargetCornerCubeColors(CornerCubePosition.DownRight);

            Dictionary<CornerCubePosition, CornerCube> cornerCubes = this.baRef.CollectCurrentCornerCubes();
            Dictionary<EdgeCubePosition, EdgeCube> upEdgeCubes = this.GetUpEdgeCubes();
            Dictionary<CornerCubePosition, CornerCube> upCornerCubes = this.GetUpCornerCubes();

            // Ako su donja desna ugaona kockica i desna ivicna kockica u gornjem sloju
            if (upEdgeCubes.Values.Any(cube => this.baRef.CheckEdgeCubeColors(cube, targetEdgeColors)) &&
            upCornerCubes.Values.Any(cube => this.baRef.CheckCornerCubeColors(cube, targetCornerColors)))
            {
                int upMovesCount = 0;
                while (!this.baRef.CheckCornerCubeColors(cornerCubes[CornerCubePosition.UpRight], targetCornerColors))
                {
                    this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
                    upMovesCount++;

                    cornerCubes = this.baRef.CollectCurrentCornerCubes();
                }

                upEdgeCubes = this.GetUpEdgeCubes();
                CubeColor currentSideColor = this.baRef.GetCurrentSideColor(CubeSide.Front);
                CubeColor rightSideColor = this.baRef.GetCurrentSideColor(CubeSide.Right);

                if (cornerCubes[CornerCubePosition.UpRight].ColorBySide[CubeSide.Up] == rightSideColor &&
                    cornerCubes[CornerCubePosition.UpRight].ColorBySide[CubeSide.Front] == currentSideColor)
                {
                    // slucaj 1
                    if (upEdgeCubes[EdgeCubePosition.Right].ColorBySide[CubeSide.Front] == rightSideColor &&
                        upEdgeCubes[EdgeCubePosition.Right].ColorBySide[CubeSide.Right] == currentSideColor)
                    {
                        solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                        solutionMoves.AddRange(this.Group4Case1());
                        continue;
                    }
                    // slucaj 2
                    else if (upEdgeCubes[EdgeCubePosition.Up].ColorBySide[CubeSide.Front] == rightSideColor &&
                        upEdgeCubes[EdgeCubePosition.Up].ColorBySide[CubeSide.Up] == currentSideColor)
                    {
                        solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                        solutionMoves.AddRange(this.Group4Case2());
                        continue;
                    }
                    // slucaj 3
                    else if (upEdgeCubes[EdgeCubePosition.Left].ColorBySide[CubeSide.Front] == rightSideColor &&
                        upEdgeCubes[EdgeCubePosition.Left].ColorBySide[CubeSide.Left] == currentSideColor)
                    {
                        solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                        solutionMoves.AddRange(this.Group4Case3());
                        continue;
                    }
                    // slucaj 4
                    else if (upEdgeCubes[EdgeCubePosition.Right].ColorBySide[CubeSide.Right] == rightSideColor &&
                        upEdgeCubes[EdgeCubePosition.Right].ColorBySide[CubeSide.Front] == currentSideColor)
                    {
                        solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                        solutionMoves.AddRange(this.Group4Case4());
                        continue;
                    }
                    // slucaj 5
                    else if (upEdgeCubes[EdgeCubePosition.Left].ColorBySide[CubeSide.Left] == rightSideColor &&
                        upEdgeCubes[EdgeCubePosition.Left].ColorBySide[CubeSide.Front] == currentSideColor)
                    {
                        solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                        solutionMoves.AddRange(this.Group4Case5());
                        continue;
                    }
                    // slucaj 6
                    else if (upEdgeCubes[EdgeCubePosition.Down].ColorBySide[CubeSide.Down] == rightSideColor &&
                        upEdgeCubes[EdgeCubePosition.Down].ColorBySide[CubeSide.Front] == currentSideColor)
                    {
                        solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                        solutionMoves.AddRange(this.Group4Case6());
                        continue;
                    }
                }
                else if (cornerCubes[CornerCubePosition.UpRight].ColorBySide[CubeSide.Right] == rightSideColor &&
                    cornerCubes[CornerCubePosition.UpRight].ColorBySide[CubeSide.Up] == currentSideColor)
                {
                    // slucaj 7
                    if (upEdgeCubes[EdgeCubePosition.Down].ColorBySide[CubeSide.Down] == rightSideColor &&
                        upEdgeCubes[EdgeCubePosition.Down].ColorBySide[CubeSide.Front] == currentSideColor)
                    {
                        solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                        solutionMoves.AddRange(this.Group4Case7());
                        continue;
                    }
                    // slucaj 8
                    else if (upEdgeCubes[EdgeCubePosition.Left].ColorBySide[CubeSide.Left] == rightSideColor &&
                        upEdgeCubes[EdgeCubePosition.Left].ColorBySide[CubeSide.Front] == currentSideColor)
                    {
                        solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                        solutionMoves.AddRange(this.Group4Case8());
                        continue;
                    }
                    // slucaj 9
                    else if (upEdgeCubes[EdgeCubePosition.Up].ColorBySide[CubeSide.Up] == rightSideColor &&
                        upEdgeCubes[EdgeCubePosition.Up].ColorBySide[CubeSide.Front] == currentSideColor)
                    {
                        solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                        solutionMoves.AddRange(this.Group4Case9());
                        continue;
                    }
                    // slucaj 10
                    else if (upEdgeCubes[EdgeCubePosition.Down].ColorBySide[CubeSide.Front] == rightSideColor &&
                        upEdgeCubes[EdgeCubePosition.Down].ColorBySide[CubeSide.Down] == currentSideColor)
                    {
                        solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                        solutionMoves.AddRange(this.Group4Case10());
                        continue;
                    }
                    // slucaj 11
                    else if (upEdgeCubes[EdgeCubePosition.Up].ColorBySide[CubeSide.Front] == rightSideColor &&
                        upEdgeCubes[EdgeCubePosition.Up].ColorBySide[CubeSide.Up] == currentSideColor)
                    {
                        solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                        solutionMoves.AddRange(this.Group4Case11());
                        continue;
                    }
                    // slucaj 12
                    else if (upEdgeCubes[EdgeCubePosition.Right].ColorBySide[CubeSide.Front] == rightSideColor &&
                        upEdgeCubes[EdgeCubePosition.Right].ColorBySide[CubeSide.Right] == currentSideColor)
                    {
                        solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                        solutionMoves.AddRange(this.Group4Case12());
                        continue;
                    }
                }

                if (upMovesCount > 0)
                    this.baRef.DoSolvingStateUpRotations(4 - upMovesCount);
            }
        }

        return solutionMoves;
    }

    private List<CubeMove> ThirdGroupCases()
    {
        var solutionMoves = new List<CubeMove>();

        foreach (KeyValuePair<CubeSide, CubeColor[]> sideState in this.baRef.CubeState)
        {
            CubeSide cubeSide = sideState.Key;

            if (cubeSide == CubeSide.Up || cubeSide == CubeSide.Down)
                continue;

            this.baRef.RotateToSide(cubeSide);
            Dictionary<CornerCubePosition, CornerCube> cornerCubes = this.baRef.CollectCurrentCornerCubes();
            Dictionary<CornerCubePosition, CornerCube> upCornerCubes = this.GetUpCornerCubes();
            Dictionary<EdgeCubePosition, EdgeCube> edgeCubes = this.baRef.CollectCurrentEdgeCubes();

            HashSet<CubeColor> targetEdgeColors = this.baRef.GetTargetEdgeCubeColors(EdgeCubePosition.Right);
            HashSet<CubeColor> targetCornerColors = this.baRef.GetTargetCornerCubeColors(CornerCubePosition.DownRight);

            // Ako je desna ivicna kockica na svojoj poziciji i ako je donja desna ugaona kockica u gornjem sloju
            if (this.baRef.CheckEdgeCubeColors(edgeCubes[EdgeCubePosition.Right], targetEdgeColors) &&
                upCornerCubes.Values.Any(cube => this.baRef.CheckCornerCubeColors(cube, targetCornerColors)))
            {
                int upMovesCount = 0;
                while (!this.baRef.CheckCornerCubeColors(cornerCubes[CornerCubePosition.UpRight], targetCornerColors))
                {
                    this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
                    upMovesCount++;

                    cornerCubes = this.baRef.CollectCurrentCornerCubes();
                }

                edgeCubes = this.baRef.CollectCurrentEdgeCubes();
                CubeColor currentSideColor = this.baRef.GetCurrentSideColor(CubeSide.Front);
                CubeColor rightSideColor = this.baRef.GetCurrentSideColor(CubeSide.Right);

                if (edgeCubes[EdgeCubePosition.Right].ColorBySide[CubeSide.Front] == currentSideColor)
                {
                    // slucaj 1
                    if (cornerCubes[CornerCubePosition.UpRight].ColorBySide[CubeSide.Right] == currentSideColor &&
                        cornerCubes[CornerCubePosition.UpRight].ColorBySide[CubeSide.Front] == rightSideColor)
                    {
                        solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                        solutionMoves.AddRange(this.Group3Case1());
                    }
                    // slucaj 2
                    else if (cornerCubes[CornerCubePosition.UpRight].ColorBySide[CubeSide.Front] == currentSideColor &&
                        cornerCubes[CornerCubePosition.UpRight].ColorBySide[CubeSide.Up] == rightSideColor)
                    {
                        solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                        solutionMoves.AddRange(this.Group3Case2());
                    }
                    // slucaj 3
                    else
                    {
                        solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                        solutionMoves.AddRange(this.Group3Case3());
                    }
                }
                else
                {
                    // slucaj 4
                    if (cornerCubes[CornerCubePosition.UpRight].ColorBySide[CubeSide.Right] == currentSideColor &&
                        cornerCubes[CornerCubePosition.UpRight].ColorBySide[CubeSide.Front] == rightSideColor)
                    {
                        solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                        solutionMoves.AddRange(this.Group3Case4());
                    }
                    // slucaj 5
                    else if (cornerCubes[CornerCubePosition.UpRight].ColorBySide[CubeSide.Front] == currentSideColor &&
                        cornerCubes[CornerCubePosition.UpRight].ColorBySide[CubeSide.Up] == rightSideColor)
                    {
                        solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                        solutionMoves.AddRange(this.Group3Case5());
                    }
                    // slucaj 6
                    else
                    {
                        solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                        solutionMoves.AddRange(this.Group3Case6());
                    }
                }
            }
        }

        return solutionMoves;
    }

    private List<CubeMove> SecondGroupCases()
    {
        var solutionMoves = new List<CubeMove>();

        foreach (KeyValuePair<CubeSide, CubeColor[]> sideState in this.baRef.CubeState)
        {
            CubeSide cubeSide = sideState.Key;

            if (cubeSide == CubeSide.Up || cubeSide == CubeSide.Down)
                continue;

            this.baRef.RotateToSide(cubeSide);
            Dictionary<CornerCubePosition, CornerCube> cornerCubes = this.baRef.CollectCurrentCornerCubes();
            Dictionary<EdgeCubePosition, EdgeCube> upEdgeCubes = this.GetUpEdgeCubes();

            HashSet<CubeColor> targetEdgeColors = this.baRef.GetTargetEdgeCubeColors(EdgeCubePosition.Right);
            HashSet<CubeColor> targetCornerColors = this.baRef.GetTargetCornerCubeColors(CornerCubePosition.DownRight);


            // Ako je donja desna ugaona kockica na svojoj poziciji i ako je desna ivicna kockica u gornjem sloju
            if (this.baRef.CheckCornerCubeColors(cornerCubes[CornerCubePosition.DownRight], targetCornerColors) &&
            upEdgeCubes.Values.Any(cube => this.baRef.CheckEdgeCubeColors(cube, targetEdgeColors)))
            {
                int upMovesCount = 0;

                for (int i = 0; i < 4; i++)
                {
                    CubeColor currentSideColor = this.baRef.GetCurrentSideColor(CubeSide.Front);
                    CubeColor rightSideColor = this.baRef.GetCurrentSideColor(CubeSide.Right);

                    if (upEdgeCubes[EdgeCubePosition.Right].ColorBySide[CubeSide.Right] == rightSideColor &&
                            upEdgeCubes[EdgeCubePosition.Right].ColorBySide[CubeSide.Front] == currentSideColor)
                    {
                        // slucaj 4
                        if (cornerCubes[CornerCubePosition.DownRight].ColorBySide[CubeSide.Front] == currentSideColor &&
                            cornerCubes[CornerCubePosition.DownRight].ColorBySide[CubeSide.Right] == rightSideColor)
                        {
                            solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                            solutionMoves.AddRange(this.Group2Case4());
                            break;
                        }
                        // slucaj 5
                        else if (cornerCubes[CornerCubePosition.DownRight].ColorBySide[CubeSide.Down] == currentSideColor &&
                            cornerCubes[CornerCubePosition.DownRight].ColorBySide[CubeSide.Front] == rightSideColor)
                        {
                            solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                            solutionMoves.AddRange(this.Group2Case5());
                            break;
                        }
                        // slucaj 3
                        else
                        {
                            solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                            solutionMoves.AddRange(this.Group2Case3());
                            break;
                        }
                    }
                    else if (upEdgeCubes[EdgeCubePosition.Down].ColorBySide[CubeSide.Front] == this.baRef.GetCurrentSideColor(CubeSide.Right) &&
                        upEdgeCubes[EdgeCubePosition.Down].ColorBySide[CubeSide.Down] == currentSideColor)
                    {
                        // slucaj 1
                        if (cornerCubes[CornerCubePosition.DownRight].ColorBySide[CubeSide.Front] == currentSideColor &&
                            cornerCubes[CornerCubePosition.DownRight].ColorBySide[CubeSide.Right] == rightSideColor)
                        {
                            solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                            solutionMoves.AddRange(this.Group2Case1());
                            break;
                        }
                        // slucaj 2
                        else if (cornerCubes[CornerCubePosition.DownRight].ColorBySide[CubeSide.Down] == currentSideColor &&
                            cornerCubes[CornerCubePosition.DownRight].ColorBySide[CubeSide.Front] == rightSideColor)
                        {
                            solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                            solutionMoves.AddRange(this.Group2Case2());
                            break;
                        }
                        // slucaj 6
                        else
                        {
                            solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                            solutionMoves.AddRange(this.Group2Case6());
                            break;
                        }
                    }

                    this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
                    upMovesCount++;

                    cornerCubes = this.baRef.CollectCurrentCornerCubes();
                    upEdgeCubes = this.GetUpEdgeCubes();
                }
            }
        }

        return solutionMoves;
    }

    private List<CubeMove> FirstGroupCases()
    {
        var solutionMoves = new List<CubeMove>();

        foreach (KeyValuePair<CubeSide, CubeColor[]> sideState in this.baRef.CubeState)
        {
            CubeSide cubeSide = sideState.Key;

            if (cubeSide == CubeSide.Up || cubeSide == CubeSide.Down)
                continue;

            this.baRef.RotateToSide(cubeSide);

            HashSet<CubeColor> targetEdgeColors = this.baRef.GetTargetEdgeCubeColors(EdgeCubePosition.Right);
            HashSet<CubeColor> targetCornerColors = this.baRef.GetTargetCornerCubeColors(CornerCubePosition.DownRight);

            Dictionary<CornerCubePosition, CornerCube> cornerCubes = this.baRef.CollectCurrentCornerCubes();
            Dictionary<EdgeCubePosition, EdgeCube> upEdgeCubes = this.GetUpEdgeCubes();
            Dictionary<CornerCubePosition, CornerCube> upCornerCubes = this.GetUpCornerCubes();

            // Ako su donja desna ugaona kockica i desna ivicna kockica u gornjem sloju
            if (upEdgeCubes.Values.Any(cube => this.baRef.CheckEdgeCubeColors(cube, targetEdgeColors)) &&
            upCornerCubes.Values.Any(cube => this.baRef.CheckCornerCubeColors(cube, targetCornerColors)))
            {
                int upMovesCount = 0;
                while (!this.baRef.CheckCornerCubeColors(cornerCubes[CornerCubePosition.UpRight], targetCornerColors))
                {
                    this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
                    upMovesCount++;

                    cornerCubes = this.baRef.CollectCurrentCornerCubes();
                }

                upEdgeCubes = this.GetUpEdgeCubes();
                CubeColor currentSideColor = this.baRef.GetCurrentSideColor(CubeSide.Front);

                if (cornerCubes[CornerCubePosition.UpRight].ColorBySide[CubeSide.Up] == this.baRef.GetCurrentSideColor(CubeSide.Right) &&
                    cornerCubes[CornerCubePosition.UpRight].ColorBySide[CubeSide.Front] == currentSideColor)
                {
                    // slucaj 1
                    if (this.baRef.CheckEdgeCubeColors(upEdgeCubes[EdgeCubePosition.Up], targetEdgeColors) &&
                        upEdgeCubes[EdgeCubePosition.Up].ColorBySide[CubeSide.Front] == currentSideColor)
                    {
                        solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                        solutionMoves.AddRange(this.Group1Case1());
                        continue;
                    }
                    // slucaj 2
                    else if (this.baRef.CheckEdgeCubeColors(upEdgeCubes[EdgeCubePosition.Down], targetEdgeColors) &&
                        upEdgeCubes[EdgeCubePosition.Down].ColorBySide[CubeSide.Down] == currentSideColor)
                    {
                        solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                        solutionMoves.AddRange(this.Group1Case2());
                        continue;
                    }
                }
                else if (cornerCubes[CornerCubePosition.UpRight].ColorBySide[CubeSide.Right] == this.baRef.GetCurrentSideColor(CubeSide.Right) &&
                    cornerCubes[CornerCubePosition.UpRight].ColorBySide[CubeSide.Up] == currentSideColor)
                {
                    // slucaj 3
                    if (this.baRef.CheckEdgeCubeColors(upEdgeCubes[EdgeCubePosition.Left], targetEdgeColors) &&
                        upEdgeCubes[EdgeCubePosition.Left].ColorBySide[CubeSide.Left] == currentSideColor)
                    {
                        solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                        solutionMoves.AddRange(this.Group1Case3());
                        continue;
                    }
                    // slucaj 4
                    else if (this.baRef.CheckEdgeCubeColors(upEdgeCubes[EdgeCubePosition.Right], targetEdgeColors) &&
                        upEdgeCubes[EdgeCubePosition.Right].ColorBySide[CubeSide.Front] == currentSideColor)
                    {
                        solutionMoves.AddRange(this.DoSolutionUpMoves(upMovesCount));
                        solutionMoves.AddRange(this.Group1Case4());
                        continue;
                    }
                }

                if (upMovesCount > 0)
                    this.baRef.DoSolvingStateUpRotations(4 - upMovesCount);
            }
        }

        return solutionMoves;
    }

    private List<CubeMove> DoSolutionUpMoves(int count)
    {
        var solutionMoves = new List<CubeMove>();

        while (count > 0)
        {
            solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up));

            count--;
        }

        return solutionMoves;
    }

    private Dictionary<EdgeCubePosition, EdgeCube> GetUpEdgeCubes()
    {
        this.baRef.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.DownLeft);

        Dictionary<EdgeCubePosition, EdgeCube> upEdgeCubes = this.baRef.CollectCurrentEdgeCubes();

        this.baRef.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.UpLeft);

        return upEdgeCubes;
    }

    private Dictionary<CornerCubePosition, CornerCube> GetUpCornerCubes()
    {
        this.baRef.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.DownLeft);

        Dictionary<CornerCubePosition, CornerCube> upCornerCubes = this.baRef.CollectCurrentCornerCubes();

        this.baRef.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.UpLeft);

        return upCornerCubes;
    }

    #region Group 1 moves

    private List<CubeMove> Group1Case1()
    {
        var solutionMoves = new List<CubeMove>();

        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Right));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Right, false));

        return solutionMoves;
    }

    private List<CubeMove> Group1Case2()
    {
        var solutionMoves = new List<CubeMove>();

        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Front, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Front));

        return solutionMoves;
    }

    private List<CubeMove> Group1Case3()
    {
        var solutionMoves = new List<CubeMove>();

        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Front, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Front));

        return solutionMoves;
    }

    private List<CubeMove> Group1Case4()
    {
        var solutionMoves = new List<CubeMove>();

        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Right));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Right, false));

        return solutionMoves;
    }

    #endregion

    #region Group 2 moves

    private List<CubeMove> Group2Case1()
    {
        var solutionMoves = new List<CubeMove>();

        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Right));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Right, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Front, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Front));

        return solutionMoves;
    }

    private List<CubeMove> Group2Case2()
    {
        var solutionMoves = new List<CubeMove>();

        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Front, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Front));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Front, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Front));

        return solutionMoves;
    }

    private List<CubeMove> Group2Case3()
    {
        var solutionMoves = new List<CubeMove>();

        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Right));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Right, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Right));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Right, false));

        return solutionMoves;
    }

    private List<CubeMove> Group2Case4()
    {
        var solutionMoves = new List<CubeMove>();

        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Front, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Front));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Right));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Right, false));

        return solutionMoves;
    }

    private List<CubeMove> Group2Case5()
    {
        var solutionMoves = new List<CubeMove>();

        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Right));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Right, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Right));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Right, false));

        return solutionMoves;
    }

    private List<CubeMove> Group2Case6()
    {
        var solutionMoves = new List<CubeMove>();

        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Front, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Front));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Front, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Front));

        return solutionMoves;
    }

    #endregion

    #region Group 3 moves

    private List<CubeMove> Group3Case1()
    {
        var solutionMoves = new List<CubeMove>();

        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Right));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Right, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Right));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Right, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Right));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Right, false));

        return solutionMoves;
    }

    private List<CubeMove> Group3Case2()
    {
        var solutionMoves = new List<CubeMove>();

        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Front, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Front));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Front, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, true, true));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up, true, true));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Front));

        return solutionMoves;
    }

    private List<CubeMove> Group3Case3()
    {
        var solutionMoves = new List<CubeMove>();

        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Right));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Right, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up, false));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Right));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, true, true));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Up, true, true));
        this.baRef.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right, false));
        solutionMoves.Add(this.baRef.CreateSolutionMove(CubeSide.Right, false));

        return solutionMoves;
    }

    private List<CubeMove> Group3Case4()
    {
        return this.StringToSolutionMoves("R U' R' U F' U F");
    }

    private List<CubeMove> Group3Case5()
    {
        return this.StringToSolutionMoves("U F' U' F U' R U R'");

    }

    private List<CubeMove> Group3Case6()
    {
        return this.StringToSolutionMoves("U' R U R' U F' U' F");
    }

    #endregion

    #region Group 4 moves
    private List<CubeMove> Group4Case1()
    {
        return this.StringToSolutionMoves("R U' R' U2 F' U' F");
    }

    private List<CubeMove> Group4Case2()
    {
        return this.StringToSolutionMoves("U F' U2 F U F' U2 F");
    }

    private List<CubeMove> Group4Case3()
    {
        return this.StringToSolutionMoves("U F' U' F U F' U2 F");
    }

    private List<CubeMove> Group4Case4()
    {
        return this.StringToSolutionMoves("U' R U' R' U R U R'");
    }

    private List<CubeMove> Group4Case5()
    {
        return this.StringToSolutionMoves("U' R U R' U R U R'");
    }

    private List<CubeMove> Group4Case6()
    {
        return this.StringToSolutionMoves("U F' U2 F U' R U R'");
    }

    private List<CubeMove> Group4Case7()
    {
        return this.StringToSolutionMoves("R B L U' L' B' R'");
    }

    private List<CubeMove> Group4Case8()
    {
        return this.StringToSolutionMoves("U' R U2 R' U' R U2 R'");
    }

    private List<CubeMove> Group4Case9()
    {
        return this.StringToSolutionMoves("U' R U R' U' R U2 R'");
    }

    private List<CubeMove> Group4Case10()
    {
        return this.StringToSolutionMoves("U F' U F U' F' U' F");
    }

    private List<CubeMove> Group4Case11()
    {
        return this.StringToSolutionMoves("U F' U' F U' F' U' F");
    }

    private List<CubeMove> Group4Case12()
    {
        return this.StringToSolutionMoves("U' R U2 R' U F' U' F");
    }

    #endregion

    #region Group 5 moves

    private List<CubeMove> Group5Case1()
    {
        return this.StringToSolutionMoves("R U R' U' U' R U R' U' R U R'");
    }

    private List<CubeMove> Group5Case2()
    {
        return this.StringToSolutionMoves("U2 R U R' U R U' R'");
    }

    private List<CubeMove> Group5Case3()
    {
        return this.StringToSolutionMoves("U R U2 R' U R U' R'");
    }

    private List<CubeMove> Group5Case4()
    {
        return this.StringToSolutionMoves("R U2 R' U' R U R'");
    }

    private List<CubeMove> Group5Case5()
    {
        return this.StringToSolutionMoves("U F' L' U L F R U R'");
    }

    private List<CubeMove> Group5Case6()
    {
        return this.StringToSolutionMoves("U2 F' U' F U' F' U F");
    }

    private List<CubeMove> Group5Case7()
    {
        return this.StringToSolutionMoves("U' F' U2 F U' F' U F");
    }

    private List<CubeMove> Group5Case8()
    {
        return this.StringToSolutionMoves("F' U2 F U F' U' F");
    }

    #endregion

    #region Group 6 moves

    private List<CubeMove> Group6Case1()
    {
        return this.StringToSolutionMoves("R2 U2 F R2 F' U2 R' U R'");
    }

    private List<CubeMove> Group6Case2()
    {
        return this.StringToSolutionMoves("R U' R' U R U2 R' U R U' R'");
    }

    private List<CubeMove> Group6Case3()
    {
        return this.StringToSolutionMoves("R U R' U' R U' R' U2 F' U' F");
    }

    private List<CubeMove> Group6Case4()
    {
        return this.StringToSolutionMoves("R U' R' U' R U R' U' R U2 R'");
    }

    private List<CubeMove> Group6Case5()
    {
        return this.StringToSolutionMoves("R F U R U' R' F' U' R'");
    }

    #endregion

    #region 2nd look oll moves

    private List<CubeMove> SunePattern()
    {
        return this.StringToSolutionMoves("R U R' U R U2 R'");
    }

    private List<CubeMove> AntiSunePattern()
    {
        return this.StringToSolutionMoves("L' U' L U' L' U2 L");
    }

    private List<CubeMove> PatternH()
    {
        return this.StringToSolutionMoves("F R U R' U' R U R' U' R U R' U' F'");
    }

    private List<CubeMove> PatternPi()
    {
        return this.StringToSolutionMoves("R' U2 R2 U R2 U R2 U2 R'");
    }

    private List<CubeMove> HeadlightsPattern()
    {
        return this.StringToSolutionMoves("R2 D R' U2 R D' R' U2 R'");
    }

    private List<CubeMove> PatternT()
    {
        return this.StringToSolutionMoves("L' B' R B L B' R' B");
    }

    private List<CubeMove> BowtiePattern()
    {
        return this.StringToSolutionMoves("F R B R' F' R B' R'");
    }

    #endregion

    #region 2nd look pll moves

    private List<CubeMove> HeadlightsBack()
    {
        this.baRef.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.DownLeft);

        List<CubeMove> solutionMoves = this.StringToSolutionMoves("R' D R' U2 R D' R' U2 R2 F'");

        this.baRef.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.UpLeft);

        return solutionMoves;
    }

    private List<CubeMove> NoHeadlights()
    {
        return this.StringToSolutionMoves("F R U' R' U' R U R' F' R U R' U' R' F R F'");
    }

    // Ua Perm je naziv sa linka
    private List<CubeMove> CounterClockwiseEdges()
    {
        return this.StringToSolutionMoves("B2 U R' U2 R L' B2 L U' B2");
    }

    // Ub Perm je naziv sa linka
    private List<CubeMove> ClockwiseEdges()
    {
        return this.StringToSolutionMoves("B2 U R L' B2 R' L U B2");
    }

    // Z Perm je naziv sa linka
    private List<CubeMove> AdjecentEdges()
    {
        return this.StringToSolutionMoves("B L' B' L R B' R L' B' L B R2");
    }

    // H Perm je naziv sa linka
    private List<CubeMove> OppositeEdges()
    {
        return this.StringToSolutionMoves("R L U2 R' L' F' B' U2 B F");
    }

    #endregion

    public List<CubeMove> StringToSolutionMoves(string movesString)
    {
        string[] movesArray = movesString.Split(' ');
        var solutionMoves = new List<CubeMove>();

        foreach (string moveString in movesArray)
        {
            CubeMove cubeMove = Helper.StringToCubeMove(moveString);

            if (cubeMove.CubeSide == CubeSide.NoSide)
                continue;

            this.baRef.SolvingCubeStateWrapper.RotateFace(cubeMove);
            solutionMoves.Add(this.baRef.CreateSolutionMove(cubeMove.CubeSide, cubeMove.Clockwise, cubeMove.DoubleMove));
        }

        return solutionMoves;
    }

    public bool AreFirstTwoLayersDone()
    {
        var edgePositionsToCheck = new List<EdgeCubePosition>() { EdgeCubePosition.Right, EdgeCubePosition.Left };

        this.baRef.RotateToSide(CubeSide.Back);

        if (!this.baRef.AreEdgeCubesOrientated(edgePositionsToCheck) ||
            !this.baRef.AreCornerCubesOrientated(CubeSide.Down))
            return false;

        this.baRef.RotateToSide(CubeSide.Front);

        if (!this.baRef.AreEdgeCubesOrientated(edgePositionsToCheck) ||
            !this.baRef.AreCornerCubesOrientated(CubeSide.Down))
            return false;

        return true;
    }

    public bool IsUpSideOneColored()
    {
        Dictionary<EdgeCubePosition, EdgeCube> upEdges = this.GetUpEdgeCubes();
        Dictionary<CornerCubePosition, CornerCube> upCorners = this.GetUpCornerCubes();
        CubeColor upSideColor = this.baRef.GetCurrentSideColor(CubeSide.Up);

        foreach (EdgeCube upEdge in upEdges.Values)
        {
            if (upEdge.ColorBySide[CubeSide.Front] != upSideColor)
                return false;
        }

        foreach (CornerCube upCorner in upCorners.Values)
        {
            if (upCorner.ColorBySide[CubeSide.Front] != upSideColor)
                return false;
        }

        return true;
    }
}