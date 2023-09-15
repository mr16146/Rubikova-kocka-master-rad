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
using System.Diagnostics;

public class BeginnerAlgorithm : MonoBehaviour
{
    public StateReader stateReader;
    public CubeRotation cubeRotation;
    public CubeSolver cubeSolver;

    #region Properties

    public Dictionary<CubeSide, CubeColor[]> CubeState
    {
        get { return this.stateReader.SolvingCubeStateWrapper.CubeStateData.CubeState; }
        set { this.stateReader.SolvingCubeStateWrapper.CubeStateData.CubeState = value; }
    }

    public CubeStateData CubeStateData
    {
        get { return this.stateReader.SolvingCubeStateWrapper.CubeStateData; }
        set { this.stateReader.SolvingCubeStateWrapper.CubeStateData = value; }
    }

    public CubeStateWrapper SolvingCubeStateWrapper
    {
        get { return this.stateReader.SolvingCubeStateWrapper; }
    }

    #endregion

    // Start is called before the first frame update
    void Start()
    {
        this.stateReader = GetComponent<StateReader>();
        this.cubeRotation = GetComponent<CubeRotation>();
        this.cubeSolver = GetComponent<CubeSolver>();
    }

    // Update is called once per frame
    void Update()
    {
    }

    public IEnumerator SolveBeginner()
    {
        if (cubeSolver.IsSolving || this.cubeSolver.IsCubeSolved())
            yield break;

        this.cubeSolver.IsSolving = true;
        Stopwatch stopwatch = Stopwatch.StartNew();

        this.stateReader.SolvingCubeStateWrapper.CubeStateData = new CubeStateData(this.stateReader.CubeStateWrapper.CubeStateData);

        var solutionMovesByStepName = new Dictionary<string, CubeMove[]>();

        // Pravi beli krst na zutoj strani kocke
        solutionMovesByStepName.Add("White Cross ", Helper.RemoveRedundantMoves(this.WhiteCrossOnYellowSide()).ToArray());
        // Pravi beli krst na beloj strani kocke
        solutionMovesByStepName.Add("White Cross", Helper.RemoveRedundantMoves(this.WhiteCross()).ToArray());
        // Ide redom kroz svaku stranu i pozicionira donje ivicne kocke
        solutionMovesByStepName.Add("First Layer", Helper.RemoveRedundantMoves(this.FirstLayer()).ToArray());
        // Nalazi ivicne kocke sa gornjeg sloja koje nemaju zutu boju i spusta ih na srednji sloj
        solutionMovesByStepName.Add("Second Layer", Helper.RemoveRedundantMoves(this.SecondLayer()).ToArray());
        // Pravi zuti krst na gornjem sloju
        solutionMovesByStepName.Add("Yellow Cross", Helper.RemoveRedundantMoves(this.YellowCross()).ToArray());
        // Pozicionira gornje ivicne kockice na preostalim stranama(sem gornje i donje), tj. pravi krsteve na njima
        solutionMovesByStepName.Add("Side Crosses", Helper.RemoveRedundantMoves(this.SideCrosses()).ToArray());
        // Pozicionira ugaone kockice na gornjem sloju
        solutionMovesByStepName.Add("Positioning Top Corners", Helper.RemoveRedundantMoves(this.RepositionTopCorners()).ToArray());
        // Orijentise ugaone kockice na gornjem sloju
        solutionMovesByStepName.Add("Orientating Top Corners", Helper.RemoveRedundantMoves(this.ReorientTopCorners()).ToArray());

        stopwatch.Stop();

        this.cubeSolver.CurrentSolvingTime = stopwatch.ElapsedMilliseconds;
        this.cubeSolver.SaveCurrentResults(solutionMovesByStepName.Values.SelectMany(el => el).ToList());

        yield return this.cubeRotation.ExecuteMovesWithStepsNames(solutionMovesByStepName, "Beginner Algorithm");
    }

    public List<CubeMove> ReorientTopCorners()
    {
        var solutionMoves = new List<CubeMove>();

        // Pronalazi prvu stranicu koja nema dobro orijentisanu gornju desnu ugaonu kockicu, poziva niz poteza koji orijentise tu kockicu,
        // zatim rotira gornji sloj i orijentise svaku naredenu lose orijentisanu gornju desnu ugaonu kockicu, na kraju rotira gornji sloj dok se ne dobija slozena Rubikova kocka
        foreach (KeyValuePair<CubeSide, CubeColor[]> sideState in this.CubeState)
        {
            CubeSide cubeSide = sideState.Key;

            if (cubeSide == CubeSide.Up || cubeSide == CubeSide.Down)
                continue;

            this.RotateToSide(cubeSide);
            Dictionary<CornerCubePosition, CornerCube> cornerCubes = this.CollectCurrentCornerCubes();

            // Pronalazi prvu stranu koja nema dobro orijentisanu gornju desnu ugaonu kockicu
            if (cornerCubes[CornerCubePosition.UpRight].ColorBySide[CubeSide.Up] != CubeColor.Yellow)
            {
                // Orijentise gornju desnu ugaonu kockicu na trenutnoj strani i proverava sve druge strane
                for (int i = 0; i < 4; i++)
                {
                    while (cornerCubes[CornerCubePosition.UpRight].ColorBySide[CubeSide.Up] != CubeColor.Yellow)
                    {
                        solutionMoves.AddRange(this.RotateUpRightCorner());
                        cornerCubes = this.CollectCurrentCornerCubes();
                    }

                    this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
                    solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up, false));

                    cornerCubes = this.CollectCurrentCornerCubes();
                }

                break;
            }

            this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.Right);
        }

        return solutionMoves;
    }

    public List<CubeMove> RotateUpRightCorner()
    {
        var solutionMoves = new List<CubeMove>();

        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right, false));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Right, false));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Down));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Down));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Right));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Down, false));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Down, false));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right, false));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Right, false));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Down));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Down));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Right));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Down, false));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Down, false));


        return solutionMoves;
    }

    public List<CubeMove> RepositionTopCorners()
    {
        var solutionMoves = new List<CubeMove>();

        // Prolazi kroz sve strane i pronalazi stranu kojoj je gornja desna ugaona kocka dobro pozicionirana, radi niz poteza za rotiranje svih ugaonih kockica sem gornje desne,
        // posle jednog ili 2 puta rotiranja ugaonih kockica sve ugaone kockice ce biti dobro pozicionirane,
        // ukoliko nijedna ugaona kockica nije dobro pozicionirana rotira ugaone kockice i ponovo poziva ovaj korak
        foreach (KeyValuePair<CubeSide, CubeColor[]> sideState in this.CubeState)
        {
            CubeSide cubeSide = sideState.Key;

            if (cubeSide == CubeSide.Up || cubeSide == CubeSide.Down)
                continue;

            this.RotateToSide(cubeSide);
            Dictionary<CornerCubePosition, CornerCube> cornerCubes = this.CollectCurrentCornerCubes();

            if (this.CheckCornerCubeColors(cornerCubes[CornerCubePosition.UpRight], this.GetTargetCornerCubeColors(CornerCubePosition.UpRight)))
            {
                if (this.CheckCornerCubeColors(cornerCubes[CornerCubePosition.UpLeft], this.GetTargetCornerCubeColors(CornerCubePosition.UpLeft)))
                    return solutionMoves;

                solutionMoves.AddRange(this.RotateTopCorners());

                // Ukoliko nisu sve ugaone kockice gornjeg sloja pozicionirane radi se jos jednom njihova rotacija
                cornerCubes = this.CollectCurrentCornerCubes();

                if (!this.CheckCornerCubeColors(cornerCubes[CornerCubePosition.UpLeft], this.GetTargetCornerCubeColors(CornerCubePosition.UpLeft)))
                    solutionMoves.AddRange(this.RotateTopCorners());

                return solutionMoves;
            }
        }

        // Ukoliko nijedna ugaona kockica nije dobro pozicionirana, rotiraju se ugaone kockice i ponovo se poziva ovaj korak
        solutionMoves.AddRange(this.RotateTopCorners());
        solutionMoves.AddRange(this.RepositionTopCorners());

        return solutionMoves;
    }

    public List<CubeMove> RotateTopCorners()
    {
        var solutionMoves = new List<CubeMove>();

        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Right));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up, false));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Left, false));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Left, false));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right, false));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Right, false));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up, false));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Left));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Left));

        return solutionMoves;
    }

    public List<CubeMove> SideCrosses()
    {
        var solutionMoves = new List<CubeMove>();
        bool foundSolvingPosition = false;
        bool alreadySolved = false;
        bool firstIteration = true;

        while (!foundSolvingPosition)
        {
            // Prolazi kroz stranice i trazi 2 stranice za redom koje imaju vec podesen krst(trenutnu i desnu),
            // ako ih ne pronadje odradi niz poteza za rotaciju ivica gornjeg sloja i ponovi postupak koji je tada zagarantovano uspesan
            foreach (KeyValuePair<CubeSide, CubeColor[]> sideState in this.CubeState)
            {
                CubeSide cubeSide = sideState.Key;

                if (cubeSide == CubeSide.Up || cubeSide == CubeSide.Down)
                    continue;

                this.RotateToSide(cubeSide);

                int upRotations = 0;

                // Rotira gornji sloj dok se ne napravi krst na trenutnoj stranici
                while (upRotations < 4)
                {
                    Dictionary<EdgeCubePosition, EdgeCube> edgeCubes = this.CollectCurrentEdgeCubes();

                    // Na trenutnoj strani postoji krst
                    if (edgeCubes[EdgeCubePosition.Up].ColorBySide[CubeSide.Front] == this.GetCurrentSideColor(CubeSide.Front))
                    {
                        this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.Left);

                        Dictionary<EdgeCubePosition, EdgeCube> rightSideEdgeCubes = this.CollectCurrentEdgeCubes();

                        // Ako i na desnoj strani postoji krst onda je ovo pozicija sa koje se moze odraditi niz poteza za rotaciju ivica gornjeg sloja,
                        // zagarantovano je da ce ivice biti dobro pozicionirane
                        if (rightSideEdgeCubes[EdgeCubePosition.Up].ColorBySide[CubeSide.Front] == this.GetCurrentSideColor(CubeSide.Front))
                            foundSolvingPosition = true;

                        this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.Right);

                        if (foundSolvingPosition)
                        {
                            // Slucaj da vec postoje krstovi na stranama, proverava se pri prvoj iteraciji kroz strane kocke
                            if (firstIteration)
                            {
                                firstIteration = false;
                                this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.Right);
                                Dictionary<EdgeCubePosition, EdgeCube> leftSideEdgeCubes = this.CollectCurrentEdgeCubes();

                                if (leftSideEdgeCubes[EdgeCubePosition.Up].ColorBySide[CubeSide.Front] == this.GetCurrentSideColor(CubeSide.Front))
                                    alreadySolved = true;

                                this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.Left);
                            }

                            // Ukoliko je ovo prava pozicija potrebno je dodati prethodne rotacije gornjeg sloja u niz poteza resenja algoritma
                            while (upRotations > 0)
                            {
                                solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up));
                                upRotations--;
                            }

                            if (!alreadySolved)
                            {
                                this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.Right);
                                solutionMoves.AddRange(this.SwapTopEdges());
                            }
                        }
                        else if (upRotations > 0)
                            this.DoSolvingStateUpRotations(4 - upRotations);


                        break;
                    }
                    else
                    {
                        // Rotira se gornji sloj dok se ne napravi krst na trenutnoj strani kocke
                        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
                        upRotations++;
                    }
                }

                if (foundSolvingPosition)
                    break;
            }

            // Slucaj da ne postoje krstevi na svim stranicama i da ne postoje krstevi na 2 stranice za redom, na ovaj nacin se dobijaju krstevi na 2 stranice za redom
            if (!foundSolvingPosition)
                solutionMoves.AddRange(this.SwapTopEdges());
        }

        return solutionMoves;
    }

    public void DoSolvingStateUpRotations(int count)
    {
        while (count > 0)
        {
            this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
            count--;
        }
    }

    public List<CubeMove> SwapTopEdges()
    {
        var solutionMoves = new List<CubeMove>();

        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Right));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right, false));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Right, false));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Right));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, true, true));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up, true, true));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right, false));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Right, false));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up));

        return solutionMoves;
    }

    public List<CubeMove> YellowCross()
    {
        var solutionMoves = new List<CubeMove>();

        this.RotateToSide(CubeSide.Front);
        this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.DownLeft);
        Dictionary<EdgeCubePosition, EdgeCube> upEdgeCubes = this.CollectCurrentEdgeCubes();
        bool done = true;

        foreach (EdgeCube upEdgeCube in upEdgeCubes.Values)
        {
            if (upEdgeCube.ColorBySide[CubeSide.Front] != CubeColor.Yellow)
            {
                done = false;
                break;
            }
        }

        this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.UpLeft);

        if (done)
            return solutionMoves;

        foreach (KeyValuePair<CubeSide, CubeColor[]> sideState in this.CubeState)
        {
            CubeSide cubeSide = sideState.Key;

            if (cubeSide == CubeSide.Up || cubeSide == CubeSide.Down)
                continue;

            this.RotateToSide(cubeSide);
            this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.DownLeft);

            Dictionary<EdgeCubePosition, EdgeCube> edgeCubes = this.CollectCurrentEdgeCubes();

            if (edgeCubes[EdgeCubePosition.Right].ColorBySide[CubeSide.Front] == CubeColor.Yellow &&
                edgeCubes[EdgeCubePosition.Left].ColorBySide[CubeSide.Front] == CubeColor.Yellow)
            {
                this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.UpLeft);
                solutionMoves.AddRange(this.SolvePatternMinus());

                return solutionMoves;
            }
            else if (edgeCubes[EdgeCubePosition.Up].ColorBySide[CubeSide.Front] == CubeColor.Yellow &&
                edgeCubes[EdgeCubePosition.Left].ColorBySide[CubeSide.Front] == CubeColor.Yellow)
            {
                this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.UpLeft);
                solutionMoves.AddRange(this.SolvePatternL());

                return solutionMoves;
            }

            this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.UpLeft);
        }

        solutionMoves.AddRange(this.SolvePatternMinus());
        solutionMoves.AddRange(this.YellowCross());

        return solutionMoves;
    }

    public List<CubeMove> SolvePatternL()
    {
        var solutionMoves = new List<CubeMove>();

        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Front));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Right));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up, false));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right, false));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Right, false));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front, false));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Front, false));

        return solutionMoves;
    }

    public List<CubeMove> SolvePatternMinus()
    {
        var solutionMoves = new List<CubeMove>();

        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Front));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Right));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right, false));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Right, false));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up, false));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front, false));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Front, false));

        return solutionMoves;
    }

    public List<CubeMove> SecondLayer()
    {
        var solutionMoves = new List<CubeMove>();
        bool done = false;
        var edgePositionsToCheck = new List<EdgeCubePosition>() { EdgeCubePosition.Right, EdgeCubePosition.Left };

        while (!done)
        {
            done = true;

            foreach (KeyValuePair<CubeSide, CubeColor[]> sideState in this.CubeState)
            {
                CubeSide cubeSide = sideState.Key;

                if (cubeSide == CubeSide.Up || cubeSide == CubeSide.Down)
                    continue;

                this.RotateToSide(cubeSide);

                if (!this.AreEdgeCubesOrientated(edgePositionsToCheck))
                {
                    done = false;
                    break;
                }
            }

            if (!done)
            {
                bool nonYellowEdgeCubesOnTopLayer = false;
                CubeSide targetSide = CubeSide.NoSide;

                this.RotateToSide(CubeSide.Front);
                this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.DownLeft);
                Dictionary<EdgeCubePosition, EdgeCube> upEdgeCubes = this.CollectCurrentEdgeCubes();

                foreach (EdgeCube upEdgeCube in upEdgeCubes.Values)
                {
                    if (upEdgeCube.ColorBySide.Values.Contains(CubeColor.Yellow))
                        continue;

                    CubeColor targetColor = upEdgeCube.GetNonFrontColor();

                    targetSide = this.CubeStateData.SideByColor[targetColor];
                    nonYellowEdgeCubesOnTopLayer = true;
                    break;
                }

                this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.UpLeft);

                // U slucaju da drugi sloj nije gotov, a da u gornjem sloju ne postoji nijedna ivicna kockica bez zutog polja, potrebno je jednu nepozicioniranu kockicu iz drugog sloja prebaciti u gornji sloj
                if (!nonYellowEdgeCubesOnTopLayer)
                {
                    foreach (KeyValuePair<CubeSide, CubeColor[]> sideState in this.CubeState)
                    {
                        CubeSide cubeSide = sideState.Key;

                        if (cubeSide == CubeSide.Up || cubeSide == CubeSide.Down)
                            continue;

                        this.RotateToSide(cubeSide);
                        Dictionary<EdgeCubePosition, EdgeCube> edgeCubes = this.CollectCurrentEdgeCubes();

                        if (!this.AreEdgeCubesOrientated(edgePositionsToCheck, edgeCubes))
                        {
                            if (!this.IsCurrentEdgeCubeOrientated(edgeCubes[EdgeCubePosition.Right]))
                            {
                                solutionMoves.AddRange(this.MoveUpEdgeToRight());
                            }
                            else
                            {
                                solutionMoves.AddRange(this.MoveUpEdgeToLeft());
                            }

                            break;
                        }
                    }

                    continue;
                }

                this.RotateToSide(targetSide);

                HashSet<CubeColor> targetRightCornerColors = this.GetTargetEdgeCubeColors(EdgeCubePosition.Right);
                HashSet<CubeColor> targetLeftCornerColors = this.GetTargetEdgeCubeColors(EdgeCubePosition.Left);
                Dictionary<EdgeCubePosition, EdgeCube> targetEdgeCubes = this.CollectCurrentEdgeCubes();

                if (!this.AreEdgeCubesOrientated(edgePositionsToCheck, targetEdgeCubes))
                {
                    bool doneMove = false;

                    while (!doneMove)
                    {
                        if (targetEdgeCubes[EdgeCubePosition.Up].ColorBySide[CubeSide.Front] == this.GetCurrentSideColor(CubeSide.Front))
                        {
                            solutionMoves.AddRange(this.MoveUpEdgeToSide());
                            doneMove = true;
                        }
                        else
                        {
                            this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
                            solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up));
                            targetEdgeCubes = this.CollectCurrentEdgeCubes();
                        }
                    }
                }
            }
        }

        return solutionMoves;
    }

    public List<CubeMove> MoveUpEdgeToSide()
    {
        Dictionary<EdgeCubePosition, EdgeCube> edgeCubes = this.CollectCurrentEdgeCubes();

        CubeColor topEdgeNonFrontColor = edgeCubes[EdgeCubePosition.Up].GetNonFrontColor();

        if (topEdgeNonFrontColor == this.GetCurrentSideColor(CubeSide.Right))
        {
            return this.MoveUpEdgeToRight();
        }
        else
        {
            return this.MoveUpEdgeToLeft();
        }
    }

    public List<CubeMove> MoveUpEdgeToRight()
    {
        var solutionMoves = new List<CubeMove>();

        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Right));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up, false));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right, false));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Right, false));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up, false));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front, false));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Front, false));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Front));

        return solutionMoves;
    }

    public List<CubeMove> MoveUpEdgeToLeft()
    {
        var solutionMoves = new List<CubeMove>();

        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up, false));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Left, false));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Left, false));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Left));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Left));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Front));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up, false));
        this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front, false));
        solutionMoves.Add(this.CreateSolutionMove(CubeSide.Front, false));

        return solutionMoves;
    }

    public HashSet<CubeColor> GetTargetEdgeCubeColors(EdgeCubePosition edgeCubePosition)
    {
        var targetColors = new HashSet<CubeColor>();

        switch (edgeCubePosition)
        {
            case EdgeCubePosition.Up:
                targetColors.Add(this.GetCurrentSideColor(CubeSide.Up));
                targetColors.Add(this.GetCurrentSideColor(CubeSide.Front));
                break;
            case EdgeCubePosition.Down:
                targetColors.Add(this.GetCurrentSideColor(CubeSide.Down));
                targetColors.Add(this.GetCurrentSideColor(CubeSide.Front));
                break;
            case EdgeCubePosition.Right:
                targetColors.Add(this.GetCurrentSideColor(CubeSide.Right));
                targetColors.Add(this.GetCurrentSideColor(CubeSide.Front));
                break;
            case EdgeCubePosition.Left:
                targetColors.Add(this.GetCurrentSideColor(CubeSide.Left));
                targetColors.Add(this.GetCurrentSideColor(CubeSide.Front));
                break;
        }

        return targetColors;
    }

    // Proverava da li ivicna kockica sadrzi sve boje iz targetColors
    public bool CheckEdgeCubeColors(EdgeCube edgeCube, HashSet<CubeColor> targetColors)
    {
        foreach (CubeColor edgeCubeColor in edgeCube.ColorBySide.Values)
            if (!targetColors.Contains(edgeCubeColor))
                return false;

        return true;
    }

    public bool AreEdgeCubesOrientated(List<EdgeCubePosition> edgeCubePositions = null, Dictionary<EdgeCubePosition, EdgeCube> edgeCubes = null)
    {
        if (edgeCubes == null)
            edgeCubes = this.CollectCurrentEdgeCubes();

        if (edgeCubePositions == null)
            edgeCubePositions = new List<EdgeCubePosition>() { EdgeCubePosition.Right, EdgeCubePosition.Left, EdgeCubePosition.Up, EdgeCubePosition.Down };

        foreach (EdgeCubePosition edgeCubePosition in edgeCubePositions)
        {
            if (!this.IsCurrentEdgeCubeOrientated(edgeCubes[edgeCubePosition]))
                return false;
        }

        return true;
    }

    public bool IsCurrentEdgeCubeOrientated(EdgeCube edgeCube)
    {
        switch (edgeCube.Position)
        {
            case EdgeCubePosition.Up:
                return edgeCube.ColorBySide[CubeSide.Front] == this.GetCurrentSideColor(CubeSide.Front) &&
                    edgeCube.ColorBySide[CubeSide.Up] == this.GetCurrentSideColor(CubeSide.Up);
            case EdgeCubePosition.Down:
                return edgeCube.ColorBySide[CubeSide.Front] == this.GetCurrentSideColor(CubeSide.Front) &&
                   edgeCube.ColorBySide[CubeSide.Down] == this.GetCurrentSideColor(CubeSide.Down);
            case EdgeCubePosition.Right:
                return edgeCube.ColorBySide[CubeSide.Front] == this.GetCurrentSideColor(CubeSide.Front) &&
                   edgeCube.ColorBySide[CubeSide.Right] == this.GetCurrentSideColor(CubeSide.Right);
            case EdgeCubePosition.Left:
                return edgeCube.ColorBySide[CubeSide.Front] == this.GetCurrentSideColor(CubeSide.Front) &&
                   edgeCube.ColorBySide[CubeSide.Left] == this.GetCurrentSideColor(CubeSide.Left);
        }

        return false;
    }

    public Dictionary<EdgeCubePosition, EdgeCube> CollectCurrentEdgeCubes()
    {
        return new Dictionary<EdgeCubePosition, EdgeCube>
        {
            { EdgeCubePosition.Up, this.GetCurrentEdgeCube(EdgeCubePosition.Up) },
            { EdgeCubePosition.Down, this.GetCurrentEdgeCube(EdgeCubePosition.Down) },
            { EdgeCubePosition.Right, this.GetCurrentEdgeCube(EdgeCubePosition.Right) },
            { EdgeCubePosition.Left, this.GetCurrentEdgeCube(EdgeCubePosition.Left) }
        };
    }

    public EdgeCube GetCurrentEdgeCube(EdgeCubePosition edgeCubePosition)
    {
        var edgeCube = new EdgeCube(edgeCubePosition);

        switch (edgeCubePosition)
        {
            case EdgeCubePosition.Up:
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Left));
                edgeCube.ColorBySide.Add(CubeSide.Up, this.GetCurrentCubieColor(CubeSide.Front, 3));
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Left, false));
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));

                edgeCube.ColorBySide.Add(CubeSide.Front, this.GetCurrentCubieColor(CubeSide.Front, 1));
                break;
            case EdgeCubePosition.Down:
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Down, false));
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Left, false));
                edgeCube.ColorBySide.Add(CubeSide.Down, this.GetCurrentCubieColor(CubeSide.Front, 3));
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Left));
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Down));

                edgeCube.ColorBySide.Add(CubeSide.Front, this.GetCurrentCubieColor(CubeSide.Front, 7));
                break;
            case EdgeCubePosition.Right:
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right));
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
                edgeCube.ColorBySide.Add(CubeSide.Right, this.GetCurrentCubieColor(CubeSide.Front, 1));
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right, false));

                edgeCube.ColorBySide.Add(CubeSide.Front, this.GetCurrentCubieColor(CubeSide.Front, 5));
                break;
            case EdgeCubePosition.Left:
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Left, false));
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
                edgeCube.ColorBySide.Add(CubeSide.Left, this.GetCurrentCubieColor(CubeSide.Front, 1));
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Left));

                edgeCube.ColorBySide.Add(CubeSide.Front, this.GetCurrentCubieColor(CubeSide.Front, 3));
                break;
        }

        return edgeCube;
    }

    public List<CubeMove> FirstLayer()
    {
        var solutionMoves = new List<CubeMove>();
        bool done = false;

        while (!done)
        {
            done = true;

            // Provera da li su sve donje ivicne kocke pozicionirane, ako jesu prekida metodu
            foreach (KeyValuePair<CubeSide, CubeColor[]> sideState in this.CubeState)
            {
                CubeSide cubeSide = sideState.Key;

                if (cubeSide == CubeSide.Up || cubeSide == CubeSide.Down)
                    continue;

                this.RotateToSide(cubeSide);

                if (!this.AreCornerCubesOrientated(CubeSide.Down))
                {
                    done = false;
                    break;
                }
            }

            if (!done)
            {
                bool whiteNonYellowCornerCubesOnTopLayer = false;
                CubeSide targetSide = CubeSide.NoSide;

                this.RotateToSide(CubeSide.Front);
                this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.DownLeft);
                Dictionary<CornerCubePosition, CornerCube> upCornerCubes = this.CollectCurrentCornerCubes();

                // Trazi ivicnu kocku koja ima boju donje strane(belu), a nema boju gornje strane(zutu)
                foreach (CornerCube upCornerCube in upCornerCubes.Values)
                {
                    if (upCornerCube.ColorBySide.Values.Contains(CubeColor.Yellow))
                        continue;

                    //targetSide = upCornerCube.GetSideByColor(CubeColor.White);

                    if (upCornerCube.GetSideByColor(CubeColor.White) != CubeSide.NoSide)
                    {
                        CubeColor targetColor = upCornerCube.ColorBySide.Values.FirstOrDefault(color => color != CubeColor.White);

                        targetSide = this.CubeStateData.SideByColor[targetColor];
                        whiteNonYellowCornerCubesOnTopLayer = true;
                        break;
                    }
                }

                this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.UpLeft);

                // U slucaju da prvi sloj nije gotov, a da u gornjem sloju ne postoji nijedna kockica sa belim poljem, potrebno je jednu nepozicioniranu kockicu iz prvog sloja prebaciti u gornji sloj
                if (!whiteNonYellowCornerCubesOnTopLayer)
                {
                    foreach (KeyValuePair<CubeSide, CubeColor[]> sideState in this.CubeState)
                    {
                        CubeSide cubeSide = sideState.Key;

                        if (cubeSide == CubeSide.Up || cubeSide == CubeSide.Down)
                            continue;

                        this.RotateToSide(cubeSide);
                        Dictionary<CornerCubePosition, CornerCube> cornerCubes = this.CollectCurrentCornerCubes();

                        if (!this.AreCornerCubesOrientated(CubeSide.Down, cornerCubes))
                        {
                            if (!this.IsCurrentCornerCubeOrientated(cornerCubes[CornerCubePosition.DownRight]))
                            {
                                solutionMoves.AddRange(this.MoveUpRightCornerDown());
                            }
                            else
                            {
                                this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.Right);
                                solutionMoves.AddRange(this.MoveUpRightCornerDown());
                                this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.Left);
                            }

                            break;
                        }
                    }

                    continue;
                }

                // Odlazi na stranu kocke koja ima boju kao ivicna kockica sa gornjeg sloja i smesta je na odgovarajuce mesto
                this.RotateToSide(targetSide);

                HashSet<CubeColor> targetRightCornerColors = this.GetTargetCornerCubeColors(CornerCubePosition.DownRight);
                HashSet<CubeColor> targetLeftCornerColors = this.GetTargetCornerCubeColors(CornerCubePosition.DownLeft);
                Dictionary<CornerCubePosition, CornerCube> targetCornerCubes = this.CollectCurrentCornerCubes();

                if (!this.AreCornerCubesOrientated(CubeSide.Down, targetCornerCubes))
                {
                    bool doneMove = false;

                    // Poredi boje ivicne kocke sa gornjeg sloja sa zeljenim bojama leve i desne ivicne kockice na trenutnoj strani. Ukoliko se boje poklapaju menjaju se mesta ivicnim kockicama
                    while (!doneMove)
                    {
                        if (this.CheckCornerCubeColors(targetCornerCubes[CornerCubePosition.UpRight], targetRightCornerColors))
                        {
                            solutionMoves.AddRange(this.MoveUpRightCornerDown());
                            doneMove = true;
                        }
                        else if (this.CheckCornerCubeColors(targetCornerCubes[CornerCubePosition.UpLeft], targetRightCornerColors))
                        {
                            this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
                            solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up, false));

                            solutionMoves.AddRange(this.MoveUpRightCornerDown());
                            doneMove = true;
                        }
                        else if (this.CheckCornerCubeColors(targetCornerCubes[CornerCubePosition.UpLeft], targetLeftCornerColors))
                        {
                            this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.Right);
                            solutionMoves.AddRange(this.MoveUpRightCornerDown());
                            this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.Left);
                            doneMove = true;
                        }
                        else if (this.CheckCornerCubeColors(targetCornerCubes[CornerCubePosition.UpRight], targetLeftCornerColors))
                        {
                            this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
                            solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up));

                            this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.Right);
                            solutionMoves.AddRange(this.MoveUpRightCornerDown());
                            this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.Left);
                            doneMove = true;
                        }
                        else
                        {
                            this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
                            solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up));
                            targetCornerCubes = this.CollectCurrentCornerCubes();
                        }
                    }
                }
            }
        }

        return solutionMoves;
    }

    public List<CubeMove> MoveUpRightCornerDown()
    {
        var solutionMoves = new List<CubeMove>();
        Dictionary<CornerCubePosition, CornerCube> cornerCubes = this.CollectCurrentCornerCubes();

        CubeSide whiteCubeSide = cornerCubes[CornerCubePosition.UpRight].GetSideByColor(CubeColor.White);

        switch (whiteCubeSide)
        {
            case CubeSide.Right:
            case CubeSide.NoSide:
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right));
                solutionMoves.Add(this.CreateSolutionMove(CubeSide.Right));
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
                solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up));
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right, false));
                solutionMoves.Add(this.CreateSolutionMove(CubeSide.Right, false));
                break;
            case CubeSide.Front:
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front, false));
                solutionMoves.Add(this.CreateSolutionMove(CubeSide.Front, false));
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
                solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up, false));
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front));
                solutionMoves.Add(this.CreateSolutionMove(CubeSide.Front));
                break;
            case CubeSide.Up:
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right));
                solutionMoves.Add(this.CreateSolutionMove(CubeSide.Right));
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
                solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up));
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
                solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up));
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right, false));
                solutionMoves.Add(this.CreateSolutionMove(CubeSide.Right, false));
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
                solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up, false));
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right));
                solutionMoves.Add(this.CreateSolutionMove(CubeSide.Right));
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
                solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up));
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right, false));
                solutionMoves.Add(this.CreateSolutionMove(CubeSide.Right, false));
                break;
        }

        return solutionMoves;
    }

    public HashSet<CubeColor> GetTargetCornerCubeColors(CornerCubePosition cornerCubePosition)
    {
        var targetColors = new HashSet<CubeColor>();

        switch (cornerCubePosition)
        {
            case CornerCubePosition.UpRight:
                targetColors.Add(this.GetCurrentSideColor(CubeSide.Up));
                targetColors.Add(this.GetCurrentSideColor(CubeSide.Right));
                targetColors.Add(this.GetCurrentSideColor(CubeSide.Front));
                break;
            case CornerCubePosition.UpLeft:
                targetColors.Add(this.GetCurrentSideColor(CubeSide.Up));
                targetColors.Add(this.GetCurrentSideColor(CubeSide.Left));
                targetColors.Add(this.GetCurrentSideColor(CubeSide.Front));
                break;
            case CornerCubePosition.DownRight:
                targetColors.Add(this.GetCurrentSideColor(CubeSide.Down));
                targetColors.Add(this.GetCurrentSideColor(CubeSide.Right));
                targetColors.Add(this.GetCurrentSideColor(CubeSide.Front));
                break;
            case CornerCubePosition.DownLeft:
                targetColors.Add(this.GetCurrentSideColor(CubeSide.Down));
                targetColors.Add(this.GetCurrentSideColor(CubeSide.Left));
                targetColors.Add(this.GetCurrentSideColor(CubeSide.Front));
                break;
        }

        return targetColors;
    }

    // Proverava da li ugaona kockica sadrzi sve boje iz targetColors
    public bool CheckCornerCubeColors(CornerCube cornerCube, HashSet<CubeColor> targetColors)
    {
        foreach (CubeColor cornerCubeColor in cornerCube.ColorBySide.Values)
            if (!targetColors.Contains(cornerCubeColor))
                return false;

        return true;
    }

    public bool AreCornerCubesOrientated(CubeSide cubeSide, Dictionary<CornerCubePosition, CornerCube> cornerCubes = null)
    {
        if (cornerCubes == null)
            cornerCubes = this.CollectCurrentCornerCubes();

        switch (cubeSide)
        {
            case CubeSide.Down:
                return this.IsCurrentCornerCubeOrientated(cornerCubes[CornerCubePosition.DownRight]) && this.IsCurrentCornerCubeOrientated(cornerCubes[CornerCubePosition.DownLeft]);
            case CubeSide.Up:
                return this.IsCurrentCornerCubeOrientated(cornerCubes[CornerCubePosition.UpRight]) && this.IsCurrentCornerCubeOrientated(cornerCubes[CornerCubePosition.UpLeft]);
            default: return false;
        }
    }

    public bool IsCurrentCornerCubeOrientated(CornerCube cornerCube)
    {
        switch (cornerCube.Position)
        {
            case CornerCubePosition.DownRight:
                return cornerCube.ColorBySide[CubeSide.Front] == this.GetCurrentSideColor(CubeSide.Front) &&
                    cornerCube.ColorBySide[CubeSide.Down] == this.GetCurrentSideColor(CubeSide.Down) &&
                    cornerCube.ColorBySide[CubeSide.Right] == this.GetCurrentSideColor(CubeSide.Right);
            case CornerCubePosition.DownLeft:
                return cornerCube.ColorBySide[CubeSide.Front] == this.GetCurrentSideColor(CubeSide.Front) &&
                   cornerCube.ColorBySide[CubeSide.Down] == this.GetCurrentSideColor(CubeSide.Down) &&
                   cornerCube.ColorBySide[CubeSide.Left] == this.GetCurrentSideColor(CubeSide.Left);
            case CornerCubePosition.UpRight:
                return cornerCube.ColorBySide[CubeSide.Front] == this.GetCurrentSideColor(CubeSide.Front) &&
                   cornerCube.ColorBySide[CubeSide.Right] == this.GetCurrentSideColor(CubeSide.Right) &&
                   cornerCube.ColorBySide[CubeSide.Up] == this.GetCurrentSideColor(CubeSide.Up);
            case CornerCubePosition.UpLeft:
                return cornerCube.ColorBySide[CubeSide.Front] == this.GetCurrentSideColor(CubeSide.Front) &&
                   cornerCube.ColorBySide[CubeSide.Up] == this.GetCurrentSideColor(CubeSide.Up) &&
                   cornerCube.ColorBySide[CubeSide.Left] == this.GetCurrentSideColor(CubeSide.Left);
        }

        return false;
    }

    public CubeColor GetCurrentSideColor(CubeSide side)
    {
        CubeColor sideColor = CubeColor.NoColor;

        switch (side)
        {
            case CubeSide.Left:
                this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.Right);
                sideColor = this.GetCurrentCubieColor(CubeSide.Front, 4);
                this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.Left);
                break;
            case CubeSide.Right:
                this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.Left);
                sideColor = this.GetCurrentCubieColor(CubeSide.Front, 4);
                this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.Right);
                break;
            case CubeSide.Front:
                sideColor = this.GetCurrentCubieColor(CubeSide.Front, 4);
                break;
            case CubeSide.Back:
                this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.Right);
                this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.Right);
                sideColor = this.GetCurrentCubieColor(CubeSide.Front, 4);
                this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.Left);
                this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.Left);
                break;
            case CubeSide.Up:
                this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.DownLeft);
                sideColor = this.GetCurrentCubieColor(CubeSide.Front, 4);
                this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.UpLeft);
                break;
            case CubeSide.Down:
                this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.UpLeft);
                sideColor = this.GetCurrentCubieColor(CubeSide.Front, 4);
                this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.DownLeft);
                break;
        }

        return sideColor;
    }

    public Dictionary<CornerCubePosition, CornerCube> CollectCurrentCornerCubes()
    {
        return new Dictionary<CornerCubePosition, CornerCube>
        {
            { CornerCubePosition.UpRight, this.GetCurrentCornerCube(CornerCubePosition.UpRight) },
            { CornerCubePosition.UpLeft, this.GetCurrentCornerCube(CornerCubePosition.UpLeft) },
            { CornerCubePosition.DownRight, this.GetCurrentCornerCube(CornerCubePosition.DownRight) },
            { CornerCubePosition.DownLeft, this.GetCurrentCornerCube(CornerCubePosition.DownLeft) }
        };
    }

    public CornerCube GetCurrentCornerCube(CornerCubePosition cornerCubePosition)
    {
        var cornerCube = new CornerCube(cornerCubePosition);

        switch (cornerCubePosition)
        {
            case CornerCubePosition.UpRight:
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right, false));
                cornerCube.ColorBySide.Add(CubeSide.Up, this.GetCurrentCubieColor(CubeSide.Front, 8));
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right));

                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
                cornerCube.ColorBySide.Add(CubeSide.Right, this.GetCurrentCubieColor(CubeSide.Front, 0));
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));

                cornerCube.ColorBySide.Add(CubeSide.Front, this.GetCurrentCubieColor(CubeSide.Front, 2));
                break;
            case CornerCubePosition.UpLeft:
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Left));
                cornerCube.ColorBySide.Add(CubeSide.Up, this.GetCurrentCubieColor(CubeSide.Front, 6));
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Left, false));

                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, false));
                cornerCube.ColorBySide.Add(CubeSide.Left, this.GetCurrentCubieColor(CubeSide.Front, 2));
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));

                cornerCube.ColorBySide.Add(CubeSide.Front, this.GetCurrentCubieColor(CubeSide.Front, 0));
                break;
            case CornerCubePosition.DownRight:
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right));
                cornerCube.ColorBySide.Add(CubeSide.Down, this.GetCurrentCubieColor(CubeSide.Front, 2));
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right, false));

                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Down, false));
                cornerCube.ColorBySide.Add(CubeSide.Right, this.GetCurrentCubieColor(CubeSide.Front, 6));
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Down));

                cornerCube.ColorBySide.Add(CubeSide.Front, this.GetCurrentCubieColor(CubeSide.Front, 8));
                break;
            case CornerCubePosition.DownLeft:
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Left, false));
                cornerCube.ColorBySide.Add(CubeSide.Down, this.GetCurrentCubieColor(CubeSide.Front, 0));
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Left));

                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Down));
                cornerCube.ColorBySide.Add(CubeSide.Left, this.GetCurrentCubieColor(CubeSide.Front, 8));
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Down, false));

                cornerCube.ColorBySide.Add(CubeSide.Front, this.GetCurrentCubieColor(CubeSide.Front, 6));
                break;
        }

        return cornerCube;
    }

    public List<CubeMove> WhiteCross()
    {
        var solutionMoves = new List<CubeMove>();

        // Prolazi kroz sve strane kocke i pronalazi zeljene boje ivicne kocke donjeg sloja, ukoliko se boje poklapaju sa bojama trenutne ivicne kocke gornjeg sloja, spusta tu ivicnu kocku na donji sloj
        foreach (KeyValuePair<CubeSide, CubeColor[]> sideState in this.CubeState)
        {
            CubeSide cubeSide = sideState.Key;

            if (cubeSide == CubeSide.Up || cubeSide == CubeSide.Down)
                continue;

            this.RotateToSide(cubeSide);
            HashSet<CubeColor> targetUpEdgeColors = this.GetTargetEdgeCubeColors(EdgeCubePosition.Down);

            // Rotira gornji sloj kocke dok se ne pronadje zeljena ivicna kocka
            while (true)
            {
                Dictionary<EdgeCubePosition, EdgeCube> edgeCubes = this.CollectCurrentEdgeCubes();

                if (this.CheckEdgeCubeColors(edgeCubes[EdgeCubePosition.Up], targetUpEdgeColors))
                    break;

                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
                solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up));
            }

            this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front, true, true));
            solutionMoves.Add(this.CreateSolutionMove(CubeSide.Front, true, true));
        }

        return solutionMoves;
    }

    public List<CubeMove> WhiteCrossOnYellowSide()
    {
        var solutionMoves = new List<CubeMove>();

        // Prolazi kroz strane kocke sve dok se ne napravi beli krst na zutoj strani
        while (this.CubeState[CubeSide.Up][1] != CubeColor.White || this.CubeState[CubeSide.Up][3] != CubeColor.White || this.CubeState[CubeSide.Up][5] != CubeColor.White || this.CubeState[CubeSide.Up][7] != CubeColor.White)
        {
            if (!(this.CubeState[CubeSide.Up][1] != CubeColor.White || this.CubeState[CubeSide.Up][3] != CubeColor.White || this.CubeState[CubeSide.Up][5] != CubeColor.White || this.CubeState[CubeSide.Up][7] != CubeColor.White))
                return solutionMoves;

            foreach (KeyValuePair<CubeSide, CubeColor[]> sideState in this.CubeState)
            {
                CubeSide cubeSide = sideState.Key;

                if (cubeSide == CubeSide.Up)
                    continue;


                // Ako data strana ima polje bele boje prebacuje to polje na zutu stranu kocke
                int[] edgePositions = this.FindColoredEdges(cubeSide, CubeColor.White);

                if (edgePositions.Length > 0)
                {
                    this.RotateToSide(cubeSide);

                    CubeColor upperEdgeColor = CubeColor.NoColor;

                    // Za donju stranu su posebne funkcije za poredjenje polja ivicne kocke sa naspramnim poljem sa gorne strane kocke
                    if (cubeSide == CubeSide.Down)
                    {
                        switch (edgePositions[0])
                        {
                            case 1:
                                upperEdgeColor = this.GetUpperEdgeColorFromBottom(EdgeCubePosition.Up);

                                // Ovakvi delovi sluze kako da rotiraju gornji sloj dok na odgovarajucem polju zute strane ne bude belo polje
                                while (upperEdgeColor == CubeColor.White)
                                {
                                    this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Back));
                                    solutionMoves.Add(this.CreateSolutionMove(CubeSide.Back));

                                    upperEdgeColor = this.GetUpperEdgeColorFromBottom(EdgeCubePosition.Up);
                                }

                                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, true, true));
                                solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up, true, true));
                                break;
                            case 7:
                                upperEdgeColor = this.GetUpperEdgeColorFromBottom(EdgeCubePosition.Down);

                                while (upperEdgeColor == CubeColor.White)
                                {
                                    this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Back));
                                    solutionMoves.Add(this.CreateSolutionMove(CubeSide.Back));

                                    upperEdgeColor = this.GetUpperEdgeColorFromBottom(EdgeCubePosition.Down);
                                }

                                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Down, true, true));
                                solutionMoves.Add(this.CreateSolutionMove(CubeSide.Down, true, true));
                                break;
                            case 3:
                                upperEdgeColor = this.GetUpperEdgeColorFromBottom(EdgeCubePosition.Left);

                                while (upperEdgeColor == CubeColor.White)
                                {
                                    this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Back));
                                    solutionMoves.Add(this.CreateSolutionMove(CubeSide.Back));

                                    upperEdgeColor = this.GetUpperEdgeColorFromBottom(EdgeCubePosition.Left);
                                }

                                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Left, true, true));
                                solutionMoves.Add(this.CreateSolutionMove(CubeSide.Left, true, true));
                                break;
                            case 5:
                                upperEdgeColor = this.GetUpperEdgeColorFromBottom(EdgeCubePosition.Right);

                                while (upperEdgeColor == CubeColor.White)
                                {
                                    this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Back));
                                    solutionMoves.Add(this.CreateSolutionMove(CubeSide.Back));

                                    upperEdgeColor = this.GetUpperEdgeColorFromBottom(EdgeCubePosition.Right);
                                }

                                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right, true, true));
                                solutionMoves.Add(this.CreateSolutionMove(CubeSide.Right, true, true));
                                break;
                        }

                        break;
                    }

                    // Poredjenje polja ivicne kocke sa naspramnim poljem sa gorne strane kocke i zamena mesta
                    switch (edgePositions[0])
                    {
                        case 3:
                            upperEdgeColor = this.GetUpperEdgeColorFromSide(EdgeCubePosition.Left);

                            while (upperEdgeColor == CubeColor.White)
                            {
                                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
                                solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up));
                                upperEdgeColor = this.GetUpperEdgeColorFromSide(EdgeCubePosition.Left);
                            }

                            this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Left, false));
                            solutionMoves.Add(this.CreateSolutionMove(CubeSide.Left, false));
                            break;
                        case 5:
                            upperEdgeColor = this.GetUpperEdgeColorFromSide(EdgeCubePosition.Right);

                            while (upperEdgeColor == CubeColor.White)
                            {
                                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
                                solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up));
                                upperEdgeColor = this.GetUpperEdgeColorFromSide(EdgeCubePosition.Right);
                            }

                            this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right));
                            solutionMoves.Add(this.CreateSolutionMove(CubeSide.Right));
                            break;
                        case 1:
                            upperEdgeColor = this.GetUpperEdgeColorFromSide(EdgeCubePosition.Left);

                            if (upperEdgeColor == CubeColor.White)
                            {
                                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front));
                                solutionMoves.Add(this.CreateSolutionMove(CubeSide.Front));

                                upperEdgeColor = this.GetUpperEdgeColorFromSide(EdgeCubePosition.Right);
                                while (upperEdgeColor == CubeColor.White)
                                {
                                    this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
                                    solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up));
                                    upperEdgeColor = this.GetUpperEdgeColorFromSide(EdgeCubePosition.Right);
                                }

                                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right));
                                solutionMoves.Add(this.CreateSolutionMove(CubeSide.Right));
                            }
                            else
                            {
                                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front, false));
                                solutionMoves.Add(this.CreateSolutionMove(CubeSide.Front, false));

                                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Left, false));
                                solutionMoves.Add(this.CreateSolutionMove(CubeSide.Left, false));
                            }
                            break;
                        case 7:
                            upperEdgeColor = this.GetUpperEdgeColorFromSide(EdgeCubePosition.Left);

                            if (upperEdgeColor == CubeColor.White)
                            {
                                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front, false));
                                solutionMoves.Add(this.CreateSolutionMove(CubeSide.Front, false));

                                upperEdgeColor = this.GetUpperEdgeColorFromSide(EdgeCubePosition.Right);
                                while (upperEdgeColor == CubeColor.White)
                                {
                                    this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up));
                                    solutionMoves.Add(this.CreateSolutionMove(CubeSide.Up));
                                    upperEdgeColor = this.GetUpperEdgeColorFromSide(EdgeCubePosition.Right);
                                }

                                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right));
                                solutionMoves.Add(this.CreateSolutionMove(CubeSide.Right));
                            }
                            else
                            {
                                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Front));
                                solutionMoves.Add(this.CreateSolutionMove(CubeSide.Front));


                                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Left, false));
                                solutionMoves.Add(this.CreateSolutionMove(CubeSide.Left, false));
                            }
                            break;
                    }

                    break;
                }
            }
        }

        return solutionMoves;
    }

    public CubeColor GetUpperEdgeColorFromSide(EdgeCubePosition edgeCubePosition)
    {
        CubeColor upperEdgeColor = CubeColor.NoColor;

        switch (edgeCubePosition)
        {
            case EdgeCubePosition.Left:
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Left));
                upperEdgeColor = this.GetCurrentCubieColor(CubeSide.Front, 3);
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Left, false));
                break;
            case EdgeCubePosition.Right:
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right, false));
                upperEdgeColor = this.GetCurrentCubieColor(CubeSide.Front, 5);
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right));
                break;
        }

        if (upperEdgeColor == CubeColor.NoColor)
            throw new System.Exception("Invalid cube color");

        return upperEdgeColor;
    }

    public CubeColor GetUpperEdgeColorFromBottom(EdgeCubePosition edgeCubePosition)
    {
        CubeColor upperEdgeColor = CubeColor.NoColor;

        switch (edgeCubePosition)
        {
            case EdgeCubePosition.Left:
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Left, true, true));
                upperEdgeColor = this.GetCurrentCubieColor(CubeSide.Front, 3);
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Left, true, true));
                break;
            case EdgeCubePosition.Right:
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right, true, true));
                upperEdgeColor = this.GetCurrentCubieColor(CubeSide.Front, 5);
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Right, true, true));
                break;
            case EdgeCubePosition.Up:
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, true, true));
                upperEdgeColor = this.GetCurrentCubieColor(CubeSide.Front, 1);
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Up, true, true));
                break;
            case EdgeCubePosition.Down:
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Down, true, true));
                upperEdgeColor = this.GetCurrentCubieColor(CubeSide.Front, 7);
                this.SolvingCubeStateWrapper.RotateFace(new CubeMove(CubeSide.Down, true, true));
                break;
        }

        if (upperEdgeColor == CubeColor.NoColor)
            throw new System.Exception("Invalid cube color");

        return upperEdgeColor;
    }

    // Pronalazi pozicije polja sa datom boju u odnosu na pocetnu stranu kocke
    public int[] FindColoredEdges(CubeSide cubeSide, CubeColor cubeColor)
    {
        var edgePositions = new List<int>();

        foreach (int edgePosition in Helper.EdgePositions)
            if (this.CubeState[cubeSide][edgePosition] == cubeColor)
                edgePositions.Add(edgePosition);

        return edgePositions.ToArray();
    }

    public int[] FindColoredCorners(CubeSide cubeSide, CubeColor cubeColor)
    {
        var cornerPositions = new List<int>();

        foreach (int cornerPosition in Helper.CornerPositions)
            if (this.CubeState[cubeSide][cornerPosition] == cubeColor)
                cornerPositions.Add(cornerPosition);

        return cornerPositions.ToArray();
    }

    public void RotateToSide(CubeSide cubeSide)
    {
        if (this.CheckSideToRotation(CubeSide.Front, cubeSide) &&
           this.CheckSideToRotation(CubeSide.Up, Helper.upperSideMapping[cubeSide]))
            return;

        if (!this.TryToRotateToSide(cubeSide))
        {
            this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.UpRight);
            this.TryToRotateToSide(cubeSide);
        }
    }

    public bool TryToRotateToSide(CubeSide cubeSide)
    {
        for (int i = 0; i < 3; i++)
        {
            this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.Right);
            if (this.CheckSideToRotation(CubeSide.Front, cubeSide))
            {
                if (!this.CheckSideToRotation(CubeSide.Up, Helper.upperSideMapping[cubeSide]))
                    for (int j = 0; j < 3; j++)
                    {
                        this.stateReader.SolvingCubeStateWrapper.RotateCube(CubeRotationDirection.UpRight);
                        if (this.CheckSideToRotation(CubeSide.Up, Helper.upperSideMapping[cubeSide]))
                        {
                            return true;
                        }
                    }
                else
                    return true;

                break;
            }
        }

        return false;
    }

    // Pronalazi boju polja na poziciji cubiePosition na trenutnoj strani cubeSide
    public CubeColor GetCurrentCubieColor(CubeSide cubeSide, int cubiePosition)
    {
        // U slucaju gornje strane potrebno je rotirati numeraciju polja u odnosu na trenutnu rotaciju kocke
        if (this.CubeStateData.SideToRotationMapping[cubeSide].Key == CubeSide.Up)
        {
            cubiePosition = Helper.GetRotatedPosition(cubiePosition, this.CubeStateData.SideToRotationMapping[CubeSide.Down].Key);
        }

        return this.CubeState[this.CubeStateData.SideToRotationMapping[cubeSide].Key][cubiePosition];
    }

    // Proverava da li je trenutna strana na mestu strane actualSide bas rotationSide, tj. proverava da li je rotationSide na mestu actualSide
    public bool CheckSideToRotation(CubeSide actualSide, CubeSide rotationSide)
    {
        return this.CubeStateData.SideToRotationMapping[actualSide].Key == rotationSide;
    }

    public CubeMove CreateSolutionMove(CubeSide cubeSide, bool clockwise = true, bool doubleMove = false)
    {
        return new CubeMove(this.CubeStateData.SideToRotationMapping[cubeSide].Key, clockwise, doubleMove);
    }

    public enum EdgeCubePosition
    {
        Right,
        Left,
        Up,
        Down
    }

    public enum CornerCubePosition
    {
        UpRight,
        UpLeft,
        DownRight,
        DownLeft
    }
}