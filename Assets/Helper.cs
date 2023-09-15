using Kociemba;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using static StateReader;
using CubeColor = StateReader.CubeColor;

// Pomocna staticka klasa
public static class Helper
{
    // Nizovi koji oznacavaju na kojim pozicijama na stranici se nalaze ivicne/ugaone kockice, tj. polja
    public static int[] EdgePositions = { 1, 3, 5, 7 };
    public static int[] CornerPositions = { 0, 2, 6, 8 };

    public static CubeColor[] DeepCopyColors(CubeColor[] oldColors)
    {
        var newColors = new CubeColor[oldColors.Length];
        Array.Copy(oldColors, newColors, oldColors.Length);

        return newColors;
    }

    // Definise na koji nacin treba rotirati kocku kako bi se citale pozicije sa trenutne strane u pravom redosledu
    public static Dictionary<CubeSide, CubeSide> upperSideMapping = new Dictionary<CubeSide, CubeSide>
    {
        { CubeSide.Front, CubeSide.Up },
        { CubeSide.Back, CubeSide.Up },
        { CubeSide.Right, CubeSide.Up },
        { CubeSide.Left, CubeSide.Up },
        { CubeSide.Down, CubeSide.Front },
        { CubeSide.Up, CubeSide.Back },
    };

    // Prilikom rotacije kocke pozicije gornje strane se menjaju u odnosu na bocne strane kocke, ovo mapiranje slika pravu poziciju polja stranice u njegovu trenutnu poziciju
    public static int GetRotatedPosition(int currentPosition, CubeSide currentDownCubeSide)
    {
        int rotationsCount;

        switch (currentDownCubeSide)
        {
            case CubeSide.Front:
                rotationsCount = 0;
                break;
            case CubeSide.Right:
                rotationsCount = 1;
                break;
            case CubeSide.Back:
                rotationsCount = 2;
                break;
            case CubeSide.Left:
                rotationsCount = 3;
                break;
            default:
                rotationsCount = 0;
                break;
        }

        var upSideNumeration = new Dictionary<int, int>
        {
        {0,0},
        {1,1},
        {2,2},
        {3,3},
        {4,4},
        {5,5},
        {6,6},
        {7,7},
        {8,8},
        };
        Dictionary<int, int> newUpSideNumeration = upSideNumeration.ToDictionary(value => value.Key, value => value.Value);

        while (rotationsCount > 0)
        {
            newUpSideNumeration[0] = upSideNumeration[6];
            newUpSideNumeration[1] = upSideNumeration[3];
            newUpSideNumeration[2] = upSideNumeration[0];
            newUpSideNumeration[3] = upSideNumeration[7];
            newUpSideNumeration[4] = upSideNumeration[4];
            newUpSideNumeration[5] = upSideNumeration[1];
            newUpSideNumeration[6] = upSideNumeration[8];
            newUpSideNumeration[7] = upSideNumeration[5];
            newUpSideNumeration[8] = upSideNumeration[2];

            upSideNumeration = newUpSideNumeration.ToDictionary(value => value.Key, value => value.Value);
            rotationsCount--;
        }

        return newUpSideNumeration[currentPosition];
    }

    public static CubeMove StringToCubeMove(string cubeMoveString)
    {
        switch (cubeMoveString)
        {
            case "R":
                return new CubeMove(CubeSide.Right);
            case "R2":
                return new CubeMove(CubeSide.Right, true, true);
            case "R'":
                return new CubeMove(CubeSide.Right, false);
            case "L":
                return new CubeMove(CubeSide.Left);
            case "L2":
                return new CubeMove(CubeSide.Left, true, true);
            case "L'":
                return new CubeMove(CubeSide.Left, false);
            case "U":
                return new CubeMove(CubeSide.Up);
            case "U2":
                return new CubeMove(CubeSide.Up, true, true);
            case "U'":
                return new CubeMove(CubeSide.Up, false);
            case "D":
                return new CubeMove(CubeSide.Down);
            case "D2":
                return new CubeMove(CubeSide.Down, true, true);
            case "D'":
                return new CubeMove(CubeSide.Down, false);
            case "F":
                return new CubeMove(CubeSide.Front);
            case "F2":
                return new CubeMove(CubeSide.Front, true, true);
            case "F'":
                return new CubeMove(CubeSide.Front, false);
            case "B":
                return new CubeMove(CubeSide.Back);
            case "B2":
                return new CubeMove(CubeSide.Back, true, true);
            case "B'":
                return new CubeMove(CubeSide.Back, false);
            default:
                return new CubeMove(CubeSide.NoSide);
        }
    }

    public static string CubeMoveToString(CubeMove cubeMove)
    {
        string stringMove = string.Empty;

        switch (cubeMove.CubeSide)
        {
            case CubeSide.Right:
                stringMove = "R";
                break;
            case CubeSide.Left:
                stringMove = "L";
                break;
            case CubeSide.Up:
                stringMove = "U";
                break;
            case CubeSide.Down:
                stringMove = "D";
                break;
            case CubeSide.Front:
                stringMove = "F";
                break;
            case CubeSide.Back:
                stringMove = "B";
                break;
        }

        if (cubeMove.DoubleMove)
            stringMove += "2";
        else if (!cubeMove.Clockwise)
            stringMove += "'";

        return stringMove;
    }

    public static string MovesArrayToString(CubeMove[] cubeMoves)
    {
        int cubeMovesCount = cubeMoves.Count();
        StringBuilder stringMoves = new StringBuilder(string.Empty, 4 * cubeMovesCount);

        for (int moveIndex = 0; moveIndex < cubeMovesCount; moveIndex++)
        {
            stringMoves.Append(CubeMoveToString(cubeMoves[moveIndex]));

            if (moveIndex < cubeMovesCount - 1)
                stringMoves.Append(" ");
        }

        return stringMoves.ToString();
    }

    public static List<CubeMove> RemoveRedundantMoves(List<CubeMove> cubeMoves)
    {
        var solutionMoves = new List<CubeMove>();

        for (int moveIndex = 0; moveIndex < cubeMoves.Count; moveIndex++)
        {
            if (moveIndex < cubeMoves.Count - 1 && cubeMoves[moveIndex].DoubleMove && AreMovesEqual(cubeMoves[moveIndex], cubeMoves[moveIndex + 1]))
            {
                moveIndex++;
                continue;
            }

            if (moveIndex < cubeMoves.Count - 2 && !cubeMoves[moveIndex].DoubleMove && AreMovesEqual(cubeMoves[moveIndex], cubeMoves[moveIndex + 1]) && AreMovesEqual(cubeMoves[moveIndex], cubeMoves[moveIndex + 2]))
            {
                solutionMoves.Add(new CubeMove(cubeMoves[moveIndex].CubeSide, !cubeMoves[moveIndex].Clockwise));
                moveIndex++;
                moveIndex++;
                continue;
            }

            solutionMoves.Add(cubeMoves[moveIndex]);
        }

        return solutionMoves;
    }

    public static bool AreMovesEqual(CubeMove firstMove, CubeMove secondMove)
    {
        return firstMove.CubeSide == secondMove.CubeSide && firstMove.Clockwise == secondMove.Clockwise && firstMove.DoubleMove == secondMove.DoubleMove;
    }
}